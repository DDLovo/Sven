using System.Collections.Generic;
using R2API.Networking;
using R2API.Networking.Interfaces;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;
namespace SvenMod
{
    internal enum SvenEffect:byte { Slash,StrengthSlash,Momentum,HammerImpact,Warcry,StrengthCast,Bkb,Blink }
    internal static class SvenEffects
    {
        private static readonly Dictionary<string,Material> materials=new Dictionary<string,Material>();
        internal static readonly Color Ivory=new Color(1,.87f,.48f),Red=new Color(1,.12f,.025f),Blue=new Color(.18f,.55f,1);
        internal static void RegisterNetwork(){NetworkingAPI.RegisterMessageType<SvenEffectMessage>();NetworkingAPI.RegisterMessageType<SvenMomentumMessage>();}
        internal static void Broadcast(SvenEffect kind,CharacterBody body,Vector3 origin,Vector3 direction,float scale)
        {
            Local(kind,body,origin,direction,scale);
            if(NetworkServer.active)new SvenEffectMessage(kind,body?body.netId:NetworkInstanceId.Invalid,origin,direction,scale).Send(NetworkDestination.Clients);
        }
        internal static Material Material(string texture)
        {
            if(materials.TryGetValue(texture,out var m)&&m)return m;
            bool sourceAtlas=texture.StartsWith("SvenSource",System.StringComparison.Ordinal);
            var shader=SvenAssets.Load<Shader>("SvenAdditive")??Shader.Find("Sven/SourceAdditive")??Shader.Find("Legacy Shaders/Particles/Additive");
            if(!shader)throw new System.InvalidOperationException("Sven additive shader missing from bundle");
            // A missing texture must not become an opaque white square.
            var t=SvenAssets.Load<Texture2D>(texture);
            if(!t&&sourceAtlas)throw new System.InvalidOperationException("Sven source cast atlas missing from bundle: "+texture);
            if(!t)t=SvenAssets.Load<Texture2D>("particle_glow_05");
            m=new Material(shader){name="Sven VFX / "+texture,mainTexture=t};m.SetColor("_TintColor",Color.white);m.SetFloat("_SourceMonochrome",sourceAtlas?0:1);
            // Source rope textures run vertically; Unity Line/TrailRenderer UVs run horizontally.
            m.SetFloat("_SoftSprite",texture.StartsWith("electrical_arc_white_",System.StringComparison.Ordinal)?1:0);
            m.SetFloat("_SourceBeam",texture.StartsWith("beam_")||texture.StartsWith("ribbon_")||texture=="particle_beam_generic"?1:0);materials[texture]=m;return m;
        }
        internal static TrailRenderer AddTrail(GameObject go,Color color,float width,float time,string texture)
        {
            var t=go.AddComponent<TrailRenderer>();t.sharedMaterial=Material(texture);t.time=time;t.widthMultiplier=width;t.minVertexDistance=.035f;
            t.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;t.receiveShadows=false;t.numCornerVertices=2;
            t.widthCurve=AnimationCurve.Linear(0,1,1,0);
            t.colorGradient=new Gradient{colorKeys=new[]{new GradientColorKey(color,0),new GradientColorKey(color,1)},alphaKeys=new[]{new GradientAlphaKey(.65f,0),new GradientAlphaKey(0,1)}};return t;
        }
        internal static void Local(SvenEffect kind,CharacterBody body,Vector3 origin,Vector3 direction,float scale)
        {
            if(kind==SvenEffect.Blink&&body)
            {
                body.GetComponent<SvenController>()?.speedMemory.Clear();
                if(Util.HasEffectiveAuthority(body.gameObject))TeleportHelper.TeleportGameObject(body.gameObject,origin);
            }
            if(Application.isBatchMode)return;
            if(kind==SvenEffect.Blink&&SvenBlinkEffects.Play(body,direction,origin))return;
            if(body&&(kind==SvenEffect.Slash||kind==SvenEffect.StrengthSlash||kind==SvenEffect.Momentum))
                foreach(var animator in SvenController.Visuals(body))
                {
                    var trail=animator.GetComponent<SvenSwordTrail>()??animator.gameObject.AddComponent<SvenSwordTrail>();
                    trail.Begin(kind==SvenEffect.StrengthSlash,body.skinIndex==1);
                }
            var go=new GameObject("Sven "+kind);go.transform.position=origin;
            bool swing=kind==SvenEffect.Slash||kind==SvenEffect.StrengthSlash;
            go.transform.rotation=swing?SvenSweepGeometry.Rotation(direction,body?body.transform.forward:Vector3.forward):Quaternion.identity;
            var effect=go.AddComponent<SvenTransientEffect>();effect.kind=kind;effect.immortal=body&&body.skinIndex==1;effect.scale=scale;
            if(kind==SvenEffect.Momentum&&body){effect.follow=body;effect.spinStrength=body.HasBuff(SvenBuffs.Strength);go.transform.rotation=SvenSweepGeometry.Rotation(Vector3.ProjectOnPlane(direction,Vector3.up),body.transform.forward);}
            if(body&&kind!=SvenEffect.HammerImpact)effect.groundOffset=body.footPosition.y-origin.y+.04f;
            if(kind==SvenEffect.HammerImpact)
            {
                effect.groundOffset=.06f;
                if(Physics.Raycast(origin+Vector3.up*.25f,Vector3.down,out var ground,3.25f,LayerIndex.world.mask,QueryTriggerInteraction.Ignore))effect.groundOffset=ground.point.y-origin.y+.06f;
            }
            if(kind==SvenEffect.HammerImpact)SvenAudio.Play("sven_storm_bolt_impact",origin,.8f);
        }
    }

    // Shared primitives for transient casts and bone-followed buffs. All geometry is owned by its instance;
    // shared materials are immutable, while tint, fade and UV scrolling use property blocks.
    internal sealed class SvenVfxLayer
    {
        internal GameObject go;internal MeshRenderer renderer;internal Mesh mesh;internal Color color;
        internal float delay,life,from,to,spin,repeat=1;internal Vector3 position,velocity,aspect=Vector3.one;internal bool billboard,sourceAtlas;
        internal float atlasHalfSize=4,clipDegrees=360,clipRadius=1;
        private readonly MaterialPropertyBlock properties=new MaterialPropertyBlock();
        internal void Tint(Color tint,float scroll=0)
        {properties.SetColor("_TintColor",tint);properties.SetVector("_MainTex_ST",new Vector4(repeat,1,scroll,0));renderer.SetPropertyBlock(properties);}
        internal void Sample(float time)
        {
            float age=time-delay,t=age/life;bool active=age>=0&&t<1;go.SetActive(active);if(!active)return;
            if(sourceAtlas)
            {
                // VRF already rendered expansion, color and all child-layer lifetimes into these frames.
                // Applying the procedural layer fade/scroll/scale here would distort the source motion.
                go.transform.localPosition=position;go.transform.localRotation=Quaternion.identity;go.transform.localScale=Vector3.one*atlasHalfSize;
                properties.SetColor("_TintColor",color);properties.SetVector("_MainTex_ST",SvenTransientEffect.SourceAtlasUV(age));
                properties.SetFloat("_ClipSector",1);properties.SetFloat("_SectorCos",clipDegrees>=359?-1:Mathf.Cos(clipDegrees*.5f*Mathf.Deg2Rad));properties.SetFloat("_ClipRadius",clipRadius);renderer.SetPropertyBlock(properties);return;
            }
            go.transform.localPosition=position+velocity*age;
            float r=Mathf.Lerp(from,to,1-Mathf.Pow(1-t,2));go.transform.localScale=aspect*r;
            if(!billboard)go.transform.localRotation=Quaternion.Euler(0,spin*age,0);
            var c=color;c.a*=Mathf.Min(1,age/.025f)*Mathf.Pow(1-t,1.35f);Tint(c,time*.3f);
        }
    }
    internal static class SvenVfx
    {
        internal static void Dispose(Object obj){if(!obj)return;if(Application.isPlaying)Object.Destroy(obj);else Object.DestroyImmediate(obj);}
        internal static SvenVfxLayer Layer(Transform parent,string name,string texture,Color color,Mesh mesh,bool billboard=false)
        {
            var go=new GameObject(name);go.layer=parent.gameObject.layer;go.transform.SetParent(parent,false);
            go.AddComponent<MeshFilter>().sharedMesh=mesh;var renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterial=SvenEffects.Material(texture);
            renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=false;
            if(billboard)go.AddComponent<SvenVfxBillboard>();
            var layer=new SvenVfxLayer{go=go,renderer=renderer,mesh=mesh,color=color,billboard=billboard};layer.Tint(color);return layer;
        }
        internal static Mesh Quad(bool vertical=false)
        {
            var m=new Mesh{name="Sven source sprite"};m.vertices=vertical?new[]{new Vector3(-1,-1,0),new Vector3(-1,1,0),new Vector3(1,1,0),new Vector3(1,-1,0)}:new[]{new Vector3(-1,0,-1),new Vector3(-1,0,1),new Vector3(1,0,1),new Vector3(1,0,-1)};
            m.uv=new[]{new Vector2(0,0),new Vector2(0,1),new Vector2(1,1),new Vector2(1,0)};m.triangles=new[]{0,1,2,0,2,3};m.colors=new[]{Color.white,Color.white,Color.white,Color.white};m.RecalculateBounds();return m;
        }
        internal static Mesh Arc(float degrees,float inner)
        {
            const int steps=96;var v=new Vector3[(steps+1)*2];var uv=new Vector2[v.Length];var colors=new Color[v.Length];var indices=new int[steps*6];
            for(int i=0;i<=steps;i++)
            {
                float u=i/(float)steps,a=(u-.5f)*degrees*Mathf.Deg2Rad;var p=new Vector3(Mathf.Sin(a),0,Mathf.Cos(a));
                v[2*i]=p*inner;v[2*i+1]=p;uv[2*i]=new Vector2(u,0);uv[2*i+1]=new Vector2(u,1);
                float edge=degrees<359?Mathf.Pow(Mathf.Clamp01(Mathf.Sin(u*Mathf.PI)),.65f):1;colors[2*i]=colors[2*i+1]=new Color(1,1,1,edge);
                if(i<steps){int k=6*i,j=2*i;indices[k]=j;indices[k+1]=j+2;indices[k+2]=j+1;indices[k+3]=j+1;indices[k+4]=j+2;indices[k+5]=j+3;}
            }
            var m=new Mesh{name="Sven source rope wave"};m.vertices=v;m.uv=uv;m.colors=colors;m.triangles=indices;m.RecalculateBounds();return m;
        }
        internal static LineRenderer Line(Transform parent,string name,string texture,Color color,float width,int points)
        {
            var go=new GameObject(name);go.layer=parent.gameObject.layer;go.transform.SetParent(parent,false);var l=go.AddComponent<LineRenderer>();
            l.sharedMaterial=SvenEffects.Material(texture);l.useWorldSpace=false;l.positionCount=points;l.widthMultiplier=width;l.numCornerVertices=2;
            l.widthCurve=new AnimationCurve(new Keyframe(0,.15f),new Keyframe(.15f,1),new Keyframe(.7f,.65f),new Keyframe(1,0));
            l.startColor=l.endColor=color;l.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;l.receiveShadows=false;return l;
        }
        internal static void Bolt(LineRenderer line,Vector3 start,Vector3 end,float time,int seed,float noise)
        {
            int count=line.positionCount;var axis=end-start;var side=Vector3.Cross(axis.normalized,Vector3.up);if(side.sqrMagnitude<.01f)side=Vector3.right;
            var other=Vector3.Cross(axis.normalized,side).normalized;int frame=Mathf.FloorToInt(time*18);
            for(int i=0;i<count;i++)
            {
                float u=i/(float)(count-1),fade=Mathf.Sin(u*Mathf.PI);
                float a=Mathf.Sin(i*33.41f+seed*17.31f+frame*3.77f),b=Mathf.Sin(i*19.13f+seed*7.71f+frame*4.23f);
                line.SetPosition(i,Vector3.Lerp(start,end,u)+(side*a+other*b)*noise*fade);
            }
        }
    }
    [ExecuteAlways]
    public sealed class SvenVfxBillboard:MonoBehaviour
    {
        internal float roll;
        private void OnWillRenderObject(){if(Camera.current)transform.rotation=Camera.current.transform.rotation*Quaternion.Euler(0,0,roll);}
    }
    [DefaultExecutionOrder(200)]
    public sealed class SvenTransientEffect:MonoBehaviour
    {
        internal SvenEffect kind;internal bool immortal;internal float scale=1,groundOffset=-1;
        private readonly List<SvenVfxLayer> layers=new List<SvenVfxLayer>();
        private readonly List<LineRenderer> bolts=new List<LineRenderer>();
        internal CharacterBody follow;internal bool spinStrength;
        private Light flash;private SvenCleaveVisual cleaveVisual;private SvenSpinVisual spinVisual;private SvenHammerBurst hammerBurst;private float age,duration;private bool initialized,preview;
        private void Start(){Initialize();}
        private SvenVfxLayer Add(string name,string texture,Color color,float life,float from,float to,float y=0,float delay=0,float arc=0,float inner=.85f,bool billboard=false)
        {
            var layer=SvenVfx.Layer(transform,name,texture,color,arc>0?SvenVfx.Arc(arc,inner):SvenVfx.Quad(billboard),billboard);
            layer.life=life;layer.delay=delay;layer.from=from;layer.to=to;layer.spin=0;layer.position=new Vector3(0,y,0);layers.Add(layer);duration=Mathf.Max(duration,delay+life);return layer;
        }
        private void AddSourceCast(string texture,string name)
        {
            // Validate before allocating the mesh or child GameObject; failed imports must be visible.
            SvenEffects.Material(texture);
            var layer=Add(name,texture,Color.white,.8f,4,4,groundOffset);layer.sourceAtlas=true;
        }
        public static int SourceAtlasFrame(float seconds)=>Mathf.Clamp(Mathf.FloorToInt(seconds*30),0,23);
        public static Vector4 SourceAtlasUV(float seconds)
        {
            int frame=SourceAtlasFrame(seconds);
            return new Vector4(1f/6,1f/4,(frame%6)/6f,(3-frame/6)/4f);
        }
        private void Initialize()
        {
            if(initialized)return;initialized=true;float size=kind==SvenEffect.Bkb||kind==SvenEffect.Blink?Mathf.Clamp(Mathf.Abs(scale),.25f,2):1;transform.localScale=new Vector3(size,1,size);
            var red=SvenEffects.Red;var gold=SvenEffects.Ivory;var blue=SvenEffects.Blue;
            switch(kind)
            {
                case SvenEffect.Slash:case SvenEffect.StrengthSlash:
                    bool empowered=kind==SvenEffect.StrengthSlash;
                    cleaveVisual=gameObject.AddComponent<SvenCleaveVisual>();
                    cleaveVisual.Initialize(false,empowered,immortal,scale);duration=SvenCleaveVisual.Lifetime;
                    break;
                case SvenEffect.Momentum:
                    spinVisual=gameObject.AddComponent<SvenSpinVisual>();spinVisual.Initialize(spinStrength);duration=SvenSpinVisual.Lifetime;
                    break;
                case SvenEffect.HammerImpact:
                    hammerBurst=gameObject.AddComponent<SvenHammerBurst>();hammerBurst.Initialize();duration=SvenHammerBurst.Lifetime;
                    break;
                case SvenEffect.Warcry:
                    AddSourceCast("SvenSourceWarcryCast","Warcry original-resource cast atlas");
                    Add("Warcry chest flare","aircraft_blue",new Color(1,.75f,.35f,.08f),.45f,.25f,1.2f,.15f,0,0,.8f,true);
                    if(immortal)for(int i=0;i<6;i++)bolts.Add(SvenVfx.Line(transform,"TI5 Warcry lightning "+i,"ribbon_electric2",gold,.045f,9));
                    break;
                case SvenEffect.StrengthCast:
                    AddSourceCast("SvenSourceStrengthCast","Strength original-resource cast atlas");
                    Add("Strength body flare","aircraft_blue",new Color(1,.13f,.035f,.12f),.6f,.25f,1.2f,.1f,0,0,.8f,true).aspect=new Vector3(.85f,1.25f,1);break;
                case SvenEffect.Bkb:
                    Add("Avatar rising flare","yellowflare2",new Color(1,.8f,.24f,.32f),.55f,.2f,2.2f,0,0,0,.8f,true);
                    Sparks(new Color(1,.88f,.35f,.7f),18,.8f,1.4f,groundOffset,true);break;
                case SvenEffect.Blink:
                    Add("Blink void flare","particle_glow_irregular1",new Color(.4f,.25f,1,.6f),.35f,.7f,2.3f,0,0,0,.8f,true);
                    Sparks(new Color(.5f,.45f,1,.8f),12,.45f,3,0,true);break;
            }
            flash=gameObject.AddComponent<Light>();flash.color=kind==SvenEffect.StrengthCast?red:gold;flash.range=kind==SvenEffect.HammerImpact?8:5;flash.shadows=LightShadows.None;Sample(0);
        }
        private void Sparks(Color color,int count,float life,float speed,float y,bool rising)
        {
            for(int i=0;i<count;i++)
            {
                float a=i*2.399963f;var layer=Add("Source spark "+i,"spark_glow_01",color,life*(.65f+.35f*(i%5)/4),.035f+.014f*(i%3),.01f,y,.02f*(i%4),0,.8f,true);
                layer.velocity=new Vector3(Mathf.Cos(a),rising?1.4f+(i%3)*.4f:.1f+(i%4)*.12f,Mathf.Sin(a))*speed*(.5f+.5f*(i%7)/6);
            }
        }
        public void Preview(int effect,bool isImmortal,float effectScale,float seconds)
        {kind=(SvenEffect)effect;immortal=isImmortal;scale=effectScale;preview=true;Initialize();Sample(seconds);}
        public void Sample(float seconds)
        {
            if(!initialized){Initialize();return;}foreach(var layer in layers)layer.Sample(seconds);if(cleaveVisual)cleaveVisual.Sample(seconds);if(hammerBurst)hammerBurst.Sample(seconds);if(spinVisual)spinVisual.Sample(seconds);
            for(int i=0;i<bolts.Count;i++)
            {
                var line=bolts[i];float life=kind==SvenEffect.Warcry?.5f:.35f;line.enabled=seconds<life;
                float a=i*2.399963f,r=kind==SvenEffect.Warcry?3.8f:SvenBalance.HammerRadius*.82f;
                var end=new Vector3(Mathf.Cos(a)*r,.15f+(i%3)*.5f,Mathf.Sin(a)*r);
                var start=i<7?Vector3.zero:bolts[i-7].GetPosition(4);if(i>=7)end=start+end*.45f;
                SvenVfx.Bolt(line,start,end,seconds,i,.3f);var c=SvenEffects.Ivory;c.a=.7f*Mathf.Clamp01(1-seconds/life);line.startColor=line.endColor=c;
            }
            if(flash)flash.intensity=1.8f*Mathf.Clamp01(1-seconds/.16f);
        }
        private void LateUpdate(){if(preview)return;if(follow)transform.position=follow.corePosition;age+=Time.deltaTime;Sample(age);if(age>duration)Destroy(gameObject);}
        private void OnDestroy(){foreach(var layer in layers)SvenVfx.Dispose(layer.mesh);}
        internal static Mesh ArcMesh(float degrees,float inner)=>SvenVfx.Arc(degrees,inner);
    }
    public sealed class SvenEffectMessage:INetMessage
    {
        private SvenEffect kind;private NetworkInstanceId owner;private Vector3 origin,direction;private float scale;
        public SvenEffectMessage(){}
        internal SvenEffectMessage(SvenEffect k,NetworkInstanceId o,Vector3 p,Vector3 d,float s){kind=k;owner=o;origin=p;direction=d;scale=s;}
        public void Serialize(NetworkWriter w){w.Write((byte)kind);w.Write(owner);w.Write(origin);w.Write(direction);w.Write(scale);}
        public void Deserialize(NetworkReader r){kind=(SvenEffect)r.ReadByte();owner=r.ReadNetworkId();origin=r.ReadVector3();direction=r.ReadVector3();scale=r.ReadSingle();}
        public void OnReceived(){if(NetworkServer.active)return;var go=ClientScene.FindLocalObject(owner);SvenEffects.Local(kind,go?go.GetComponent<CharacterBody>():null,origin,direction,scale);}
    }
}
