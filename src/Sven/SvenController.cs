using RoR2;
using UnityEngine;
using UnityEngine.Networking;
namespace SvenMod
{
    public sealed class SvenController:MonoBehaviour,ITeleportHandler
    {
        public GenericSkill relic;
        internal CharacterBody body;
        internal bool flying,strengthCasting;
        internal readonly SpeedRecord speedMemory=new SpeedRecord();
        internal readonly SvenCastLedger<SvenMomentumShot> casts=new SvenCastLedger<SvenMomentumShot>();
        internal SvenSpinAttack activeSpin;
        private uint nextCast;
        internal uint NextCast(){if(++nextCast==0)++nextCast;return nextCast;}
        internal int nextAttack;
        internal float echoReadyAt, lastHurt=-100;
        private bool wasGrounded;
        private float previousSpeed;
        public void OnTeleport(Vector3 oldPosition,Vector3 newPosition){speedMemory.Clear();previousSpeed=0;wasGrounded=true;}
        internal bool Locked
        {
            get
            {
                if(flying||strengthCasting)return true;
                var m=EntityStateMachine.FindByCustomName(gameObject,"Body");
                return m && (m.state is EntityStates.StunState || m.state is EntityStates.FrozenState || m.state is SvenFlight);
            }
        }
        internal bool HasRelic(RoR2.Skills.SkillDef d)=>relic&&relic.skillDef==d;
        private void Awake(){body=GetComponent<CharacterBody>();}
        private void FixedUpdate()
        {
            UpdateEchoStatus();
            var motor=body.characterMotor;if(!motor)return;
            if(NetworkServer.active)casts.Expire(Time.fixedTime);
            bool grounded=motor.isGrounded;
            if(grounded&&!wasGrounded&&!flying)speedMemory.Save(previousSpeed,Time.fixedTime);
            wasGrounded=grounded;previousSpeed=motor.velocity.magnitude;
            if(!body.healthComponent||!body.healthComponent.alive){speedMemory.Clear();flying=false;}
        }
        internal void BeginEchoCooldown()
        {
            if(!NetworkServer.active||!body)return;
            if(body.HasBuff(SvenBuffs.EchoReady))body.RemoveBuff(SvenBuffs.EchoReady);
            body.ClearTimedBuffs(SvenBuffs.EchoCooldown);
            // One timed status: timer UI shows duration without a second stack counter.
            body.AddTimedBuff(SvenBuffs.EchoCooldown,SvenBalance.EchoCooldown);
        }
        private void UpdateEchoStatus()
        {
            if(!NetworkServer.active||!body)return;
            bool equipped=HasRelic(SvenSkills.Echo)&&body.healthComponent&&body.healthComponent.alive;
            bool ready=equipped&&!body.HasBuff(SvenBuffs.EchoCooldown)&&Time.fixedTime>=echoReadyAt;
            bool displayed=body.HasBuff(SvenBuffs.EchoReady);
            if(ready&&!displayed)body.AddBuff(SvenBuffs.EchoReady);
            else if(!ready&&displayed)body.RemoveBuff(SvenBuffs.EchoReady);
            if(!equipped)body.ClearTimedBuffs(SvenBuffs.EchoCooldown);
        }
        internal void ActivateBkb(float duration=6f)
        {
            if(!NetworkServer.active||!HasRelic(SvenSkills.Bkb))return;
            Util.CleanseBody(body,true,false,false,true,true,false);
            SvenBuffs.Refresh(body,SvenBuffs.Bkb,duration);
        }
        internal void Play(string state,float duration)
        {
            foreach(var a in Visuals(body))
            {
                a.Action(state,duration);
                if(state.StartsWith("Attack",System.StringComparison.Ordinal)||state=="MomentumCleave")
                {
                    var trail=a.GetComponent<SvenSwordTrail>()??a.gameObject.AddComponent<SvenSwordTrail>();
                    trail.Schedule(state=="MomentumCleave"?SvenBalance.SpinEntry:duration*.18f,state=="MomentumCleave"?SvenBalance.SpinTurnDuration*2:duration*.42f,body.HasBuff(SvenBuffs.Strength),body.skinIndex==1);
                }
                else a.GetComponent<SvenSwordTrail>()?.StopCapture();
            }
        }
        // ModelLocator.Start detaches the visible model from the body hierarchy.
        // This also selects only the active skin after ModelSkinController switches it.
        internal static SvenAnimator[] Visuals(CharacterBody owner)
        {
            var locator=owner?owner.modelLocator:null;
            return locator&&locator.modelTransform
                ?locator.modelTransform.GetComponentsInChildren<SvenAnimator>()
                :System.Array.Empty<SvenAnimator>();
        }
    }
}
