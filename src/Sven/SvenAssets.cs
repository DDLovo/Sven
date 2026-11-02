using System;
using UnityEngine;
namespace SvenMod
{
    internal static class SvenAssets
    {
        internal static T Load<T>(string name) where T:UnityEngine.Object
        {
            var bundle=SvenPlugin.bundle;
            var direct=bundle.LoadAsset<T>(name);if(direct)return direct;
            foreach(var path in bundle.GetAllAssetNames())
                if(System.IO.Path.GetFileNameWithoutExtension(path).Equals(name,StringComparison.OrdinalIgnoreCase))
                {var asset=bundle.LoadAsset<T>(path);if(asset)return asset;}
            return null;
        }
        internal static Sprite Icon(string name)
        {
            var s=Load<Sprite>(name);if(s)return s;
            var t=Load<Texture2D>(name);return t?Sprite.Create(t,new Rect(0,0,t.width,t.height),new Vector2(.5f,.5f)):null;
        }
    }
}
