using RoR2;
using UnityEngine;
using UnityEngine.Networking;
using R2API.Networking;
using R2API.Networking.Interfaces;
namespace SvenMod
{
    internal struct SvenMomentumShot{internal CharacterBody body;internal Vector3 direction;internal float speed;internal uint cast;internal int index;}
    internal static class SvenMomentumNetwork
    {
        internal static void Send(CharacterBody body,uint cast,int index,float speed,Vector3 direction)
        {
            if(NetworkServer.active)Receive(body,cast,index,speed,direction);
            else new SvenMomentumMessage(body.netId,cast,index,speed,direction).Send(NetworkDestination.Server);
        }
        internal static void Receive(CharacterBody body,uint cast,int index,float speed,Vector3 direction)
        {
            if(!NetworkServer.active||!body||!body.healthComponent.alive||index<0||index>=2||float.IsNaN(speed)||float.IsInfinity(speed)||speed<0||float.IsNaN(direction.sqrMagnitude)||float.IsInfinity(direction.sqrMagnitude)||direction.sqrMagnitude<.1f)return;
            var c=body.GetComponent<SvenController>();if(!c)return;
            c.casts.Submit(cast,0,index,Time.fixedTime,new SvenMomentumShot{body=body,cast=cast,index=index,speed=speed,direction=direction.normalized},Execute);
        }
        internal static void Cancel(CharacterBody body,uint cast)
        {
            if(NetworkServer.active){var c=body.GetComponent<SvenController>();c?.casts.Cancel(cast,Time.fixedTime);c?.activeSpin?.Cancel(cast);}
            else new SvenMomentumMessage(body.netId,cast,0,0,Vector3.zero,true).Send(NetworkDestination.Server);
        }
        internal static void Execute(SvenMomentumShot shot)
        {if(shot.body&&shot.body.healthComponent.alive)shot.body.GetComponent<SvenController>()?.activeSpin?.BeginTurn(shot);}
    }
    public sealed class SvenMomentumMessage:INetMessage
    {
        private NetworkInstanceId body;private uint cast;private byte index;private float speed;private Vector3 direction;private bool canceled;
        public SvenMomentumMessage(){}
        internal SvenMomentumMessage(NetworkInstanceId id,uint c,int i,float s,Vector3 d,bool cancel=false){body=id;cast=c;index=(byte)i;speed=s;direction=d;canceled=cancel;}
        public void Serialize(NetworkWriter w){w.Write(body);w.Write(cast);w.Write(index);w.Write(speed);w.Write(direction);w.Write(canceled);}
        public void Deserialize(NetworkReader r){body=r.ReadNetworkId();cast=r.ReadUInt32();index=r.ReadByte();speed=r.ReadSingle();direction=r.ReadVector3();canceled=r.ReadBoolean();}
        public void OnReceived(){if(!NetworkServer.active)return;var go=NetworkServer.FindLocalObject(body);if(!go)return;var c=go.GetComponent<SvenController>();if(canceled){c?.casts.Cancel(cast,Time.fixedTime);c?.activeSpin?.Cancel(cast);return;}SvenMomentumNetwork.Receive(go.GetComponent<CharacterBody>(),cast,index,speed,direction);}
    }
}
