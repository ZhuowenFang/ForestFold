using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ForestFold
{
    public static class RoadCurves
    {
        public static Vector3[] Arm(int direction)
        {
            Vector3 end=ForestGame.HexWorld(RoadMap.Directions[direction])*.5f;
            Vector3 bend=Vector3.Cross(end.normalized,Vector3.up)*.25f;
            return Enumerable.Range(0,13).Select(i=>end*(i/12f)+bend*Mathf.Sin(i/12f*Mathf.PI)).ToArray();
        }
        public static Vector3[] Bend(int from,int to)
        {
            Vector3 a=ForestGame.HexWorld(RoadMap.Directions[from])*.5f,b=ForestGame.HexWorld(RoadMap.Directions[to])*.5f;
            return Enumerable.Range(0,21).Select(i=> { float t=i/20f; return (1-t)*(1-t)*a+t*t*b; }).ToArray();
        }
        public static IEnumerable<Vector3[]> Paths(int ports)
        {
            var edges=Enumerable.Range(0,6).Where(d=>RoadMap.Has(ports,d)).ToArray();
            if(edges.Length==2) return new [] {Bend(edges[0],edges[1])};
            return edges.Select(Arm);
        }
        public static Vector3[] Traverse(int ports,int from,int to)
        {
            if(to>=0 && Enumerable.Range(0,6).Count(d=>RoadMap.Has(ports,d))==2) return Bend(from,to);
            var incoming=Arm(from).Reverse();
            return to<0?incoming.ToArray():incoming.Concat(Arm(to).Skip(1)).ToArray();
        }
    }
    public partial class ForestGame
    {
        void DrawRoadMesh(Vector3 center,Vector3[] points)
        {
            var verts=new List<Vector3>(); var triangles=new List<int>();
            for(int i=0;i<points.Length;i++) {
                var delta=points[Mathf.Min(i+1,points.Length-1)]-points[Mathf.Max(0,i-1)];
                var side=Vector3.Cross(delta.normalized,Vector3.up)*.40f;
                verts.Add(points[i]-side+Vector3.up*.13f); verts.Add(points[i]+side+Vector3.up*.13f);
                if(i>0) { int n=i*2; triangles.AddRange(new [] {n-2,n-1,n,n-1,n+1,n}); }
            }
            var mesh=new Mesh(); mesh.SetVertices(verts); mesh.SetTriangles(triangles,0); mesh.RecalculateNormals();
            var obj=new GameObject("Curved path"); obj.transform.SetParent(terrain); obj.transform.position=center;
            obj.AddComponent<MeshFilter>().sharedMesh=mesh; obj.AddComponent<MeshRenderer>().sharedMaterial=Mat(new Color(.83f,.77f,.55f)); obj.AddComponent<OwnedHexMesh>().mesh=mesh;
            for(int i=2;i<points.Length;i+=4) Primitive("Curve stepping stone",PrimitiveType.Cylinder,center+points[i]+Vector3.up*.15f,new Vector3(.28f,.01f,.22f),Cream,terrain);
        }
        List<Vector3> BuildCurveRoute(RoadEntrance entrance)
        {
            var cells=RoadMap.PathToCore(tiles,entrance.cell); var route=new List<Vector3>();
            for(int i=0;i<cells.Count;i++) {
                var tile=tiles.Find(t=>t.cell==cells[i]);
                int from=i==0?entrance.direction:System.Array.IndexOf(RoadMap.Directions,cells[i-1]-cells[i]);
                int to=i==cells.Count-1?-1:System.Array.IndexOf(RoadMap.Directions,cells[i+1]-cells[i]);
                foreach(var p in RoadCurves.Traverse(tile.ports,from,to)) {
                    var point=Position(tile.cell)+p+Vector3.up*.45f;
                    if(route.Count==0 || Vector3.Distance(route[route.Count-1],point)>.001f) route.Add(point);
                }
            }
            return route;
        }
        float DistanceRemaining(Enemy enemy)
        {
            float length=Vector3.Distance(enemy.obj.transform.position,enemy.path[enemy.segment]);
            for(int i=enemy.segment+1;i<enemy.path.Count;i++) length+=Vector3.Distance(enemy.path[i-1],enemy.path[i]);
            return length;
        }
        bool AdvanceEnemy(Enemy enemy,float distance)
        {
            while(enemy.segment<enemy.path.Count) {
                var target=enemy.path[enemy.segment]; float length=Vector3.Distance(enemy.obj.transform.position,target);
                if(distance<length) { enemy.obj.transform.position=Vector3.MoveTowards(enemy.obj.transform.position,target,distance); return false; }
                enemy.obj.transform.position=target; distance-=length; enemy.segment++;
            }
            return true;
        }
    }
}
