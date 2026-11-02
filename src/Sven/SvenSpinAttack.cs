using System.Collections.Generic;
using RoR2;
using UnityEngine;
namespace SvenMod
{
    // One live server cast; requests authorize turns, the state clock drives their sweep.
    internal sealed class SvenSpinAttack
    {
        private sealed class Turn
        {
            internal readonly HashSet<HealthComponent> hit=new HashSet<HealthComponent>();
            internal float swept;internal Quaternion frame;internal bool ready,crit;
        }
        private readonly Turn[] turns={new Turn(),new Turn()};
        private readonly CharacterBody body;private readonly uint cast;
        private float coefficient;private bool snapshot,canceled;
        internal SvenSpinAttack(CharacterBody owner,uint id){body=owner;cast=id;}
        internal void BeginTurn(SvenMomentumShot shot)
        {
            if(canceled||shot.cast!=cast||shot.index<0||shot.index>=2||turns[shot.index].ready)return;
            if(!snapshot){coefficient=SvenBalance.SpinDamage(shot.speed);snapshot=true;}
            var turn=turns[shot.index];turn.ready=true;turn.crit=body.RollCrit();
            turn.frame=SvenSweepGeometry.Rotation(Vector3.ProjectOnPlane(shot.direction,Vector3.up),body.transform.forward);
        }
        internal void Advance(float stateAge)
        {
            if(canceled||!body||!body.healthComponent.alive)return;
            for(int i=0;i<2;i++)
            {
                var turn=turns[i];if(!turn.ready)continue;
                float elapsed=stateAge-SvenBalance.SpinEntry-i*SvenBalance.SpinTurnDuration;
                float target=elapsed>=SvenBalance.SpinTurnDuration-.000001f?360:Mathf.Clamp01(elapsed/SvenBalance.SpinTurnDuration)*360;
                // Bounded sectors remain convex and cover every missed angle on a long tick.
                while(turn.swept<target&&!canceled&&body.healthComponent.alive)
                {
                    float from=turn.swept,to=Mathf.Min(target,from+12);turn.swept=to;
                    SvenCombat.Spin(body,coefficient,turn.frame,SvenBalance.SpinStartDegrees+from,SvenBalance.SpinStartDegrees+to,turn.hit,turn.crit);
                }
            }
        }
        internal void Cancel(){canceled=true;}
        internal void Cancel(uint id){if(id==cast)Cancel();}
    }
}
