using System;
namespace SvenMod
{
    internal static class SvenBalance
    {
        internal const float ModelScale=1.3f, MomentumCooldown=7f, AghanimHammer=3f;
        internal const float Primary=3f, PrimaryInterval=.9f, EchoCooldown=3f, Hammer=6f, HammerRange=80f;
        internal const float HammerSpeed=40f, HammerRadius=8f, Stun=1.75f, StrengthDuration=10f;
        internal const float AghanimRange=40f, BlinkRange=30f, HammerEnemyRadius=4f, HammerWorldRadius=.3f;
        internal const float HammerLifetime=HammerRange/HammerSpeed+.5f, FlightTimeout=.2f+AghanimRange/HammerSpeed+.3f;
        internal const float MomentumBase=20f, MomentumPerSpeed=.6f, MomentumWindup=.2f, SpeedMemory=.2f;
        internal const float SpinEntry=.15f, SpinTurnDuration=.4f, SpinDuration=1.1f;
        internal const float SpinStartDegrees=-25.3f;
        internal static float SpinHitTime(int index)=>SpinEntry+(index+1)*SpinTurnDuration;
        internal static float SpinDamage(float speed)=>Momentum(speed)*.5f;
        internal const float PrimaryRadius=10f, PrimaryAngle=120f, MomentumRadius=20f, MomentumRearRadius=3f;
        internal const float PrimaryHalfHeight=5f, MomentumHalfHeight=6f;
        internal const float PrimaryHalfThickness=2f, MomentumHalfThickness=3f;
        internal const float StrengthCastDuration=.8f, StrengthCastPoint=.55f, MomentumAnimationHit=10f/34f;
        // Audited source sword-tip paths for both skins: overhead, right-to-left, left-to-right.
        internal static float SwingDirection(int attackIndex)=>attackIndex%3==0?0:attackIndex%3==1?1:-1;
        internal static float SweepRadius(bool heavy)=>heavy?MomentumRadius:PrimaryRadius;
        internal static float SweepAngle(bool heavy)=>heavy?180f:PrimaryAngle;
        internal static float SweepHalfHeight(bool heavy)=>heavy?MomentumHalfHeight:PrimaryHalfHeight;
        internal static float SweepHalfThickness(bool heavy)=>heavy?MomentumHalfThickness:PrimaryHalfThickness;
        internal static float SweepBroadphase(bool heavy)=>(float)Math.Sqrt(SweepRadius(heavy)*SweepRadius(heavy)+SweepHalfThickness(heavy)*SweepHalfThickness(heavy));
        internal static float Momentum(float speed)=>MomentumBase+MomentumPerSpeed*(float.IsNaN(speed)||float.IsInfinity(speed) ? 0 : Math.Max(0,speed));
    }
    internal sealed class SpeedRecord
    {
        private float speed,expires=-1;
        internal void Save(float value,float now) {speed=Math.Max(0,value);expires=now+SvenBalance.SpeedMemory;}
        internal float Consume(float now) {float result=now<=expires?speed:0;Clear();return result;}
        internal void Clear() {speed=0;expires=-1;}
    }
}
