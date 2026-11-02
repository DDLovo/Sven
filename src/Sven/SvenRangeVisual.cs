using System.Collections.Generic;
using UnityEngine;
namespace SvenMod
{
    // Exact horizontal hit footprint: fixed outer edge, two side edges and a faint interior.
    // The source plasma travels across this footprint separately, so its expansion cannot
    // imply a smaller damage radius at the instant of impact.
    public sealed class SvenRangeVisual:MonoBehaviour
    {
        private readonly List<LineRenderer> edges=new List<LineRenderer>();
        private readonly List<SvenVfxLayer> fills=new List<SvenVfxLayer>();
        private Color color;private float lifetime;
        public void Initialize(float radius,float degrees,float rearRadius,Color tint,float y,float duration)
        {
            color=tint;lifetime=duration;
            Sector(radius,degrees,0,y);
            if(rearRadius>0)Sector(rearRadius,180,180,y);
            Sample(0);
        }
        private void Sector(float radius,float degrees,float yaw,float y)
        {
            var rotation=Quaternion.Euler(0,yaw,0);
            const int count=97;
            var edge=SvenVfx.Line(transform,"Hit range outer boundary","beam_hotwhite",color,.08f+radius*.012f,count);
            edge.widthCurve=AnimationCurve.Constant(0,1,1);edges.Add(edge);
            for(int i=0;i<count;i++)
            {
                float a=(i/(float)(count-1)-.5f)*degrees*Mathf.Deg2Rad;
                edge.SetPosition(i,rotation*new Vector3(Mathf.Sin(a)*radius,y,Mathf.Cos(a)*radius));
            }
            if(degrees<359)
            {
                foreach(int sign in new[]{-1,1})
                {
                    var side=SvenVfx.Line(transform,"Hit range side boundary","beam_hotwhite",color,.055f+radius*.007f,2);
                    side.widthCurve=AnimationCurve.Constant(0,1,1);side.SetPosition(0,Vector3.up*y);
                    float a=sign*degrees*.5f*Mathf.Deg2Rad;
                    side.SetPosition(1,rotation*new Vector3(Mathf.Sin(a)*radius,y,Mathf.Cos(a)*radius));edges.Add(side);
                }
            }
            var layer=SvenVfx.Layer(transform,"Hit range interior","particle_glow_05",color,SvenVfx.Arc(degrees,0));
            layer.go.transform.localPosition=Vector3.up*y;layer.go.transform.localRotation=rotation;layer.go.transform.localScale=Vector3.one*radius;
            fills.Add(layer);
        }
        public void Sample(float seconds)
        {
            float t=Mathf.Clamp01(seconds/lifetime);
            // Hold full size and a crisp perimeter through contact, then let it dissolve.
            float fade=1-Mathf.SmoothStep(0,1,Mathf.Clamp01((t-.25f)/.75f));
            var edgeColor=color;edgeColor.a=.85f*fade;
            foreach(var edge in edges){edge.enabled=t<1;edge.startColor=edge.endColor=edgeColor;}
            var fillColor=color;fillColor.a=.16f*fade;
            foreach(var fill in fills){fill.go.SetActive(t<1);fill.Tint(fillColor);}
        }
        private void OnDestroy(){foreach(var fill in fills)SvenVfx.Dispose(fill.mesh);}
    }
}
