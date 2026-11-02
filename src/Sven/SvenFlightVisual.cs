using UnityEngine;

namespace SvenMod
{
    // Hero-sized travel pressure waves. Source textures are reused, but this is a spatial
    // adaptation, not a claim that Dota's projectile child system is a flight-ring emitter.
    internal sealed class SvenFlightVisual
    {
        private const int Waves=3,Steps=64;
        private readonly Transform root;
        private readonly SvenSpatialMesh waves,electric;

        internal SvenFlightVisual(Transform parent)
        {
            root=parent;
            waves=new SvenSpatialMesh(parent,"Flight expanding pressure waves","beam_plasma_04",Waves,Steps,5);
            electric=new SvenSpatialMesh(parent,"Flight pressure wave electric edges","ribbon_electric2",Waves,Steps,3);
        }

        internal void Sample(Vector3 center,Vector3 direction,float scale,float seconds)
        {
            var forward=direction.sqrMagnitude>.0001f?direction.normalized:Vector3.forward;
            // Avoid the vertical-flight singularity without flattening the actual travel vector.
            var reference=Mathf.Abs(Vector3.Dot(forward,Vector3.up))>.92f?Vector3.right:Vector3.up;
            var side=Vector3.Cross(reference,forward).normalized;
            var up=Vector3.Cross(forward,side).normalized;
            for(int wave=0;wave<Waves;wave++)
            {
                float age=Mathf.Repeat(seconds*2.5f+wave/(float)Waves,1);
                float radius=Mathf.Lerp(.42f,1.12f,age)*scale;
                float depth=Mathf.Lerp(.45f,-1.8f,age)*scale;
                float fade=Mathf.Sin(age*Mathf.PI)*.72f;
                for(int row=0;row<=Steps;row++)
                {
                    float angle=row/(float)Steps*Mathf.PI*2;
                    var radial=side*Mathf.Cos(angle)+up*Mathf.Sin(angle);
                    float ripple=(Mathf.Sin(angle*7+seconds*12+wave)*.022f+Mathf.Sin(angle*11-seconds*8)*.013f)*scale;
                    // A curved band has axial thickness as well as radial width. Side views
                    // retain a bowed pressure front rather than collapsing into a flat ring.
                    for(int column=0;column<5;column++)
                    {
                        float v=column/4f,edge=Mathf.Sin(v*Mathf.PI);
                        float r=radius+(v-.5f)*.24f*scale+ripple;
                        float z=depth-(v-.5f)*.32f*scale-(v-.5f)*(v-.5f)*.3f*scale;
                        var color=new Color(1.6f,1.3f,.64f,fade*edge*.7f);
                        waves.Set(wave,row,column,root.InverseTransformPoint(center+radial*r+forward*z),color);
                    }
                    for(int column=0;column<3;column++)
                    {
                        float v=column/2f;
                        var point=center+radial*(radius+ripple+(v-.5f)*.09f*scale)+forward*(depth+(v-.5f)*.055f*scale);
                        electric.Set(wave,row,column,root.InverseTransformPoint(point),new Color(1.7f,1.6f,1,fade*(column==1?.7f:0)));
                    }
                }
            }
            waves.Upload();electric.Upload();
        }

        internal void Dispose(){waves.Dispose();electric.Dispose();}
    }
}
