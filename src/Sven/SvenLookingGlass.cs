using System.Collections.Generic;
using System.Reflection;
using BepInEx.Bootstrap;
using RoR2;
namespace SvenMod
{
    internal static class SvenLookingGlass
    {
        private static System.Type type;
        internal static void Init()
        {
            if(!Chainloader.PluginInfos.TryGetValue("droppod.lookingglass",out var plugin))return;
            type=plugin.Instance.GetType().Assembly.GetType("LookingGlass.ItemStatsNameSpace.ProcCoefficientData");Refresh();Language.onCurrentLanguageChanged+=Refresh;
        }
        private static void Refresh()
        {
            try
            {
                var skills=type?.GetField("skills",BindingFlags.Public|BindingFlags.Static)?.GetValue(null) as IDictionary<string,float>;
                var notes=type?.GetField("skillsAdditional",BindingFlags.Public|BindingFlags.Static)?.GetValue(null) as IDictionary<string,string>;
                if(skills==null||notes==null)return;
                foreach(string key in new[]{"PRIMARY","HAMMER","AGHANIM","MOMENTUM"})skills["SVEN_"+key+"_NAME"]=1;
                foreach(string key in new[]{"WARCRY","SHARD"})skills["SVEN_"+key+"_NAME"]=0;
                foreach(string key in new[]{"STRENGTH","ECHO","BLINK","BKB"})skills["SVEN_"+key+"_NAME"]=-1;
                notes["SVEN_MOMENTUM_NAME"]=Language.currentLanguage?.name=="zh-CN"?"\n旋斩两圈，每圈对同一目标结算一次，触发系数均为1。":"\nTwo circular strikes. Each can hit a target once, with a proc coefficient of 1 per hit.";
                notes["SVEN_PRIMARY_NAME"]=Language.currentLanguage?.name=="zh-CN"?"\n每次挥砍对同一目标触发一次；回音追加挥砍触发系数为1。":"\nOnce per target per swing. Echo Sabre's extra swing has a proc coefficient of 1.";
            }
            catch(System.Exception e){Log.Warning("LookingGlass integration unavailable: "+e.Message);}
        }
        internal static void Shutdown(){Language.onCurrentLanguageChanged-=Refresh;type=null;}
    }
}
