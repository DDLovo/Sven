using System.Collections;
using UnityEngine;
using UnityEngine.AddressableAssets;
namespace SvenMod.Modules
{
    internal static class SvenBootstrap
    {
        private static bool initialized;
        internal static IEnumerator InitializeContent()
        {
            if(initialized)yield break;
            var body=Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Huntress/HuntressBody.prefab");yield return body;
            var arrow=Addressables.LoadAssetAsync<GameObject>("RoR2/Junk/Huntress/Arrow.prefab");yield return arrow;
            var shader=Addressables.LoadAssetAsync<Shader>("RoR2/Base/Shaders/HGStandard.shader");yield return shader;
            if(!body.Result||!arrow.Result||!shader.Result)throw new System.InvalidOperationException("Sven native asset bootstrap failed");
            Materials.SetHotpoo(shader.Result);
            var blink=Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Huntress/HuntressBlinkEffect.prefab");yield return blink;
            var flash=Addressables.LoadAssetAsync<Material>("RoR2/Base/Huntress/matHuntressFlashBright.mat");yield return flash;
            var expanded=Addressables.LoadAssetAsync<Material>("RoR2/Base/Huntress/matHuntressFlashExpanded.mat");yield return expanded;
            if(!blink.Result||!flash.Result||!expanded.Result)throw new System.InvalidOperationException("Sven native blink assets failed to load");
            SvenBlinkEffects.Initialize(blink.Result,flash.Result,expanded.Result);
            SvenBuffs.Init();SvenProjectiles.Init(arrow.Result);SvenSurvivor.Init(body.Result);SvenLookingGlass.Init();
            initialized=true;
        }
    }
}
