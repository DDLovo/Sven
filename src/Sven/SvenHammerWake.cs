using UnityEngine;
namespace SvenMod
{
    // Source glow_b/c orient to the moving CP. Keep the wake in the projectile's
    // flight frame instead of using screen-up as a directional sprite axis.
    internal sealed class SvenHammerWake:MonoBehaviour
    {
        private SvenSpatialMesh envelope,streams,sparks;
        internal void Initialize()
        {
            envelope=new SvenSpatialMesh(transform,"Hammer golden flight envelope","beam_generic_2",1,24,17);
            streams=new SvenSpatialMesh(transform,"Hammer axial golden streams","ribbon_electric2",12,18,3);
            sparks=new SvenSpatialMesh(transform,"Hammer backward flying sparks","particle_glow_05",96,1,3);
            Sample(0);
        }
        internal static Vector3 SparkPosition(int i,float seconds)
        {
            float t=Mathf.Repeat(seconds*2.4f+i*.618034f,1),a=i*2.399963f;
            float radius=(.35f+.6f*((i*7)%13)/12f)+t*.75f;
            return new Vector3(Mathf.Cos(a)*radius,Mathf.Sin(a)*radius,.1f-t*6.5f);
        }
        internal void Sample(float seconds)
        {
            if(envelope==null)return;
            for(int row=0;row<=24;row++)
            {
                float t=row/24f,z=Mathf.Lerp(.75f,-6.5f,t);
                float radius=(.18f+1.65f*Mathf.Sin(Mathf.Pow(t,.55f)*Mathf.PI))*Mathf.Lerp(1,.3f,t);
                for(int j=0;j<=16;j++)
                {
                    float a=j/16f*Mathf.PI*2;float flutter=1+.06f*Mathf.Sin(a*3+seconds*11-t*9);
                    var tint=Color.Lerp(new Color(2.2f,1.65f,.45f),new Color(1.3f,.83f,.12f),t);
                    tint.a=.38f*Mathf.Clamp01(t*14)*Mathf.Pow(1-t,1.5f);
                    envelope.Set(0,row,j,new Vector3(Mathf.Cos(a)*radius*flutter,Mathf.Sin(a)*radius*flutter,z),tint);
                }
            }
            envelope.Upload();
            for(int i=0;i<6;i++)for(int row=0;row<=18;row++)
            {
                float t=row/18f,a=i*Mathf.PI/3+seconds*.65f+t*.7f;
                var radial=new Vector3(Mathf.Cos(a),Mathf.Sin(a),0);var tangent=new Vector3(-radial.y,radial.x,0);
                float radius=.28f+Mathf.Sin(t*Mathf.PI)*(.85f+.13f*Mathf.Sin(t*17-seconds*16+i));
                var center=radial*radius+Vector3.forward*Mathf.Lerp(.5f,-5.7f,t);
                for(int cross=0;cross<2;cross++)for(int j=0;j<3;j++)
                {
                    var tint=Color.Lerp(new Color(2.5f,2.15f,.8f),new Color(1.9f,1.1f,.12f),t);
                    tint.a=(j==1?.6f:0)*Mathf.Clamp01(t*12)*Mathf.Pow(1-t,1.4f);
                    streams.Set(i*2+cross,row,j,center+(cross==0?radial:tangent)*(j-1)*(.12f*(1-t)+.03f),tint);
                }
            }
            streams.Upload();
            for(int i=0;i<48;i++)
            {
                float t=Mathf.Repeat(seconds*2.4f+i*.618034f,1);var p=SparkPosition(i,seconds);
                for(int cross=0;cross<2;cross++)for(int row=0;row<=1;row++)for(int j=0;j<3;j++)
                {
                    var tint=new Color(2.4f,2.2f,.8f,(j==1?.85f:0)*Mathf.Sin(t*Mathf.PI));
                    sparks.Set(i*2+cross,row,j,p+Vector3.back*(row*.22f)+(cross==0?Vector3.right:Vector3.up)*(j-1)*.07f,tint);
                }
            }
            sparks.Upload();
        }
        private void OnDestroy(){envelope?.Dispose();streams?.Dispose();sparks?.Dispose();}
    }
}
