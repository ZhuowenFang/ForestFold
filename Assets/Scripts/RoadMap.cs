using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ForestFold
{
    // Ports are six axial hex edges. Neighbouring road edges must agree.
    public class RoadTile
    {
        public Vector2Int cell;
        public int ports;
        public Habitat habitat; public int level=1;
    }
    public struct RoadEntrance
    {
        public Vector2Int cell;
        public int direction;
    }
    public static class RoadMap
    {
        public static readonly Vector2Int[] Directions = { new Vector2Int(1,0), new Vector2Int(0,1), new Vector2Int(-1,1), new Vector2Int(-1,0), new Vector2Int(0,-1), new Vector2Int(1,-1) };
        public static readonly int[] Templates = { 9, 5, 21, 27, 3 };
        public static readonly string[] Names = { "林间长径", "林荫弯道", "三岔林地", "四岔花园", "急弯小径" };
        public static int[] Draft(List<int> available,System.Random random)
        {
            int[] weights={30,25,35,3,25}; var pool=new List<int>(available); var result=new List<int>();
            for(int i=0;i<3;i++) {
                if(pool.Count==0) pool.AddRange(available);
                int roll=random.Next(pool.Sum(k=>weights[k])),chosen=pool[0];
                foreach(int k in pool) { roll-=weights[k]; if(roll<0) { chosen=k; break; } }
                result.Add(chosen); pool.Remove(chosen);
            }
            if(!result.Any(k=>EcologyRules.Slots(Templates[k]).Length>0)) {
                if(available.Contains(2)) result[2]=2;
                else if(available.Contains(3)) result[2]=3;
            }
            return result.ToArray();
        }
        public static int Rotate(int mask, int steps)
        {
            for (int i=0;i<((steps%6)+6)%6;i++) mask=((mask<<1)&63)|((mask>>5)&1);
            return mask;
        }
        public static bool Has(int mask,int direction) => (mask & (1<<direction))!=0;
        public static List<RoadEntrance> Entrances(List<RoadTile> tiles)
        {
            var occupied=new HashSet<Vector2Int>(tiles.Select(t=>t.cell));
            var result=new List<RoadEntrance>();
            foreach (var tile in tiles)
                for (int d=0;d<6;d++)
                    if (Has(tile.ports,d) && !occupied.Contains(tile.cell+Directions[d]))
                        result.Add(new RoadEntrance {cell=tile.cell,direction=d});
            return result;
        }
        public static bool CanPlace(List<RoadTile> tiles,Vector2Int cell,int ports,out string reason)
        {
            if (tiles.Any(t=>t.cell==cell)) { reason="这里已有地块"; return false; }
            int connections=0;
            for (int d=0;d<6;d++) {
                var neighbor=tiles.Find(t=>t.cell==cell+Directions[d]);
                if (neighbor==null) continue;
                bool a=Has(ports,d), b=Has(neighbor.ports,(d+3)%6);
                if (a!=b) { reason="相邻道路接口不匹配，按 R 旋转"; return false; }
                if (a) connections++;
            }
            if (connections==0) { reason="至少连接一处已有道路"; return false; }
            // Keep an exposed entrance so later waves and picks cannot be soft-locked.
            int newExits=Enumerable.Range(0,6).Count(d=>Has(ports,d))-connections;
            if (Entrances(tiles).Count-connections+newExits==0) { reason="需要保留至少一个暗影入口"; return false; }
            var proposed=new List<RoadTile>(tiles) {new RoadTile {cell=cell,ports=ports}};
            if (!Entrances(proposed).Any(e=>e.cell!=Vector2Int.zero)) { reason="需要保留至少一个外围暗影入口"; return false; }
            reason="松开放置"; return true;
        }
        public static List<Vector2Int> PathToCore(List<RoadTile> tiles,Vector2Int start)
        {
            var lookup=tiles.ToDictionary(t=>t.cell);
            var parents=new Dictionary<Vector2Int,Vector2Int>();
            var seen=new HashSet<Vector2Int> { start };
            var queue=new Queue<Vector2Int>(); queue.Enqueue(start);
            while (queue.Count>0) {
                var p=queue.Dequeue();
                if (p==Vector2Int.zero) {
                    var result=new List<Vector2Int> { p };
                    while (p!=start) { p=parents[p]; result.Add(p); }
                    result.Reverse(); return result;
                }
                for (int d=0;d<6;d++) {
                    var next=p+Directions[d];
                    if (!Has(lookup[p].ports,d) || !lookup.TryGetValue(next,out var tile) || !Has(tile.ports,(d+3)%6) || !seen.Add(next)) continue;
                    parents[next]=p; queue.Enqueue(next);
                }
            }
            return new List<Vector2Int>();
        }
        public static IEnumerable<Vector2Int> Frontier(List<RoadTile> tiles) => Entrances(tiles).Select(e=>e.cell+Directions[e.direction]).Distinct();
        public static bool Validate(List<RoadTile> tiles,out string reason)
        {
            if(tiles.Select(t=>t.cell).Distinct().Count()!=tiles.Count) { reason="地块不能重叠"; return false; }
            foreach(var tile in tiles) {
                for(int d=0;d<6;d++) {
                    var neighbor=tiles.Find(t=>t.cell==tile.cell+Directions[d]);
                    if(neighbor!=null && Has(tile.ports,d)!=Has(neighbor.ports,(d+3)%6)) { reason="相邻道路接口不匹配，请移动或旋转地块"; return false; }
                }
                if(PathToCore(tiles,tile.cell).Count==0) { reason="有地块未连接森林核心，请继续调整"; return false; }
            }
            if(!Entrances(tiles).Any(e=>e.cell!=Vector2Int.zero)) { reason="请保留外围暗影入口"; return false; }
            reason="道路连通，可以开战"; return true;
        }
    }
}


