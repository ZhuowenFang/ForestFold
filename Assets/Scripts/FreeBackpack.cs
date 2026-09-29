using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ForestFold
{
    public partial class ForestGame
    {
        readonly HashSet<Vector2Int> bagCells=new HashSet<Vector2Int>();
        readonly int[] expansionSizes=new int[4];
        readonly Vector2Int[][] expansionShapes=new Vector2Int[4][];
        Vector2Int[] pendingExpansionShape;
        int expansionShopIndex=-1, expansionPaid;
        Vector2Int? expansionPreviewAnchor;
        int expansionPending;
        readonly Dictionary<Relic,Vector2Int> dragOrigins=new Dictionary<Relic,Vector2Int>();
        readonly Dictionary<Relic,int> dragRotations=new Dictionary<Relic,int>();
        static readonly Vector2Int[] BagSides={Vector2Int.up,Vector2Int.down,Vector2Int.left,Vector2Int.right};
        void ResetBackpack()
        {
            width=8; height=6; bagCells.Clear(); expansionPending=0; pendingExpansionShape=null; expansionShopIndex=-1; dragOrigins.Clear(); dragRotations.Clear();
            for(int y=1;y<5;y++) for(int x=2;x<6;x++) bagCells.Add(new Vector2Int(x,y));
        }
        Vector2Int[] RollExpansionShape(int count)
        {
            if(count<=1) return new [] {Vector2Int.zero};
            var options=ForestRules.ShapeOptions(count);
            var shape=options[rng.Next(options.Length)].ToArray(); int rotation=rng.Next(4);
            for(int r=0;r<rotation;r++) shape=shape.Select(p=>new Vector2Int(-p.y,p.x)).ToArray();
            var origin=new Vector2Int(shape.Min(p=>p.x),shape.Min(p=>p.y));
            return shape.Select(p=>p-origin).ToArray();
        }
        bool ExpansionFits(Vector2Int[] shape,Vector2Int anchor)
        {
            if(shape==null || shape.Length==0) return false;
            var cells=shape.Select(p=>p+anchor).ToArray();
            return cells.All(p=>p.x>=0 && p.x<width && p.y>=0 && p.y<height && !bagCells.Contains(p)) && cells.Any(p=>BagSides.Any(d=>bagCells.Contains(p+d)));
        }
        bool CanFitExpansion(Vector2Int[] shape) => Enumerable.Range(0,height).Any(y=>Enumerable.Range(0,width).Any(x=>ExpansionFits(shape,new Vector2Int(x,y))));
        bool ExpansionAllowed(Vector2Int p) => expansionPending>0 && ExpansionFits(pendingExpansionShape,p);
        bool ExpandAt(Vector2Int p)
        {
            if(expansionPending<=0 || !ExpansionAllowed(p)) return false;
            foreach(var offset in pendingExpansionShape) bagCells.Add(p+offset);
            expansionPending=0; pendingExpansionShape=null; expansionShopIndex=-1;
            Notify("整块扩容完成，当前 "+bagCells.Count+" 格。"); return true;
        }
        void CancelExpansion()
        {
            if(expansionPending<=0) return;
            gold+=expansionPaid; if(expansionShopIndex>=0) sold[expansionShopIndex]=false;
            expansionPending=0; pendingExpansionShape=null; expansionShopIndex=-1;
            Notify("扩容已取消，金币退回；商品保留原形状。");
        }
        void DrawExpansionDrag()
        {
            if(expansionPending<=0 || paused || help || BuffChoosing || cultivationOpen) return;
            var m=Event.current.mousePosition;
            var anchor=expansionPreviewAnchor??new Vector2Int(Mathf.FloorToInt((m.x-1124)/Cell),Mathf.FloorToInt((m.y-236)/Cell));
            bool valid=ExpansionAllowed(anchor);
            foreach(var p in pendingExpansionShape.Select(p=>p+anchor)) {
                var rect=new Rect(1124+p.x*Cell,236+p.y*Cell,Cell-3,Cell-3);
                Round(rect,valid?new Color(.46f,.82f,.60f,.88f):new Color(.91f,.38f,.29f,.85f),5);
                Outline(rect,valid?DeepGreen:new Color(.65f,.18f,.10f),2);
            }
            if(Event.current.type==EventType.MouseUp && Event.current.button==0) {
                if(!ExpandAt(anchor)) Notify("整块放不下：不能覆盖已有背包，至少一条边相连，不能越界。Esc取消并退款。");
                Event.current.Use();
            }
        }
        void BeginBagDrag(Relic item,Vector2Int offset)
        {
            if(dragging!=null || expansionPending>0) return;
            dragOrigins.Clear(); dragRotations.Clear();
            foreach(var r in bag) { dragOrigins[r]=r.anchor; dragRotations[r]=r.rotation; }
            dragging=selected=item; grabOffset=offset;
        }
        bool CanDropAt(Vector2Int anchor,out Relic overlap)
        {
            overlap=null; if(dragging==null) return false;
            var cells=dragging.Cells().Select(c=>c+anchor).ToArray();
            if(cells.Any(c=>!bagCells.Contains(c))) return false;
            var hits=bag.Where(r=>r!=dragging && r.Cells().Any(c=>cells.Contains(c+r.anchor))).ToArray();
            if(hits.Length>1) return false;
            overlap=hits.FirstOrDefault(); return true;
        }
        bool DropAt(Vector2Int anchor)
        {
            if(!CanDropAt(anchor,out var displaced)) return false;
            dragging.anchor=anchor;
            dragging=displaced;
            if(displaced!=null) { selected=displaced; grabOffset=Vector2Int.zero; }
            else { dragOrigins.Clear(); dragRotations.Clear(); }
            return true;
        }
        void CancelBagDrag()
        {
            foreach(var r in bag) { if(dragOrigins.TryGetValue(r,out var p)) r.anchor=p; if(dragRotations.TryGetValue(r,out int rotation)) r.rotation=rotation; }
            dragging=null; dragOrigins.Clear(); dragRotations.Clear();
            Notify("已撤销本次拖动及连续交换。");
        }
        void RotateSelected()
        {
            if(selected==null || gold<1 || expansionPending>0 || (phase!=Phase.Planning && phase!=Phase.Shop)) return;
            var item=dragging??selected; int rotation=(item.rotation+1)%4;
            if(item.Cells(rotation).OrderBy(c=>c.x).ThenBy(c=>c.y).SequenceEqual(item.Cells().OrderBy(c=>c.x).ThenBy(c=>c.y))) { Notify("这个形状旋转后相同，不扣钱。"); return; }
            if(dragging==null && !ForestRules.CanPlace(bag,item,item.anchor,width,height,rotation,bagCells)) { Notify("原位旋转放不下，请先拿起或腾出空间。不扣钱。"); return; }
            item.rotation=rotation; gold--;
        }
    }
}
