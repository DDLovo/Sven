using EntityStates;
using RoR2;
using RoR2.Projectile;
using UnityEngine;
using UnityEngine.Networking;
namespace SvenMod
{
    public class SvenSwing:BaseSkillState
    {
        private SvenController controller;
        private float duration,hitAt,echoAt;
        private bool hit,echo,echoPlayed,echoHit,receivedSwing;
        private int index;
        public override void OnEnter()
        {
            base.OnEnter();controller=GetComponent<SvenController>();
            // Native NetworkStateMachine deserializes before OnEnter. Preserve the owner's
            // sequence and timing on relays instead of reinterpreting a local interrupted Echo.
            if(!receivedSwing)
            {
                index=controller.nextAttack%3;
                duration=SvenBalance.PrimaryInterval/Mathf.Max(.1f,attackSpeedStat);
                echo=controller.HasRelic(SvenSkills.Echo)&&Time.fixedTime+duration*(index==1?.276f:.345f)>=controller.echoReadyAt;
            }
            controller.nextAttack=(index+1)%3;
            hitAt=duration*(index==1?.276f:.345f);
            echoAt=duration*.62f;
            controller.Play("Attack"+(index+1),duration);
            StartAimMode(duration+.2f);characterBody.isSprinting=false;
        }
        public override void FixedUpdate()
        {
            base.FixedUpdate();if(controller.flying||controller.strengthCasting){if(isAuthority)outer.SetNextStateToMain();return;}
            if(!hit&&fixedAge>=hitAt)
            {
                hit=true;
                if(echo){controller.echoReadyAt=Time.fixedTime+SvenBalance.EchoCooldown;controller.BeginEchoCooldown();}
                Strike(false);
            }
            if(echo&&!echoPlayed&&fixedAge>=echoAt)
            {echoPlayed=true;controller.nextAttack=(index+2)%3;controller.Play("Attack"+((index+1)%3+1),duration*.5f);}
            if(echo&&!echoHit&&fixedAge>=echoAt+duration*.16f){echoHit=true;Strike(true);}
            if(isAuthority&&fixedAge>=duration*(echo?1.12f:1f))outer.SetNextStateToMain();
        }
        private void Strike(bool repeated)
        {
            var direction=GetAimRay().direction;bool strength=characterBody.HasBuff(SvenBuffs.Strength);
            float swing=SvenBalance.SwingDirection((index+(repeated?1:0))%3);
            SvenCombat.Sweep(characterBody,direction,false,SvenBalance.Primary*(strength?3:1),swing);
            SvenEffects.Local(strength?SvenEffect.StrengthSlash:SvenEffect.Slash,characterBody,characterBody.corePosition,direction,swing);
            SvenAudio.Play("sven_attack0"+(index+1),characterBody.corePosition,.7f,transform);
        }
        public override InterruptPriority GetMinimumInterruptPriority()=>InterruptPriority.Skill;
        public override void OnSerialize(NetworkWriter writer)
        {base.OnSerialize(writer);writer.Write((byte)index);writer.Write(echo);writer.Write(duration);}
        public override void OnDeserialize(NetworkReader reader)
        {
            base.OnDeserialize(reader);index=reader.ReadByte()%3;echo=reader.ReadBoolean();duration=reader.ReadSingle();
            if(float.IsNaN(duration)||float.IsInfinity(duration)||duration<=0)duration=SvenBalance.PrimaryInterval;
            receivedSwing=true;
        }
    }
    public class SvenThrow:BaseSkillState
    {
        private float duration;private bool fired;
        public override void OnEnter(){base.OnEnter();duration=.45f/Mathf.Max(.1f,attackSpeedStat);GetComponent<SvenController>().Play("CastHammer",duration);StartAimMode(duration+.5f);}
        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if(!fired&&fixedAge>=duration*.45f)
            {
                fired=true;
                if(isAuthority){var ray=GetAimRay();ProjectileManager.instance.FireProjectile(new FireProjectileInfo{projectilePrefab=SvenProjectiles.hammer,owner=gameObject,position=ray.origin,rotation=Util.QuaternionSafeLookRotation(ray.direction),damage=damageStat*SvenBalance.Hammer,crit=RollCrit(),speedOverride=SvenBalance.HammerSpeed});}
                SvenAudio.Play("sven_storm_bolt",characterBody.corePosition,.8f);
            }
            if(isAuthority&&fixedAge>=duration)outer.SetNextStateToMain();
        }
        public override InterruptPriority GetMinimumInterruptPriority()=>InterruptPriority.Skill;
    }
    public class SvenWarcry:BaseSkillState
    {
        private bool blink;
        public override void OnEnter()
        {
            base.OnEnter();var c=GetComponent<SvenController>();bool shard=skillLocator.utility.skillDef==SvenSkills.Shard;
            var weapon=EntityStateMachine.FindByCustomName(gameObject,"Weapon");if(weapon&&weapon.state is Idle)c.Play("Warcry",.55f);
            blink=c.HasRelic(SvenSkills.Blink)&&characterBody.isSprinting&&Time.fixedTime-c.lastHurt>=3;
            if(NetworkServer.active)SvenCombat.Warcry(characterBody,shard);
            SvenEffects.Local(SvenEffect.Warcry,characterBody,characterBody.corePosition,Vector3.up,shard?1.3f:1);
            SvenAudio.PlayWarcry(shard,characterBody.skinIndex==1,characterBody.corePosition,characterBody.transform);
            if(NetworkServer.active&&blink)
            {
                var direction=GetAimRay().direction.normalized;float distance=SvenBalance.BlinkRange;
                if(Physics.SphereCast(characterBody.corePosition,.65f,direction,out var hit,SvenBalance.BlinkRange,LayerIndex.world.mask,QueryTriggerInteraction.Ignore))distance=Mathf.Max(0,hit.distance-.15f);
                Vector3 target=transform.position+direction*distance;
                while(distance>0&&Physics.CheckCapsule(target+Vector3.up*.6f,target+Vector3.up*1.5f,.5f,LayerIndex.world.mask,QueryTriggerInteraction.Ignore))
                {distance-=.25f;target=transform.position+direction*Mathf.Max(0,distance);}
                c.speedMemory.Clear();
                // Blink uses the direction payload for the departure position, so relays
                // cannot lose the start effect when transform synchronization arrives first.
                SvenEffects.Broadcast(SvenEffect.Blink,characterBody,target,transform.position,1);
            }
        }
        public override void FixedUpdate(){base.FixedUpdate();if(isAuthority&&fixedAge>=.12f)outer.SetNextStateToMain();}
    }
    public class SvenMomentum:BaseSkillState
    {
        private float boundSpeed;private bool started;private int hits,turns;private uint cast;private SvenSpinAttack serverSpin;
        public override void OnEnter()
        {
            base.OnEnter();var c=GetComponent<SvenController>();boundSpeed=c.speedMemory.Consume(Time.fixedTime);
            if(isAuthority)cast=c.NextCast();
            if(NetworkServer.active)
            {
                c.activeSpin?.Cancel();serverSpin=new SvenSpinAttack(characterBody,cast);c.activeSpin=serverSpin;
                c.casts.Begin(cast,0,2,Time.fixedTime,SvenBalance.SpinDuration,SvenMomentumNetwork.Execute);
            }
            if(NetworkServer.active)c.ActivateBkb(2f);c.Play("MomentumCleave",SvenBalance.SpinDuration);StartAimMode(SvenBalance.SpinDuration);
        }
        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if(!started&&fixedAge>=SvenBalance.SpinEntry)
            {
                started=true;boundSpeed=Mathf.Max(boundSpeed,characterMotor?characterMotor.velocity.magnitude:0);
            }
            while(turns<2&&fixedAge>=SvenBalance.SpinEntry+turns*SvenBalance.SpinTurnDuration)
            {
                var direction=characterDirection?characterDirection.forward:transform.forward;
                if(isAuthority)SvenMomentumNetwork.Send(characterBody,cast,turns,boundSpeed,direction);
                turns++;SvenEffects.Local(SvenEffect.Momentum,characterBody,characterBody.corePosition,direction,1);
            }
            while(hits<2&&fixedAge>=SvenBalance.SpinHitTime(hits))
            {
                hits++;
                SvenAudio.Play("sven_gods_strength_heavy_sword_impact1",characterBody.corePosition,1);
            }
            if(NetworkServer.active)serverSpin?.Advance(fixedAge);
            if(isAuthority&&fixedAge>=SvenBalance.SpinDuration)outer.SetNextStateToMain();
        }
        public override InterruptPriority GetMinimumInterruptPriority()=>InterruptPriority.PrioritySkill;
        public override void OnSerialize(NetworkWriter w){base.OnSerialize(w);w.Write(cast);}
        public override void OnDeserialize(NetworkReader r){base.OnDeserialize(r);cast=r.ReadUInt32();}
        public override void OnExit()
        {
            serverSpin?.Cancel();var c=GetComponent<SvenController>();if(c&&c.activeSpin==serverSpin)c.activeSpin=null;
            if(isAuthority||NetworkServer.active)SvenMomentumNetwork.Cancel(characterBody,cast);
            base.OnExit();
        }
    }
    public class SvenFlight:BaseSkillState
    {
        private SvenController controller;
        private Vector3 direction;
        private float traveled;
        private bool immune,launched;
        public bool travelActive=>launched;
        public Vector3 travelDirection=>direction;
        public override void OnEnter()
        {
            base.OnEnter();controller=GetComponent<SvenController>();controller.flying=true;
            if(isAuthority||direction.sqrMagnitude<.1f)direction=GetAimRay().direction.normalized;
            if(NetworkServer.active){characterBody.AddBuff(RoR2Content.Buffs.HiddenInvincibility);characterBody.AddBuff(SvenBuffs.Flight);immune=true;}
            var weapon=EntityStateMachine.FindByCustomName(gameObject,"Weapon");if(weapon!=null)weapon.SetNextStateToMain();
            controller.Play("CastHammer",.2f);
            SvenAudio.Play("sven_storm_bolt",characterBody.corePosition,.85f);
        }
        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if(fixedAge<.2f)return;
            if(!launched){launched=true;controller.Play("Flight",1.67f);}
            if(!isAuthority)return;
            float step=Mathf.Min(SvenBalance.HammerSpeed*GetDeltaTime(),SvenBalance.AghanimRange-traveled);
            if(SvenHammerCollision.TryHit(characterBody.corePosition,direction,step,.5f,
                LayerIndex.world.mask,LayerIndex.entityPrecise.mask,c=>
                {var hb=c.GetComponent<HurtBox>();return hb&&SvenCombat.Enemy(characterBody,hb.healthComponent);},out var point))
            {Finish(point);return;}
            if(traveled>=SvenBalance.AghanimRange||fixedAge>=SvenBalance.FlightTimeout){Finish(characterBody.corePosition);return;}
            characterMotor.Motor.ForceUnground();characterMotor.velocity=direction*(step/GetDeltaTime());
            traveled+=step;var facing=Vector3.ProjectOnPlane(direction,Vector3.up);if(facing.sqrMagnitude>.0001f)characterDirection.forward=facing.normalized;
        }
        private void Finish(Vector3 point)
        {
            float actualSpeed=characterMotor.velocity.magnitude;
            characterMotor.velocity=Vector3.zero;
            outer.SetNextState(new SvenFlightExit{impact=point,arrivalSpeed=actualSpeed});
        }
        public override void OnExit()
        {
            controller.flying=false;
            if(immune&&NetworkServer.active){characterBody.RemoveBuff(RoR2Content.Buffs.HiddenInvincibility);characterBody.RemoveBuff(SvenBuffs.Flight);}
            if(isAuthority&&characterMotor)characterMotor.velocity=Vector3.zero;
            base.OnExit();
        }
        public override void OnSerialize(NetworkWriter writer){base.OnSerialize(writer);writer.Write(direction);}
        public override void OnDeserialize(NetworkReader reader){base.OnDeserialize(reader);direction=reader.ReadVector3();}
        public override InterruptPriority GetMinimumInterruptPriority()=>InterruptPriority.Death;
    }
    public class SvenFlightExit:BaseSkillState
    {
        public Vector3 impact;public float arrivalSpeed;
        public override void OnEnter()
        {
            base.OnEnter();var c=GetComponent<SvenController>();c.flying=false;
            c.speedMemory.Save(float.IsNaN(arrivalSpeed)||float.IsInfinity(arrivalSpeed)?0:Mathf.Max(0,arrivalSpeed),Time.fixedTime);
            if(NetworkServer.active&&(impact-characterBody.corePosition).sqrMagnitude<100)
                SvenCombat.Explode(characterBody,impact,damageStat*SvenBalance.AghanimHammer,RollCrit());
        }
        public override void FixedUpdate(){base.FixedUpdate();if(isAuthority)outer.SetNextStateToMain();}
        public override void OnSerialize(NetworkWriter writer){base.OnSerialize(writer);writer.Write(impact);writer.Write(arrivalSpeed);}
        public override void OnDeserialize(NetworkReader reader){base.OnDeserialize(reader);impact=reader.ReadVector3();arrivalSpeed=reader.ReadSingle();}
    }
}
