using UnityEngine;
namespace SvenMod
{
    // TI7: layered plasma/hot core/electric crest and inward radial trails. The source's
    // ground placement is replaced by the same pitched aim frame used by the melee query.
    // Each crest has a closed elliptical cross-section, not a texture baked onto a floor quad.
    internal sealed class SvenCleaveVisual:MonoBehaviour
    {
        internal const float SectorLifetime=.55f, SweepDelay=.15f, Lifetime=SectorLifetime+.25f;
        private const int Steps=80,Across=9,StreakSteps=7,StreaksPerSheet=12;
        private SvenSpatialMesh[] crests,streaks;
        private float radius,degrees,transverseScale;
        private Vector3 cutAxis,planeNormal;
        private bool empowered,heavy;
        private Material hotCoreMaterial;
        private SvenVfxLayer[] tips;
        internal void Initialize(bool heavyStrike,bool strength,bool immortal,float mirror)
        {
            heavy=heavyStrike;empowered=strength;radius=SvenBalance.SweepRadius(heavy);degrees=SvenBalance.SweepAngle(heavy);
            cutAxis=SvenSwingProfiles.Axis(heavy,immortal,strength,mirror);
            planeNormal=Vector3.Cross(Vector3.forward,cutAxis).normalized;
            // Preserve the cut plane, but avoid turning a 20m heavy into a 40m tall wall.
            // Combat uses this exact elliptical sector, with added normal thickness.
            transverseScale=SvenSweepGeometry.TransverseScale(heavy,cutAxis);
            crests=new[]{
                new SvenSpatialMesh(transform,"Cleave plasma volume",strength?"beam_plasma_05":"beam_plasma_06",1,Steps,Across),
                new SvenSpatialMesh(transform,"Cleave hot curved blade",strength?"beam_hotwhite":"beam_hotblue",1,Steps,Across),
                new SvenSpatialMesh(transform,"Cleave electric edge",strength?"beam_jagged_01":"ribbon_electric2",1,Steps,Across),
                new SvenSpatialMesh(transform,"Cleave swept residual","beam_plasma_06",1,Steps,Across)};
            if(strength)
            {
                // Retain a saturated red blade on RoR2's bright terrain. Only the hot core
                // partially covers the backdrop; plasma and electrical layers stay additive.
                var renderer=crests[1].gameObject.GetComponent<MeshRenderer>();hotCoreMaterial=new Material(renderer.sharedMaterial);
                hotCoreMaterial.SetFloat("_DstBlend",(float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);renderer.sharedMaterial=hotCoreMaterial;
                tips=new SvenVfxLayer[2];
                for(int i=0;i<2;i++)
                {
                    tips[i]=SvenVfx.Layer(transform,"Cleave golden blade tip "+i,"particle_glow_05",new Color(2.5f,1.7f,.18f,.7f),SvenVfx.Quad(true),true);
                    tips[i].life=SectorLifetime;tips[i].from=radius*.018f;tips[i].to=radius*.075f;
                }
            }
            streaks=new SvenSpatialMesh[4];
            for(int i=0;i<4;i++)streaks[i]=new SvenSpatialMesh(transform,"Cleave inward source trails "+i,"electrical_arc2_seq"+i,StreaksPerSheet*2,StreakSteps,3);
            if(heavy)
            {
                rear=gameObject.AddComponent<SvenRearCleaveVisual>();rear.Initialize();
            }
            Sample(0);
        }
        private SvenRearCleaveVisual rear;
        internal static float Travel(float seconds)=>Mathf.Clamp01((1-Mathf.Exp(-Mathf.Max(0,seconds)*1.35f*8.63046f))/(1-Mathf.Exp(-.35f*8.63046f)));
        // The baked axis points along blade motion. u=0 always emits first, including
        // top-to-bottom overhead cuts. Timing is independent of Animator/render FPS.
        internal static float SectorAge(float seconds,float u)=>seconds-SweepDelay*u;
        private float Age(float time,float u)=>SectorAge(time,u);
        private static float Opacity(float age)=>Mathf.Clamp01(age/.025f)*Mathf.Clamp01((SectorLifetime-age)/.28f);
        private static Color WarmTint(int layer,float u,float age)
        {
            float heat=.5f+.5f*Mathf.Sin(u*9-age*7);
            if(layer==1)return Color.Lerp(new Color(2.8f,.06f,.045f),new Color(3.1f,.48f,.035f),heat);
            if(layer==2)return Color.Lerp(new Color(2.6f,.75f,.12f),new Color(2.6f,2f,.72f),heat);
            if(layer==3)return Color.Lerp(new Color(1.25f,.025f,.12f),new Color(2f,.18f,.035f),heat);
            return Color.Lerp(new Color(2.2f,.12f,.04f),new Color(3f,.7f,.08f),heat);
        }
        private Vector3 OnArc(float angle,float r,float lift=0)
            =>cutAxis*(Mathf.Sin(angle)*r*transverseScale)+Vector3.forward*(Mathf.Cos(angle)*r)+planeNormal*lift;
        private static Color CoolTint(int layer,float u,float age)
        {
            float blend=.5f+.5f*Mathf.Sin(u*7-age*3);
            if(layer==1)return Color.Lerp(new Color(1.25f,2.15f,2.6f),new Color(1.7f,1.85f,2.6f),blend);
            return Color.Lerp(new Color(.18f,.95f,1.85f),new Color(.5f,.48f,2f),blend);
        }
        internal void Sample(float time)
        {
            if(crests==null)return;
            bool visible=time>=0&&time<Lifetime;
            foreach(var m in crests)m.gameObject.SetActive(visible);
            foreach(var m in streaks)m.gameObject.SetActive(visible);
            if(rear)rear.Sample(time);
            if(tips!=null)for(int i=0;i<2;i++)
            {
                float u=i==0?.03f:.97f,age=Age(time,u);
                tips[i].position=OnArc((u-.5f)*degrees*Mathf.Deg2Rad,radius*Mathf.Lerp(.12f,1,Travel(age)));
                tips[i].Sample(age);
            }
            if(!visible)return;
            for(int layer=0;layer<crests.Length;layer++)
            {
                float width=radius*(layer==0?.085f:layer==1?.021f:layer==2?.036f:.045f);
                float thickness=radius*(layer==0?.09f:layer==1?.035f:layer==2?.045f:.025f);
                if(empowered&&layer==1){width*=1.4f;thickness*=1.3f;}
                float opacity=layer==0?.3f:layer==1?.95f:layer==2?.45f:.18f;
                if(!heavy&&layer==2){width*=.8f;thickness*=.8f;opacity*=.8f;}
                for(int i=0;i<=Steps;i++)
                {
                    float u=i/(float)Steps,angle=(u-.5f)*degrees*Mathf.Deg2Rad;
                    float age=Age(time,u),fade=Opacity(age);
                    float rr=radius*Mathf.Lerp(.12f,1,Travel(age))*(layer==3?.86f:1);
                    var tint=empowered?WarmTint(layer,u,age):CoolTint(layer,u,age);
                    float taper=Mathf.Pow(Mathf.Max(0,Mathf.Sin(u*Mathf.PI)),.6f);
                    var center=OnArc(angle,rr,layer==3?-.25f:0);
                    var radial=(cutAxis*(Mathf.Sin(angle)*transverseScale)+Vector3.forward*Mathf.Cos(angle)).normalized;
                    float ripple=layer==2?Mathf.Sin(u*129+age*25)*.045f:0;
                    for(int j=0;j<Across;j++)
                    {
                        float phase=j/(float)(Across-1)*Mathf.PI*2;
                        var point=center+radial*(Mathf.Cos(phase)*width*taper+ripple)+planeNormal*(Mathf.Sin(phase)*thickness*taper);
                        var c=tint;c.a=fade*taper*opacity;
                        crests[layer].Set(0,i,j,point,c);
                    }
                }
                crests[layer].Upload();
            }
            for(int sheet=0;sheet<4;sheet++)
            {
                for(int s=0;s<StreaksPerSheet;s++)
                {
                    int id=s*4+sheet;float u=(id+.5f)/48,angle=(u-.5f)*degrees*Mathf.Deg2Rad;
                    float age=Age(time,u),fade=Opacity(age),r=radius*Mathf.Lerp(.12f,1,Travel(age));
                    float noise=.5f+.5f*Mathf.Sin(id*17.13f),inner=Mathf.Lerp(.18f,.68f,noise);
                    float vertical=Mathf.Sin(id*7.31f)*radius*.11f;
                    var end=OnArc(angle,r,vertical*.3f);var start=OnArc(angle+.04f*Mathf.Sin(id),r*inner,vertical);
                    var axis=(end-start).normalized;var side=Vector3.Cross(axis,planeNormal).normalized;
                    var up=Vector3.Cross(side,axis).normalized;
                    float width=radius*(.018f+.024f*noise);
                    if(!heavy)width*=.7f;
                    for(int i=0;i<=StreakSteps;i++)
                    {
                        float t=i/(float)StreakSteps;
                        var center=Vector3.Lerp(start,end,t)+up*(Mathf.Sin(t*13+id*3)*.055f*r*Mathf.Sin(t*Mathf.PI));
                        float taper=Mathf.Clamp01(Mathf.Sin(t*Mathf.PI))*(.3f+.7f*t);
                        for(int cross=0;cross<2;cross++)for(int j=0;j<3;j++)
                        {
                            var tint=empowered?WarmTint(id%3,u,age):CoolTint(2,u,age);
                            tint.a=fade*(j==1?1:0)*taper*.52f*(heavy?1:.65f);
                            streaks[sheet].Set(s*2+cross,i,j,center+(cross==0?side:up)*(j-1)*width,tint);
                        }
                    }
                }
                streaks[sheet].Upload();
            }
        }
        private void OnDestroy(){if(crests!=null)foreach(var m in crests)m.Dispose();if(streaks!=null)foreach(var m in streaks)m.Dispose();if(tips!=null)foreach(var p in tips)SvenVfx.Dispose(p.mesh);SvenVfx.Dispose(hotCoreMaterial);}
    }
    // The heavy strike's short rear lobe remains visibly distinct from its long forward cleave.
    internal sealed class SvenRearCleaveVisual:MonoBehaviour
    {
        private SvenSpatialMesh mesh;
        internal void Initialize(){mesh=new SvenSpatialMesh(transform,"Momentum short rear crescent","beam_hotblue",1,40,5);}
        internal void Sample(float t)
        {
            if(mesh==null)return;mesh.gameObject.SetActive(t>=0&&t<.45f);if(t<0||t>=.45f)return;
            float radius=SvenBalance.MomentumRearRadius*Mathf.Lerp(.2f,1,SvenCleaveVisual.Travel(t));
            for(int i=0;i<=40;i++)for(int j=0;j<5;j++)
            {
                float a=(90+i*180f/40)*Mathf.Deg2Rad,phase=j/4f*Mathf.PI*2;
                var p=new Vector3(Mathf.Sin(a)*(radius+.1f*Mathf.Cos(phase)),.15f*Mathf.Sin(phase),Mathf.Cos(a)*(radius+.1f*Mathf.Cos(phase)));
                mesh.Set(0,i,j,p,new Color(.4f,.8f,1.6f,Mathf.Min(1,t/.025f)*(1-t/.45f)*Mathf.Clamp01(Mathf.Sin(i/40f*Mathf.PI))));
            }
            mesh.Upload();
        }
        private void OnDestroy(){mesh?.Dispose();}
    }
}
