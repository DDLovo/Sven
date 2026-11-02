using System;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace SvenMod.Modules
{
    internal static class AddressablesUtil
    {
        /// <summary>
        /// RoR2 1.5 replaced the Resources tree with Addressables. This blocks
        /// until the asset is available; only call it during content loading or
        /// other points where a short stall is acceptable.
        /// </summary>
        public static T Load<T>(string key) where T : UnityEngine.Object
        {
            try
            {
                var asset = Addressables.LoadAssetAsync<T>(key).WaitForCompletion();
                if (!asset) Log.Error($"Addressables key resolved to null: {key}");
                return asset;
            }
            catch (Exception e)
            {
                Log.Error($"Addressables load failed for {key}: {e}");
                return null;
            }
        }
    }
}
