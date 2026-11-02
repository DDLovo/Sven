using R2API;
using RoR2;
using UnityEngine;
namespace SvenMod
{
    internal static class SvenHooks
    {
        private static bool installed;
        internal static void Init()
        {
            if(installed)return;installed=true;RecalculateStatsAPI.GetStatCoefficients+=Stats;
            GlobalEventManager.onServerDamageDealt+=Hurt;
            On.RoR2.SetStateOnHurt.SetStun+=Stun;On.RoR2.SetStateOnHurt.SetFrozen+=Freeze;
            On.RoR2.SetStateOnHurt.SetMidasTouchFrozen+=MidasFreeze;
            On.RoR2.CharacterBody.AddTimedBuff_BuffDef_float+=TimedBuff;
            On.RoR2.CharacterBody.AddBuff_BuffIndex+=Buff;
            On.RoR2.HealthComponent.TakeDamageForce_PhysForceInfo+=Force;
        }
        internal static void Shutdown()
        {
            if(!installed)return;installed=false;RecalculateStatsAPI.GetStatCoefficients-=Stats;GlobalEventManager.onServerDamageDealt-=Hurt;
            On.RoR2.SetStateOnHurt.SetStun-=Stun;On.RoR2.SetStateOnHurt.SetFrozen-=Freeze;
            On.RoR2.SetStateOnHurt.SetMidasTouchFrozen-=MidasFreeze;
            On.RoR2.CharacterBody.AddTimedBuff_BuffDef_float-=TimedBuff;
            On.RoR2.CharacterBody.AddBuff_BuffIndex-=Buff;
            On.RoR2.HealthComponent.TakeDamageForce_PhysForceInfo-=Force;
        }
        private static bool Immune(CharacterBody b)=>b&&SvenBuffs.Bkb&&b.HasBuff(SvenBuffs.Bkb);
        private static void Stats(CharacterBody b,RecalculateStatsAPI.StatHookEventArgs args)
        {
            if(SvenBuffs.Warcry&&b.HasBuff(SvenBuffs.Warcry)){args.armorAdd+=100;args.moveSpeedMultAdd+=.5f;}
            else if(SvenBuffs.Shard&&b.HasBuff(SvenBuffs.Shard))args.moveSpeedMultAdd+=.3f;
        }
        private static void Hurt(DamageReport report)
        {var c=report.victimBody?report.victimBody.GetComponent<SvenController>():null;if(c&&report.damageDealt>0)c.lastHurt=Time.fixedTime;}
        private static void Stun(On.RoR2.SetStateOnHurt.orig_SetStun orig,SetStateOnHurt self,float duration)
        {if(!Immune(self.GetComponent<CharacterBody>()))orig(self,duration);}
        private static void Freeze(On.RoR2.SetStateOnHurt.orig_SetFrozen orig,SetStateOnHurt self,float duration)
        {if(!Immune(self.GetComponent<CharacterBody>()))orig(self,duration);}
        private static void MidasFreeze(On.RoR2.SetStateOnHurt.orig_SetMidasTouchFrozen orig,SetStateOnHurt self,float duration)
        {if(!Immune(self.GetComponent<CharacterBody>()))orig(self,duration);}
        private static bool Control(BuffDef buff)
        {
            if(!buff)return false;
            if(buff==RoR2Content.Buffs.ClayGoo||buff==RoR2Content.Buffs.NullifyStack||buff==RoR2Content.Buffs.Nullified||buff==DLC3Content.Buffs.Jailed)return true;
            if(!buff.isDebuff)return false;
            var name=buff.name.ToLowerInvariant();
            return name.Contains("slow")||name.Contains("cripple")||name.Contains("root")||name.Contains("frost")||name.Contains("freeze");
        }
        private static void Buff(On.RoR2.CharacterBody.orig_AddBuff_BuffIndex orig,CharacterBody body,BuffIndex index)
        {if(!Immune(body)||!Control(BuffCatalog.GetBuffDef(index)))orig(body,index);}
        private static void TimedBuff(On.RoR2.CharacterBody.orig_AddTimedBuff_BuffDef_float orig,CharacterBody body,BuffDef buff,float duration)
        {
            if(Immune(body)&&Control(buff))return;
            orig(body,buff,duration);
        }
        private static void Force(On.RoR2.HealthComponent.orig_TakeDamageForce_PhysForceInfo orig,HealthComponent self,PhysForceInfo force)
        {if(!Immune(self.body))orig(self,force);}
    }
}

