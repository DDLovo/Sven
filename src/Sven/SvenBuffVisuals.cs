using System.Collections.Generic;
using System.Linq;
using RoR2;
using UnityEngine;

namespace SvenMod
{
    // Owned by each visual model, not by CharacterBody (ModelLocator detaches its model at runtime).
    // Tick supplies gameplay state; this component only draws and never changes buffs or damage.
    public sealed class SvenBuffVisuals:MonoBehaviour
    {
        private Transform head,left,right,elbowLeft,elbowRight,sword,flightTorso,flightPelvis;
        private GameObject strengthRoot,armorRoot,avatarRoot,weaponRoot,flightRoot;
        private readonly List<SvenVfxLayer> sprites=new List<SvenVfxLayer>();
        private readonly List<Material> ownedMaterials=new List<Material>();
        private LineRenderer[] armEnergy,weaponEnergy,armorEdges,avatarArcs,flightLines;
        private SvenFlightVisual flightVisual;
        private SvenVfxLayer[] embers,eyeGlows,avatarGlints;
        private Renderer[] shell;private Transform shield;
        private bool initialized,strength,bkb,warcry,shard,flying,immortal,preview;
        private CharacterBody owner;private float clock;
        private MaterialPropertyBlock properties;
        private Transform Find(params string[] names)=>GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>names.Contains(t.name));
        private GameObject Group(string name){var go=new GameObject(name);go.layer=gameObject.layer;go.transform.SetParent(transform,false);return go;}
        private SvenVfxLayer Sprite(Transform parent,string name,string texture,Color tint)
        {var layer=SvenVfx.Layer(parent,name,texture,tint,SvenVfx.Quad(true),true);sprites.Add(layer);return layer;}
        private LineRenderer[] Lines(Transform parent,string name,string texture,int count,float width,Color tint)
        {
            var result=new LineRenderer[count];for(int i=0;i<count;i++){result[i]=SvenVfx.Line(parent,name+" "+i,texture,tint,width,17);result[i].useWorldSpace=true;}return result;
        }
        private void Initialize(bool isImmortal)
        {
            if(initialized)return;properties=new MaterialPropertyBlock();initialized=true;immortal=isImmortal;
            head=Find("head");left=Find("wrist_L");right=Find("wrist_R");elbowLeft=Find("elbow_L","lowerarm_L");elbowRight=Find("elbow_R","lowerarm_R");
            flightTorso=Find("spine2")??Find("spine3");flightPelvis=Find("root");
            sword=Find(immortal?"sven_ti7_blade_tip":"sword_1");
            // Capture source geometry before adding a shell, so overlays never recursively copy themselves.
            var sourceMeshes=GetComponentsInChildren<SkinnedMeshRenderer>(true);
            strengthRoot=Group("God's Strength source ambient");weaponRoot=Group("Sven source weapon energy");armorRoot=Group("Warcry armor protection");avatarRoot=Group("Black King Bar avatar");flightRoot=Group("Storm Hammer flight current");
            armEnergy=Lines(strengthRoot.transform,"Strength arm current","ribbon_electric2",4,.052f,SvenEffects.Red);
            weaponEnergy=Lines(weaponRoot.transform,"Blade plasma","ribbon_electric2",immortal?3:2,.055f,SvenEffects.Red);
            eyeGlows=new[]{Sprite(strengthRoot.transform,"Left eye glow","particle_glow_05",SvenEffects.Red),Sprite(strengthRoot.transform,"Right eye glow","particle_glow_05",SvenEffects.Red)};
            embers=new SvenVfxLayer[12];for(int i=0;i<embers.Length;i++)embers[i]=Sprite(strengthRoot.transform,"Strength rising ember "+i,i%3==0?"flame_omni_seq0_18":"spark_glow_01",SvenEffects.Red);
            armorEdges=Lines(armorRoot.transform,"Warcry shield contour","beam_hotwhite",3,.038f,SvenEffects.Ivory);
            avatarArcs=Lines(avatarRoot.transform,"Avatar ascending energy","beam_hotwhite",3,.034f,SvenEffects.Ivory);
            avatarGlints=new SvenVfxLayer[8];for(int i=0;i<avatarGlints.Length;i++)avatarGlints[i]=Sprite(avatarRoot.transform,"Avatar gold mote "+i,"particle_glow_05",SvenEffects.Ivory);
            flightLines=Lines(flightRoot.transform,"Flight lightning","ribbon_electric2",3,.05f,SvenEffects.Ivory);
            flightVisual=new SvenFlightVisual(flightRoot.transform);
            if(immortal)
            {
                var prefab=SvenAssets.Load<GameObject>("SvenImmortalShieldModel");
                if(prefab)
                {
                    shield=Instantiate(prefab,armorRoot.transform,false).transform;shield.name="Source TI5 armor shield";
                    foreach(var renderer in shield.GetComponentsInChildren<Renderer>())
                    {
                        var previous=renderer.sharedMaterials;var replacements=new Material[previous.Length];
                        for(int i=0;i<previous.Length;i++){var material=new Material(SvenEffects.Material("particle_glow_05"));if(previous[i]&&previous[i].mainTexture)material.mainTexture=previous[i].mainTexture;replacements[i]=material;ownedMaterials.Add(material);}
                        renderer.sharedMaterials=replacements;renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
                        properties.Clear();properties.SetColor("_TintColor",new Color(1,.85f,.4f,.35f));renderer.SetPropertyBlock(properties);
                    }
                }
            }
            var rimShader=SvenAssets.Load<Shader>("SvenRim")??Shader.Find("Sven/SourceRim");
            var overlays=new List<Renderer>();
            if(rimShader)
            {
                var rimMaterial=new Material(rimShader){name="Sven avatar edge shell"};ownedMaterials.Add(rimMaterial);
                foreach(var source in sourceMeshes)
                {
                    if(!source.sharedMesh)continue;
                    var go=new GameObject("Avatar rim / "+source.name);go.layer=gameObject.layer;go.transform.SetParent(source.transform,false);
                    var renderer=go.AddComponent<SkinnedMeshRenderer>();renderer.sharedMesh=source.sharedMesh;renderer.bones=source.bones;renderer.rootBone=source.rootBone;renderer.localBounds=source.localBounds;
                    renderer.sharedMaterials=Enumerable.Repeat(rimMaterial,source.sharedMesh.subMeshCount).ToArray();renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=false;renderer.enabled=false;overlays.Add(renderer);
                }
            }
            shell=overlays.ToArray();ApplyVisibility();
        }
        public void Tick(CharacterBody body,bool strengthActive,bool bkbActive,bool warcryActive,bool shardActive,bool flightActive)
        {
            owner=body;Initialize(body&&body.skinIndex==1);
            bool alive=body&&(!body.healthComponent||body.healthComponent.alive);
            strength=alive&&strengthActive;bkb=alive&&bkbActive;warcry=alive&&warcryActive;shard=alive&&shardActive;flying=alive&&flightActive;
            ApplyVisibility(alive);
        }
        private void ApplyVisibility(bool alive=true)
        {
            if(!initialized)return;
            strengthRoot.SetActive(alive&&strength);weaponRoot.SetActive(alive&&(strength||immortal));armorRoot.SetActive(alive&&(warcry||shard));avatarRoot.SetActive(alive&&bkb);
            // The replicated flight buff includes cast windup. Only the travel state draws a wake.
            var flight=owner?EntityStateMachine.FindByCustomName(owner.gameObject,"Body")?.state as SvenFlight:null;
            flightRoot.SetActive(alive&&flying&&(preview||(flight!=null&&flight.travelActive)));
            foreach(var renderer in shell)if(renderer)renderer.enabled=alive&&bkb;
        }
        public void Preview(bool strengthActive,bool bkbActive,bool warcryActive,bool shardActive,bool flightActive,bool isImmortal,float seconds)
        {
            preview=true;Initialize(isImmortal);strength=strengthActive;bkb=bkbActive;warcry=warcryActive;shard=shardActive;flying=flightActive;ApplyVisibility();Sample(seconds);
        }
        private void LateUpdate(){if(preview||!initialized)return;clock+=Time.deltaTime;Sample(clock);}
        private Vector3 Point(Transform bone,Vector3 fallback)=>bone?bone.position:transform.TransformPoint(fallback);
        public void Sample(float time)
        {
            if(!initialized)return;var up=transform.up;float modelScale=transform.lossyScale.x;
            var headPosition=Point(head,new Vector3(0,2.2f,0));var chest=headPosition-up*.55f*modelScale;
            var hand=Point(left,new Vector3(-.7f,1.1f,0));var otherHand=Point(right,new Vector3(.7f,1.1f,0));
            var tip=sword?sword.TransformPoint(immortal?new Vector3(-.447167f,.167242f,-.000034f):new Vector3(1.662024f,-.218142f,-.243191f)):hand+up*1.7f;
            if(strength||immortal)
            {
                var tint=strength?SvenEffects.Red:SvenEffects.Blue;
                for(int i=0;i<weaponEnergy.Length;i++)
                {
                    var line=weaponEnergy[i];float phase=time*7+i*Mathf.PI*2/weaponEnergy.Length;var axis=(tip-hand).normalized;var side=Vector3.Cross(axis,transform.forward).normalized;
                    for(int p=0;p<line.positionCount;p++)
                    {float t=p/(float)(line.positionCount-1),a=phase+t*10;line.SetPosition(p,Vector3.Lerp(hand,tip,t)+(side*Mathf.Sin(a)+transform.forward*Mathf.Cos(a))*(immortal?.09f:.055f)*modelScale*Mathf.Sin(t*Mathf.PI));}
                    tint.a=(strength?.65f:.35f)*(.8f+.2f*Mathf.Sin(time*17+i));line.startColor=line.endColor=tint;line.widthMultiplier=(strength?.075f:.04f)*modelScale;
                }
            }
            if(strength)
            {
                for(int i=0;i<armEnergy.Length;i++)
                {
                    var a=i%2==0?(elbowLeft?elbowLeft.position:chest):(elbowRight?elbowRight.position:chest);var b=i%2==0?hand:otherHand;
                    SvenVfx.Bolt(armEnergy[i],a,b,time,i,.06f*modelScale);var c=i<2?SvenEffects.Red:new Color(1,.45f,.16f);c.a=i<2?.45f:.22f;armEnergy[i].startColor=armEnergy[i].endColor=c;
                }
                for(int i=0;i<embers.Length;i++)
                {
                    float t=Mathf.Repeat(time*.9f+i*.173f,1),a=i*2.399963f+time*.4f;
                    var origin=i%3==0?Vector3.Lerp(hand,tip,.3f+(i%5)*.12f):i%3==1?hand:otherHand;
                    embers[i].go.transform.position=origin+(transform.right*Mathf.Cos(a)+transform.forward*Mathf.Sin(a))*.13f*modelScale+up*t*.65f*modelScale;
                    embers[i].go.transform.localScale=Vector3.one*(i%3==0?.13f:.035f)*(1-t*.6f);
                    var c=new Color(1,.13f+.2f*(1-t),.025f,Mathf.Sin(t*Mathf.PI)*.4f);embers[i].Tint(c);
                }
                for(int i=0;i<2;i++){eyeGlows[i].go.transform.position=headPosition+transform.forward*.14f*modelScale+transform.right*(i==0?-.075f:.075f)*modelScale;eyeGlows[i].go.transform.localScale=Vector3.one*.085f;eyeGlows[i].Tint(new Color(1,.3f,.06f,.45f));}
            }
            if(warcry||shard)
            {
                float alpha=warcry?.65f:.23f;var center=chest+transform.forward*.36f*modelScale;
                for(int j=0;j<armorEdges.Length;j++)
                {
                    var line=armorEdges[j];for(int p=0;p<line.positionCount;p++)
                    {
                        float t=p/(float)(line.positionCount-1),a=t*Mathf.PI*2;
                        // A pointed heraldic shield, leaving the body readable through its open center.
                        float x=Mathf.Sin(a)*(.46f+j*.035f),y=Mathf.Cos(a)*.6f;
                        if(y<0)x*=.65f+.35f*(1+y/.6f);
                        line.SetPosition(p,center+transform.right*x*modelScale+up*y*modelScale+transform.forward*j*.025f*modelScale);
                    }
                    var c=SvenEffects.Ivory;c.a=alpha*(j==0?1:.3f)*(.85f+.15f*Mathf.Sin(time*3));line.startColor=line.endColor=c;
                }
                if(shield){shield.position=center+transform.forward*.06f*modelScale;shield.rotation=transform.rotation*Quaternion.Euler(90,0,0);shield.localScale=Vector3.one*.72f;}
            }
            if(bkb)
            {
                var feet=owner?owner.footPosition:transform.position;
                for(int i=0;i<avatarArcs.Length;i++)
                {
                    var line=avatarArcs[i];for(int p=0;p<line.positionCount;p++)
                    {float t=p/(float)(line.positionCount-1),a=time*1.7f+i*2.0944f+t*3;line.SetPosition(p,feet+up*(.15f+t*2.25f)*modelScale+(transform.right*Mathf.Cos(a)+transform.forward*Mathf.Sin(a))*(.43f+.13f*Mathf.Sin(t*Mathf.PI))*modelScale);}
                    line.startColor=line.endColor=new Color(1,.73f,.18f,.25f);
                }
                for(int i=0;i<avatarGlints.Length;i++)
                {
                    float t=Mathf.Repeat(time*.65f+i*.137f,1),a=i*2.4f+t;
                    avatarGlints[i].go.transform.position=feet+up*(.2f+t*2)*modelScale+(transform.right*Mathf.Cos(a)+transform.forward*Mathf.Sin(a))*.6f*modelScale;
                    avatarGlints[i].go.transform.localScale=Vector3.one*.07f;avatarGlints[i].Tint(new Color(1,.83f,.3f,Mathf.Sin(t*Mathf.PI)*.55f));
                }
                properties.Clear();properties.SetColor("_TintColor",new Color(1,.7f,.13f,1));properties.SetFloat("_Opacity",.48f+.05f*Mathf.Sin(time*3));properties.SetFloat("_RimPower",3.2f);
                foreach(var renderer in shell)renderer.SetPropertyBlock(properties);
            }
            if(flying)
            {
                var flight=owner?EntityStateMachine.FindByCustomName(owner.gameObject,"Body")?.state as SvenFlight:null;
                bool traveling=preview||(flight!=null&&flight.travelActive);
                flightRoot.SetActive(traveling);
                if(traveling)
                {
                    // State direction is serialized and remains reliable on remote clients where
                    // characterMotor.velocity can be zero. Never project it onto the ground.
                    var direction=flight!=null?flight.travelDirection:transform.forward;
                    if(owner&&owner.characterMotor&&owner.characterMotor.velocity.sqrMagnitude>25)
                        direction=owner.characterMotor.velocity.normalized;
                    if(direction.sqrMagnitude<.0001f)direction=transform.forward;
                    // Flight animates the body horizontally: an upright offset below the head
                    // misses the torso. Anchor only this wake to the animated source spine.
                    var flightCenter=flightTorso?flightTorso.position:flightPelvis?Vector3.Lerp(flightPelvis.position,headPosition,.55f):chest;
                    flightVisual.Sample(flightCenter,direction,modelScale,time);
                    var side=Vector3.Cross(direction,Mathf.Abs(direction.y)>.92f?Vector3.right:Vector3.up).normalized;
                    for(int i=0;i<flightLines.Length;i++)
                    {
                        var end=flightCenter-direction*(1.3f+i*.35f)*modelScale+side*(i-1)*.3f*modelScale;
                        SvenVfx.Bolt(flightLines[i],i==0?hand:i==1?otherHand:flightCenter,end,time,i,.1f*modelScale);
                        flightLines[i].widthMultiplier=.045f*modelScale;
                    }
                }
            }
        }
        private void OnDisable(){ApplyVisibility(false);clock=0;}
        private void OnDestroy()
        {
            foreach(var layer in sprites)SvenVfx.Dispose(layer.mesh);
            foreach(var material in ownedMaterials)SvenVfx.Dispose(material);
            flightVisual?.Dispose();
            if(shell!=null)foreach(var renderer in shell)if(renderer)SvenVfx.Dispose(renderer.gameObject);
            SvenVfx.Dispose(strengthRoot);SvenVfx.Dispose(weaponRoot);SvenVfx.Dispose(armorRoot);SvenVfx.Dispose(avatarRoot);SvenVfx.Dispose(flightRoot);
        }
    }
}
