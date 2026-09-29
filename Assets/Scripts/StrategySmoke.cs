using System;
using System.Linq;
using UnityEngine;

namespace ForestFold
{
    public partial class ForestGame
    {
        System.Collections.IEnumerator StrategySmoke()
        {
            muted=true; yield return null; CheckStarCombat();
            NewRun(); trainingPoints=3; Train(0); if(trainingPoints!=2 || TrainingPower(Kind.Sprout)<1.049f) throw new Exception("Training failed"); NewRun(); if(trainingRanks[0]!=1) throw new Exception("Permanent rank reset"); Array.Clear(trainingRanks,0,4); trainingPoints=0;
            GainXp(22); if(runLevel!=3 || pendingBuffs!=2 || buffOffers.Distinct().Count()!=3) throw new Exception("XP level queue failed"); ChooseBuff(0); ChooseBuff(0); buffs[1]=5; RollShop(); if(Enumerable.Range(0,4).Any(i=>ForestRules.IsPartner(offers[i]) && offerLevels[i]!=2)) throw new Exception("Elite shop failed"); phase=Phase.Shop; gold=100; bag.Clear(); int price=ShopCost(0); Buy(0); if(bag[0].level!=2 || gold!=100-price) throw new Exception("Elite purchase failed");
            Debug.Log("FOREST_PROGRESSION_RULES_OK: training cost, permanent rank, XP queue, unique choices, elite purchase");
            NewRun(); rng=new System.Random(42); gold=50;
            tiles.Add(new RoadTile {cell=RoadMap.Directions[0],ports=63,habitat=Habitat.Water});
            tiles.Add(new RoadTile {cell=RoadMap.Directions[1],ports=63,habitat=Habitat.Water}); laid=PicksPerWave; adjustmentSnapshot=CloneRoads();
            movingTile=tiles[1]; BeginRebuild(); int count=tiles.Count,beforeGold=gold;
            StartWave(); if(phase!=Phase.Planning || gold!=beforeGold) throw new Exception("Pending rebuild charged or allowed battle");
            roadChoice=0; roadOffers[0]=3; roadRotation=0; roadHabitats[0]=Habitat.Swamp;
            if(!CommitRebuild(tiles[1].cell) || gold!=beforeGold-5 || tiles.Count!=count || tiles[1].habitat!=Habitat.Swamp) throw new Exception("Paid rebuild failed");
            RestoreAdjustment(); if(tiles[1].habitat!=Habitat.Swamp) throw new Exception("Restore bypassed paid rebuild");
            tiles[1].habitat=Habitat.Water; movingTile=tiles[1]; beforeGold=gold; MergeTerrain();
            if(tiles.Count!=count-1 || movingTile.level!=2 || gold!=beforeGold-8) throw new Exception("Terrain merge transaction failed");
            RestoreAdjustment(); if(tiles.Count!=count-1) throw new Exception("Restore duplicated merged terrain");
            NewRun(); gold=30; bag.Clear();
            for(int i=0;i<3;i++) bag.Add(new Relic(Kind.Owl) {anchor=new Vector2Int(i,0)});
            selected=bag[0]; MergeConnected();
            if(bag.Count!=1 || selected.level!=2 || gold!=33) throw new Exception("Compact spatial merge reward failed");
            string key=ShapeKey(selected); RerollShape();
            if(gold!=30 || selected.Cells().Count!=3 || selected.level!=2 || ShapeKey(selected)==key || !ForestRules.CanPlace(bag,selected,selected.anchor,width,height)) throw new Exception("Reroll failed area, fit or price");
            bag.Clear(); selected=new Relic(Kind.Sprout); bag.Add(selected); width=1;height=2; beforeGold=gold; RerollShape();
            if(gold!=beforeGold || selected.rotation!=0) throw new Exception("Impossible reroll charged");
            NewRun();
            var target=new Enemy {obj=new GameObject("Terrain test"),hp=100,kind=EnemyKind.Shell}; target.obj.transform.SetParent(actors);
            DirectDamage(target,20); if(target.hp!=90) throw new Exception("Armor did not halve direct damage");
            var testTile=new RoadTile {cell=RoadMap.Directions[0],ports=63,habitat=Habitat.Woodland,level=2}; tiles.Add(testTile);
            target.obj.transform.position=Position(testTile.cell); ApplyTerrain(target,.1f); DirectDamage(target,20);
            if(Mathf.Abs(target.hp-78)>.001f) throw new Exception("Woodland vulnerability did not affect damage");
            testTile.habitat=Habitat.Water; target.kind=EnemyKind.Cleanser; ApplyTerrain(target,.1f);
            if(target.terrainSlow<=0 || target.terrainSlow>=.1f) throw new Exception("Cleanser terrain slow resistance failed");
            testTile.habitat=Habitat.Swamp; ApplyTerrain(target,.1f);
            if(target.poisonTime<=0 || target.poisonDps!=4) throw new Exception("Swamp failed to apply poison");
            testTile.habitat=Habitat.Meadow; target.lastTerrain=new Vector2Int(999,999); beforeGold=(int)target.hp; ApplyTerrain(target,.1f);
            float afterThorns=target.hp; ApplyTerrain(target,.1f);
            if(afterThorns>=beforeGold || target.hp!=afterThorns) throw new Exception("Grass thorns missing or repeated every frame");
            Debug.Log("FOREST_TERRAIN_COMBAT_OK: armor, vulnerability, status resistance, poison, one-hit entry trap");
            Debug.Log("FOREST_TRANSACTIONS_OK: paid replacement, tile merge, restore boundaries, spatial merge, compact reward, area-preserving reroll, no-fit refund");
            NewRun(); rng=new System.Random(42); RollRoads(); speed=16;
            for(int stage=1;stage<=3;stage++) for(int w=1;w<=WavesPerMap;w++) {
                // Use a large test core on later maps to check flow independently of bot strategy.
                if(stage>1 && w==1) { health=1000; Debug.Log("FOREST_FLOW_ONLY_TEST_CORE map="+stage); }
                SmokePick(); SmokePick();
                if(stage==1 && w==1) SmokeAdjustment();
                var expected=StrategyRules.Wave(stage,w); StartWave();
                if(phase!=Phase.Battle || !wavePlan.SequenceEqual(expected)) throw new Exception("Forecast differs from spawn plan");
                float deadline=Time.realtimeSinceStartup+45;
                while(phase==Phase.Battle && Time.realtimeSinceStartup<deadline) { if(BuffChoosing) ChooseBuff(0); yield return null; }
                if(phase==Phase.Lost) throw new Exception("Smoke strategy lost at map="+stage+" wave="+w);
                if(!spawnedWave.SequenceEqual(expected)) throw new Exception("Actual spawns differ from forecast");
                if(phase!=(w==WavesPerMap?(stage<3?Phase.LevelComplete:Phase.Won):Phase.Shop)) throw new Exception("Progression failed map="+stage+" wave="+w+" health="+health);
                Debug.Log("FOREST_STRATEGY_WAVE map="+stage+" wave="+w+" health="+health+" gold="+gold);
                if(stage==1 && w==1) {
                    yield return new WaitForSeconds(1);
                    ScreenCapture.CaptureScreenshot(Application.dataPath+"/../hex-shop-preview.png");
                    yield return new WaitForSeconds(1);
                }
                if(w==WavesPerMap) {
                    if(trainingPoints!=stage*3) throw new Exception("Clear rewards failed");
                    if(bag.Count!=0) throw new Exception("Completed level kept inventory");
                    if(stage<3) {
                        NextLevel();
                        if(runLevel!=1 || runXp!=0 || buffs.Any(n=>n!=0)) throw new Exception("Run buffs carried across maps");
                        if(bag.Count!=2 || bag[0].kind!=Kind.Sprout || bagCells.Count!=16 || width!=8 || height!=6 || gold!=18 || health!=20 || wave!=0 || tiles.Count!=1) throw new Exception("Independent level reset failed");
                    }
                    continue;
                }
                for(int i=0;i<4;i++) if(ForestRules.IsPartner(offers[i]) && !bag.Any(r=>r.kind==offers[i])) Buy(i);
                for(int i=0;i<4;i++) { Buy(i); while(expansionPending>0) ExpandAt((from y in Enumerable.Range(0,height) from x in Enumerable.Range(0,width) let p=new Vector2Int(x,y) where ExpansionAllowed(p) select p).First()); }
                if(CanMerge) MergeConnected();
                int oldCount=tiles.Count; LeaveShop();
                if(w<WavesPerMap && tiles.Count!=oldCount) throw new Exception("Wave transition removed terrain");
            }
            if(phase!=Phase.Won) throw new Exception("Victory missing");
            NewRun(); health=1; SmokePick(); SmokePick(); StartWave();
            foreach(var ally in allies) Destroy(ally.obj); allies.Clear(); speed=20;
            float end=Time.realtimeSinceStartup+20;
            while(phase==Phase.Battle && Time.realtimeSinceStartup<end) { if(BuffChoosing) ChooseBuff(0); yield return null; }
            if(phase!=Phase.Lost) throw new Exception("Loss missing");
            Debug.Log("FOREST_STRATEGY_ALL_OK: 24 waves and independent level resets, exact forecast, purchases, hex draft and paths, victory, loss");
            NewRun(); gold=45; AddStarter(Kind.Owl); AddStarter(Kind.Mushroom); AddStarter(Kind.Frog); AddStarter(Kind.Bell);
            SmokePick();
            yield return new WaitForSeconds(2); ScreenCapture.CaptureScreenshot(Application.dataPath+"/../hex-draft-preview.png");
            yield return new WaitForSeconds(1); SmokePick(); movingTile=tiles[1];
            yield return new WaitForSeconds(2); ScreenCapture.CaptureScreenshot(Application.dataPath+"/../hex-adjust-preview.png");
            yield return new WaitForSeconds(1); movingTile=null; StartWave(); speed=1;
            yield return new WaitForSeconds(5); ScreenCapture.CaptureScreenshot(Application.dataPath+"/../hex-battle-preview.png");
            var ranged=allies.FirstOrDefault(a=>a.relic.kind==Kind.Sprout)??allies.First();
            rangeTestPointer=MapScreen(ranged.obj.transform.position+Vector3.up*.7f);
            if(HoveredPartner(rangeTestPointer.Value)!=ranged) throw new Exception("Partner body hover did not select range");
            yield return new WaitForSeconds(1); ScreenCapture.CaptureScreenshot(Application.dataPath+"/../circular-range-preview.png");
            yield return new WaitForSeconds(1); Application.Quit();
        }
    }
}








