using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ForestFold
{
    public partial class ForestGame
    {
        const float HexRadius=TileSize/1.7320508f;
        public static Vector3 HexWorld(Vector2Int p) => new Vector3(3.2f*(p.x+p.y*.5f),0,3.2f*.8660254f*p.y);
        public static Vector2Int HexCell(Vector3 p)
        {
            float r=p.z/(3.2f*.8660254f),q=p.x/3.2f-r*.5f,s=-q-r;
            int x=Mathf.RoundToInt(q),z=Mathf.RoundToInt(r),y=Mathf.RoundToInt(s);
            if(Mathf.Abs(x-q)>Mathf.Abs(z-r) && Mathf.Abs(x-q)>Mathf.Abs(y-s)) x=-y-z;
            else if(Mathf.Abs(z-r)>Mathf.Abs(y-s)) z=-x-y;
            return new Vector2Int(x,z);
        }
        Vector3 HexVertex(int i,float radius) => new Vector3(Mathf.Cos((30+i*60)*Mathf.Deg2Rad)*radius,0,Mathf.Sin((30+i*60)*Mathf.Deg2Rad)*radius);
        void HexPrism(string name,Vector3 center,float radius,float bottom,float top,Color color)
        {
            var vertices=new List<Vector3>(); var indices=new List<int>();
            for(int i=0;i<6;i++) {
                Vector3 a=HexVertex(i,radius),b=HexVertex((i+1)%6,radius);
                int n=vertices.Count;
                vertices.Add(Vector3.up*top); vertices.Add(b+Vector3.up*top); vertices.Add(a+Vector3.up*top);
                vertices.Add(a+Vector3.up*bottom); vertices.Add(a+Vector3.up*top); vertices.Add(b+Vector3.up*top);
                vertices.Add(a+Vector3.up*bottom); vertices.Add(b+Vector3.up*top); vertices.Add(b+Vector3.up*bottom);
                for(int j=0;j<9;j++) indices.Add(n+j);
            }
            var mesh=new Mesh(); mesh.SetVertices(vertices); mesh.SetTriangles(indices,0); mesh.RecalculateNormals();
            var obj=new GameObject(name); obj.transform.SetParent(terrain); obj.transform.position=center;
            obj.AddComponent<MeshFilter>().sharedMesh=mesh; obj.AddComponent<MeshRenderer>().sharedMaterial=Mat(color);
            obj.AddComponent<OwnedHexMesh>().mesh=mesh;
        }
        Texture2D hexIcon;
        void HexFill(Rect rect,Color color)
        {
            if(!hexIcon) {
                hexIcon=new Texture2D(128,128,TextureFormat.RGBA32,false);
                for(int y=0;y<128;y++) for(int x=0;x<128;x++) {
                    float u=Mathf.Abs((x+.5f)/128*2-1),v=Mathf.Abs((y+.5f)/128*2-1);
                    hexIcon.SetPixel(x,y,new Color(1,1,1,u<=1 && v<=1-u*.5f?1:0));
                }
                hexIcon.Apply();
            }
            var old=GUI.color; GUI.color=color; GUI.DrawTexture(rect,hexIcon); GUI.color=old;
        }
        void GuiLine(Vector2 a,Vector2 b,Color color,float thickness)
        {
            var old=GUI.matrix; Vector2 delta=b-a;
            GUIUtility.RotateAroundPivot(Mathf.Atan2(delta.y,delta.x)*Mathf.Rad2Deg,a);
            Box(new Rect(a.x,a.y-thickness*.5f,delta.magnitude,thickness),color); GUI.matrix=old;
        }
        void HexOutline(Rect rect,Color color,float thickness=2)
        {
            for(int i=0;i<6;i++) GuiLine(HexPoint(rect,i),HexPoint(rect,(i+1)%6),color,thickness);
        }
        Vector2 HexPoint(Rect rect,int i) { var p=HexVertex(i,1); return rect.center+new Vector2(p.x/.8660254f*rect.width*.5f,-p.z*rect.height*.5f); }
    }
    public class OwnedHexMesh:MonoBehaviour { public Mesh mesh; void OnDestroy() { if(mesh) Destroy(mesh); } }
}
