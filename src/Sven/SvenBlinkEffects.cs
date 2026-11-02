using RoR2;
using UnityEngine;
namespace SvenMod
{
    internal static class SvenBlinkEffects
    {
        private static GameObject effect;
        private static Material bright,expanded;
        internal static void Initialize(GameObject prefab,Material flash,Material outerFlash)
        {effect=prefab;bright=flash;expanded=outerFlash;}
        internal static bool Play(CharacterBody body,Vector3 departure,Vector3 arrival)
        {
            if(!effect)return false;
            var offset=body?body.corePosition-body.transform.position:Vector3.up;
            var delta=arrival-departure;
            var rotation=Util.QuaternionSafeLookRotation(delta.sqrMagnitude>.001f?delta.normalized:Vector3.forward);
            EffectManager.SpawnEffect(effect,new EffectData{origin=departure+offset,rotation=rotation},false);
            EffectManager.SpawnEffect(effect,new EffectData{origin=arrival+offset,rotation=rotation},false);
            var model=body&&body.modelLocator?body.modelLocator.modelTransform:null;
            var characterModel=model?model.GetComponent<CharacterModel>():null;
            if(characterModel){Flash(characterModel,bright,.6f);Flash(characterModel,expanded,.7f);}
            return true;
        }
        private static void Flash(CharacterModel model,Material material,float duration)
        {
            if(!material)return;
            var overlay=TemporaryOverlayManager.AddOverlay(model.gameObject);
            overlay.duration=duration;overlay.animateShaderAlpha=true;
            overlay.alphaCurve=AnimationCurve.EaseInOut(0,1,1,0);
            overlay.destroyComponentOnEnd=true;overlay.originalMaterial=material;
            overlay.AddToCharacterModel(model);
        }
    }
}
