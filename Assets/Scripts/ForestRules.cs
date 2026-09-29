using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ForestFold
{
    public enum Kind { Sprout, Owl, Mushroom, Crystal, Bell, Frog, Expansion }

    [Serializable]
    public class Relic
    {
        public Kind kind;
        public int level = 1;
        public int rotation;
        public int shape=-1;
        public Vector2Int anchor;
        public Relic(Kind k) { kind = k; }
        public bool Partner => ForestRules.IsPartner(kind);
        public string Name => ForestRules.Names[(int)kind];
        public Color Color => ForestRules.Colors[(int)kind];
        public List<Vector2Int> Cells(int orientation = -1)
        {
            if(kind==Kind.Expansion) return new List<Vector2Int>();
            int r = orientation < 0 ? rotation : orientation;
            var result = new List<Vector2Int>();
            foreach (var original in shape<0?ForestRules.Shapes[(int)kind]:ForestRules.ShapeOptions(ForestRules.Shapes[(int)kind].Length)[shape])
            {
                var p = original;
                for (int i = 0; i < r; i++) p = new Vector2Int(-p.y, p.x);
                result.Add(p);
            }
            int minX = result.Min(p => p.x), minY = result.Min(p => p.y);
            return result.Select(p => p - new Vector2Int(minX, minY)).ToList();
        }
    }

    public static class ForestRules
    {
        public static readonly Kind[] Partners={Kind.Sprout,Kind.Owl,Kind.Mushroom,Kind.Frog};
        public static bool IsPartner(Kind kind)=>Partners.Contains(kind);
        public static readonly string[] Names = { "芽芽射手", "月羽猫头鹰", "孢子蘑菇", "力量水晶", "风铃", "涟漪青蛙", "背包扩展片" };
        public static readonly string[] ShortNames = { "芽", "鸮", "菇", "晶", "铃", "蛙", "包" };
        public static readonly Color[] Colors = {
            new Color(.42f,.82f,.49f), new Color(.64f,.65f,.96f), new Color(.95f,.56f,.48f),
            new Color(.38f,.79f,.89f), new Color(.98f,.79f,.38f), new Color(.38f,.77f,.67f), new Color(.81f,.67f,.43f)
        };
        public static readonly Vector2Int[][] Shapes = {
            new [] { new Vector2Int(0,0), new Vector2Int(0,1) },
            new [] { new Vector2Int(0,0), new Vector2Int(0,1), new Vector2Int(0,2) },
            new [] { new Vector2Int(0,0), new Vector2Int(0,1), new Vector2Int(1,1) },
            new [] { new Vector2Int(0,0), new Vector2Int(1,0), new Vector2Int(0,1), new Vector2Int(1,1) },
            new [] { new Vector2Int(0,0), new Vector2Int(1,0) },
            new [] { new Vector2Int(0,0), new Vector2Int(1,0), new Vector2Int(0,1), new Vector2Int(1,1) },
            new Vector2Int[0]
        };
        public static int Count(Kind kind) => IsPartner(kind)?3:0;
        public static Vector2Int[][] ShapeOptions(int count)
        {
            if(count==2) return new [] { Shapes[0] };
            if(count==3) return new [] { Shapes[1],Shapes[2] };
            return new [] { Shapes[3], new [] {new Vector2Int(0,0),new Vector2Int(0,1),new Vector2Int(0,2),new Vector2Int(0,3)},
                new [] {new Vector2Int(0,0),new Vector2Int(0,1),new Vector2Int(0,2),new Vector2Int(1,2)},
                new [] {new Vector2Int(0,0),new Vector2Int(1,0),new Vector2Int(2,0),new Vector2Int(1,1)},
                new [] {new Vector2Int(1,0),new Vector2Int(2,0),new Vector2Int(0,1),new Vector2Int(1,1)} };
        }
        public static bool Adjacent(Relic a,Relic b)
        {
            var cells=new HashSet<Vector2Int>(b.Cells().Select(c=>c+b.anchor));
            return a.Cells().Any(c=>new [] {Vector2Int.up,Vector2Int.down,Vector2Int.left,Vector2Int.right}.Any(d=>cells.Contains(a.anchor+c+d)));
        }
        public static List<Relic> MergeSet(List<Relic> bag,Relic preferred=null)
        {
            foreach(var start in bag.Where(r=>r.Partner && r.level<3).OrderBy(r=>r==preferred?0:1)) {
                var result=bag.Where(r=>r.kind==start.kind && r.level==start.level).OrderBy(r=>r==start?0:1).Take(3).ToList();
                if(result.Count==3) return result;
            }
            return new List<Relic>();
        }
        public static float Haste(List<Relic> bag,Relic partner) => 1+bag.Where(r=>r.kind==Kind.Bell && bag.Any(p=>p.kind==partner.kind && Adjacent(r,p))).Sum(r=>.25f*r.level);
        public static List<Relic> ActiveSpecies(IEnumerable<Relic> bag) => bag.Where(r=>r.Partner)
            .GroupBy(r=>r.kind).Select(g=>g.OrderByDescending(r=>r.level).First()).ToList();
        public static int DeploymentLimit(IEnumerable<Relic> bag) => bag.Where(r=>r.Partner).Sum(r=>Mathf.Clamp(r.level,1,3));
        public static int Cost(Kind kind) => new [] { 9, 12, 11, 10, 10, 12, 12 }[(int)kind];
        public static string Detail(Kind kind)
        {
            switch (kind)
            {
                case Kind.Sprout: return "每件按星数出战 · 叶弹 · 圆形射程 4.1\n喜草甸：额外2枚叶弹 · 厌沼泽：迟缓弱射";
                case Kind.Owl: return "每件按星数出战 · 跨地块狙击 · 射程 8.5\n喜林地：穿透光束 · 厌沼泽：迟缓弱射";
                case Kind.Mushroom: return "每件按星数出战 · 孢子 · 圆形射程 3.4\n喜沼泽：持续毒池 · 厌水域：失去毒性";
                case Kind.Frog: return "每件按星数出战 · 跨地块水波 · 射程 5.1\n喜水域：扩散冲击波 · 厌草甸：失去减速";
                case Kind.Crystal: return "全体伙伴伤害 +18%\n占格 2×2 · 放入即生效";
                case Kind.Expansion: return "随机1～4格 · 每格3金币\n固定形状整块拖放 · 边缘相连 · 不拆分旋转 · 画布8×6";
                default: return "仅边缘相邻伙伴攻速 +25%\n金色描边为已激活 · 斜角不算相邻";
            }
        }
        public static bool CanPlace(List<Relic> bag, Relic item, Vector2Int anchor, int width, int height, int rotation = -1, HashSet<Vector2Int> area=null)
        {
            if(item.kind==Kind.Expansion) return false;
            var occupied = new HashSet<Vector2Int>();
            foreach (var other in bag)
                if (other != item)
                    foreach (var p in other.Cells()) occupied.Add(other.anchor + p);
            foreach (var p in item.Cells(rotation))
            {
                var c = anchor + p;
                if (c.x < 0 || c.y < 0 || c.x >= width || c.y >= height || (area!=null && !area.Contains(c)) || occupied.Contains(c)) return false;
            }
            return true;
        }
        public static bool AutoPlace(List<Relic> bag, Relic item, int width, int height, HashSet<Vector2Int> area=null)
        {
            for (int r = 0; r < 4; r++)
                for (int y = 0; y < height; y++)
                    for (int x = 0; x < width; x++)
                    {
                        var p = new Vector2Int(x,y);
                        if (!CanPlace(bag, item, p, width, height, r,area)) continue;
                        item.anchor = p;
                        item.rotation = r;
                        return true;
                    }
            return false;
        }
        public static Vector2Int Turn(Vector2Int heading, int turn)
        {
            if (turn < 0) return new Vector2Int(-heading.y, heading.x);
            if (turn > 0) return new Vector2Int(heading.y, -heading.x);
            return heading;
        }
    }
}







