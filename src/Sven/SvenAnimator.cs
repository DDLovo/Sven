using System.Linq;
using System.Collections.Generic;
using RoR2;
using UnityEngine;
namespace SvenMod
{
    public sealed class SvenDisplayMarker:MonoBehaviour{}
    public sealed class SvenAnimator:MonoBehaviour
    {
        private Animator animator;private CharacterBody body;private float actionUntil;private string locomotion;
        private bool display,dead,hasStrength,hasPlayback,lastBkb;private float introUntil;
        private Vector3 baseScale;private Renderer[] renderers;
        private readonly HashSet<string> missingStates=new HashSet<string>();
        private readonly Dictionary<string,float> clipLengths=new Dictionary<string,float>();
        private SvenBuffVisuals buffVisuals;
        private SvenSurfaceVariants surfaces;
        private void Awake(){animator=GetComponent<Animator>();baseScale=transform.localScale;renderers=GetComponentsInChildren<Renderer>(true);}
        private void OnEnable()
        {
            if(!animator)animator=GetComponent<Animator>();ResolveBody();
            display=GetComponent<SvenDisplayMarker>();dead=false;lastBkb=false;locomotion=null;actionUntil=0;
            if(!animator||!animator.runtimeAnimatorController){Log.Error("Sven visual has no animator controller: "+name);return;}
            animator.enabled=true;animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
            hasStrength=animator.parameters.Any(p=>p.name=="GodStrengthActive");
            hasPlayback=animator.parameters.Any(p=>p.name=="PlaybackRate");
            clipLengths.Clear();
            foreach(var clip in animator.runtimeAnimatorController.animationClips)clipLengths[clip.name]=clip.length;
            if(!display)buffVisuals=GetComponent<SvenBuffVisuals>()??gameObject.AddComponent<SvenBuffVisuals>();
            if(display){Play("DisplayIntro",1);introUntil=Time.time+ClipLength("DisplayIntro");}else Play("Idle",1);
        }
        private void ResolveBody(){if(!body)body=GetComponentInParent<CharacterModel>()?.body;if(!body)body=GetComponentInParent<CharacterBody>();}
        private float ClipLength(string state)
        {foreach(var clip in clipLengths)if(clip.Key.EndsWith("_"+state,System.StringComparison.Ordinal))return clip.Value;return 1;}
        private bool State(string state,out int hash)
        {
            hash=Animator.StringToHash("Base Layer."+state);
            if(animator&&animator.HasState(0,hash))return true;
            if(missingStates.Add(state))Log.Error("Sven animation state missing: "+state+" on "+name);
            return false;
        }
        private void Play(string state,float speed)
        {
            if(!State(state,out var hash))return;
            SetSpeed(speed);animator.CrossFadeInFixedTime(hash,.08f,0,0);locomotion=state;
        }
        private void SetSpeed(float speed){if(hasPlayback){animator.speed=1;animator.SetFloat("PlaybackRate",speed);}else animator.speed=speed;}
        public void Action(string state,float duration)
        {
            ResolveBody();
            if(!State(state,out var hash))return;
            SetSpeed(Mathf.Max(.1f,ClipLength(state)/Mathf.Max(.05f,duration)));animator.CrossFadeInFixedTime(hash,Mathf.Min(.05f,duration*.12f),0,0);
            actionUntil=Time.time+duration;locomotion=null;
        }
        private void Update()
        {
            if(!animator)return;
            if(display){if(Time.time>=introUntil&&locomotion!="DisplayIdle")Play("DisplayIdle",1);return;}
            ResolveBody();
            if(!body)return;
            bool strength=body.HasBuff(SvenBuffs.Strength),bkb=body.HasBuff(SvenBuffs.Bkb);
            if(bkb&&!lastBkb){SvenEffects.Local(SvenEffect.Bkb,body,body.corePosition,Vector3.up,1);SvenAudio.Play("sven_bkb",body.corePosition,.8f);}lastBkb=bkb;
            if(hasStrength)animator.SetBool("GodStrengthActive",strength);
            transform.localScale=Vector3.Lerp(transform.localScale,baseScale*(bkb?1.2f:1),1-Mathf.Exp(-Time.deltaTime*15));
            if(surfaces==null)surfaces=new SvenSurfaceVariants(renderers);
            surfaces.Apply(strength,bkb);
            var c=body.GetComponent<SvenController>();
            if(buffVisuals)buffVisuals.Tick(body,strength,bkb,body.HasBuff(SvenBuffs.Warcry),body.HasBuff(SvenBuffs.Shard),c&&c.flying);
            if(body.healthComponent&&!body.healthComponent.alive){if(!dead){dead=true;Play("Death",1);}return;}
            if(c&&c.flying&&Time.time>=actionUntil){if(locomotion!="Flight")Play("Flight",1);return;}
            if(Time.time<actionUntil)return;
            var motor=body.characterMotor;float speed=motor?Vector3.ProjectOnPlane(motor.velocity,Vector3.up).magnitude:0;
            string next=motor&&!motor.isGrounded?(motor.velocity.y>0?"Jump":"Fall"):speed>.15f?"Run":"Idle";
            if(next!=locomotion)Play(next,next=="Run"?Mathf.Clamp(speed/7,.6f,2):1);
        }
    }
}
