using UnityEngine;
namespace SvenMod
{
    // Reusable strip buffers: no per-frame lists or mesh allocations, and no camera-dependent vertices.
    internal sealed class SvenSpatialMesh
    {
        internal readonly Mesh mesh;
        internal readonly GameObject gameObject;
        internal readonly Vector3[] vertices;
        internal readonly Vector2[] uv;
        internal readonly Color[] colors;
        private readonly int columns,rows;
        internal SvenSpatialMesh(Transform parent,string name,string texture,int strips,int steps,int across)
        {
            columns=across;rows=steps+1;
            vertices=new Vector3[strips*rows*columns];uv=new Vector2[vertices.Length];colors=new Color[vertices.Length];
            var indices=new int[strips*steps*(columns-1)*6];int n=0;
            for(int s=0;s<strips;s++)for(int i=0;i<rows;i++)for(int j=0;j<columns;j++)
            {
                int k=(s*rows+i)*columns+j;uv[k]=new Vector2(i/(float)steps,j/(float)(columns-1));
                if(i==steps||j==columns-1)continue;
                indices[n++]=k;indices[n++]=k+columns;indices[n++]=k+1;
                indices[n++]=k+1;indices[n++]=k+columns;indices[n++]=k+columns+1;
            }
            mesh=new Mesh{name=name};mesh.MarkDynamic();mesh.vertices=vertices;mesh.uv=uv;mesh.colors=colors;mesh.triangles=indices;
            gameObject=SvenVfx.Layer(parent,name,texture,Color.white,mesh).go;
        }
        internal void Set(int strip,int row,int column,Vector3 point,Color color)
        {int index=(strip*rows+row)*columns+column;vertices[index]=point;colors[index]=color;}
        internal void Upload(){mesh.vertices=vertices;mesh.colors=colors;mesh.RecalculateBounds();}
        internal void Dispose()=>SvenVfx.Dispose(mesh);
    }
}
