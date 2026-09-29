using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ForestFold
{
    public partial class ForestGame
    {
        RoadTile movingTile;
        bool moveHeld;
        List<RoadTile> adjustmentSnapshot;
        List<RoadTile> CloneRoads() => tiles.Select(t=>new RoadTile {cell=t.cell,ports=t.ports,habitat=t.habitat,level=t.level}).ToList();
        void RestoreAdjustment()
        {
            if(adjustmentSnapshot==null || rebuildTarget!=null) return;
            tiles.Clear(); tiles.AddRange(adjustmentSnapshot.Select(t=>new RoadTile {cell=t.cell,ports=t.ports,habitat=t.habitat,level=t.level}));
            movingTile=null; moveHeld=false; RebuildTerrain();
        }
        void RotateMoving()
        {
            if(phase!=Phase.Planning || laid!=PicksPerWave || movingTile==null) return;
            movingTile.ports=RoadMap.Rotate(movingTile.ports,1); RebuildTerrain();
        }
        bool MoveExisting(RoadTile tile,Vector2Int destination)
        {
            if(phase!=Phase.Planning || laid!=PicksPerWave || tile==null || tile.cell==Vector2Int.zero || destination==Vector2Int.zero || !tiles.Contains(tile)) return false;
            var other=tiles.Find(t=>t.cell==destination);
            if(other!=null) other.cell=tile.cell;
            tile.cell=destination; RebuildTerrain(); return true;
        }
        void DrawMapAdjustment()
        {
            if(paused || help || BuffChoosing || cultivationOpen || dragging!=null) return;
            var ev=Event.current; var mouse=ev.mousePosition;
            foreach(var tile in tiles.Where(t=>t.cell!=Vector2Int.zero))
                HexOutline(TileRect(tile.cell),tile==movingTile?GoldColor:new Color(.86f,.93f,.70f,.6f),tile==movingTile?3:1);
            if(ev.type==EventType.MouseDown && ev.button==0 && MapRect.Contains(mouse)) {
                var tile=tiles.Find(t=>t.cell==CellAt(mouse) && t.cell!=Vector2Int.zero);
                movingTile=tile; moveHeld=tile!=null; ev.Use();
            }
            if(movingTile!=null && moveHeld && MapRect.Contains(mouse)) {
                var rect=TileRect(CellAt(mouse));
                RoadPicture(rect,movingTile.ports,EcologyRules.Colors[(int)movingTile.habitat]);
                HexOutline(rect,GoldColor,3);
            }
            if(ev.type==EventType.MouseUp && ev.button==0 && moveHeld) {
                moveHeld=false;
                if(MapRect.Contains(mouse)) { if(!MoveExisting(movingTile,CellAt(mouse))) Notify("森林核心固定，其他地块可自由移动或互换。"); }
                ev.Use();
            }
        }
        void DrawAdjustmentPanel()
        {
            bool valid=RoadMap.Validate(tiles,out string reason);
            Text(new Rect(26,690,740,34),"地图调整  ·  本轮 2 块已选完",24,null,true);
            Text(new Rect(26,732,740,26),"拖动移动 / 互换 · R 旋转60° · 重建与合成需金币",17,Muted);
            Text(new Rect(26,762,740,28),reason,16,valid?Mint:GoldColor,true);
            DrawTerrainStrategy();
            if(Button(new Rect(784,696,288,40),"恢复本轮调整前布局",!moveHeld)) RestoreAdjustment();
            if(Button(new Rect(784,750,288,43),"旋转选中地块  [R]",movingTile!=null)) RotateMoving();
            if(Button(new Rect(784,810,288,67),"确认布局 · 开始第 "+(wave+1)+" 波",valid && !moveHeld && dragging==null,new Color(.26f,.44f,.29f))) { movingTile=null; StartWave(); }
        }
        void SmokeAdjustment()
        {
            var before=CloneRoads(); int count=tiles.Count;
            movingTile=tiles[1]; MoveExisting(movingTile,new Vector2Int(9,9)); RotateMoving();
            if(tiles.Count!=count || RoadMap.Validate(tiles,out _)) throw new Exception("Adjustment conservation/disconnect validation failed");
            movingTile=null; StartWave(); if(phase!=Phase.Planning) throw new Exception("Disconnected map started battle");
            RestoreAdjustment(); movingTile=tiles[1]; var first=movingTile.cell; var second=tiles[2].cell;
            MoveExisting(movingTile,second);
            if(tiles[2].cell!=first || tiles.Count!=count) throw new Exception("Swap changed tile inventory");
            RestoreAdjustment();
            if(!RoadMap.Validate(tiles,out _) || !tiles.Select(t=>t.cell).SequenceEqual(before.Select(t=>t.cell))) throw new Exception("Adjustment restore failed");
            Debug.Log("FOREST_ADJUSTMENT_OK: move, rotate, swap, fixed count, invalid start blocked, restore");
        }
    }
}



