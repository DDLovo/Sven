using UnityEngine;
namespace SvenMod
{
    // Reconstruct the source hierarchy as spatial geometry: rising rope wave, radial
    // lightning and ballistic trails. A faint spherical shock joins these layers for
    // RoR2's free camera and spherical damage volume; it is not a top-view bake.
    internal sealed class SvenHammerBurst:MonoBehaviour
    {
        internal const float Lifetime=.8f;
        private SvenSpatialMesh shell,wave,trails,arcs;
        private Material shellMaterial;private Vector3[] shellNormals;
        private SvenVfxLayer core;
        private readonly SvenVfxLayer[] cloud=new SvenVfxLayer[12];
        private const int Longitude=64,Latitude=24,WaveSteps=96,ArcCount=14,ArcSteps=12,SparkCount=32;
        internal void Initialize()
        {
            shell=new SvenSpatialMesh(transform,"Hammer spherical pressure front","particle_glow_irregular1",1,Latitude,Longitude+1);
            var shader=SvenAssets.Load<Shader>("SvenStormShell")??Shader.Find("Sven/StormShell");
            if(!shader)throw new System.InvalidOperationException("Sven spherical shock shader missing");
            shellNormals=new Vector3[shell.vertices.Length];
            shellMaterial=new Material(shader){mainTexture=SvenAssets.Load<Texture2D>("particle_glow_irregular1")};
            shell.gameObject.GetComponent<MeshRenderer>().sharedMaterial=shellMaterial;
            wave=new SvenSpatialMesh(transform,"Hammer rising plasma coils","beam_plasma_04",1,WaveSteps,7);
            arcs=new SvenSpatialMesh(transform,"Hammer radial lightning volume","ribbon_electric2",ArcCount*2,ArcSteps,3);
            trails=new SvenSpatialMesh(transform,"Hammer ballistic source sparks","sparks_seq0",SparkCount*2,5,3);
            core=SvenVfx.Layer(transform,"Hammer source flash","particle_glow_05",new Color(2.5f,2.2f,.9f,.8f),SvenVfx.Quad(true),true);
            core.life=.12f;core.from=.4f;core.to=SvenBalance.HammerRadius*.32f;
            for(int i=0;i<cloud.Length;i++)
            {
                var p=SvenVfx.Layer(transform,"Hammer source electrical cloud "+i,"electrical_arc_white_seq"+(i%4),new Color(2.1f,1.95f,1.05f,.72f),SvenVfx.Quad(true),true);
                p.life=.235f+.151f*((i*7)%11)/10f;p.from=SvenBalance.HammerRadius*.07f;p.to=SvenBalance.HammerRadius*(.24f+.06f*(i%3));
                p.position=SphereDirection(i,12)*SvenBalance.HammerRadius*.08f+Vector3.up*SvenBalance.HammerRadius*.06f;
                p.velocity=SphereDirection(i,12)*SvenBalance.HammerRadius*1.25f;
                p.go.GetComponent<SvenVfxBillboard>().roll=i*131.7f;cloud[i]=p;
            }
            Sample(0);
        }
        internal static Vector3 SphereDirection(int index,int count)
        {
            float y=1-2*(index+.5f)/count,angle=index*2.39996323f;
            float r=Mathf.Sqrt(Mathf.Max(0,1-y*y));return new Vector3(Mathf.Cos(angle)*r,y,Mathf.Sin(angle)*r);
        }
        internal void Sample(float time)
        {
            if(shell==null)return;
            float radius=SvenBalance.HammerRadius;
            bool visible=time>=0&&time<Lifetime;
            shell.gameObject.SetActive(visible&&time<.34f);wave.gameObject.SetActive(visible&&time<.35f);
            arcs.gameObject.SetActive(visible&&time<.35f);trails.gameObject.SetActive(visible);
            core.Sample(time);
            foreach(var p in cloud)
            {
                p.Sample(time);
                // Billboard corners must also stay inside the spherical gameplay extent.
                float remaining=Mathf.Max(0,radius-p.go.transform.localPosition.magnitude);
                float size=Mathf.Min(p.go.transform.localScale.x,remaining/Mathf.Sqrt(2));p.go.transform.localScale=Vector3.one*size;
            }
            if(!visible)return;
            float shock=radius*Mathf.Lerp(.08f,1,Mathf.Clamp01(time/.18f));
            float fade=Mathf.Min(1,time/.018f)*Mathf.Pow(Mathf.Clamp01(1-time/.34f),1.3f);
            if(time<.34f)
            {
            for(int i=0;i<=Latitude;i++)for(int j=0;j<=Longitude;j++)
            {
                float lat=i/(float)Latitude*Mathf.PI,lon=j/(float)Longitude*Mathf.PI*2;
                var dir=new Vector3(Mathf.Sin(lat)*Mathf.Cos(lon),Mathf.Cos(lat),Mathf.Sin(lat)*Mathf.Sin(lon));
                shellNormals[i*(Longitude+1)+j]=dir;
                shell.Set(0,i,j,dir*shock,new Color(1.7f,1.45f,.62f,fade*.075f));
            }
            shell.Upload();shell.mesh.normals=shellNormals;
            }
            // Source ring particles rise (+1000..1200 z) as they spread. Two irregular
            // coils carry the original plasma texture through 3D rather than a flat disc.
            float expansion=radius*Mathf.Lerp(.1f,.94f,Mathf.Clamp01(time/.2f));
            float waveFade=Mathf.Min(1,time/.015f)*Mathf.Clamp01(1-time/.35f);
            if(time<.35f)
            {
            for(int i=0;i<=WaveSteps;i++)
            {
                float u=i/(float)WaveSteps,angle=u*Mathf.PI*4;
                float h=(u-.5f)*radius*.9f+radius*.15f*Mathf.Sin(angle*2+time*17);
                float planar=Mathf.Sqrt(Mathf.Max(0,radius*radius-h*h));
                float r=Mathf.Min(planar,expansion*(.8f+.13f*Mathf.Sin(angle*3+.2f)));
                var p=new Vector3(Mathf.Cos(angle)*r,h*Mathf.Clamp01(time/.12f),Mathf.Sin(angle)*r);
                var radial=new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle));
                for(int j=0;j<7;j++)
                {
                    float cross=j/6f*Mathf.PI*2,width=radius*.10f*waveFade;
                    var c=new Color(3.1f,2.8f,1.65f,waveFade*Mathf.Clamp01(Mathf.Sin(u*Mathf.PI))*.85f);
                    wave.Set(0,i,j,Vector3.ClampMagnitude(p+radial*Mathf.Cos(cross)*width+Vector3.up*Mathf.Sin(cross)*width*1.8f,radius),c);
                }
            }
            wave.Upload();
            for(int id=0;id<ArcCount;id++)
            {
                var dir=SphereDirection(id,ArcCount);var right=Vector3.Cross(dir,Vector3.up).normalized;var up=Vector3.Cross(dir,right).normalized;
                float reach=radius*Mathf.Lerp(.25f,.98f,Mathf.Clamp01(time/.11f));
                for(int i=0;i<=ArcSteps;i++)
                {
                    float t=i/(float)ArcSteps,w=Mathf.Clamp01(Mathf.Sin(t*Mathf.PI));
                    float tick=Mathf.Floor(time*30);
                    var p=dir*(reach*t)+(right*Mathf.Sin(id*7+i*19+tick*5)+up*Mathf.Sin(id*13+i*11+tick*3))*radius*.045f*w;
                    for(int cross=0;cross<2;cross++)for(int j=0;j<3;j++)
                    {
                        var c=new Color(2.7f,2.45f,1.35f,waveFade*w*(j==1?.8f:0));
                        arcs.Set(id*2+cross,i,j,Vector3.ClampMagnitude(p+(cross==0?right:up)*(j-1)*radius*.014f,radius),c);
                    }
                }
            }
            arcs.Upload();
            }
            for(int id=0;id<SparkCount;id++)
            {
                float life=.4f+.4f*((id*7)%31)/30f,fadeSpark=Mathf.Clamp01(1-time/life);
                var dir=SphereDirection(id,SparkCount);var right=Vector3.Cross(dir,Vector3.up).normalized;var up=Vector3.Cross(dir,right).normalized;
                for(int i=0;i<=5;i++)
                {
                    float u=i/5f,age=Mathf.Max(0,time-(1-u)*.075f);
                    var p=dir*(radius*(.1f+.85f*(1-Mathf.Exp(-age*6))))-Vector3.up*(age*age*radius*.35f);
                    for(int cross=0;cross<2;cross++)for(int j=0;j<3;j++)
                    {
                        var c=new Color(2.6f,1.75f,.65f,fadeSpark*(j==1?1:0)*u);
                        trails.Set(id*2+cross,i,j,Vector3.ClampMagnitude(p+(cross==0?right:up)*(j-1)*radius*.009f,radius),c);
                    }
                }
            }
            trails.Upload();
        }
        private void OnDestroy(){shell?.Dispose();wave?.Dispose();arcs?.Dispose();trails?.Dispose();if(core!=null)SvenVfx.Dispose(core.mesh);foreach(var p in cloud)if(p!=null)SvenVfx.Dispose(p.mesh);SvenVfx.Dispose(shellMaterial);}
    }
}
