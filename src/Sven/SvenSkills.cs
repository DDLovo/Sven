using EntityStates;
using RoR2;
using RoR2.Skills;
using UnityEngine;
using SvenMod.Modules;
namespace SvenMod
{
    public sealed class SvenSkillDef:SkillDef
    {
        public override bool CanExecute(GenericSkill skill)
        {
            var c=skill.GetComponent<SvenController>();
            return (!c||!c.Locked) && base.CanExecute(skill);
        }
    }
    internal static class SvenSkills
    {
        internal static SkillDef Echo,Blink,Bkb,Shard,Aghanim;
        internal static void Init(GameObject prefab)
        {
            var l=prefab.GetComponent<SkillLocator>();
            var relic=prefab.AddComponent<GenericSkill>();relic.skillName="SvenRelic";relic.loadoutTitleToken="SVEN_RELIC_TITLE";
            prefab.GetComponent<SvenController>().relic=relic;
            Echo=Create("ECHO","iconEcho",typeof(Idle),"Stance",0);Blink=Create("BLINK","iconBlink",typeof(Idle),"Stance",0);Bkb=Create("BKB","iconBkb",typeof(Idle),"Stance",0);
            foreach(var d in new[]{Echo,Blink,Bkb}){d.stockToConsume=0;d.hideCooldown=true;d.hideStockCount=true;}
            Assign(relic,Echo,Blink,Bkb);
            var primary=Create("PRIMARY","iconCleave",typeof(SvenSwing),"Weapon",0);primary.mustKeyPress=false;primary.cancelSprintingOnActivation=true;primary.interruptPriority=InterruptPriority.Any;
            Assign(l.primary,primary);
            Aghanim=Create("AGHANIM","iconAghanim",typeof(SvenFlight),"Body",10);
            Assign(l.secondary,Create("HAMMER","iconHammer",typeof(SvenThrow),"Weapon",8),Aghanim);
            Shard=Create("SHARD","iconWarcryImmortal",typeof(SvenWarcry),"Stance",14);
            Assign(l.utility,Create("WARCRY","iconWarcry",typeof(SvenWarcry),"Stance",14),Shard);
            var strength=Create("STRENGTH","iconGodStrength",typeof(SvenStrength),"Body",30);
            strength.cancelSprintingOnActivation=true;strength.beginSkillCooldownOnSkillEnd=true;
            Assign(l.special,strength,Create("MOMENTUM","iconCleaveImmortal",typeof(SvenMomentum),"Weapon",SvenBalance.MomentumCooldown));
            Content.AddEntityState(typeof(SvenFlightExit));
            l.allSkills=prefab.GetComponents<GenericSkill>();l.passiveSkill=new SkillLocator.PassiveSkill{enabled=false};
        }
        private static SkillDef Create(string token,string icon,System.Type state,string machine,float cooldown)
        {
            var d=ScriptableObject.CreateInstance<SvenSkillDef>();d.skillName="Sven"+token;((ScriptableObject)d).name=d.skillName;
            d.skillNameToken="SVEN_"+token+"_NAME";d.skillDescriptionToken="SVEN_"+token+"_DESC";d.icon=SvenAssets.Icon(icon);
            if(token=="HAMMER"||token=="AGHANIM")d.keywordTokens=new[]{"KEYWORD_STUNNING"};
            if(token=="MOMENTUM")d.keywordTokens=new[]{"KEYWORD_HEAVY"};
            d.activationState=new SerializableEntityStateType(state);d.activationStateMachineName=machine;
            d.interruptPriority=InterruptPriority.Skill;d.baseMaxStock=1;d.baseRechargeInterval=cooldown;d.rechargeStock=1;d.requiredStock=1;d.stockToConsume=1;
            d.fullRestockOnAssign=true;d.mustKeyPress=true;d.cancelSprintingOnActivation=false;d.canceledFromSprinting=false;d.beginSkillCooldownOnSkillEnd=false;
            d.isCombatSkill=machine!="Stance";Content.AddSkillDef(d);if(!ContentPacks.entityStates.Contains(state))Content.AddEntityState(state);return d;
        }
        private static void Assign(GenericSkill slot,params SkillDef[] defs)
        {
            var family=ScriptableObject.CreateInstance<SkillFamily>();((ScriptableObject)family).name=defs[0].skillName+"Family";
            family.variants=new SkillFamily.Variant[defs.Length];
            for(int i=0;i<defs.Length;i++)family.variants[i]=new SkillFamily.Variant{skillDef=defs[i],viewableNode=new ViewablesCatalog.Node(defs[i].skillNameToken,false,null)};
            slot._skillFamily=family;Content.AddSkillFamily(family);
        }
    }
}
