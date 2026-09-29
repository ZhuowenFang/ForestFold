using System;
using System.Linq;
using UnityEngine;

namespace ForestFold
{
    public partial class ForestGame
    {
        static readonly Rect MapRect = new Rect(0,139.5f,1097.6f,540);
        Vector2 MapScreen(Vector3 world)
        {
            var v=cam.WorldToViewportPoint(world);
            return new Vector2(MapRect.x+v.x*MapRect.width,MapRect.y+(1-v.y)*MapRect.height);
        }
        Rect TileRect(Vector2Int cell)
        {
            var points=Enumerable.Range(0,6).Select(i=>MapScreen(Position(cell)+HexVertex(i,HexRadius))).ToArray();
            return Rect.MinMaxRect(points.Min(p=>p.x),points.Min(p=>p.y),points.Max(p=>p.x),points.Max(p=>p.y));
        }        Vector2Int CellAt(Vector2 point)
        {
            var ray=cam.ViewportPointToRay(new Vector3((point.x-MapRect.x)/MapRect.width,1-(point.y-MapRect.y)/MapRect.height,0));
            new Plane(Vector3.up,Vector3.zero).Raycast(ray,out float distance);
            var pos=ray.GetPoint(distance);
            return HexCell(pos);
        }
        void Outline(Rect rect,Color color,float size=2)
        {
            Box(new Rect(rect.x,rect.y,rect.width,size),color);
            Box(new Rect(rect.x,rect.yMax-size,rect.width,size),color);
            Box(new Rect(rect.x,rect.y,size,rect.height),color);
            Box(new Rect(rect.xMax-size,rect.y,size,rect.height),color);
        }
        void RoadPicture(Rect rect,int ports,Color color)
        {
            HexFill(rect,color); HexOutline(rect,new Color(.85f,.89f,.65f),1);
            foreach(var curve in RoadCurves.Paths(ports)) {
                for(int i=1;i<curve.Length;i++) {
                    var a=rect.center+new Vector2(curve[i-1].x/TileSize*rect.width,-curve[i-1].z/(2*HexRadius)*rect.height);
                    var b=rect.center+new Vector2(curve[i].x/TileSize*rect.width,-curve[i].z/(2*HexRadius)*rect.height);
                    var overlap=(b-a).normalized*.65f;
                    GuiLine(a-overlap,b+overlap,new Color(.90f,.83f,.60f),rect.width*.18f);
                }
            }            foreach(var slot in EcologyRules.Slots(ports)) {
                var point=rect.center+new Vector2(slot.x/TileSize*rect.width,-slot.y/(2*HexRadius)*rect.height);
                DrawSlotMark(point,Mathf.Clamp(rect.width*.23f,16,27),DeepGreen);
            }
        }        void DrawRoadCard(int index,Rect rect)
        {
            bool enabled=(laid<PicksPerWave || rebuildTarget!=null) && !paused && !help && !BuffChoosing && !cultivationOpen && dragging==null;
            bool chosen=roadChoice==index;
            bool hovering=rect.Contains(HoverPointer);
            Card(rect,enabled?(hovering?Color.white:Cream):Panel,12);
            int ports=RoadMap.Rotate(RoadMap.Templates[roadOffers[index]],chosen?roadRotation:roadRotations[index]);
            RoadPicture(new Rect(rect.x+10,rect.y+14,86,86),ports,EcologyRules.Colors[(int)roadHabitats[index]]);
            string shape=new [] {"直路","弯道","三岔","四岔","急弯"}[roadOffers[index]];
            Text(new Rect(rect.x+105,rect.y+12,124,28),EcologyRules.Names[(int)roadHabitats[index]]+"·"+shape,16,enabled?TextColor:Muted,true);
            int slots=EcologyRules.Slots(ports).Length;
            Round(new Rect(rect.x+102,rect.y+43,119,30),slots>0?DeepGreen:new Color(.88f,.85f,.76f),7);
            Text(new Rect(rect.x+109,rect.y+45,114,27),slots>0?slots+" 个伙伴位":"无伙伴位",18,slots>0?new Color(1,.91f,.53f):Muted,true);
            Text(new Rect(rect.x+105,rect.y+77,124,30),laid>=PicksPerWave && rebuildTarget==null?"本轮完成":chosen?"R 旋转后放置":"拖入地图",14,Muted);
            if(hovering) ShowHover(EcologyRules.Names[(int)roadHabitats[index]]+" · "+shape+" · "+slots+" 个生成位\n"+TerrainDetail(new RoadTile {cell=new Vector2Int(999,999),habitat=roadHabitats[index]})+"\n"+(slots==0?"这块没有伙伴位，仍会触发地形效果。":HabitatTip(roadHabitats[index])),rect);
            if (chosen || (hovering && enabled)) Outline(new Rect(rect.x+2,rect.y+2,rect.width-4,rect.height-4),Mint,chosen?2:1);
            if (enabled && rect.Contains(Event.current.mousePosition) && Event.current.type==EventType.MouseDown && Event.current.button==0) {
                roadChoice=index; roadRotation=roadRotations[index]; roadHeld=true; Chime(clickSound); Event.current.Use();
            }
        }
        void DrawMapPlacement()
        {
            if(laid==PicksPerWave && rebuildTarget==null) { DrawMapAdjustment(); return; }
            bool interactive=!paused && !help && !BuffChoosing && !cultivationOpen && dragging==null && (laid<PicksPerWave || rebuildTarget!=null);
            var frontier=rebuildTarget!=null?new [] {rebuildTarget.cell}:RoadMap.Frontier(tiles).ToArray();
            foreach (var cell in frontier) {
                var rect=TileRect(cell);
                bool valid=roadChoice>=0 && (rebuildTarget!=null?cell==rebuildTarget.cell:RoadMap.CanPlace(tiles,cell,SelectedPorts,out _));
                HexFill(rect,valid?new Color(.84f,.96f,.69f,.28f):new Color(.95f,.96f,.79f,.09f));
                HexOutline(new Rect(rect.x+4,rect.y+4,rect.width-8,rect.height-8),valid?new Color(.91f,.98f,.67f):new Color(.82f,.88f,.67f,.65f),valid?2:1);
                if (roadChoice<0) Text(new Rect(rect.center.x-10,rect.center.y-17,30,34),"+",26,new Color(.94f,.95f,.74f));
            }
            var mouse=Event.current.mousePosition;
            if (roadChoice<0 || !interactive) return;
            bool overMap=MapRect.Contains(mouse);
            if (overMap) {
                var cell=CellAt(mouse); var rect=TileRect(cell);
                string reason="请放回高亮的重建地块";
                bool valid=rebuildTarget!=null?cell==rebuildTarget.cell:RoadMap.CanPlace(tiles,cell,SelectedPorts,out reason);
                RoadPicture(new Rect(rect.x+3,rect.y+3,rect.width-6,rect.height-6),SelectedPorts,
                    valid?EcologyRules.Colors[(int)roadHabitats[roadChoice]]:new Color(.76f,.35f,.30f,.92f));
                HexOutline(rect,valid?Mint:new Color(.98f,.4f,.4f),3);
                ShowHover(valid?"道路已对齐\n松开鼠标放置 · R 旋转":reason,rect);
            }
            if (Event.current.type==EventType.MouseDown && Event.current.button==0 && overMap) {
                roadHeld=true; Event.current.Use();
            }
            if (Event.current.type==EventType.MouseUp && Event.current.button==0 && roadHeld) {
                roadHeld=false;
                if (overMap) PlaceRoad(CellAt(mouse));
                // Releasing outside the board keeps the choice ready; no pick is consumed.
                Event.current.Use();
            }
        }
        // Drives the same placement validation and candidate consumption as user drops.
        void SmokePick()
        {
            var options=(from i in Enumerable.Range(0,3)
                from p in RoadMap.Frontier(tiles)
                from r in Enumerable.Range(0,6)
                let mask=RoadMap.Rotate(RoadMap.Templates[roadOffers[i]],r)
                where RoadMap.CanPlace(tiles,p,mask,out _)
                let proposed=tiles.Concat(new [] { new RoadTile {cell=p,ports=mask,habitat=roadHabitats[i]} }).ToList()
                let deployed=EcologyRules.Deploy(bag,proposed,new System.Random(1))
                let shortest=RoadMap.Entrances(proposed).Where(e=>e.cell!=Vector2Int.zero).Min(e=>RoadMap.PathToCore(proposed,e.cell).Count)
                orderby deployed.Count descending, deployed.Count(d=>d.slot.habitat==EcologyRules.Preference(d.relic.kind)) descending,
                    shortest descending, Enumerable.Range(0,6).Count(d=>RoadMap.Has(mask,d)) ascending
                select new {i,p,r}).FirstOrDefault();
            if (options==null) throw new Exception("No playable candidate");
            roadChoice=options.i; roadRotation=options.r;
            if (!PlaceRoad(options.p)) throw new Exception("Candidate drop rejected");
        }
    }
}







