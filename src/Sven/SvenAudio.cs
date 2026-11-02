using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using RoR2;
using UnityEngine;

namespace SvenMod
{
    internal static class SvenAudio
    {
        private sealed class PositionedVoice
        {
            internal SvenPcmOutput.Voice voice;
            internal Vector3 position;
            internal Transform parent;
            internal float gain;
        }
        private static readonly Dictionary<string, float[]> clips = new Dictionary<string, float[]>();
        private static readonly List<PositionedVoice> voices = new List<PositionedVoice>();
        private static SvenPcmOutput output;
        private static float nextVolumeUpdate;
        private static float gameVolume = 1f;
        // Dota's sven_CastExecuteIsWarcry response rule uses exactly these four yells.
        private static readonly string[] warcryVoices =
        {
            "sven_warcry_voice_01", "sven_warcry_voice_02",
            "sven_warcry_voice_03", "sven_warcry_voice_04"
        };
        private static int lastWarcryVoice = -1;

        public static void Init()
        {
            if (Application.isBatchMode || Application.platform != RuntimePlatform.WindowsPlayer) return;
            try
            {
                var assembly = typeof(SvenAudio).Assembly;
                foreach (var name in assembly.GetManifestResourceNames().Where(n => n.StartsWith("SvenAudio.")))
                    clips[name.Substring(10, name.Length - 14)] = SvenPcmOutput.ReadWave(assembly.GetManifestResourceStream(name));
                output = new SvenPcmOutput();
                SvenPlugin.instance.gameObject.AddComponent<SvenAudioDriver>();
                Log.Info($"Dota audio: {clips.Count} original WAV samples; Windows PCM output ready.");
            }
            catch (Exception e) { Shutdown(); Log.Error($"Dota audio unavailable: {e}"); }
        }

        public static SvenPcmOutput.Voice Play(string sound, Vector3 position, float volume = 1f, Transform parent = null)
        {
            var listener = Listener();
            if (output == null || !listener) return null;
            var key = sound == "arrow" ? "arrow_" + UnityEngine.Random.Range(1, 5)
                : sound == "frost" ? "frost_arrow" + UnityEngine.Random.Range(1, 5)
                : sound == "impact" ? "arrow_impact_" + UnityEngine.Random.Range(1, 4) : sound;
            if (!clips.TryGetValue(key, out var clip)) return null;
            var voice = new PositionedVoice { position = position, parent = parent, gain = volume };
            Gains(voice, listener, out var left, out var right);
            voice.voice = output.Play(clip, left, right);
            voices.Add(voice);
            return voice.voice;
        }

        public static SvenPcmOutput.Voice PlayVariant(string prefix, Vector3 position, float volume, Transform parent)
        {
            var names = clips.Keys.Where(k => k.StartsWith(prefix, StringComparison.Ordinal)).ToArray();
            return names.Length == 0 ? null : Play(names[UnityEngine.Random.Range(0, names.Length)], position, volume, parent);
        }

        public static void PlayWarcry(bool shard, bool immortal, Vector3 position, Transform parent)
        {
            Play(shard ? "sven_warcry_shield" : immortal ? "sven_warcry_signet" : "sven_warcry",
                position, 0.85f, parent);
            var index = UnityEngine.Random.Range(0, warcryVoices.Length - (lastWarcryVoice >= 0 ? 1 : 0));
            if (lastWarcryVoice >= 0 && index >= lastWarcryVoice) index++;
            if (Play(warcryVoices[index], position, 0.9f, parent) != null) lastWarcryVoice = index;
        }

        internal static void Tick()
        {
            if (output == null) return;
            if (output.Error != null) { Log.Error(output.Error); Shutdown(); return; }
            var listener = Listener();
            voices.RemoveAll(v => !v.voice.IsPlaying);
            foreach (var voice in voices)
            {
                if (voice.parent) voice.position = voice.parent.position;
                Gains(voice, listener, out var left, out var right);
                output.SetGain(voice.voice, left, right);
            }
        }

        private static Transform Listener()
        {
            foreach (var rig in CameraRigController.readOnlyInstancesList)
                if (rig.sceneCam && rig.sceneCam.isActiveAndEnabled) return rig.sceneCam.transform;
            return null;
        }

        private static void Gains(PositionedVoice voice, Transform listener, out float left, out float right)
        {
            left = right = 0f;
            if (!listener) return;
            var offset = voice.position - listener.position;
            var distance = offset.magnitude;
            var gain = voice.gain * Volume * Mathf.Clamp01(1f - distance / 70f)
                * Mathf.Min(1f, 8f / Mathf.Max(1f, distance));
            var pan = Mathf.Clamp(Vector3.Dot(listener.right, offset.normalized), -1f, 1f);
            left = gain * Mathf.Sqrt((1f - pan) * 0.5f);
            right = gain * Mathf.Sqrt((1f + pan) * 0.5f);
        }

        private static float Volume
        {
            get
            {
                if (Time.unscaledTime >= nextVolumeUpdate)
                {
                    nextVolumeUpdate = Time.unscaledTime + 0.25f;
                    gameVolume = ReadVolume("volume_master") * ReadVolume("volume_sfx");
                    var focus = RoR2.Console.instance ? RoR2.Console.instance.FindConVar("audio_focused_only") : null;
                    if (!Application.isFocused && focus != null && focus.GetString() != "0") gameVolume = 0f;
                }
                return gameVolume * SvenPlugin.SoundVolume.Value;
            }
        }

        private static float ReadVolume(string name)
        {
            var variable = RoR2.Console.instance ? RoR2.Console.instance.FindConVar(name) : null;
            return variable != null && float.TryParse(variable.GetString(), NumberStyles.Float,
                CultureInfo.InvariantCulture, out var value) ? Mathf.Clamp01(value / 100f) : 1f;
        }

        internal static void Shutdown()
        {
            foreach (var voice in voices) voice.voice.Stop();
            voices.Clear();
            lastWarcryVoice = -1;
            output?.Dispose();
            output = null;
        }
    }

    public sealed class SvenAudioDriver : MonoBehaviour
    {
        private void Update() => SvenAudio.Tick();
        private void OnDestroy() => SvenAudio.Shutdown();
        private void OnApplicationQuit() => SvenAudio.Shutdown();
    }

    public class SvenEffectSound : MonoBehaviour
    {
        public string sound;
        public float volume = 1f;
        private void OnEnable() => StartCoroutine(PlayNextFrame());
        private System.Collections.IEnumerator PlayNextFrame()
        {
            yield return null;
            SvenAudio.Play(sound, transform.position, volume);
        }
    }
}
