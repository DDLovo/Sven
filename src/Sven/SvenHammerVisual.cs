using RoR2.Projectile;
using UnityEngine;
using SvenMod.Modules;
namespace SvenMod
{
    public sealed class SvenHammerVisual:MonoBehaviour
    {
        private const float VisualScale=2.1f;
        private SvenHammerWake wake;
        private Transform model;private Quaternion modelRotation;private float age;private bool initialized,released;
        private SvenVfxLayer core,halo;private SvenVfxLayer[] sourceArcs;private LineRenderer[] arcs;private TrailRenderer[] trails;private Light glow;
        private void Start(){Initialize();}
        private void Initialize()
        {
            if(initialized)return;initialized=true;
            var prefab=SvenAssets.Load<GameObject>("SvenHammerModel");
            if(prefab){model=Instantiate(prefab,transform,false).transform;model.localScale=Vector3.one*(1.33f*VisualScale);model.localPosition=new Vector3(.010902f,.018556f,.195220f)*(1.33f*VisualScale);foreach(var r in model.GetComponentsInChildren<Renderer>()){var mats=r.sharedMaterials;for(int i=0;i<mats.Length;i++)mats[i]=mats[i].ConvertDefaultShaderToHopoo();r.sharedMaterials=mats;}}
            modelRotation=model?model.localRotation:Quaternion.identity;
            trails=new TrailRenderer[3];
            for(int i=0;i<trails.Length;i++)
            {
                var tail=new GameObject("Storm Bolt source tail "+i);tail.transform.SetParent(transform,false);tail.transform.localPosition=new Vector3((i-1)*.11f,0,-.25f)*VisualScale;
                trails[i]=SvenEffects.AddTrail(tail,i==0?new Color(1.5f,1.15f,.3f):SvenEffects.Ivory,(i==0?.55f:.14f)*VisualScale,i==0?.32f:.28f,i==0?"beam_generic_2":"ribbon_electric2");
            }
            core=SvenVfx.Layer(transform,"Hammer ivory core","particle_glow_05",new Color(1.5f,1.45f,1.1f,.42f),SvenVfx.Quad(true),true);
            halo=SvenVfx.Layer(transform,"Hammer soft corona","particle_glow_05",new Color(1.6f,1.25f,.3f,.38f),SvenVfx.Quad(true),true);
            halo.go.transform.localPosition=Vector3.back*1.5f;
            wake=gameObject.AddComponent<SvenHammerWake>();wake.Initialize();
            sourceArcs=new SvenVfxLayer[4];
            for(int i=0;i<4;i++)sourceArcs[i]=SvenVfx.Layer(transform,"Source electrical flash "+i,"electrical_arc_white_seq"+i,new Color(2.94f,2.89f,2.2f,.5f),SvenVfx.Quad(true),true);
            arcs=new LineRenderer[4];for(int i=0;i<arcs.Length;i++)arcs[i]=SvenVfx.Line(transform,"Hammer electrical cage "+i,"ribbon_electric2",new Color(1,.92f,.62f,.7f),.035f*VisualScale,11);
            glow=gameObject.AddComponent<Light>();glow.color=SvenEffects.Ivory;glow.range=3*VisualScale;glow.intensity=.8f;
            var ghost=GetComponent<ProjectileGhostController>();if(ghost)ghost.ParentProjectileDestroyedEvent.AddListener(Release);
            Sample(0);
        }
        public void Sample(float seconds)
        {
            if(!initialized){Initialize();}if(model)model.localRotation=modelRotation*Quaternion.AngleAxis(seconds*540,Vector3.forward);
            wake.Sample(seconds);
            core.go.transform.localScale=Vector3.one*(.52f+.025f*Mathf.Sin(seconds*29))*VisualScale;halo.go.transform.localScale=Vector3.one*(2f*VisualScale);
            // Original glow system randomly chooses four static texture sequences for 10-15ms flashes.
            int pulse=Mathf.FloorToInt(seconds/.0125f);
            for(int i=0;i<sourceArcs.Length;i++)
            {
                sourceArcs[i].go.SetActive(i==pulse%4||i==(pulse+1)%4);
                float a=i*2.399963f+pulse*.7f;
                sourceArcs[i].go.transform.localPosition=new Vector3(Mathf.Cos(a)*.32f,Mathf.Sin(a)*.32f,-.2f)*VisualScale;
                sourceArcs[i].go.transform.localScale=Vector3.one*(.7f+.07f*(pulse%3))*VisualScale;
                sourceArcs[i].go.GetComponent<SvenVfxBillboard>().roll=a*Mathf.Rad2Deg;
            }
            for(int i=0;i<arcs.Length;i++)
            {
                float a=i*Mathf.PI*.5f+seconds*4;var side=new Vector3(Mathf.Cos(a),Mathf.Sin(a),0);
                for(int p=0;p<arcs[i].positionCount;p++)
                {
                    float u=p/(float)(arcs[i].positionCount-1),noise=Mathf.Sin(p*17.3f+i*9+Mathf.Floor(seconds*24)*3.7f)*.055f;
                    arcs[i].SetPosition(p,(Vector3.Lerp(Vector3.back*1.6f,Vector3.forward*.65f,u)+side*(.52f+noise*2)*Mathf.Sin(u*Mathf.PI))*VisualScale);
                }
            }
        }
        private void Update(){age+=Time.deltaTime;Sample(age);}
        private void Release()
        {
            if(released)return;released=true;
            // Match Drow's completed projectile behavior: the world-space wake survives impact briefly.
            if(trails!=null)foreach(var trail in trails)if(trail){if(!Application.isPlaying){SvenVfx.Dispose(trail.gameObject);continue;}trail.transform.SetParent(null,true);trail.emitting=false;Destroy(trail.gameObject,trail.time+.05f);}
        }
        private void OnDestroy(){Release();if(core!=null)SvenVfx.Dispose(core.mesh);if(halo!=null)SvenVfx.Dispose(halo.mesh);if(sourceArcs!=null)foreach(var sprite in sourceArcs)SvenVfx.Dispose(sprite.mesh);}
    }
}
