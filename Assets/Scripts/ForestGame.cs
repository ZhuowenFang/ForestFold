using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ForestFold
{
    public partial class ForestGame : MonoBehaviour
    {
        enum Phase { Planning, Battle, Shop, LevelComplete, Won, Lost }
        const int WavesPerMap=8, PicksPerWave=2;
        class Enemy { public LineRenderer slowRing; public GameObject obj; public float hp, maxHp, speed,poisonDps,poisonTime,slow,slowTime; public int segment; public Vector2Int lastTerrain=new Vector2Int(999,999); public float exposedTime,vulnerability,terrainSlow; public EnemyKind kind; public bool boss; public List<Vector3> path; }
        class Ally { public GameObject obj; public Relic relic; public float cooldown,pulse; public Habitat habitat; public HabitatAura aura; public Vector2Int home; }
        class Shot { public GameObject obj; public Vector3 start,target; public float elapsed,duration,damage,multiplier,fan; public Kind kind; public Enemy enemy; public AttackProfile attack; public LineRenderer trail; }
        readonly List<Relic> bag = new List<Relic>();
        readonly List<RoadTile> tiles = new List<RoadTile>();
        readonly List<Enemy> enemies = new List<Enemy>();
        readonly List<Ally> allies = new List<Ally>();
        readonly List<Shot> shots = new List<Shot>();
        readonly List<List<Vector3>> routes = new List<List<Vector3>>();
        readonly int[] roadOffers = new int[3];
        readonly int[] roadRotations = new int[3];
        readonly Habitat[] roadHabitats = new Habitat[3];
        readonly Stack<int[]> roadHistory = new Stack<int[]>();
        int roadChoice = -1, roadRotation;
        bool roadHeld;
        readonly Dictionary<Color, Material> materials = new Dictionary<Color, Material>();
        readonly Kind[] offers = new Kind[4];
        readonly int[] offerShapes=new int[4];
        readonly bool[] sold = new bool[4];
        Phase phase;
        Transform terrain, actors;
        Camera cam;
        Font font;
        GUIStyle label, bold;
        int map = 1, wave, gold, health, width, height, laid, remainingSpawn, totalWave, defeated;
        float spawnTimer, speed = 1, noticeUntil;
        string notice = "", hover = "";
        Relic selected, dragging;
        Vector2Int grabOffset;

        bool paused, help;
        int poisonApplications,slowApplications;
        float poisonDamageDealt,slowedTravelTime;
        System.Random rng = new System.Random();
        const float TileSize = 3.2f, Cell = 48;
        static readonly Color Ink = new Color(.94f,.93f,.86f), Panel = new Color(.96f,.95f,.89f);
        static readonly Color TextColor = new Color(.19f,.28f,.22f), Muted = new Color(.43f,.49f,.40f);
        static readonly Color Mint = new Color(.27f,.49f,.32f), GoldColor = new Color(.64f,.43f,.18f);
        Vector2 Mouse => new Vector2(Input.mousePosition.x / Screen.width * 1600, (Screen.height - Input.mousePosition.y) / Screen.height * 900);

        void Start()
        {
            Application.targetFrameRate = 60;
            font = Font.CreateDynamicFontFromOSFont(new [] { "Microsoft YaHei", "PingFang SC", "Hiragino Sans GB", "Heiti SC", "SimHei", "Arial" }, 20);
            var background = new GameObject("Full Frame Clear").AddComponent<Camera>();
            background.depth = -10;
            background.cullingMask = 0;
            background.clearFlags = CameraClearFlags.SolidColor;
            background.backgroundColor = Ink;
            cam = new GameObject("Map Camera").AddComponent<Camera>();
            cam.orthographic = true;
            cam.transform.rotation = Quaternion.Euler(65, 0, 0);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(.075f,.13f,.13f);
            cam.rect = new Rect(0,.245f,.686f,.60f);
            var light = new GameObject("Sun").AddComponent<Light>();
            light.type = LightType.Directional;
            light.transform.rotation = Quaternion.Euler(55,-30,0);
            light.intensity = 1.1f;
            RenderSettings.ambientLight = new Color(.64f,.74f,.70f);
            terrain = new GameObject("Terrain").transform;
            actors = new GameObject("Actors").transform;
            InitializePolish();
            LoadCultivation(); NewRun();
            if(Environment.GetCommandLineArgs().Contains("-forestBackpackChecks")) StartCoroutine(BackpackChecks());
            if(Environment.GetCommandLineArgs().Contains("-forestStarPreview")) StartCoroutine(StarPreview());
            if(Environment.GetCommandLineArgs().Contains("-forestProgressPreview")) StartCoroutine(ProgressPreview());
            if(Environment.GetCommandLineArgs().Contains("-forestTooltipPreview")) StartCoroutine(TooltipPreview());
            if (Environment.GetCommandLineArgs().Contains("-forestAuraPreview")) StartCoroutine(AuraPreview());
            if (Environment.GetCommandLineArgs().Contains("-forestSmoke")) StartCoroutine(StrategySmoke());
        }

        void NewRun()
        {
            ClearActors(); ResetRunProgress(); cultivationOpen=false;
            bag.Clear(); selected = dragging = null;
            map = 1; wave = 0; gold = 18; health = 20; ResetBackpack();
            paused = help = false; speed = 1;
            AddStarter(Kind.Sprout); AddStarter(Kind.Sprout);
            ResetMap();
            Notify("欢迎来到折叠森林。选 2 块道路，准备迎接第一波暗影。");
        }
        void AddStarter(Kind kind) { var item = new Relic(kind); ForestRules.AutoPlace(bag,item,width,height,bagCells); bag.Add(item); }
        void Notify(string message) { notice = message; noticeUntil = Time.unscaledTime + 5; }
        void ResetMap()
        {
            ClearActors(); tiles.Clear(); wave = 0; laid = 0;
            tiles.Add(new RoadTile { cell = Vector2Int.zero, ports = 63 });
            roadChoice=-1; roadHeld=false; roadHistory.Clear(); rebuildTarget=null; movingTile=null; adjustmentSnapshot=null;
            phase = Phase.Planning;
            RollRoads();
            RebuildTerrain();
        }
        void ClearActors()
        {
            if (actors) foreach (Transform child in actors) Destroy(child.gameObject);
            enemies.Clear(); allies.Clear(); shots.Clear(); routes.Clear();
            sparks.Clear(); words.Clear();
            ClearAttackVisuals();
        }
        Vector3 Position(Vector2Int p) => HexWorld(p);
        Material Mat(Color color)
        {
            if (!materials.TryGetValue(color,out var material)) {
                material = new Material(Resources.Load<Shader>("Forest")); material.color = color;
                materials.Add(color,material);
            }
            return material;
        }
        GameObject Primitive(string name, PrimitiveType type, Vector3 pos, Vector3 scale, Color color, Transform parent)
        {
            var obj = GameObject.CreatePrimitive(type); obj.name = name;
            obj.transform.SetParent(parent,false); obj.transform.position = pos; obj.transform.localScale = scale;
            obj.GetComponent<Renderer>().sharedMaterial = Mat(color);
            var collider = obj.GetComponent<Collider>(); if (collider) Destroy(collider);
            return obj;
        }
        void Road(Vector3 a, Vector3 b)
        {
            var delta = b-a;
            var obj = Primitive("Path",PrimitiveType.Cube,(a+b)*.5f + Vector3.up*.12f,
                new Vector3(.80f,.07f,delta.magnitude+.10f),new Color(.83f,.77f,.55f),terrain);
            obj.transform.rotation = Quaternion.LookRotation(delta);
            for(int i=0;i<4;i++) {
                var point=Vector3.Lerp(a,b,(i+.45f)/4f)+Vector3.up*.175f;
                Primitive("Path stepping stone",PrimitiveType.Cylinder,point,new Vector3(.34f,.009f,.24f),
                    i%2==0?new Color(.91f,.86f,.66f):new Color(.77f,.73f,.52f),terrain);
            }
        }
        void RebuildTerrain()
        {
            foreach (Transform child in terrain) Destroy(child.gameObject);
            for (int i = 0; i < tiles.Count; i++)
            {
                var t = tiles[i]; var p = Position(t.cell);
                BuildTileBase(p,i,t.habitat);
                foreach(var curve in RoadCurves.Paths(t.ports)) DrawRoadMesh(p,curve);
                DecorateTile(terrain,p,t.cell.x*73+t.cell.y*137+211);
                DecorateHabitat(t);
            }
            var basePos = Position(tiles[0].cell);
            BuildSanctuary(basePos);
            foreach (var entrance in RoadMap.Entrances(tiles).Where(e=>e.cell!=Vector2Int.zero)) {
                var portal = Position(entrance.cell) + Position(RoadMap.Directions[entrance.direction])*.46f;
                Primitive("Shadow gate",PrimitiveType.Cylinder,portal+Vector3.up*.22f,new Vector3(.38f,.05f,.38f),new Color(.63f,.50f,.76f),terrain);
                for(int k=0;k<7;k++) {
                    float a=k*Mathf.PI*2/7;
                    Primitive("Portal rune",PrimitiveType.Sphere,portal+new Vector3(Mathf.Cos(a)*.25f,.22f,Mathf.Sin(a)*.25f),Vector3.one*.055f,new Color(.83f,.74f,.98f),terrain);
                }
            }
            FitCamera();
        }
        void FitCamera()
        {
            var visible=tiles.Select(t=>t.cell).Concat(RoadMap.Frontier(tiles)).ToArray();
            float minX=visible.Min(t=>Position(t).x), maxX=visible.Max(t=>Position(t).x);
            float minZ=visible.Min(t=>Position(t).z), maxZ=visible.Max(t=>Position(t).z);
            cam.transform.position = new Vector3((minX+maxX)*.5f,0,(minZ+maxZ)*.5f)-cam.transform.forward*30;
            float aspect = (Screen.width*.686f)/(Screen.height*.60f);
            cam.orthographicSize = Mathf.Max(6,(maxZ-minZ+5.3f)*.5f,(maxX-minX+5.3f)*.5f/aspect);
        }
        void RollRoads()
        {
            var available=Enumerable.Range(0,RoadMap.Templates.Length).Where(k=>RoadMap.Frontier(tiles).Any(p=>
                Enumerable.Range(0,6).Any(r=>RoadMap.CanPlace(tiles,p,RoadMap.Rotate(RoadMap.Templates[k],r),out _)))).OrderBy(_=>rng.Next()).ToList();
            var habitats=Enumerable.Range(0,4).OrderBy(_=>rng.Next()).ToArray();
            var draft=RoadMap.Draft(available,rng);
            for (int i=0;i<3;i++) { roadOffers[i]=draft[i]; roadRotations[i]=rng.Next(6); roadHabitats[i]=(Habitat)habitats[i]; }
            // Keep a habitat-bearing option available; taking it is still the player's choice.

        }
        int SelectedPorts => RoadMap.Rotate(RoadMap.Templates[roadOffers[roadChoice]],roadRotation);
        bool PlaceRoad(Vector2Int cell)
        {
            if(phase==Phase.Planning && rebuildTarget!=null && !paused && !help) return CommitRebuild(cell);
            if (phase!=Phase.Planning || laid>=PicksPerWave || roadChoice<0 || paused || help) return false;
            if (!RoadMap.CanPlace(tiles,cell,SelectedPorts,out var reason)) { Notify(reason); return false; }
            roadHistory.Push(roadOffers.Concat(roadRotations).Concat(roadHabitats.Select(h=>(int)h)).ToArray());
            tiles.Add(new RoadTile {cell=cell,ports=SelectedPorts,habitat=roadHabitats[roadChoice]});
            laid++; roadChoice=-1; roadHeld=false;
            if (laid<PicksPerWave) RollRoads(); else adjustmentSnapshot=CloneRoads();
            RebuildTerrain();
            ParticleBurst(Position(cell)+Vector3.up*.3f,new Color(.88f,.83f,.43f),14);
            Chime(placeSound);
            Notify(laid==PicksPerWave?"进入调整阶段：拖动已有地块，R 旋转，拼好后开战。":"地块已放置，已刷新下一组三选一。");
            return true;
        }
        void Undo()
        {
            if (phase != Phase.Planning || laid == 0 || laid==PicksPerWave) return;
            tiles.RemoveAt(tiles.Count-1); laid--; roadChoice=-1; roadHeld=false;
            var previous=roadHistory.Pop();
            Array.Copy(previous,0,roadOffers,0,3); Array.Copy(previous,3,roadRotations,0,3);
            for(int i=0;i<3;i++) roadHabitats[i]=(Habitat)previous[i+6];
            RebuildTerrain();
        }
        void StartWave()
        {
            if (BuffChoosing || expansionPending>0 || phase != Phase.Planning || laid!=PicksPerWave || dragging != null || roadChoice>=0 || moveHeld || rebuildTarget!=null) return;
            if(!RoadMap.Validate(tiles,out var mapReason)) { Notify(mapReason); return; }
            movingTile=null; ClearActors(); wave++; defeated=0;
            wavePlan=StrategyRules.Wave(map,wave); spawnedWave.Clear(); totalWave=wavePlan.Count;
            remainingSpawn = totalWave; spawnTimer=.5f;
            foreach (var entrance in RoadMap.Entrances(tiles).Where(e=>e.cell!=Vector2Int.zero)) {
                routes.Add(BuildCurveRoute(entrance));
            }
            var deployment=EcologyRules.Deploy(bag,tiles,rng);
            foreach(var assignment in deployment)
                {
                    var item=assignment.relic;
                    var pos=Position(assignment.slot.cell)+new Vector3(assignment.slot.offset.x,.25f,assignment.slot.offset.y);
                    var root=new GameObject(item.Name).transform; root.SetParent(actors); root.position=pos;
                    BuildPartner(root,item.kind);
                    for(int star=0;star<item.level;star++) Part(root,"Star rank",new Vector3((star-(item.level-1)*.5f)*.22f,1.38f,0),Vector3.one*.11f,GoldColor);
                    var ally=new Ally { obj=root.gameObject,relic=item,cooldown=(float)rng.NextDouble()*.6f,habitat=assignment.slot.habitat,home=assignment.slot.cell };
                    ally.aura=CreateHabitatAura(ally); allies.Add(ally);
                }
            phase=Phase.Battle;
            banner="第 "+wave+" 波 · 森林守护开始"; bannerUntil=Time.unscaledTime+2.2f;
            Chime(winSound,.6f);
            int waiting=ForestRules.DeploymentLimit(bag)-allies.Count;
            Notify("出战 "+allies.Count+" 只 · 喜爱强化 "+allies.Count(a=>EcologyRules.Affinity(a.relic.kind,a.habitat)>0)+" · 厌恶削弱 "+allies.Count(a=>EcologyRules.Affinity(a.relic.kind,a.habitat)<0)+(waiting>0?" · 缺位置待命 "+waiting:""));
        }
        void SpawnEnemy()
        {
            var kind=wavePlan[totalWave-remainingSpawn]; bool boss=kind==EnemyKind.Siege;
            var path=routes[rng.Next(routes.Count)];
            var pos=path[0];
            var obj=Primitive(boss?"Shadow guardian":"Shadow",PrimitiveType.Sphere,pos,
                Vector3.one*(boss?.95f:.55f),enemyColors[(int)kind],actors);
            for(int s=-1;s<=1;s+=2) {
                Part(obj.transform,"Shadow eye",new Vector3(s*.11f,.13f,-.23f),new Vector3(.08f,.1f,.05f),new Color(.98f,.79f,.43f));
                Part(obj.transform,"Shadow ear",new Vector3(s*.18f,.27f,.01f),new Vector3(.14f,.27f,.14f),new Color(.38f,.29f,.47f));
            }
            float hp=StrategyRules.WaveHealth(map,wave)*StrategyRules.Health(kind);
            enemies.Add(new Enemy { obj=obj,hp=hp,maxHp=hp,speed=(1.0f+map*.09f+wave*.05f)*1.10f*StrategyRules.Speed(kind),boss=boss,kind=kind,segment=1,path=path });
            DecorateEnemy(enemies[enemies.Count-1]); spawnedWave.Add(kind); remainingSpawn--;
        }
        void Update()
        {
            UpdatePolish();
            if(BuffChoosing || cultivationOpen) return;
            if (Input.GetKeyDown(KeyCode.Escape)) { if(expansionPending>0) CancelExpansion(); else if (movingTile!=null) { movingTile=null; moveHeld=false; } else if (dragging!=null) CancelDrag(); else if (roadChoice>=0) { roadChoice=-1; roadHeld=false; } else paused=!paused; }
            if (Input.GetKeyDown(KeyCode.R) && !paused && !help) {
                if (dragging!=null) RotateSelected();
                else if (movingTile!=null) RotateMoving(); else if (roadChoice>=0) roadRotation=(roadRotation+1)%6; else if(selected!=null) RotateSelected();
            }
            if (Input.GetKeyDown(KeyCode.Space) && !paused && !help) {
                if (phase==Phase.Planning) StartWave(); else if (phase==Phase.Battle) speed=speed==1?2:1;
            }
            if (Input.GetKeyDown(KeyCode.F1)) help=!help;
            if (phase!=Phase.Battle || paused || help) return;
            float dt=Time.deltaTime*speed;
            spawnTimer-=dt;
            if (remainingSpawn>0 && spawnTimer<=0) { SpawnEnemy(); spawnTimer=StrategyRules.SpawnInterval(wave); }
            foreach (var enemy in enemies.ToArray())
            {
                ApplyTerrain(enemy,dt);
                if(enemy.poisonTime>0) { float tick=enemy.poisonDps*Mathf.Min(dt,enemy.poisonTime)*StrategyRules.StatusMultiplier(enemy.kind); enemy.hp-=tick; poisonDamageDealt+=tick; enemy.poisonTime-=dt; }
                if(enemy.slowTime>0) enemy.slowTime-=dt;
                if (enemy.hp<=0) { Kill(enemy); continue; }
                if(enemy.slowTime>0) slowedTravelTime+=dt;
                float movement=enemy.speed*dt*(1-Mathf.Max(enemy.terrainSlow,enemy.slowTime>0?enemy.slow*StrategyRules.StatusMultiplier(enemy.kind):0));
                if(AdvanceEnemy(enemy,movement)) {
                    health-=enemy.boss?3:1; enemies.Remove(enemy); Destroy(enemy.obj);
                    ParticleBurst(Vector3.up*.6f,new Color(.89f,.45f,.30f),12);
                    if(health<=0) { health=0; phase=Phase.Lost; Notify("森林核心失守。这次构筑结束了。"); return; }
                }
            }
            if(BuffChoosing) return;
            float power=(1+bag.Where(r=>r.kind==Kind.Crystal).Sum(r=>.18f*r.level))*RunPower;
            
            foreach (var ally in allies)
            {
                ally.cooldown-=dt;
                if (ally.cooldown>0) continue;
                var attack=PartnerAttack(ally);
                float range=attack.range;
                var target=enemies.Where(e=>e.hp>0 && StrategyRules.CanReach(ally.relic.kind,ally.home,ally.obj.transform.position,e.obj.transform.position,range))
                    .OrderBy(DistanceRemaining).FirstOrDefault();
                if (target==null) continue;
                float unitPower=Mathf.Pow(2.15f,ally.relic.level-1)*power*TrainingPower(ally.relic.kind);
                float damage=attack.damage*unitPower;
                LaunchAttack(ally,target,attack,damage,unitPower);
                ally.cooldown=attack.interval/(ForestRules.Haste(bag,ally.relic)*RunHaste);
            }
            UpdateProjectiles(dt);
            if (remainingSpawn==0 && enemies.Count==0 && shots.Count==0) EndWave();
        }
        void Kill(Enemy enemy)
        {
            gold+=enemy.boss?3:1; defeated++; GainXp(enemy.boss?5:1);
            ParticleBurst(enemy.obj.transform.position,new Color(.84f,.88f,.53f),8);
            enemies.Remove(enemy); Destroy(enemy.obj);
        }
        void EndWave()
        {
            gold+=5; laid=0;
            banner="暗影消散 · 波次净化完成"; bannerUntil=Time.unscaledTime+2.5f; Chime(winSound);
            if(wave==WavesPerMap) {
                ClearActors(); bag.Clear(); selected=dragging=null; pendingBuffs=0;
                trainingPoints+=3; SaveCultivation();
                phase=map<3?Phase.LevelComplete:Phase.Won;
                Notify("本关8波完成！获得3培养点，背包清空，下一关重新构筑。"); return;
            }
            phase=Phase.Shop; RollShop();
            Notify("第 "+wave+" / 8 波净化完成，采购后继续拼图。");
        }
        void RollShop()
        {
            for (int i=0;i<4;i++) { offers[i]=(Kind)rng.Next(bagCells.Count==48?6:7); sold[i]=false; }
            // Always offer at least one partner and one reinforcement item.
            offers[0]=ForestRules.Partners[rng.Next(ForestRules.Partners.Length)];
            if(wave==1) offers[0]=Kind.Owl;
            for(int i=0;i<4;i++) offerShapes[i]=offers[i]==Kind.Expansion?-1:rng.Next(ForestRules.ShapeOptions(ForestRules.Shapes[(int)offers[i]].Length).Length);
            for(int i=0;i<4;i++) { offerLevels[i]=RollPartnerLevel(offers[i]); expansionSizes[i]=Mathf.Min(rng.Next(1,5),48-bagCells.Count); expansionShapes[i]=RollExpansionShape(expansionSizes[i]); }
            // Expansion competes for one of the four stock slots; it is not always stocked.
        }
        void Buy(int i)
        {
            if (phase!=Phase.Shop || sold[i] || dragging!=null || expansionPending>0) return;
            if(offers[i]==Kind.Expansion) {
                if(expansionSizes[i]<1 || gold<ShopCost(i) || bagCells.Count+expansionSizes[i]>48) return;
                if(!CanFitExpansion(expansionShapes[i])) { Notify("当前背包边缘放不下这个固定形状，不扣钱。"); return; }
                expansionPaid=ShopCost(i); gold-=expansionPaid; sold[i]=true; expansionPending=expansionSizes[i]; pendingExpansionShape=expansionShapes[i].ToArray(); expansionShopIndex=i;
                
                Notify("已拿起 "+expansionPending+" 格扩容块，移动到背包边缘并松手放下。Esc取消退款。"); return;
            }
            var item=new Relic(offers[i]) {shape=offerShapes[i],level=offerLevels[i]}; int cost=ShopCost(i);
            if (gold<cost) { Notify("金币不足。"); return; }
            if (!ForestRules.AutoPlace(bag,item,width,height,bagCells)) { Notify("背包放不下：整理、出售或扩容后再购买。"); return; }
            gold-=cost; bag.Add(item); sold[i]=true; selected=item;
            Notify("已购买"+item.Name+"，并放入背包。");
        }
        bool CanMerge => ForestRules.MergeSet(bag,selected).Count==3;
        void Merge() => MergeConnected();
        void LeaveShop()
        {
            if(phase!=Phase.Shop || dragging!=null || expansionPending>0) return;
            ClearActors(); movingTile=null; moveHeld=false; phase=Phase.Planning; laid=0; roadHistory.Clear(); RollRoads(); Notify("继续选择 2 块地块。直路、弯道没有生成位。");
        }
        void NextLevel()
        {
            if(phase!=Phase.LevelComplete) return;
            map++; ResetRunProgress(); bag.Clear(); selected=dragging=null; gold=18; health=20; ResetBackpack();
            AddStarter(Kind.Sprout); AddStarter(Kind.Sprout); ResetMap();
            Notify("全新关卡：背包、金币、生命和容量重新开始，每波选择2块地块。");
        }
        void CancelDrag() { CancelBagDrag(); }

        void Box(Rect rect, Color color) { var previous=GUI.color; GUI.color=color; GUI.DrawTexture(rect,Texture2D.whiteTexture); GUI.color=previous; }
        void Text(Rect rect,string text,int size=18,Color? color=null,bool strong=false)
        {
            var style=strong?bold:label; style.fontSize=size; style.normal.textColor=color??TextColor;
            GUI.Label(rect,text,style);
        }
        bool Button(Rect rect,string title,bool enabled=true,Color? accent=null)
        {
            bool over=rect.Contains(Event.current.mousePosition);
            enabled &= GUI.enabled;
            Color c=accent??new Color(.85f,.89f,.78f);
            Card(rect,enabled?(over?Color.Lerp(c,Color.white,.12f):c):new Color(.89f,.89f,.82f),8);
            Text(new Rect(rect.x+12,rect.y+(rect.height-27)/2,rect.width-24,29),title,17,
                !enabled?new Color(.58f,.62f,.53f):accent.HasValue?Cream:DeepGreen,true);
            bool clicked=enabled && GUI.Button(rect,GUIContent.none,GUIStyle.none);
            if(clicked) Chime(clickSound,.5f);
            return clicked;
        }
        void OnGUI()
        {
            if (font==null) return;
            if (label==null) {
                label=new GUIStyle(GUI.skin.label) { font=font,wordWrap=true,richText=false };
                bold=new GUIStyle(label) { fontStyle=FontStyle.Bold };
            }
            GUI.matrix=Matrix4x4.Scale(new Vector3(Screen.width/1600f,Screen.height/900f,1));
            GUI.enabled=!BuffChoosing && !cultivationOpen;
            hover="";
            if (phase==Phase.Planning) DrawMapPlacement();
            DrawAtmosphere();
            DrawEcologyOverlay(); DrawForecast(); DrawRangePreview();
            DrawHeader();
            Box(new Rect(1100,124,500,776),Panel);
            Box(new Rect(1100,124,2,776),new Color(.79f,.81f,.69f));
            DrawBag();
            Box(new Rect(0,680,1100,220),Ink);
            bool controls=!paused && !help && !BuffChoosing && !cultivationOpen;
            GUI.enabled=controls;
            if (phase==Phase.Planning) DrawPlanning();
            if (phase==Phase.Battle) DrawBattle();
            if (phase==Phase.Shop) DrawShop();
            if (phase==Phase.Won || phase==Phase.Lost || phase==Phase.LevelComplete) DrawEnd();
            GUI.enabled=true;
            DrawWorldLabels();
            if (Time.unscaledTime<noticeUntil) {
                Card(new Rect(24,630,1050,38),new Color(.96f,.94f,.83f,.94f),9);
                Text(new Rect(36,636,1028,30),notice,16,DeepGreen);
            }
            if (dragging!=null && !paused && !help && !BuffChoosing && !cultivationOpen) DrawDrag();
            DrawExpansionDrag();
            DrawTerrainLinks();
            DrawHoverBubble();
            if ((paused || help) && !cultivationOpen && !BuffChoosing) DrawModal();
            GUI.enabled=true;
            if(BuffChoosing) DrawBuffChoice();
            if(cultivationOpen) DrawCultivation();
        }
        string PhaseName() => phase==Phase.Planning?"道路规划":phase==Phase.Battle?"自动守护":phase==Phase.Shop?"林间商店":phase==Phase.Won?"章节完成":phase==Phase.LevelComplete?"关卡完成":"本局结束";
        void DrawPlanning()
        {
            if(laid==PicksPerWave && rebuildTarget==null) { DrawAdjustmentPanel(); return; }
            Text(new Rect(26,690,740,34),rebuildTarget!=null?"付费重建 · 放回高亮地块":laid<PicksPerWave?"地块三选一  ·  第 "+(laid+1)+" / 2 次":"本轮地图已拼好  ·  2 / 2",24,null,true);
            Text(new Rect(26,726,750,25),"R 旋转 · 圆形 + 为生成位 · 直路/弯道无位置 · 生态匹配更强",16,Muted);
            for (int i=0;i<3;i++) DrawRoadCard(i,new Rect(26+i*244,762,230,116));
            if(rebuildTarget!=null) { if(Button(new Rect(784,696,288,40),"取消重建 · 不扣钱")) { rebuildTarget=null; roadChoice=-1; roadHeld=false; } }
            else if (Button(new Rect(784,696,288,40),"撤回本轮上一块",laid>0 && roadChoice<0 && dragging==null)) Undo();
            if (Button(new Rect(784,750,288,43),"旋转选中地块  [R]",roadChoice>=0)) roadRotation=(roadRotation+1)%6;
            if (Button(new Rect(784,810,288,67),"开始第 "+(wave+1)+" 波  [空格]",laid==PicksPerWave && dragging==null,new Color(.26f,.44f,.29f))) StartWave();
        }
        void DrawBattle()
        {
            Text(new Rect(26,696,600,40),"森林正在守护你",26,Mint,true);
            Text(new Rect(26,740,730,36),"场上暗影 "+enemies.Count+"    待出现 "+remainingSpawn+"    已净化 "+defeated+"    出战伙伴 "+allies.Count,20);
            Box(new Rect(26,799,710,10),new Color(.2f,.28f,.24f));
            Box(new Rect(26,799,710*(totalWave-remainingSpawn-enemies.Count)/(float)totalWave,10),Mint);
            if (Button(new Rect(780,747,290,60),"战斗速度  ×"+speed+"  [空格]")) speed=speed==1?2:1;
            DrawAttackLegend();
        }
        void DrawShop()
        {
            Text(new Rect(26,693,760,34),"波次商店  ·  地图 "+map+" / 第 "+wave+" 波已净化",23,Mint,true);
            for (int i=0;i<4;i++) {
                var k=offers[i]; var rect=new Rect(26+i*196,732,184,145);
                Card(rect,Cream); Portrait(new Rect(rect.x+3,rect.y+1,48,45),k);
                Text(new Rect(rect.x+49,rect.y+11,132,26),(k==Kind.Expansion?"扩容 "+expansionSizes[i]+" 格":ForestRules.Names[(int)k])+(ForestRules.IsPartner(k)?" Lv."+offerLevels[i]:""),14,DeepGreen,true);
                if (Button(new Rect(rect.x+10,rect.y+93,164,42),sold[i]?"已售罄":k==Kind.Expansion && bagCells.Count+expansionSizes[i]>48?"容量已满":"购买  $"+ShopCost(i),!sold[i] && gold>=ShopCost(i) && dragging==null && expansionPending==0 && !(k==Kind.Expansion && bagCells.Count+expansionSizes[i]>48))) Buy(i);
                if(k==Kind.Expansion && expansionShapes[i]!=null) foreach(var p in expansionShapes[i]) Round(new Rect(rect.x+66+p.x*14,rect.y+40+p.y*14,12,12),Mint,2);
                var sample=new Relic(k) {shape=offerShapes[i]};
                foreach(var cell in sample.Cells()) Round(new Rect(rect.x+66+cell.x*11,rect.y+43+cell.y*11,10,10),sample.Color,1);
                if (rect.Contains(HoverPointer)) ShowHover(ForestRules.Names[(int)k]+" Lv."+offerLevels[i]+" · $"+ShopCost(i)+"\n"+(k==Kind.Expansion?"购买 "+expansionSizes[i]+" 格扩容，每格3金币。\n按展示的形状整块拖放到背包边缘，不旋转、不拆分；斜角不算相邻。":ForestRules.Detail(k)+"\n卡片中部显示本件占格形状。"),rect);
            }
            if (Button(new Rect(824,735,246,44),"刷新商品  $2",gold>=2 && dragging==null && expansionPending==0)) { gold-=2; RollShop(); }
            if (Button(new Rect(824,792,246,48),"继续拼图 · 第 "+(wave+1)+" / 8 波",dragging==null && expansionPending==0,new Color(.26f,.44f,.29f))) LeaveShop();
            Text(new Rect(824,849,246,42),"扩展片随机上架\n本波采购后继续拼图",15,Muted);
        }
        void DrawEnd()
        {
            Text(new Rect(26,700,700,40),phase==Phase.LevelComplete?"本关8波完成 · 下一关重新构筑":phase==Phase.Won?"微光重现 · 本章净化完成":"核心失守 · 再试一种构筑",28,phase==Phase.Won?Mint:GoldColor,true);
            Text(new Rect(26,750,700,64),"抵达地图 "+map+" / 3   ·   剩余生命 "+health+"   ·   金币 "+gold+"\n每关8波，背包独立。下一关重新获得初始伙伴、18金币、20生命和4×4背包。",19,Muted);
            if (Button(new Rect(804,750,266,62),phase==Phase.LevelComplete?"进入下一关":"开始新的冒险",true,new Color(.26f,.44f,.29f))) { if(phase==Phase.LevelComplete) NextLevel(); else NewRun(); }
        }
        void DrawBag()
        {
            bool editable=(phase==Phase.Planning || phase==Phase.Shop) && !paused && !help && !BuffChoosing && !cultivationOpen && roadChoice<0 && expansionPending==0;
            Text(new Rect(1124,143,410,36),"旅人的背包",27,null,true);
            int used=bag.Sum(r=>r.Cells().Count);
            Text(new Rect(1124,186,460,30),"已用 "+used+" / "+bagCells.Count+" 格  ·  "+ForestRules.ActiveSpecies(bag).Count+" 种 / 最多 "+ForestRules.DeploymentLimit(bag)+" 只",18,Muted);
            float bx=1124, by=236;
            Card(new Rect(1114,226,404,307),new Color(.78f,.82f,.68f),14);
            Round(new Rect(1540,236,12,286),new Color(.82f,.85f,.74f),6);
            Round(new Rect(1540,236,12,286*used/bagCells.Count),Mint,6);
            for (int y=0;y<6;y++) for (int x=0;x<8;x++) {
                var rect=new Rect(bx+x*Cell,by+y*Cell,Cell-3,Cell-3);
                var cell=new Vector2Int(x,y); bool owned=bagCells.Contains(cell);
                Round(rect,owned?new Color(.90f,.91f,.81f):new Color(.72f,.76f,.65f),5);
                if(!owned) Text(new Rect(rect.x+16,rect.y+10,24,26),"·",19,Muted);
            }
            foreach (var item in bag)
            {
                if (item==dragging) continue;
                bool first=true;
                foreach (var cell in item.Cells()) {
                    var p=item.anchor+cell;
                    var rect=new Rect(bx+p.x*Cell,by+p.y*Cell,Cell-3,Cell-3);
                    Round(rect,Color.Lerp(item.Color,Cream,.35f),5);
                    if(selected==item) Outline(new Rect(rect.x+1,rect.y+1,rect.width-2,rect.height-2),Mint,1);
                    if (first) {
                        Portrait(new Rect(rect.x-2,rect.y-2,48,48),item.kind);
                        if(item.Partner) { Round(new Rect(rect.x+31,rect.y+30,14,15),Cream,4); Text(new Rect(rect.x+33,rect.y+28,18,22),item.level.ToString(),12,DeepGreen,true); }
                        first=false;
                    }
                    if (rect.Contains(HoverPointer)) {
                        ShowHover(item.Name+"  Lv."+item.level+"\n"+ForestRules.Detail(item.kind)+(item.Partner?"\n"+EcologyRules.StarDetail(item.kind,item.level):""),rect);
                        if (Event.current.type==EventType.MouseDown && Event.current.button==0 && editable && dragging==null) {
                            BeginBagDrag(item,cell); Event.current.Use();
                        }
                    }
                }
            }
            DrawBagLinks();
            Text(new Rect(1124,536,450,52),expansionPending>0?"手持扩容块："+expansionPending+" 格，整块移动后松手\n形状固定；边缘相连；Esc取消退款。":editable?"拖动交换 · R旋转 $1 · Esc撤销交换\n风铃金边=邻接生效；同种同星三件可合成":"战斗中背包已锁定，波次结束后可整理",16,Muted);
            GUI.enabled=editable;
            if (Button(new Rect(1124,597,212,43),"三合一进化",CanMerge && dragging==null)) Merge();
            if (Button(new Rect(1348,597,222,43),"出售选中  +$"+(selected==null?0:Mathf.Max(1,ForestRules.Cost(selected.kind)/2)*selected.level),selected!=null && dragging==null && (!selected.Partner || bag.Count(r=>r.Partner)>1))) {
                gold+=Mathf.Max(1,ForestRules.Cost(selected.kind)/2)*selected.level; bag.Remove(selected); selected=null;
            }
            if(Button(new Rect(1124,650,212,42),"重掷形状 $3",selected!=null && dragging==null && gold>=3)) RerollShape();
            if(Button(new Rect(1348,650,222,42),"旋转90° $1",selected!=null && gold>=1)) RotateSelected();
            GUI.enabled=true;
            Text(new Rect(1124,711,450,28),"本局的森林羁绊",18,Mint,true);
            Card(new Rect(1124,748,212,41),new Color(.87f,.91f,.81f),8);
            Card(new Rect(1348,748,222,41),new Color(.93f,.88f,.74f),8);
            Text(new Rect(1135,756,201,30),"伤害  +"+bag.Where(r=>r.kind==Kind.Crystal).Sum(r=>18*r.level)+"%",18);
            Text(new Rect(1359,756,205,30),"邻接风铃  +25%",18);
            DrawRunProgress();
        }
        void DrawDrag()
        {
            var m=Event.current.mousePosition;
            var anchor=new Vector2Int(Mathf.FloorToInt((m.x-1124)/Cell),Mathf.FloorToInt((m.y-236)/Cell))-grabOffset;
            bool valid=CanDropAt(anchor,out var overlap);
            foreach (var cell in dragging.Cells()) {
                var p=anchor+cell;
                Round(new Rect(1124+p.x*Cell,236+p.y*Cell,Cell-3,Cell-3),valid?Color.Lerp(dragging.Color,Cream,.2f):new Color(.9f,.3f,.3f,.8f),5);
            }
            if (Event.current.type==EventType.MouseUp && Event.current.button==0) {
                if (valid) { DropAt(anchor); Chime(placeSound,.4f); }
                else { Notify("无法放置：只能覆盖一个物品，且必须在背包内。仍拿在手上，Esc撤销。"); }
                Event.current.Use();
            }
        }
        void DrawWorldLabels()
        {
            if (tiles.Count==0) return;
            WorldTag(Position(tiles[0].cell),"森林核心",Mint);
            if (phase!=Phase.Planning) foreach (var entry in RoadMap.Entrances(tiles).Where(e=>e.cell!=Vector2Int.zero)) {
                var location=Position(entry.cell)+Position(RoadMap.Directions[entry.direction])*.45f;
                if(Vector2.Distance(MapScreen(location),Event.current.mousePosition)<23)
                    ShowHover("暗影入口\n敌人从这里沿道路前往森林核心。",new Rect(MapScreen(location)-Vector2.one*12,Vector2.one*24));
            }
            if (phase==Phase.Battle)
                foreach (var e in enemies) {
                    var p=cam.WorldToViewportPoint(e.obj.transform.position);
                    float x=p.x*1097.6f, y=(1-(.245f+p.y*.60f))*900;
                    Box(new Rect(x-15,y-22,30,4),Ink);
                    Box(new Rect(x-15,y-22,30*Mathf.Clamp01(e.hp/e.maxHp),4),new Color(.93f,.48f,.58f));
                }
        }
        void WorldTag(Vector3 pos,string text,Color color)
        {
            var p=cam.WorldToViewportPoint(pos);
            float x=p.x*1097.6f, y=(1-(.245f+p.y*.60f))*900;
            Round(new Rect(x-51,y+23,102,27),new Color(.98f,.96f,.86f,.94f),6);
            Text(new Rect(x-44,y+26,96,28),text,15,color,true);
        }
        void DrawModal()
        {
            Box(new Rect(0,0,1600,900),new Color(.025f,.045f,.04f,.93f));
            Box(new Rect(360,180,880,530),Panel);
            Text(new Rect(410,218,780,50),help?"如何守护这片森林":"冒险已暂停",32,Mint,true);
            Text(new Rect(410,287,780,322),
                "1  拼图：每次从 3 张地块选 1 张，拖入地图，R 旋转。\n     选完2块可移动/旋转；选中后花钱重建或合成地块。\n\n"+
                "2  背包：每件1/2/3星伙伴分别生成1/2/3只，逐件累加。\n     同种同级三合一；选中后可花3金重掷同格数形状。\n\n"+
                "3  生态：直路弯道无生成位；六边分叉提供 3 / 4 个位置。\n     喜爱改变技能，厌恶削弱，其他正常；多余伙伴待命。\n\n"+
                "4  商店：每波开放，扩展片为随机商品，购买后售罄。\n     每图8波；通关清空背包，下关从头构筑。",20);
            if (Button(new Rect(410,636,780,48),"回到森林",true,new Color(.26f,.44f,.29f))) { help=false; paused=false; }
        }

    }
}



















