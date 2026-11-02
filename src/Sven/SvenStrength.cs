using EntityStates;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;
namespace SvenMod
{
    public class SvenStrength:BaseSkillState
    {
        private SvenController controller;private bool activated;
        public override void OnEnter()
        {
            base.OnEnter();controller=GetComponent<SvenController>();controller.strengthCasting=true;
            var weapon=EntityStateMachine.FindByCustomName(gameObject,"Weapon");if(weapon)weapon.SetNextStateToMain();
            characterBody.isSprinting=false;StartAimMode(SvenBalance.StrengthCastDuration);
            controller.Play("GodStrength",SvenBalance.StrengthCastDuration);
        }
        public override void FixedUpdate()
        {
            base.FixedUpdate();
            // Committed cast: stop planar locomotion, retaining gravity while airborne.
            if(isAuthority&&characterMotor){characterMotor.moveDirection=Vector3.zero;characterMotor.velocity=new Vector3(0,characterMotor.velocity.y,0);}
            if(!activated&&fixedAge>=SvenBalance.StrengthCastPoint)
            {
                activated=true;
                if(NetworkServer.active){controller.ActivateBkb();SvenBuffs.Refresh(characterBody,SvenBuffs.Strength,SvenBalance.StrengthDuration);}
                SvenEffects.Local(SvenEffect.StrengthCast,characterBody,characterBody.corePosition,Vector3.up,1);
                SvenAudio.Play("sven_gods_strength",characterBody.corePosition,.9f);
            }
            if(isAuthority&&fixedAge>=SvenBalance.StrengthCastDuration)outer.SetNextStateToMain();
        }
        public override void OnExit(){if(controller)controller.strengthCasting=false;base.OnExit();}
        public override InterruptPriority GetMinimumInterruptPriority()=>InterruptPriority.PrioritySkill;
    }
}
