using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace SvenMod
{
    // Real weapon sweep: sample the original animated sword anchors, not a static muzzle line.
    [DefaultExecutionOrder(100)]
    public sealed class SvenSwordTrail:MonoBehaviour
    {
        private struct BladeSample{internal Vector3 start,end;internal float time;}
        private readonly List<BladeSample> samples=new List<BladeSample>();
        private Transform wrist,tip;private Vector3 tipOffset;private Mesh mesh;private GameObject visual;private MeshRenderer renderer;
        private float from,until;private Color color;private bool initialized;
        private void Initialize(bool strength,bool immortal)
        {
            if(!initialized)
            {
                var bones=GetComponentsInChildren<Transform>(true);wrist=bones.FirstOrDefault(t=>t.name=="wrist_L");
                tip=bones.FirstOrDefault(t=>t.name==(immortal?"sven_ti7_blade_tip":"sword_1"));
                tipOffset=immortal?new Vector3(-.447167f,.167242f,-.000034f):new Vector3(1.662024f,-.218142f,-.243191f);
                if(!wrist||!tip)return;
                visual=new GameObject("Sven source sword ribbon");visual.layer=gameObject.layer;visual.transform.SetParent(transform,false);mesh=new Mesh{name="Sven sampled blade"};mesh.MarkDynamic();visual.AddComponent<MeshFilter>().sharedMesh=mesh;
                renderer=visual.AddComponent<MeshRenderer>();renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=false;initialized=true;
            }
            // The source attack blur uses a clean generic beam; the noisy plasma texture belongs
            // to the forward cleave wave and makes a full blade sweep look like hanging cloth.
            renderer.sharedMaterial=SvenEffects.Material(immortal?"beam_hotblue2":"particle_beam_generic");
            color=strength?new Color(1,.15f,.035f):immortal?new Color(.25f,.65f,1):new Color(.85f,.8f,.64f);visual.SetActive(true);
        }
        public void Schedule(float delay,float duration,bool strength,bool immortal)
        {ScheduleAt(Time.time,delay,duration,strength,immortal);}
        internal void ScheduleAt(float now,float delay,float duration,bool strength,bool immortal)
        {Initialize(strength,immortal);samples.Clear();from=now+Mathf.Max(0,delay);until=from+Mathf.Max(.05f,duration);}
        internal void StopCapture(){until=float.NegativeInfinity;}
        public void Begin(bool strength,bool immortal)
        {Initialize(strength,immortal);if(Time.time>until)from=Time.time;until=Mathf.Max(until,Time.time+.12f);}
        private void LateUpdate()
        {
            if(!initialized)return;
            Sample(Time.time,Time.time>=from&&Time.time<=until);
        }
        // Editor validation feeds the actual source animation poses through this same sampler.
        public void Sample(float now,bool emit)
        {
            if(!initialized||!wrist||!tip)return;
            if(emit)
            {
                var next=new BladeSample{start=wrist.position,end=tip.TransformPoint(tipOffset),time=now};
                if(samples.Count>0&&(next.start-samples[samples.Count-1].start).sqrMagnitude>25)samples.Clear();
                if(samples.Count>0)
                {
                    var previous=samples[samples.Count-1];float distance=Vector3.Distance(previous.end,next.end);
                    int subdivisions=Mathf.Min(6,Mathf.FloorToInt(distance/.18f));
                    for(int j=1;j<=subdivisions;j++){float t=j/(float)(subdivisions+1);samples.Add(new BladeSample{start=Vector3.Lerp(previous.start,next.start,t),end=Vector3.Lerp(previous.end,next.end,t),time=Mathf.Lerp(previous.time,now,t)});}
                }
                samples.Add(next);
            }
            samples.RemoveAll(s=>now-s.time>.18f);while(samples.Count>32)samples.RemoveAt(0);
            if(samples.Count<2){mesh.Clear();return;}
            const int columns=5;
            var vertices=new Vector3[samples.Count*columns];var uv=new Vector2[vertices.Length];var colors=new Color[vertices.Length];var indices=new int[(samples.Count-1)*(columns-1)*6];
            for(int i=0;i<samples.Count;i++)
            {
                var s=samples[i];float fade=Mathf.Pow(Mathf.Clamp01(1-(now-s.time)/.15f),1.7f);
                for(int column=0;column<columns;column++)
                {
                    int j=i*columns+column;float across=column/(float)(columns-1);
                    vertices[j]=visual.transform.InverseTransformPoint(Vector3.Lerp(s.start,s.end,.38f+.62f*across));
                    uv[j]=new Vector2(i/(float)(samples.Count-1),across);
                    var c=color;c.a=fade*Mathf.Clamp01(Mathf.Sin(across*Mathf.PI))*.65f;colors[j]=c;
                    if(i<samples.Count-1&&column<columns-1)
                    {int k=(i*(columns-1)+column)*6;indices[k]=j;indices[k+1]=j+columns;indices[k+2]=j+1;indices[k+3]=j+1;indices[k+4]=j+columns;indices[k+5]=j+columns+1;}
                }
            }
            mesh.Clear();mesh.vertices=vertices;mesh.uv=uv;mesh.colors=colors;mesh.triangles=indices;mesh.RecalculateBounds();
        }
        private void OnDisable(){StopCapture();samples.Clear();if(mesh)mesh.Clear();if(visual)visual.SetActive(false);}
        private void OnDestroy(){SvenVfx.Dispose(visual);SvenVfx.Dispose(mesh);}
    }
}

