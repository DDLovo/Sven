using UnityEngine;
namespace SvenMod
{
    // A blade-shaped spatial wake rotates once per source turn, rather than a floor decal.
    internal sealed class SvenSpinVisual:MonoBehaviour
    {
        internal const float Lifetime=SvenBalance.SpinTurnDuration+.22f;
        private const int Steps=120,Across=9;
        private SvenSpatialMesh[] bands;
        private SvenSpatialMesh[] trails;
        private bool strength;
        internal void Initialize(bool empowered)
        {
            strength=empowered;
            bands=new[]{new SvenSpatialMesh(transform,"Spin plasma wake","beam_plasma_06",1,Steps,Across),new SvenSpatialMesh(transform,"Spin hot blade",strength?"beam_hotwhite":"beam_hotblue",1,Steps,Across),new SvenSpatialMesh(transform,"Spin inner pressure","ribbon_electric2",1,Steps,Across)};
            trails=new SvenSpatialMesh[4];for(int i=0;i<4;i++)trails[i]=new SvenSpatialMesh(transform,"Spin inward volume trails "+i,"electrical_arc2_seq"+i,24,8,3);
            Sample(0);
        }
        internal void Sample(float time)
        {
            if(bands==null)return;bool active=time>=0&&time<Lifetime;
            foreach(var band in bands)band.gameObject.SetActive(active);foreach(var trail in trails)trail.gameObject.SetActive(active);if(!active)return;
            float reach=SvenBalance.MomentumRadius;
            for(int layer=0;layer<bands.Length;layer++)
            {
                float width=reach*(layer==0?.085f:layer==1?.035f:.05f),height=reach*(layer==0?.095f:layer==1?.065f:.075f);
                float radius=reach*(layer==2?.72f:1)-width;
                for(int i=0;i<=Steps;i++)
                {
                    float u=i/(float)Steps,age=time-u*SvenBalance.SpinTurnDuration;
                    float alpha=age<0?0:Mathf.Clamp01(age/.015f)*Mathf.Clamp01(1-age/.22f);
                    float angle=(SvenBalance.SpinStartDegrees+u*360)*Mathf.Deg2Rad;
                    var radial=new Vector3(Mathf.Sin(angle),0,Mathf.Cos(angle));
                    var center=radial*radius+Vector3.up*(layer==2?Mathf.Sin(angle*2)*.65f:0);
                    Color color=strength?Color.Lerp(new Color(2.7f,.1f,.025f),new Color(3f,1.4f,.2f),.5f+.5f*Mathf.Sin(angle*3)):(layer==1?new Color(1.5f,2f,2.6f):new Color(.2f,.7f,2.2f));
                    color.a=alpha*(layer==1?.85f:layer==0?.28f:.2f);
                    for(int j=0;j<Across;j++)
                    {
                        float phase=j/(float)(Across-1)*Mathf.PI*2;
                        bands[layer].Set(0,i,j,center+radial*(Mathf.Cos(phase)*width)+Vector3.up*(Mathf.Sin(phase)*height),color);
                    }
                }
                bands[layer].Upload();
            }
            for(int sheet=0;sheet<4;sheet++)
            {
                for(int strip=0;strip<12;strip++)
                {
                    int id=strip*4+sheet;float u=(id+.5f)/48f,age=time-u*SvenBalance.SpinTurnDuration;
                    float alpha=age<0?0:Mathf.Clamp01(age/.015f)*Mathf.Clamp01(1-age/.22f);
                    float angle=(SvenBalance.SpinStartDegrees+u*360)*Mathf.Deg2Rad;
                    var radial=new Vector3(Mathf.Sin(angle),0,Mathf.Cos(angle));var side=new Vector3(radial.z,0,-radial.x);
                    float noise=.5f+.5f*Mathf.Sin(id*17.13f);var start=radial*(reach*Mathf.Lerp(.2f,.6f,noise))+Vector3.up*(Mathf.Sin(id*7.3f)*reach*.075f);
                    var end=radial*(reach*.96f);float width=reach*(.018f+.02f*noise);
                    for(int row=0;row<=8;row++)
                    {
                        float t=row/8f;var center=Vector3.Lerp(start,end,t)+Vector3.up*(Mathf.Sin(t*11+id)*reach*.035f*Mathf.Sin(t*Mathf.PI));
                        float taper=Mathf.Max(0,Mathf.Sin(t*Mathf.PI));
                        for(int cross=0;cross<2;cross++)for(int col=0;col<3;col++)
                        {
                            Color tint=strength?Color.Lerp(new Color(2.5f,.15f,.025f),new Color(2.7f,1.35f,.12f),noise):new Color(.18f,.8f,2.2f);
                            tint.a=alpha*(col==1?1:0)*taper*.45f;
                            trails[sheet].Set(strip*2+cross,row,col,center+(cross==0?side:Vector3.up)*(col-1)*width,tint);
                        }
                    }
                }
                trails[sheet].Upload();
            }
        }
        private void OnDestroy(){if(bands!=null)foreach(var band in bands)band.Dispose();if(trails!=null)foreach(var trail in trails)trail.Dispose();}
    }
}
