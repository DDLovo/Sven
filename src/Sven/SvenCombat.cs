using System.Collections.Generic;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;
namespace SvenMod
{
    internal static class SvenCombat
    {
        internal static void Spin(CharacterBody owner,float coefficient,Quaternion frame,float from,float to,HashSet<HealthComponent> hits,bool crit)
        {
            if(!NetworkServer.active||!owner||!owner.healthComponent.alive)return;
            var origin=owner.corePosition;
            foreach(var collider in Physics.OverlapSphere(origin,SvenBalance.SweepBroadphase(true),LayerIndex.entityPrecise.mask,QueryTriggerInteraction.Collide))
            {
                var hb=collider.GetComponent<HurtBox>();var health=hb?hb.healthComponent:null;
                if(!Enemy(owner,health)||hits.Contains(health))continue;
                if(!SvenSpinGeometry.SectorContact(collider,origin,frame,from,to,out var point)||Physics.Linecast(origin,point,LayerIndex.world.mask,QueryTriggerInteraction.Ignore))continue;
                hits.Add(health);Hit(owner,health,point,owner.damage*coefficient,crit,DamageSource.Special);
            }
        }
        internal static void Sweep(CharacterBody owner,Vector3 forward,bool heavy,float coefficient,float swing)
        {
            if(!NetworkServer.active||!owner||!owner.healthComponent.alive)return;
            var origin=owner.corePosition;
            var rotation=SvenSweepGeometry.Rotation(forward,owner.transform.forward);
            // Heavy's existing visual is the unempowered Momentum profile in either buff state.
            var axis=SvenSwingProfiles.Axis(heavy,owner.skinIndex==1,!heavy&&owner.HasBuff(SvenBuffs.Strength),swing);
            var hits=new HashSet<HealthComponent>();bool crit=owner.RollCrit();
            foreach(var collider in Physics.OverlapSphere(origin,SvenBalance.SweepBroadphase(heavy),LayerIndex.entityPrecise.mask,QueryTriggerInteraction.Collide))
            {
                var hb=collider.GetComponent<HurtBox>();var health=hb?hb.healthComponent:null;
                if(!Enemy(owner,health)||hits.Contains(health))continue;
                if(!SvenSweepGeometry.Contact(collider,origin,rotation,heavy,axis,out var point)||Physics.Linecast(origin,point,LayerIndex.world.mask,QueryTriggerInteraction.Ignore))continue;
                hits.Add(health);Hit(owner,health,point,owner.damage*coefficient,crit,heavy?DamageSource.Special:DamageSource.Primary);
            }
        }
        internal static bool Enemy(CharacterBody owner,HealthComponent health)=>health&&health.alive&&health.body&&health.body!=owner&&health.body.teamComponent&&health.body.teamComponent.teamIndex!=owner.teamComponent.teamIndex;
        internal static void Hit(CharacterBody owner,HealthComponent health,Vector3 point,float damage,bool crit,DamageSource source)
        {
            var info=new DamageInfo{attacker=owner.gameObject,inflictor=owner.gameObject,position=point,damage=damage,crit=crit,procCoefficient=1,
                damageType=new DamageTypeCombo(DamageType.Generic,DamageTypeExtended.Generic,source)};
            health.TakeDamage(info);
            if(!info.rejected){GlobalEventManager.instance.OnHitEnemy(info,health.gameObject);GlobalEventManager.instance.OnHitAll(info,health.gameObject);}
        }
        internal static void Explode(CharacterBody owner,Vector3 position,float damage,bool crit)
        {
            if(!NetworkServer.active||!owner)return;
            var hit=new HashSet<HealthComponent>();
            foreach(var c in Physics.OverlapSphere(position,SvenBalance.HammerRadius,LayerIndex.entityPrecise.mask,QueryTriggerInteraction.Collide))
            {
                var hb=c.GetComponent<HurtBox>();var health=hb?hb.healthComponent:null;
                if(!Enemy(owner,health)||!hit.Add(health))continue;
                var point=c.ClosestPoint(position);
                if(Physics.Linecast(position,point,LayerIndex.world.mask,QueryTriggerInteraction.Ignore))continue;
                Hit(owner,health,point,damage,crit,DamageSource.Secondary);
                var hurt=health.GetComponent<SetStateOnHurt>();if(hurt&&hurt.canBeStunned)hurt.SetStun(SvenBalance.Stun);
            }
            SvenEffects.Broadcast(SvenEffect.HammerImpact,owner,position,Vector3.up,1);
        }
        internal static void Warcry(CharacterBody owner,bool shard)
        {
            if(!NetworkServer.active)return;
            foreach(var mate in TeamComponent.GetTeamMembers(owner.teamComponent.teamIndex))
            {
                var body=mate.body;if(!body||!body.healthComponent.alive||(body.corePosition-owner.corePosition).sqrMagnitude>400)continue;
                SvenBuffs.Refresh(body,shard?SvenBuffs.Shard:SvenBuffs.Warcry,8);
                if(shard)body.healthComponent.AddBarrier(body.maxHealth*.5f);
            }
        }
    }
}
