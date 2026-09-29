using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ForestFold
{
    public partial class ForestGame
    {
        string ShapeKey(Relic item) => string.Join(";",item.Cells().OrderBy(c=>c.x).ThenBy(c=>c.y).Select(c=>c.x+","+c.y));
        void RerollShape()
        {
            if(selected==null || dragging!=null || gold<3 || (phase!=Phase.Planning && phase!=Phase.Shop)) return;
            int originalShape=selected.shape,originalRotation=selected.rotation; var originalAnchor=selected.anchor;
            string original=ShapeKey(selected); var options=new List<int[]>(); var keys=new HashSet<string>();
            for(int s=0;s<ForestRules.ShapeOptions(selected.Cells().Count).Length;s++) for(int r=0;r<4;r++) {
                selected.shape=s; selected.rotation=r; selected.anchor=originalAnchor;
                string key=ShapeKey(selected);
                if(key==original || !keys.Add(key)) continue;
                bool fits=ForestRules.CanPlace(bag,selected,originalAnchor,width,height,area:bagCells);
                if(!fits) for(int y=0;y<height && !fits;y++) for(int x=0;x<width && !fits;x++)
                    if(ForestRules.CanPlace(bag,selected,new Vector2Int(x,y),width,height,area:bagCells)) { selected.anchor=new Vector2Int(x,y); fits=true; }
                if(fits) options.Add(new [] {s,r,selected.anchor.x,selected.anchor.y});
            }
            selected.shape=originalShape; selected.rotation=originalRotation; selected.anchor=originalAnchor;
            if(options.Count==0) { Notify("没有放得下的新形状，本次不扣钱。先腾出空间再试。"); return; }
            var option=options[rng.Next(options.Count)]; selected.shape=option[0]; selected.rotation=option[1]; selected.anchor=new Vector2Int(option[2],option[3]);
            gold-=3; Notify("形状已重掷，格数和等级不变。两格物品只会改变朝向。");
        }
        void MergeConnected()
        {
            if(dragging!=null || (phase!=Phase.Planning && phase!=Phase.Shop)) return;
            var set=ForestRules.MergeSet(bag,selected);
            if(set.Count!=3) { Notify("拥有 3 件同种同级伙伴即可合成。"); return; }
            var cells=set.SelectMany(r=>r.Cells().Select(c=>c+r.anchor)).ToArray();
            bool compact=(cells.Max(c=>c.x)-cells.Min(c=>c.x)+1)*(cells.Max(c=>c.y)-cells.Min(c=>c.y)+1)==cells.Length;
            selected=set[0]; selected.level++; bag.Remove(set[1]); bag.Remove(set[2]);
            if(compact) gold+=3;
            Notify("三合一完成！保留选中形状并释放另外两件占格。"+(compact?"无空洞拼成矩形，额外 +3 金币。":"拼成无空洞矩形可额外获得 3 金币。"));
        }
        void DrawBagLinks()
        {
            foreach(var bell in bag.Where(r=>r.kind==Kind.Bell && r!=dragging)) {
                bool active=bag.Any(r=>r.Partner && r!=dragging && ForestRules.Adjacent(bell,r));
                foreach(var cell in bell.Cells()) {
                    var p=cell+bell.anchor; var rect=new Rect(1124+p.x*Cell+2,236+p.y*Cell+2,Cell-7,Cell-7);
                    Outline(rect,active?new Color(.95f,.65f,.12f):Muted,3);
                }
            }
            if(selected!=null && selected.Partner) foreach(var bell in bag.Where(r=>r.kind==Kind.Bell && ForestRules.Adjacent(r,selected)))
                foreach(var cell in selected.Cells()) { var p=cell+selected.anchor; Outline(new Rect(1124+p.x*Cell+3,236+p.y*Cell+3,Cell-9,Cell-9),GoldColor,2); }
        }
    }
}

