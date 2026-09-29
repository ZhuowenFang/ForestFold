using System.Linq;
using UnityEngine;

namespace ForestFold
{
    public partial class ForestGame
    {
        RoadTile rebuildTarget;
        void BeginRebuild()
        {
            if(movingTile==null || gold<5 || rebuildTarget!=null || dragging!=null) return;
            rebuildTarget=movingTile; movingTile=null; moveHeld=false; roadChoice=-1; RollRoads();
            Notify("重建：三选一后放回高亮地块，成交扣5金；可取消，不扣钱。等级重置为1。");
        }
        bool CommitRebuild(Vector2Int cell)
        {
            if(rebuildTarget==null || cell!=rebuildTarget.cell || gold<5 || roadChoice<0) { Notify("请将新地块放在待重建的高亮位置。"); return false; }
            rebuildTarget.ports=SelectedPorts; rebuildTarget.habitat=roadHabitats[roadChoice]; rebuildTarget.level=1;
            gold-=5; rebuildTarget=null; roadChoice=-1; roadHeld=false; adjustmentSnapshot=CloneRoads(); RebuildTerrain();
            Notify("已花5金重建。可继续免费调整位置与方向，连通后开战。"); return true;
        }
        RoadTile MergeNeighbor() => movingTile==null?null:tiles.FirstOrDefault(t=>t!=movingTile && t.cell!=Vector2Int.zero && t.level==movingTile.level && t.habitat==movingTile.habitat && t.level<3 && RoadMap.Directions.Any(d=>t.cell==movingTile.cell+d));
        void MergeTerrain()
        {
            var other=MergeNeighbor();
            if(other==null || gold<8 || rebuildTarget!=null) return;
            gold-=8; tiles.Remove(other); movingTile.level++; adjustmentSnapshot=CloneRoads(); RebuildTerrain();
            Notify("地块合成升至 Lv."+movingTile.level+"，消耗相邻同生态同级地块；道路如断开可免费重排。强化水域/沼泽及联动效果。");
        }
        string TerrainDetail(RoadTile tile)
        {
            string extra=tile.habitat==Habitat.Water?"减速 "+Mathf.RoundToInt(StrategyRules.TerrainSlow(tiles,tile)*100)+"%":tile.habitat==Habitat.Swamp?"毒伤 "+StrategyRules.TerrainPoison(tiles,tile)+" / 秒":tile.habitat==Habitat.Woodland?"直伤易伤 "+(10*tile.level)+"% · 邻草甸减速":"入场荆棘 "+(2*tile.level)+" 伤害 · 邻林地翻倍并减速";
            return EcologyRules.Names[(int)tile.habitat]+" Lv."+tile.level+" · "+extra;
        }
        void DrawTerrainLinks()
        {
            foreach(var tile in tiles.Where(t=>t.cell!=Vector2Int.zero)) {
                if(tile.level>1) {
                    var center=MapScreen(Position(tile.cell));
                    Round(new Rect(center.x-21,center.y+14,42,20),GoldColor,5);
                    Text(new Rect(center.x-18,center.y+12,40,24),"Lv."+tile.level,13,Cream,true);
                }
                foreach(var other in tiles.Where(t=>t.cell!=Vector2Int.zero && (int)t.habitat>(int)tile.habitat && RoadMap.Directions.Any(d=>t.cell==tile.cell+d))) {
                    bool linked=(tile.habitat==Habitat.Water && other.habitat==Habitat.Swamp) || (tile.habitat==Habitat.Meadow && other.habitat==Habitat.Woodland);
                    if(!linked) continue;
                    var p=MapScreen((Position(tile.cell)+Position(other.cell))*.5f+Vector3.up*.3f);
                    Round(new Rect(p.x-19,p.y-9,38,18),new Color(.18f,.43f,.39f),6);
                    Text(new Rect(p.x-16,p.y-12,36,24),"联动",12,Cream,true);
                    var hit=new Rect(p.x-28,p.y-17,56,34);
                    if(hit.Contains(HoverPointer)) ShowHover(tile.habitat==Habitat.Water?
                        "水域 × 沼泽 · 湿地联动\n水域额外减速15%；沼泽额外造成2点/秒毒伤。\n当前水域减速 "+Mathf.RoundToInt(StrategyRules.TerrainSlow(tiles,tile)*100)+"%，沼泽毒伤 "+StrategyRules.TerrainPoison(tiles,other)+"/秒。\n需要两块相邻；同类联动不重复叠加。":
                        "草甸 × 林地 · 荆棘联动\n草甸入场地刺伤害翻倍至 "+(4*tile.level)+"；两块地形都减缓敌人移动。\n草甸减速 "+Mathf.RoundToInt(StrategyRules.TerrainSlow(tiles,tile)*100)+"%，林地减速 "+Mathf.RoundToInt(StrategyRules.TerrainSlow(tiles,other)*100)+"%。\n需要两块相邻；同类联动不重复叠加。",hit);
                }
            }
        }
        void DrawTerrainStrategy()
        {
            if(rebuildTarget!=null) {
                HexOutline(TileRect(rebuildTarget.cell),GoldColor,4);
                return;
            }
            if(movingTile!=null) {
                Text(new Rect(26,790,720,26),TerrainDetail(movingTile),17,DeepGreen,true);
                if(Button(new Rect(26,825,208,44),"清除重建  $5",gold>=5)) BeginRebuild();
                if(Button(new Rect(245,825,225,44),"相邻地块合成  $8",gold>=8 && MergeNeighbor()!=null)) MergeTerrain();
                if(Button(new Rect(482,825,240,44),"旋转 60°  [R]")) RotateMoving();
                var other=MergeNeighbor(); if(other!=null) HexOutline(TileRect(other.cell),new Color(1,.65f,.25f),3);
            } else Text(new Rect(26,825,720,48),"选中地块：$5 重建 / $8 相邻同生态同级合成\n移动、互换、旋转免费；合成后可重排断路。",16,Muted);
        }
    }
}

