using RoR2;
using UnityEngine;
using SvenMod.Modules;
namespace SvenMod
{
    internal static class SvenBuffs
    {
        internal static BuffDef Strength,Warcry,Shard,Bkb,Flight,EchoReady,EchoCooldown;
        internal static void Init()
        {
            Strength=New("Strength","buffGodStrength",Color.white);
            Warcry=New("Warcry","buffWarcry",Color.white);Shard=New("Shard","buffWarcryImmortal",Color.white);
            Bkb=New("BKB","buffBkb",Color.white);Flight=New("Flight","iconHammer",Color.white);Flight.isHidden=true;
            EchoReady=New("EchoReady","buffEcho",Color.white);EchoReady.ignoreGrowthNectar=true;
            EchoCooldown=New("EchoCooldown","buffEchoCooldown",new Color(.32f,.32f,.32f,1f));
            EchoCooldown.isCooldown=true;EchoCooldown.canStack=false;EchoCooldown.ignoreGrowthNectar=true;
        }
        private static BuffDef New(string name,string icon,Color color)=>Content.CreateAndAddBuff("Sven"+name,SvenAssets.Icon(icon),color,false,false);
        internal static void Refresh(CharacterBody body,BuffDef buff,float time)
        {body.ClearTimedBuffs(buff);body.AddTimedBuff(buff,time);}
    }
}
