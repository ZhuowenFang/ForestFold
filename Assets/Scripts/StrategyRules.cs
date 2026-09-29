using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ForestFold
{
    public enum EnemyKind { Shadow, Runner, Shell, Swarm, Cleanser, Siege }
    public static class StrategyRules
    {
        public static readonly string[] EnemyNames={"暗影","疾行兽","铁甲龟","虫群","净化灵","攻城兽"};
        public static readonly string[] Counters={"常规敌人","高速低血：水域 / 青蛙","抗直伤：沼泽毒伤 / 蘑菇","密集低血：范围攻击","抗毒减速：猫头鹰直伤","高血量漏怪扣3：集火"};
        public static List<EnemyKind> Wave(int map,int wave)
        {
            var result=new List<EnemyKind>(); int total=8+(wave<=2?1:map)*2+wave*3+Mathf.Max(0,wave-3)*2;
            for(int i=0;i<total;i++) {
                if((i==total-1 && (wave==4 || wave==8)) || (wave==8 && i==total/2)) result.Add(EnemyKind.Siege);
                else if(wave==1) result.Add(i%4==0?EnemyKind.Runner:EnemyKind.Shadow);
                else if(wave==2) result.Add(i%3==0?EnemyKind.Shell:EnemyKind.Swarm);
                else result.Add((EnemyKind)(i%5));
            }
            return result;
        }
        public static float Health(EnemyKind kind) => kind==EnemyKind.Siege?4:kind==EnemyKind.Shell?1.7f:kind==EnemyKind.Runner?.65f:kind==EnemyKind.Swarm?.45f:1;
        public static float WaveHealth(int map,int wave) => (18+(wave<=2?1:map)*8+wave*6)*(1.30f+(wave<=2?1:map)*.05f)*(wave==1?.62f:1+.18f*(wave-1)+.035f*(wave-1)*(wave-1));
        public static float SpawnInterval(int wave) => Mathf.Max(.38f,.72f-.045f*(wave-1));
        public static float Speed(EnemyKind kind) => kind==EnemyKind.Runner?1.7f:kind==EnemyKind.Shell?.65f:kind==EnemyKind.Siege?.6f:kind==EnemyKind.Swarm?1.15f:1;
        public static float DirectMultiplier(EnemyKind kind) => kind==EnemyKind.Shell?.5f:1;
        public static float StatusMultiplier(EnemyKind kind) => kind==EnemyKind.Cleanser?.25f:1;
        public static bool Linked(List<RoadTile> tiles,RoadTile tile,Habitat habitat) => tiles.Any(t=>t.cell!=Vector2Int.zero && t.habitat==habitat && RoadMap.Directions.Any(d=>tile.cell+d==t.cell));
        public static float TerrainSlow(List<RoadTile> tiles,RoadTile tile)
        {
            if(tile==null || tile.cell==Vector2Int.zero) return 0;
            if(tile.habitat==Habitat.Water) return Mathf.Min(.6f,.20f+tile.level*.05f+(Linked(tiles,tile,Habitat.Swamp)?.15f:0));
            if((tile.habitat==Habitat.Woodland && Linked(tiles,tile,Habitat.Meadow)) || (tile.habitat==Habitat.Meadow && Linked(tiles,tile,Habitat.Woodland))) return .15f+.05f*tile.level;
            return 0;
        }
        public static float TerrainPoison(List<RoadTile> tiles,RoadTile tile) => tile!=null && tile.cell!=Vector2Int.zero && tile.habitat==Habitat.Swamp?tile.level*2+(Linked(tiles,tile,Habitat.Water)?2:0):0;
        public static bool CanReach(Kind kind,Vector2Int home,Vector3 origin,Vector3 target,float range) =>
            new Vector2(origin.x-target.x,origin.z-target.z).magnitude<=range;
    }
}



