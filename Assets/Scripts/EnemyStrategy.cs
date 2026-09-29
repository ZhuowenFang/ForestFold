using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ForestFold
{
    public partial class ForestGame
    {
        List<EnemyKind> wavePlan=new List<EnemyKind>();
        readonly List<EnemyKind> spawnedWave=new List<EnemyKind>();
        readonly Color[] enemyColors={new Color(.40f,.33f,.56f),new Color(.91f,.52f,.22f),new Color(.34f,.51f,.57f),new Color(.68f,.73f,.24f),new Color(.50f,.85f,.91f),new Color(.75f,.37f,.62f)};
        void DecorateEnemy(Enemy enemy)
        {
            var root=enemy.obj.transform; var kind=enemy.kind;
            if(kind==EnemyKind.Shell) {
                Part(root,"Armored shell",new Vector3(0,.18f,.02f),new Vector3(.8f,.35f,.72f),new Color(.31f,.42f,.44f),PrimitiveType.Cube);
                for(int i=-1;i<=1;i++) Part(root,"Shell rivet",new Vector3(i*.2f,.39f,0),Vector3.one*.09f,GoldColor);
            } else if(kind==EnemyKind.Runner) {
                for(int i=-1;i<=1;i+=2) Part(root,"Runner fin",new Vector3(i*.32f,0,.2f),new Vector3(.1f,.3f,.6f),new Color(1,.72f,.3f));
            } else if(kind==EnemyKind.Cleanser) {
                for(int i=0;i<5;i++) { float a=i*Mathf.PI*2/5; Part(root,"Cleanser halo",new Vector3(Mathf.Cos(a)*.4f,.35f,Mathf.Sin(a)*.4f),Vector3.one*.12f,Color.white); }
            } else if(kind==EnemyKind.Swarm) {
                for(int i=-1;i<=1;i+=2) Part(root,"Swarm wing",new Vector3(i*.3f,.15f,0),new Vector3(.4f,.08f,.3f),new Color(.86f,.93f,.56f));
            } else if(kind==EnemyKind.Siege) {
                for(int i=-1;i<=1;i+=2) Part(root,"Siege horn",new Vector3(i*.3f,.45f,-.05f),new Vector3(.15f,.55f,.15f),Cream,PrimitiveType.Cylinder);
            }
        }
        void ApplyTerrain(Enemy enemy,float dt)
        {
            var tile=tiles.Find(t=>t.cell==HexCell(enemy.obj.transform.position));
            enemy.exposedTime=Mathf.Max(0,enemy.exposedTime-dt);
            if(tile!=null && tile.cell!=Vector2Int.zero) {
                if(tile.habitat==Habitat.Woodland) { enemy.exposedTime=2; enemy.vulnerability=.1f*tile.level; }
                if(tile.habitat==Habitat.Meadow && enemy.lastTerrain!=tile.cell) {
                    DirectDamage(enemy,2*tile.level*(StrategyRules.Linked(tiles,tile,Habitat.Woodland)?2:1));
                    ThornBurst(enemy.obj.transform.position);
                }
                enemy.lastTerrain=tile.cell;
            }
            enemy.terrainSlow=StrategyRules.TerrainSlow(tiles,tile)*StrategyRules.StatusMultiplier(enemy.kind);
            float poison=StrategyRules.TerrainPoison(tiles,tile);
            if(poison>0) { enemy.poisonDps=Mathf.Max(enemy.poisonDps,poison); enemy.poisonTime=Mathf.Max(enemy.poisonTime,2); }
        }
        void DirectDamage(Enemy enemy,float damage) { enemy.hp-=damage*StrategyRules.DirectMultiplier(enemy.kind)*(1+(enemy.exposedTime>0?enemy.vulnerability:0)); }
        void DrawForecast()
        {
            if(phase!=Phase.Planning && phase!=Phase.Shop) return;
            if(phase==Phase.Shop && wave==WavesPerMap && map==3) return;
            int nextMap=wave==WavesPerMap?map+1:map,nextWave=wave==WavesPerMap?1:wave+1;
            var forecast=StrategyRules.Wave(nextMap,nextWave).GroupBy(k=>k).ToArray();
            Card(new Rect(24,145,1050,82),new Color(.97f,.96f,.88f,.97f),9);
            Text(new Rect(36,149,1000,27),"下一波预告 · 地图 "+nextMap+" / 波次 "+nextWave+"   （悬停敌人查看应对）",17,DeepGreen,true);
            float cell=1028f/forecast.Length;
            for(int i=0;i<forecast.Length;i++) {
                var g=forecast[i]; var rect=new Rect(36+i*cell,180,cell-8,34);
                Round(rect,Color.Lerp(enemyColors[(int)g.Key],Cream,.55f),6);
                Text(new Rect(rect.x+8,rect.y+3,rect.width-8,28),StrategyRules.EnemyNames[(int)g.Key]+" ×"+g.Count(),16,DeepGreen,true);
                if(rect.Contains(HoverPointer)) ShowHover(StrategyRules.EnemyNames[(int)g.Key]+" ×"+g.Count()+"\n"+StrategyRules.Counters[(int)g.Key]+"\n选择相应生态、伙伴与背包搭配。",rect);
            }
        }
        Vector2? rangeTestPointer;
        Rect PartnerHitRect(Ally ally)
        {
            var root=ally.obj.transform.position;
            var left=MapScreen(root+Vector3.left*.65f); var right=MapScreen(root+Vector3.right*.65f);
            var head=MapScreen(root+Vector3.up*1.3f); var foot=MapScreen(root);
            return Rect.MinMaxRect(left.x-4,head.y-4,right.x+4,foot.y+14);
        }
        Ally HoveredPartner(Vector2 pointer) => allies.Where(a=>PartnerHitRect(a).Contains(pointer)).OrderBy(a=>Vector2.Distance(PartnerHitRect(a).center,pointer)).FirstOrDefault();
        void DrawRangePreview()
        {
            if(phase!=Phase.Battle) return;
            var ally=HoveredPartner(rangeTestPointer??HoverPointer);
            if(ally==null) return;
            var attack=PartnerAttack(ally);
            var center=ally.obj.transform.position; center.y=.18f;
            for(int i=0;i<64;i++) {
                float a=i*Mathf.PI/32,b=(i+1)*Mathf.PI/32;
                GuiLine(MapScreen(center+new Vector3(Mathf.Cos(a),0,Mathf.Sin(a))*attack.range),MapScreen(center+new Vector3(Mathf.Cos(b),0,Mathf.Sin(b))*attack.range),GoldColor,2);
            }
            ShowHover(ally.relic.Name+" · 圆形射程 "+attack.range+"\n"+EcologyRules.Names[(int)ally.habitat]+(EcologyRules.Affinity(ally.relic.kind,ally.habitat)>0?"（喜爱强化）":EcologyRules.Affinity(ally.relic.kind,ally.habitat)<0?"（厌恶削弱）":"（正常）")+"\n"+EcologyRules.StarDetail(ally.relic.kind,ally.relic.level),PartnerHitRect(ally));
        }
    }
}






