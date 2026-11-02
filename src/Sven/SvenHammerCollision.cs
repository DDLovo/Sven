using System;
using UnityEngine;

namespace SvenMod
{
    // Enemy assistance must not enlarge the projectile against walls or the ground.
    // Both ordinary projectiles and Aghanim flight use the same swept query.
    internal static class SvenHammerCollision
    {
        internal static bool TryHit(Vector3 origin,Vector3 direction,float distance,float worldRadius,
            int worldMask,int enemyMask,Func<Collider,bool> enemy,out Vector3 point)
        {
            point=origin;
            direction=direction.normalized;distance=Mathf.Max(0,distance);
            bool wall=Physics.CheckSphere(origin,worldRadius,worldMask,QueryTriggerInteraction.Ignore);
            float nearest=wall?0:distance;
            if(!wall&&Physics.SphereCast(origin,worldRadius,direction,out var obstruction,distance,worldMask,QueryTriggerInteraction.Ignore))
            {wall=true;nearest=obstruction.distance;}
            // Keep the burst center on the incident side of terrain, never inside a wall.
            if(wall)point=origin+direction*nearest;
            bool found=wall;
            foreach(var collider in Physics.OverlapSphere(origin,SvenBalance.HammerEnemyRadius,enemyMask,QueryTriggerInteraction.Collide))
            {
                if(!enemy(collider))continue;
                var contact=collider.ClosestPoint(origin);
                if(Physics.Linecast(origin,contact,worldMask,QueryTriggerInteraction.Ignore))continue;
                if(wall&&nearest<=0)continue;
                point=origin;return true;
            }
            if(distance<=0)return found;
            foreach(var hit in Physics.SphereCastAll(origin,SvenBalance.HammerEnemyRadius,direction,distance,enemyMask,QueryTriggerInteraction.Collide))
            {
                if(hit.distance>nearest||(wall&&hit.distance>=nearest)||!enemy(hit.collider))continue;
                var center=origin+direction*hit.distance;
                var contact=hit.collider.ClosestPoint(center);
                if(Physics.Linecast(origin,center,worldMask,QueryTriggerInteraction.Ignore)||
                   Physics.Linecast(center,contact,worldMask,QueryTriggerInteraction.Ignore))continue;
                nearest=hit.distance;point=center;found=true;
            }
            return found;
        }
    }
}
