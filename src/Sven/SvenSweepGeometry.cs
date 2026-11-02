using UnityEngine;
namespace SvenMod
{
    // One aim frame for server hit queries and client presentation, including vertical aim.
    internal static class SvenSweepGeometry
    {
        internal static Quaternion Rotation(Vector3 direction,Vector3 fallbackForward)
        {
            if(!Finite(direction)||direction.sqrMagnitude<.0001f)direction=fallbackForward;
            if(!Finite(direction)||direction.sqrMagnitude<.0001f)direction=Vector3.forward;
            direction.Normalize();
            var right=Vector3.Cross(Vector3.up,direction);
            if(right.sqrMagnitude<.0001f)
            {
                var horizontal=Vector3.ProjectOnPlane(fallbackForward,Vector3.up);
                if(!Finite(horizontal)||horizontal.sqrMagnitude<.0001f)horizontal=Vector3.forward;
                right=Vector3.Cross(Vector3.up,horizontal.normalized);
            }
            return Quaternion.LookRotation(direction,Vector3.Cross(direction,right).normalized);
        }
        internal static float TransverseScale(bool heavy,Vector3 cutAxis)
        {
            float elevation=SvenBalance.SweepRadius(heavy)*Mathf.Sin(SvenBalance.SweepAngle(heavy)*.5f*Mathf.Deg2Rad)*Mathf.Abs(cutAxis.y);
            return Mathf.Min(1,SvenBalance.SweepHalfHeight(heavy)/Mathf.Max(.001f,elevation));
        }
        internal static bool Contains(Vector3 local,bool heavy,Vector3 cutAxis)
        {
            if(!Finite(local))return false;
            const float epsilon=.001f;
            // Momentum's short rear lobe has its own horizontal visual and remains independent.
            if(heavy&&local.z<0)
                return local.x*local.x+local.z*local.z<=Mathf.Pow(SvenBalance.MomentumRearRadius+epsilon,2)&&Mathf.Abs(local.y)<=SvenBalance.MomentumHalfHeight+epsilon;
            var normal=Vector3.Cross(Vector3.forward,cutAxis);
            if(Mathf.Abs(Vector3.Dot(local,normal))>SvenBalance.SweepHalfThickness(heavy)+epsilon)return false;
            float across=Vector3.Dot(local,cutAxis)/TransverseScale(heavy,cutAxis);
            float radius=Mathf.Sqrt(across*across+local.z*local.z);
            return radius<=SvenBalance.SweepRadius(heavy)+epsilon&&(radius<epsilon||local.z/radius>=Mathf.Cos(SvenBalance.SweepAngle(heavy)*.5f*Mathf.Deg2Rad)-.00001f);
        }
        // Probe the actual collider toward the finite volume. A tall enemy can intersect
        // the slash even when its closest point to the caster sits outside the cut plane.
        // No bounding-sphere-only hits: the final point must belong to the collider AND volume.
        internal static bool Contact(Collider collider,Vector3 origin,Quaternion rotation,bool heavy,Vector3 cutAxis,out Vector3 point)
        {
            var inverse=Quaternion.Inverse(rotation);
            point=collider.ClosestPoint(origin);
            if(Contains(inverse*(point-origin),heavy,cutAxis))return true;
            for(int lobe=0;lobe<(heavy?2:1);lobe++)
            {
                var local=inverse*(collider.bounds.center-origin);
                for(int step=0;step<8;step++)
                {
                    var projected=Project(local,heavy,cutAxis,lobe==1);
                    point=collider.ClosestPoint(origin+rotation*projected);
                    var next=inverse*(point-origin);
                    if(Contains(next,heavy,cutAxis))return true;
                    if((next-local).sqrMagnitude<.00000001f)break;
                    local=next;
                }
            }
            return false;
        }
        private static Vector3 Project(Vector3 local,bool heavy,Vector3 cutAxis,bool rear)
        {
            if(rear)
            {
                var radial=Vector3.ClampMagnitude(new Vector3(local.x,0,Mathf.Min(0,local.z)),SvenBalance.MomentumRearRadius);
                return radial+Vector3.up*Mathf.Clamp(local.y,-SvenBalance.MomentumHalfHeight,SvenBalance.MomentumHalfHeight);
            }
            float scale=TransverseScale(heavy,cutAxis),across=Vector3.Dot(local,cutAxis)/scale;
            float limit=SvenBalance.SweepAngle(heavy)*.5f*Mathf.Deg2Rad;
            float angle=Mathf.Clamp(Mathf.Atan2(across,local.z),-limit,limit);
            float radius=Mathf.Clamp(across*Mathf.Sin(angle)+local.z*Mathf.Cos(angle),0,SvenBalance.SweepRadius(heavy));
            var normal=Vector3.Cross(Vector3.forward,cutAxis);float thickness=SvenBalance.SweepHalfThickness(heavy);
            return cutAxis*(Mathf.Sin(angle)*radius*scale)+Vector3.forward*(Mathf.Cos(angle)*radius)+normal*Mathf.Clamp(Vector3.Dot(local,normal),-thickness,thickness);
        }
        private static bool Finite(Vector3 value)=>!float.IsNaN(value.sqrMagnitude)&&!float.IsInfinity(value.sqrMagnitude);
    }
}
