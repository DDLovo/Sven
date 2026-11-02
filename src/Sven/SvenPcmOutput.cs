using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

namespace SvenMod
{
    // Unity audio is disabled in RoR2. One Windows output device mixes bounded
    // voices independently of Unity/Wwise; no Unity APIs run on the worker.
    internal sealed class SvenPcmOutput : IDisposable
    {
        internal sealed class Voice
        {
            internal float[] samples;
            internal int cursor;
            internal float left, right;
            internal volatile bool stopped;
            public bool IsPlaying => !stopped;
            public void Stop() => stopped = true;
        }
        private readonly object gate = new object();
        private readonly List<Voice> voices = new List<Voice>();
        private readonly Thread worker;
        private volatile bool running = true;
        internal volatile string Error;
        private IntPtr device;
        private readonly IntPtr[] headers = new IntPtr[4];
        private readonly IntPtr[] buffers = new IntPtr[4];
        private readonly bool[] prepared = new bool[4];
        private static readonly uint HeaderSize = (uint)Marshal.SizeOf<WaveHeader>();
        private const int Frames = 441; // Four 10 ms buffers.

        internal SvenPcmOutput()
        {
            try
            {
                var format = new WaveFormat { tag = 1, channels = 2, rate = 44100,
                    bytesPerSecond = 176400, blockAlign = 4, bits = 16 };
                Check(waveOutOpen(out device, uint.MaxValue, ref format, IntPtr.Zero, IntPtr.Zero, 0), "open");
                for (var i = 0; i < headers.Length; i++)
                {
                    buffers[i] = Marshal.AllocHGlobal(Frames * 4);
                    headers[i] = Marshal.AllocHGlobal((int)HeaderSize);
                    Marshal.StructureToPtr(new WaveHeader { data = buffers[i], length = Frames * 4 }, headers[i], false);
                    Check(waveOutPrepareHeader(device, headers[i], HeaderSize), "prepare");
                    prepared[i] = true;
                }
                worker = new Thread(Pump) { IsBackground = true, Name = "Sven PCM output" };
                worker.Start();
            }
            catch { Release(); throw; }
        }

        internal Voice Play(float[] samples, float left, float right)
        {
            if (samples == null || samples.Length == 0) throw new ArgumentException("Empty sound");
            var voice = new Voice { samples = samples, left = left, right = right };
            lock (gate)
            {
                if (!running) { voice.Stop(); return voice; }
                voices.RemoveAll(v => v.stopped);
                if (voices.Count >= 40) { voices[0].Stop(); voices.RemoveAt(0); }
                voices.Add(voice);
            }
            return voice;
        }

        internal void SetGain(Voice voice, float left, float right)
        {
            lock (gate) { voice.left = left; voice.right = right; }
        }

        internal void Mix(short[] output)
        {
            lock (gate)
            {
                for (int frame = 0; frame < output.Length / 2; frame++)
                {
                    float left = 0, right = 0;
                    foreach (var voice in voices)
                    {
                        if (voice.stopped) continue;
                        var sample = voice.samples[voice.cursor++];
                        left += sample * voice.left;
                        right += sample * voice.right;
                        if (voice.cursor == voice.samples.Length) voice.Stop();
                    }
                    output[frame * 2] = (short)(Math.Max(-1f, Math.Min(1f, left)) * 32767);
                    output[frame * 2 + 1] = (short)(Math.Max(-1f, Math.Min(1f, right)) * 32767);
                }
                voices.RemoveAll(v => v.stopped);
            }
        }

        private void Pump()
        {
            try
            {
                var output = new short[Frames * 2];
                var submitted = new bool[headers.Length];
                var index = 0;
                while (running)
                {
                    var header = Marshal.PtrToStructure<WaveHeader>(headers[index]);
                    if (submitted[index] && (header.flags & 1) == 0) { Thread.Sleep(2); continue; }
                    Mix(output);
                    Marshal.Copy(output, 0, buffers[index], output.Length);
                    Check(waveOutWrite(device, headers[index], HeaderSize), "write");
                    submitted[index] = true;
                    index = (index + 1) % headers.Length;
                }
            }
            catch (Exception e) { Error = e.Message; }
            finally
            {
                running = false;
                lock (gate) { foreach (var voice in voices) voice.Stop(); voices.Clear(); }
                Release();
            }
        }

        public void Dispose()
        {
            running = false;
            // Worker owns memory and releases after reset, even if join times out.
            worker?.Join(1000);
        }

        private void Release()
        {
            if (device != IntPtr.Zero && waveOutReset(device) != 0) return;
            for (var i = 0; i < headers.Length; i++)
            {
                if (prepared[i] && waveOutUnprepareHeader(device, headers[i], HeaderSize) != 0) continue;
                if (headers[i] != IntPtr.Zero) Marshal.FreeHGlobal(headers[i]);
                if (buffers[i] != IntPtr.Zero) Marshal.FreeHGlobal(buffers[i]);
                headers[i] = buffers[i] = IntPtr.Zero;
            }
            if (device != IntPtr.Zero) waveOutClose(device);
            device = IntPtr.Zero;
        }

        internal static float[] ReadWave(Stream stream)
        {
            using (var reader = new BinaryReader(stream))
            {
                if (new string(reader.ReadChars(4)) != "RIFF") throw new InvalidDataException("Not RIFF");
                reader.ReadUInt32();
                if (new string(reader.ReadChars(4)) != "WAVE") throw new InvalidDataException("Not WAVE");
                ushort channels = 0;
                byte[] data = null;
                while (stream.Position + 8 <= stream.Length)
                {
                    var name = new string(reader.ReadChars(4));
                    var size = reader.ReadUInt32();
                    var end = stream.Position + size;
                    if (end > stream.Length) throw new InvalidDataException("Truncated WAV chunk");
                    if (name == "fmt ")
                    {
                        if (size < 16 || reader.ReadUInt16() != 1) throw new InvalidDataException("Expected PCM WAV");
                        channels = reader.ReadUInt16();
                        var rate = reader.ReadUInt32();
                        reader.ReadUInt32(); reader.ReadUInt16();
                        if ((channels != 1 && channels != 2) || rate != 44100 || reader.ReadUInt16() != 16)
                            throw new InvalidDataException("Expected mono/stereo PCM16 44100 Hz");
                    }
                    else if (name == "data") data = reader.ReadBytes(checked((int)size));
                    stream.Position = end + (size & 1);
                }
                if (channels == 0 || data == null || data.Length == 0 || data.Length % (2 * channels) != 0)
                    throw new InvalidDataException("Missing or incomplete WAV samples");
                var samples = new float[data.Length / (2 * channels)];
                for (var i = 0; i < samples.Length; i++)
                    for (var c = 0; c < channels; c++)
                        samples[i] += BitConverter.ToInt16(data, (i * channels + c) * 2) / (32768f * channels);
                return samples;
            }
        }

        private static void Check(uint result, string operation)
        {
            if (result != 0) throw new InvalidOperationException($"Sven audio {operation} failed ({result})");
        }
        [StructLayout(LayoutKind.Sequential, Pack = 2)]
        private struct WaveFormat
        {
            public ushort tag, channels;
            public uint rate, bytesPerSecond;
            public ushort blockAlign, bits, extra;
        }
        [StructLayout(LayoutKind.Sequential)]
        private struct WaveHeader
        {
            public IntPtr data;
            public uint length, recorded;
            public UIntPtr user;
            public uint flags, loops;
            public IntPtr next;
            public UIntPtr reserved;
        }
        [DllImport("winmm.dll")] private static extern uint waveOutOpen(out IntPtr device, uint id, ref WaveFormat format, IntPtr callback, IntPtr instance, uint flags);
        [DllImport("winmm.dll")] private static extern uint waveOutPrepareHeader(IntPtr device, IntPtr header, uint size);
        [DllImport("winmm.dll")] private static extern uint waveOutWrite(IntPtr device, IntPtr header, uint size);
        [DllImport("winmm.dll")] private static extern uint waveOutReset(IntPtr device);
        [DllImport("winmm.dll")] private static extern uint waveOutUnprepareHeader(IntPtr device, IntPtr header, uint size);
        [DllImport("winmm.dll")] private static extern uint waveOutClose(IntPtr device);
    }
}
