using System.Linq;
using UnityEngine;

namespace ForestFold
{
    public partial class ForestGame
    {
        void DecorateHabitat(RoadTile tile)
        {
            if(tile.cell==Vector2Int.zero) return;
            var p=Position(tile.cell);
            foreach(var corner in EcologyRules.Corners) {
                var v=p+new Vector3(corner.x*1.32f,.13f,corner.y*1.32f);
                if(tile.habitat==Habitat.Water) {
                    Primitive("Pond ripple",PrimitiveType.Cylinder,v,new Vector3(.43f,.015f,.42f),new Color(.62f,.84f,.84f),terrain);
                    Primitive("Lily pad",PrimitiveType.Cylinder,v+new Vector3(.06f,.025f,.05f),new Vector3(.24f,.012f,.21f),new Color(.35f,.66f,.38f),terrain);
                } else if(tile.habitat==Habitat.Swamp) {
                    Primitive("Peat pool",PrimitiveType.Cylinder,v,new Vector3(.40f,.013f,.37f),new Color(.29f,.34f,.29f),terrain);
                    for(int n=0;n<3;n++) {
                        Primitive("Swamp reed",PrimitiveType.Cylinder,v+new Vector3((n-1)*.06f,.20f,0),new Vector3(.023f,.21f,.023f),new Color(.56f,.62f,.33f),terrain);
                        Primitive("Reed head",PrimitiveType.Sphere,v+new Vector3((n-1)*.06f,.41f,0),new Vector3(.047f,.12f,.047f),new Color(.47f,.34f,.24f),terrain);
                    }
                } else if(tile.habitat==Habitat.Woodland) {
                    Primitive("Sapling trunk",PrimitiveType.Cylinder,v+Vector3.up*.17f,new Vector3(.09f,.19f,.09f),new Color(.48f,.32f,.19f),terrain);
                    Primitive("Sapling crown",PrimitiveType.Sphere,v+Vector3.up*.39f,new Vector3(.37f,.45f,.34f),new Color(.36f,.63f,.34f),terrain);
                }
            }
        }
        void DrawSlotMark(Vector2 point,float size,Color color)
        {
            // A bright rim and dark center keep nests readable on every habitat.
            Round(new Rect(point.x-size/2+1,point.y-size/2+2,size,size),new Color(.12f,.20f,.12f,.65f),size/2);
            Round(new Rect(point.x-size/2,point.y-size/2,size,size),new Color(1,.88f,.38f),size/2);
            float inner=size-5;
            Round(new Rect(point.x-inner/2,point.y-inner/2,inner,inner),color,size/2);
            float stroke=Mathf.Max(3,size*.14f);
            Box(new Rect(point.x-size*.23f,point.y-stroke/2,size*.46f,stroke),Color.white);
            Box(new Rect(point.x-stroke/2,point.y-size*.23f,stroke,size*.46f),Color.white);
        }
        void DrawEcologyOverlay()
        {

            if(phase==Phase.Planning) {
                foreach(var slot in EcologyRules.MapSlots(tiles)) {
                    var p=MapScreen(Position(slot.cell)+new Vector3(slot.offset.x,.20f,slot.offset.y));
                    DrawSlotMark(p,Mathf.Clamp(TileRect(slot.cell).width*.23f,18,26),DeepGreen);
                }
                if(roadChoice<0 && MapRect.Contains(HoverPointer) && HoverPointer.y>227) {
                    var tile=tiles.Find(t=>t.cell==CellAt(HoverPointer));
                    if(tile!=null && tile.cell!=Vector2Int.zero) {
                        ShowHover(TerrainDetail(tile)+"\n"+EcologyRules.Slots(tile.ports).Length+" 个伙伴位\n"+HabitatTip(tile.habitat),TileRect(tile.cell));
                    }
                }
            }
            if(phase==Phase.Battle) {
                Card(new Rect(24,145,470,32),Cream);
                Text(new Rect(36,148,450,27),"金色 ↑ 喜爱强化   ·   紫色 ↓ 厌恶削弱",17,DeepGreen,true);
                foreach(var ally in allies.Where(a=>EcologyRules.Affinity(a.relic.kind,a.habitat)!=0)) {
                    var p=MapScreen(ally.obj.transform.position);
                    bool liked=EcologyRules.Affinity(ally.relic.kind,ally.habitat)>0;
                    var badge=new Rect(p.x-27,p.y+10,54,22);
                    Round(badge,liked?new Color(1,.87f,.38f):new Color(.36f,.23f,.44f),6);
                    Text(new Rect(badge.x+3,badge.y,52,22),liked?"↑ 强化":"↓ 削弱",13,liked?DeepGreen:Color.white,true);
                }
                foreach(var enemy in enemies) {
                    var p=MapScreen(enemy.obj.transform.position);
                    DrawEnemyStatus(enemy,p);
                    if(enemy.exposedTime>0) Round(new Rect(p.x-18,p.y-31,6,6),new Color(1,.72f,.22f),3);
                    if(enemy.poisonTime>0) Round(new Rect(p.x-8,p.y-31,6,6),new Color(.56f,.75f,.27f),3);
                    if(enemy.slowTime>0 || enemy.terrainSlow>0) Round(new Rect(p.x+2,p.y-31,6,6),new Color(.35f,.80f,.96f),3);
                }
            }
        }
        string HabitatTip(Habitat habitat)
        {
            switch(habitat) {
                case Habitat.Woodland:return "喜：猫头鹰穿透光束";
                case Habitat.Water:return "喜：青蛙扩散波 / 厌：蘑菇";
                case Habitat.Swamp:return "喜：蘑菇毒池 / 厌：芽芽、猫头鹰";
                default:return "喜：芽芽额外2枚叶弹 / 厌：青蛙";
            }
        }
    }
}





