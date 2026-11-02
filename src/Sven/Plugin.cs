using System.IO;
using System.Security;
using System.Security.Permissions;
using BepInEx;
using BepInEx.Configuration;
using R2API.Utils;
using UnityEngine;
using SvenMod.Modules;

// GameLibs exposes internal game members at compile time. Unity Mono also needs
// these declarations at runtime, as in DrowRanger, to allow those accesses.
[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]

namespace SvenMod
{
    [BepInPlugin(MODUID, "Sven", MODVERSION)]
    [BepInDependency(R2API.R2API.PluginGUID)]
    [BepInDependency("droppod.lookingglass", BepInDependency.DependencyFlags.SoftDependency)]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.EveryoneNeedSameModVersion)]
    public sealed class SvenPlugin : BaseUnityPlugin
    {
        public const string MODUID="DDL.Sven", MODVERSION="0.2.0";
        public static AssetBundle bundle;
        public static SvenPlugin instance;
        public static ConfigEntry<float> SoundVolume;
        private void Awake()
        {
            instance=this;Log.Init(Logger);
            var config=new ConfigFile(Path.Combine(Paths.ConfigPath,"Sven.cfg"),true,Info.Metadata);
            SoundVolume=config.Bind("Audio","Volume",.75f,new ConfigDescription("Sven weapon and skill sound volume.",new AcceptableValueRange<float>(0,1)));
            bundle=AssetBundle.LoadFromFile(Path.Combine(Path.GetDirectoryName(Info.Location),"AssetBundles","sven"));
            if(!bundle) {Log.Error("Sven asset bundle missing. Reinstall the complete package.");return;}
            SvenTokens.Init();SvenHooks.Init();SvenEffects.RegisterNetwork();SvenAudio.Init();
            new ContentPacks().Initialize();
            Log.Info("Sven "+MODVERSION+" initialized; independent content pack loading.");
        }
        private void OnDestroy() {SvenHooks.Shutdown();SvenLookingGlass.Shutdown();SvenAudio.Shutdown();}
    }
}
