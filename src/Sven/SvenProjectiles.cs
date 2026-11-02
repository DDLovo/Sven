using R2API;
using RoR2;
using RoR2.Projectile;
using UnityEngine;
using UnityEngine.Networking;
using SvenMod.Modules;
namespace SvenMod
{
    internal static class SvenProjectiles
    {
        internal static GameObject hammer;
        internal static void Init(GameObject source)
        {
            hammer=PrefabAPI.InstantiateClone(source,"SvenStormHammer",true);
            foreach(var c in hammer.GetComponentsInChildren<Collider>())Object.DestroyImmediate(c);
            Remove<ProjectileSingleTargetImpact>();Remove<ProjectileSteerTowardTarget>();Remove<ProjectileDirectionalTargetFinder>();
            var rb=hammer.GetComponent<Rigidbody>();rb.useGravity=false;
            var movement=hammer.GetComponent<ProjectileSimple>();movement.desiredForwardSpeed=SvenBalance.HammerSpeed;movement.lifetime=SvenBalance.HammerLifetime;movement.enableVelocityOverLifetime=false;movement.oscillate=false;
            var controller=hammer.GetComponent<ProjectileController>();controller.procCoefficient=1;controller.ghostPrefab=null;controller.ghostPrefabAddress=new UnityEngine.AddressableAssets.AssetReferenceT<GameObject>(string.Empty);controller.ghostTransformAnchor=null;controller.startSound=string.Empty;
            var ghostTemplate=new GameObject("SvenHammerGhostTemplate");
            var ghost=PrefabAPI.InstantiateClone(ghostTemplate,"SvenHammerGhost",false);Object.DestroyImmediate(ghostTemplate);
            ghost.AddComponent<ProjectileGhostController>().ParentProjectileDestroyedEvent=new UnityEngine.Events.UnityEvent();
            ghost.AddComponent<VFXAttributes>().DoNotPool=true;ghost.AddComponent<SvenHammerVisual>();controller.ghostPrefab=ghost;
            hammer.AddComponent<SvenHammerProjectile>();Content.AddProjectilePrefab(hammer);
        }
        private static void Remove<T>() where T:Component{var c=hammer.GetComponent<T>();if(c)Object.DestroyImmediate(c);}
    }
    public sealed class SvenHammerProjectile:MonoBehaviour
    {
        private Vector3 previous;private float traveled;private bool finished;
        private ProjectileController controller;private ProjectileDamage damage;
        private void Awake(){previous=transform.position;controller=GetComponent<ProjectileController>();damage=GetComponent<ProjectileDamage>();}
        private void FixedUpdate()
        {
            if(!NetworkServer.active||finished||!controller.owner)return;
            var body=controller.owner.GetComponent<CharacterBody>();if(!body)return;
            var current=transform.position;var delta=current-previous;float length=delta.magnitude;
            if(length<.00001f)return;
            float step=Mathf.Min(length,SvenBalance.HammerRange-traveled);
            if(SvenHammerCollision.TryHit(previous,delta/length,step,SvenBalance.HammerWorldRadius,
                LayerIndex.world.mask,LayerIndex.entityPrecise.mask,c=>
                {var hb=c.GetComponent<HurtBox>();return hb&&SvenCombat.Enemy(body,hb.healthComponent);},out var point))
            {Impact(body,point);return;}
            traveled+=step;
            if(traveled>=SvenBalance.HammerRange){Impact(body,previous+delta.normalized*step);return;}previous=current;
        }
        private void Impact(CharacterBody body,Vector3 point)
        {finished=true;SvenCombat.Explode(body,point,damage.damage,damage.crit);NetworkServer.Destroy(gameObject);}
    }
}
