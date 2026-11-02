using UnityEngine;
namespace SvenMod
{
    // Preserve painted shading and metal detail during color changes. Multiplying
    // blue albedo by red/gold cannot perform the Source material's hue replacement.
    internal sealed class SvenSurfaceVariants
    {
        private struct Slot
        {
            internal Renderer renderer;internal Texture normal,strength,gold;
            internal Color color,emission;internal Texture emissionMap;internal float emissionPower;
        }
        private readonly Slot[] slots;
        private readonly MaterialPropertyBlock properties=new MaterialPropertyBlock();
        internal SvenSurfaceVariants(Renderer[] renderers)
        {
            var values=new System.Collections.Generic.List<Slot>();
            foreach(var renderer in renderers)
            {
                var materials=renderer.sharedMaterials;
                // Current imported body meshes have one material each. Preserve CharacterModel's
                // renderer-wide flash/fade/elite block rather than shadowing it with indexed blocks.
                if(materials.Length!=1){Log.Error("Sven variant renderer requires one material: "+renderer.name);continue;}
                for(int i=0;i<materials.Length;i++)
                {
                    var m=materials[i];if(!m||!m.mainTexture)continue;
                    var texture=m.mainTexture;
                    var red=SvenAssets.Load<Texture2D>("SvenStrength_"+texture.name);
                    var gold=SvenAssets.Load<Texture2D>("SvenBkb_"+texture.name);
                    if(!red||!gold)Log.Error("Sven surface variants missing for "+texture.name);
                    values.Add(new Slot{renderer=renderer,normal=texture,strength=red?red:texture,gold=gold?gold:texture,
                        color=m.HasProperty("_Color")?m.GetColor("_Color"):Color.white,
                        emission=m.HasProperty("_EmColor")?m.GetColor("_EmColor"):Color.black,
                        emissionMap=m.HasProperty("_EmTex")?m.GetTexture("_EmTex"):null,
                        emissionPower=m.HasProperty("_EmPower")?m.GetFloat("_EmPower"):0});
                }
            }
            slots=values.ToArray();
        }
        internal void Apply(bool strength,bool bkb)
        {
            foreach(var slot in slots)
            {
                if(!slot.renderer)continue;
                slot.renderer.GetPropertyBlock(properties);
                properties.SetTexture("_MainTex",bkb?slot.gold:strength?slot.strength:slot.normal);
                properties.SetColor("_Color",slot.color);
                properties.SetColor("_EmColor",bkb?new Color(1,.75f,.25f):strength?new Color(1,.2f,.1f):slot.emission);
                properties.SetTexture("_EmTex",bkb?slot.gold:strength?slot.strength:slot.emissionMap?slot.emissionMap:Texture2D.blackTexture);
                properties.SetFloat("_EmPower",bkb?.06f:strength?.12f:slot.emissionPower);
                slot.renderer.SetPropertyBlock(properties);
            }
        }
    }
}
