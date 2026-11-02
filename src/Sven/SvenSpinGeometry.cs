using UnityEngine;
namespace SvenMod
{
    // Blade Fury is a horizontal full-circle cut; looking upward does not tilt the body spin.
    internal static class SvenSpinGeometry
    {
        internal static bool InSector(Vector3 local,float from,float to)
        {
            if(!Contains(local))return false;
            if(local.x*local.x+local.z*local.z<.000001f)return true;
            float angle=Mathf.Atan2(local.x,local.z)*Mathf.Rad2Deg;
            return Mathf.Abs(Mathf.DeltaAngle((from+to)*.5f,angle))<=(to-from)*.5f+.001f;
        }
        private static Vector3 ProjectSector(Vector3 local,float from,float to)
        {
            float mid=(from+to)*.5f,half=(to-from)*.5f;
            float angle=Mathf.Atan2(local.x,local.z)*Mathf.Rad2Deg;
            float clipped=mid+Mathf.Clamp(Mathf.DeltaAngle(mid,angle),-half,half);
            var radial=new Vector3(Mathf.Sin(clipped*Mathf.Deg2Rad),0,Mathf.Cos(clipped*Mathf.Deg2Rad));
            return radial*Mathf.Clamp(Vector3.Dot(local,radial),0,SvenBalance.MomentumRadius)+Vector3.up*Mathf.Clamp(local.y,-SvenBalance.MomentumHalfThickness,SvenBalance.MomentumHalfThickness);
        }
        internal static bool SectorContact(Collider collider,Vector3 origin,Quaternion frame,float from,float to,out Vector3 point)
        {
            var inverse=Quaternion.Inverse(frame);
            point=collider.ClosestPoint(origin);
            if(InSector(inverse*(point-origin),from,to))return true;
            var local=inverse*(collider.bounds.center-origin);
            for(int i=0;i<12;i++)
            {
                point=collider.ClosestPoint(origin+frame*ProjectSector(local,from,to));
                var next=inverse*(point-origin);if(InSector(next,from,to))return true;
                if((next-local).sqrMagnitude<.00000001f)break;local=next;
            }
            return false;
        }
        internal static bool Contains(Vector3 local)
            =>!float.IsNaN(local.sqrMagnitude)&&!float.IsInfinity(local.sqrMagnitude)
            &&local.x*local.x+local.z*local.z<=(SvenBalance.MomentumRadius+.001f)*(SvenBalance.MomentumRadius+.001f)
            &&Mathf.Abs(local.y)<=SvenBalance.MomentumHalfThickness+.001f;
        internal static bool Contact(Collider collider,Vector3 origin,out Vector3 point)
        {
            point=collider.ClosestPoint(origin);if(Contains(point-origin))return true;
            var local=collider.bounds.center-origin;
            for(int i=0;i<8;i++)
            {
                var flat=Vector3.ClampMagnitude(new Vector3(local.x,0,local.z),SvenBalance.MomentumRadius);
                point=collider.ClosestPoint(origin+flat+Vector3.up*Mathf.Clamp(local.y,-SvenBalance.MomentumHalfThickness,SvenBalance.MomentumHalfThickness));
                var next=point-origin;if(Contains(next))return true;if((next-local).sqrMagnitude<.00000001f)break;local=next;
            }
            return false;
        }
    }
}
