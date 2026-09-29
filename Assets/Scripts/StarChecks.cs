using System;
using System.Linq;
using UnityEngine;

namespace ForestFold
{
    public partial class ForestGame
    {
        void CheckStarCombat()
        {
            NewRun();
            var root=new GameObject("Star skill check"); root.transform.SetParent(actors);
            var ally=new Ally {obj=root,relic=new Relic(Kind.Sprout) {level=2},habitat=Habitat.Woodland};
            for(int i=0;i<3;i++) {
                var obj=new GameObject("Skill target"); obj.transform.SetParent(actors); obj.transform.position=new Vector3(i*.5f,0,1);
                enemies.Add(new Enemy {obj=obj,hp=10000,kind=EnemyKind.Shadow});
            }
            LaunchAttack(ally,enemies[0],PartnerAttack(ally),10,1);
            if(shots.Count!=2) throw new Exception("Two-star sprout volley failed");
            ally.relic=new Relic(Kind.Owl) {level=3}; ally.habitat=Habitat.Meadow;
            LaunchAttack(ally,enemies[0],PartnerAttack(ally),10,1);
            if(enemies.Any(e=>e.hp>=10000)) throw new Exception("Three-star owl chain failed");
            ClearAttackVisuals();
            var mushroom=EcologyRules.Attack(Kind.Mushroom,Habitat.Meadow,3);
            ApplyImpact(Kind.Mushroom,enemies[0],enemies[0].obj.transform.position,mushroom,10,1);
            if(groundSkills.Count!=3) throw new Exception("Three-star mushroom clusters failed");
            ClearAttackVisuals();
            var frog=EcologyRules.Attack(Kind.Frog,Habitat.Woodland,3);
            float before=enemies[0].hp;
            ApplyImpact(Kind.Frog,enemies[0],enemies[0].obj.transform.position,frog,10,1);
            UpdateAttackVisuals(.7f); UpdateAttackVisuals(.6f);
            if(Mathf.Abs(enemies[0].hp-(before-20))>.01f) throw new Exception("Three-star frog second wave failed");
            NewRun(); Debug.Log("FOREST_STAR_COMBAT_OK: two-leaf volley, chain targets, triple pools, double wave");
        }
        System.Collections.IEnumerator StarPreview()
        {
            muted=true; CheckStarCombat(); NewRun(); bag.Clear();
            foreach(int rank in Enumerable.Range(1,3)) bag.Add(new Relic(Kind.Owl) {level=rank,anchor=new Vector2Int(rank-1,0)});
            tiles.Add(new RoadTile {cell=RoadMap.Directions[0],ports=63,habitat=Habitat.Water});
            tiles.Add(new RoadTile {cell=RoadMap.Directions[1],ports=63,habitat=Habitat.Swamp});
            tiles.Add(new RoadTile {cell=RoadMap.Directions[2],ports=63,habitat=Habitat.Meadow});
            tiles.Add(new RoadTile {cell=RoadMap.Directions[3],ports=63,habitat=Habitat.Woodland});
            laid=PicksPerWave; RebuildTerrain(); StartWave();
            if(allies.Count!=6 || Enumerable.Range(1,3).Any(n=>allies.Count(a=>a.relic.level==n)!=n)) throw new Exception("Runtime mixed star deployment failed");
            remainingSpawn=0; foreach(var a in allies) a.cooldown=999;
            foreach(var tile in tiles.Where(t=>t.cell!=Vector2Int.zero)) {
                var obj=Primitive("Terrain status preview",PrimitiveType.Sphere,Position(tile.cell)+Vector3.up*.4f,Vector3.one*.55f,enemyColors[0],actors);
                enemies.Add(new Enemy {obj=obj,hp=1000,maxHp=1000,kind=EnemyKind.Shadow,path=new System.Collections.Generic.List<Vector3> {obj.transform.position,obj.transform.position+Vector3.right},segment=1,speed=0});
            }
            yield return new WaitForSeconds(1);
            tooltipPreviewPointer=MapScreen((Position(tiles[1].cell)+Position(tiles[2].cell))*.5f+Vector3.up*.3f);
            yield return new WaitForSeconds(.2f); ScreenCapture.CaptureScreenshot(Application.dataPath+"/../star-link-preview.png");
            yield return new WaitForSeconds(1); tooltipPreviewPointer=null;
            ThornBurst(Position(tiles[3].cell));
            yield return new WaitForSeconds(.1f); ScreenCapture.CaptureScreenshot(Application.dataPath+"/../terrain-status-preview.png");
            yield return new WaitForSeconds(1); Debug.Log("FOREST_STAR_PREVIEW_OK: mixed deployment, link tooltip, status and thorns"); Application.Quit();
        }
    }
}
