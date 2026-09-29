using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ForestFold;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class DemoBuilder
{
    [MenuItem("Forest Fold/Build Windows Demo")]
    public static void Build()
    {
        BuildPlatform(false);
    }

    [MenuItem("Forest Fold/Build macOS Demo")]
    public static void BuildMac()
    {
        BuildPlatform(true);
    }

    static void BuildPlatform(bool mac)
    {
        RunChecks();
        Directory.CreateDirectory("Assets/Scenes");
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        new GameObject("Forest Fold — Game").AddComponent<ForestGame>();
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/ForestFold.unity");
        EditorBuildSettings.scenes = new [] { new EditorBuildSettingsScene("Assets/Scenes/ForestFold.unity", true) };
        PlayerSettings.companyName = "Forest Fold Studio";
        PlayerSettings.productName = "Forest Fold";
        PlayerSettings.defaultScreenWidth = 1600;
        PlayerSettings.defaultScreenHeight = 900;
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        PlayerSettings.runInBackground = true;
        PlayerSettings.resizableWindow = true;
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.Standalone, ScriptingImplementation.Mono2x);
        if(mac) PlayerSettings.SetArchitecture(BuildTargetGroup.Standalone,2);
        QualitySettings.vSyncCount = 1;
        QualitySettings.antiAliasing = 4;
        AssetDatabase.SaveAssets();
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
            scenes = new [] { "Assets/Scenes/ForestFold.unity" },
            locationPathName = mac?"Builds/macOS/ForestFold.app":"Builds/Windows/ForestFold.exe",
            target = mac?BuildTarget.StandaloneOSX:BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        });
        if (report.summary.result != BuildResult.Succeeded) throw new Exception("Build failed: " + report.summary.result);
        Debug.Log("FOREST_BUILD_OK " + report.summary.totalSize);
    }

    [MenuItem("Forest Fold/Run Rule Checks")]
    public static void RunChecks()
    {
        foreach(var kind in ForestRules.Partners.Concat(new [] {Kind.Crystal,Kind.Bell})) {
            int count=ForestRules.Shapes[(int)kind].Length;
            for(int shape=0;shape<ForestRules.ShapeOptions(count).Length;shape++) for(int rotation=0;rotation<4;rotation++) {
                var item=new Relic(kind) {shape=shape,rotation=rotation};
                if(item.Cells().Count!=count || item.Cells().Distinct().Count()!=count) throw new Exception("Shape area changed");
                if(!ForestRules.AutoPlace(new List<Relic>(),item,6,4)) throw new Exception("Variant cannot fit empty backpack");
            }
        }
        var a=new Relic(Kind.Owl) {anchor=Vector2Int.zero};
        var b=new Relic(Kind.Owl) {anchor=Vector2Int.right};
        var c=new Relic(Kind.Owl) {anchor=Vector2Int.right*2};
        var bag=new List<Relic> {a,b,c};
        if(ForestRules.MergeSet(bag,a).Count!=3) throw new Exception("Connected merge missing");
        c.anchor=new Vector2Int(5,0);
        if(ForestRules.MergeSet(bag,a).Count!=3) throw new Exception("Remote merge rejected");
        var bell=new Relic(Kind.Bell) {anchor=new Vector2Int(3,3)}; bag.Add(bell);
        if(ForestRules.Haste(bag,c)!=1 || ForestRules.Haste(bag,b)!=1) throw new Exception("Remote bell activated");
        bell.anchor=new Vector2Int(2,0);
        if(ForestRules.Haste(bag,b)<=1) throw new Exception("Adjacent bell inactive");
        for(int q=-5;q<=5;q++) for(int r=-5;r<=5;r++) {
            var cell=new Vector2Int(q,r);
            if(ForestGame.HexCell(ForestGame.HexWorld(cell))!=cell) throw new Exception("Hex round trip");
        }
        for(int mask=1;mask<64;mask++) {
            if(RoadMap.Rotate(mask,6)!=mask) throw new Exception("Hex rotation failed");
            int count=Enumerable.Range(0,6).Count(d=>RoadMap.Has(mask,d));
            if(EcologyRules.Slots(mask).Length!=(count<3?0:count)) throw new Exception("Hex slots incorrect");
        }
        var roads=new List<RoadTile> {new RoadTile {cell=Vector2Int.zero,ports=63}};
        foreach(var direction in RoadMap.Directions) {
            var test=new List<RoadTile>(roads);
            if(!RoadMap.CanPlace(test,direction,63,out _)) throw new Exception("Hex direction rejected");
            test.Add(new RoadTile {cell=direction,ports=63,habitat=Habitat.Water});
            if(!RoadMap.Validate(test,out _) || RoadMap.PathToCore(test,direction).Count!=2) throw new Exception("Hex road path failed");
        }
        roads.Add(new RoadTile {cell=RoadMap.Directions[0],ports=63,habitat=Habitat.Water});
        roads.Add(new RoadTile {cell=RoadMap.Directions[1],ports=63,habitat=Habitat.Swamp});
        if(StrategyRules.TerrainSlow(roads,roads[1])<=.25f || StrategyRules.TerrainPoison(roads,roads[2])<=2) throw new Exception("Habitat adjacency failed");
        roads[1].level=2;
        if(StrategyRules.TerrainSlow(roads,roads[1])<=.4f) throw new Exception("Tile level effect failed");
        if(StrategyRules.DirectMultiplier(EnemyKind.Shell)>=1 || StrategyRules.StatusMultiplier(EnemyKind.Cleanser)>=1) throw new Exception("Enemy counter traits absent");
        if(!StrategyRules.CanReach(Kind.Sprout,Vector2Int.zero,Vector3.zero,ForestGame.HexWorld(Vector2Int.right),4.1f)) throw new Exception("Circular range should cross tile boundaries");
        if(!StrategyRules.CanReach(Kind.Owl,Vector2Int.zero,Vector3.zero,ForestGame.HexWorld(Vector2Int.right),8.5f)) throw new Exception("Ranged partner cannot cover neighboring road");
        var team=ForestRules.Partners.Select(k=>new Relic(k)).ToList(); team.Add(new Relic(Kind.Owl) {level=3});
        var units=EcologyRules.Deploy(team,roads,new System.Random(42));
        if(units.Count!=7 || units.Count(d=>d.relic.kind==Kind.Owl && d.relic.level==1)!=1 || units.Count(d=>d.relic.kind==Kind.Owl && d.relic.level==3)!=3) throw new Exception("Individual deployment levels failed");
        var mixed=Enumerable.Range(1,3).Select(n=>new Relic(Kind.Owl) {level=n}).ToList();
        var mixedUnits=EcologyRules.Deploy(mixed,roads,new System.Random(4));
        if(ForestRules.DeploymentLimit(mixed)!=6 || mixedUnits.Count!=6 || Enumerable.Range(1,3).Any(n=>mixedUnits.Count(d=>d.relic.level==n)!=n)) throw new Exception("1+2+3 mixed star counts failed");
        var singles=Enumerable.Range(0,3).Select(_=>new Relic(Kind.Owl)).ToList();
        if(EcologyRules.Deploy(singles,roads,new System.Random(2)).Count!=3) throw new Exception("Duplicate one stars failed");
        foreach(var k in ForestRules.Partners) foreach(Habitat h in Enum.GetValues(typeof(Habitat))) for(int n=1;n<=3;n++) if(EcologyRules.Attack(k,h,n).star!=n) throw new Exception("Star profile overwritten");
        if(StrategyRules.WaveHealth(1,8)<StrategyRules.WaveHealth(1,2)*4 || StrategyRules.Wave(1,8).Count(k=>k==EnemyKind.Siege)!=2) throw new Exception("Late wave escalation absent");
        var random=new System.Random(21); int crossroads=0;
        for(int i=0;i<10000;i++) crossroads+=RoadMap.Draft(Enumerable.Range(0,5).ToList(),random).Count(k=>k==3);
        if(crossroads>1800) throw new Exception("Four-way draft probability too high");
        for(int r=0;r<6;r++) {
            int mask=RoadMap.Rotate(3,r);
            if(EcologyRules.Slots(mask).Length!=0) throw new Exception("Adjacent-edge bend gained slots");
            var curve=RoadCurves.Bend(r,(r+1)%6);
            if(Vector3.Distance(curve[0],ForestGame.HexWorld(RoadMap.Directions[r])*.5f)>.001f || Vector3.Distance(curve.Last(),ForestGame.HexWorld(RoadMap.Directions[(r+1)%6])*.5f)>.001f) throw new Exception("Curve exits not aligned");
        }
        Debug.Log("FOREST_DRAFT_FOUR_WAY " + crossroads + "/30000 cards");
        Debug.Log("FOREST_RULE_CHECKS_OK: hex roundtrip, six edges, all port masks, variants area, spatial merge, adjacency items, terrain synergy, enemy counters, range limits, deployment");
    }
}




