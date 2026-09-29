using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ForestFold
{
    public partial class ForestGame
    {
        readonly Dictionary<Kind,Texture2D> portraits=new Dictionary<Kind,Texture2D>();
        readonly List<Spark> sparks=new List<Spark>();
        readonly List<FloatingWord> words=new List<FloatingWord>();
        class Spark { public Transform obj; public Vector3 velocity; public float life,maximum; }
        class FloatingWord { public Vector3 pos; public string text; public Color color; public float life=1; }
        AudioSource sound;
        AudioClip clickSound, placeSound, hitSound, winSound;
        bool muted;
        float nextHitSound, bannerUntil;
        string banner="";
        Transform backdrop;
        static readonly Color DeepGreen=new Color(.16f,.29f,.22f), Cream=new Color(.99f,.97f,.88f);

        void InitializePolish()
        {
            cam.gameObject.AddComponent<AudioListener>();
            cam.cullingMask=~(1<<9);
            sound=gameObject.AddComponent<AudioSource>(); sound.playOnAwake=false; sound.volume=.22f;
            clickSound=Tone(660,.07f); placeSound=Tone(440,.19f); hitSound=Tone(880,.07f); winSound=Tone(523.25f,.7f);
            InitializeAttackVisuals();
            var art=Resources.Load<Texture2D>("ForestBackdrop");
            if (art!=null) {
                var obj=GameObject.CreatePrimitive(PrimitiveType.Quad); obj.name="Painted forest clearing";
                Destroy(obj.GetComponent<Collider>()); backdrop=obj.transform; backdrop.SetParent(cam.transform,false);
                backdrop.localPosition=new Vector3(0,0,65);
                var mat=new Material(Resources.Load<Shader>("PaintedBackdrop")); mat.mainTexture=art;
                obj.GetComponent<Renderer>().sharedMaterial=mat;
            }
            for (int i=0;i<7;i++) CreatePortrait((Kind)i);
        }
        AudioClip Tone(float frequency,float duration)
        {
            const int rate=22050;
            var samples=new float[(int)(rate*duration)];
            for(int i=0;i<samples.Length;i++) {
                float t=i/(float)rate, envelope=Mathf.Sin(Mathf.PI*i/samples.Length)*Mathf.Exp(-t*5);
                samples[i]=(Mathf.Sin(2*Mathf.PI*frequency*t)+.25f*Mathf.Sin(2*Mathf.PI*frequency*2*t))*envelope*.3f;
            }
            var clip=AudioClip.Create("Forest chime",samples.Length,1,rate,false); clip.SetData(samples,0); return clip;
        }
        void Chime(AudioClip clip,float volume=1) { if (!muted && sound && clip) sound.PlayOneShot(clip,volume); }
        GameObject Part(Transform root,string name,Vector3 offset,Vector3 size,Color color,PrimitiveType type=PrimitiveType.Sphere)
            => Primitive(name,type,root.position+offset,size,color,root);
        void Eyes(Transform root,float y,float z,float separation=.14f)
        {
            for(int s=-1;s<=1;s+=2) {
                Part(root,"Cream eye",new Vector3(s*separation,y,z),new Vector3(.19f,.23f,.12f),Cream);
                Part(root,"Eye pupil",new Vector3(s*separation,y-.006f,z-.057f),new Vector3(.086f,.12f,.055f),DeepGreen);
                Part(root,"Eye glint",new Vector3(s*separation-.02f,y+.025f,z-.086f),Vector3.one*.031f,Color.white);
                Part(root,"Rosy cheek",new Vector3(s*(separation+.10f),y-.14f,z+.035f),new Vector3(.12f,.055f,.05f),new Color(.95f,.66f,.56f));
            }
        }
        void BuildPartner(Transform root,Kind kind)
        {
            Color green=new Color(.44f,.69f,.32f), coral=new Color(.83f,.38f,.30f), purple=new Color(.48f,.47f,.68f);
            if (kind==Kind.Sprout) {
                Part(root,"Terracotta pot",new Vector3(0,.16f,0),new Vector3(.48f,.18f,.48f),new Color(.69f,.40f,.25f),PrimitiveType.Cylinder);
                Part(root,"Soil",new Vector3(0,.34f,0),new Vector3(.44f,.035f,.44f),new Color(.30f,.26f,.14f),PrimitiveType.Cylinder);
                Part(root,"Stem",new Vector3(0,.49f,0),new Vector3(.13f,.25f,.13f),green,PrimitiveType.Cylinder);
                var leaf=Part(root,"Left leaf",new Vector3(-.27f,.48f,0),new Vector3(.55f,.13f,.23f),green);
                leaf.transform.rotation=Quaternion.Euler(0,0,-24);
                leaf=Part(root,"Right leaf",new Vector3(.27f,.56f,0),new Vector3(.51f,.13f,.24f),green);
                leaf.transform.rotation=Quaternion.Euler(0,0,30);
                Part(root,"Seedling head",new Vector3(0,.82f,0),new Vector3(.61f,.57f,.55f),new Color(.67f,.84f,.42f));
                Part(root,"Crown leaf",new Vector3(.09f,1.12f,.02f),new Vector3(.21f,.28f,.15f),green);
                Eyes(root,.84f,-.25f,.13f);
            } else if(kind==Kind.Owl) {
                Part(root,"Perch",new Vector3(0,.1f,0),new Vector3(.72f,.17f,.40f),new Color(.48f,.34f,.22f),PrimitiveType.Cylinder);
                Part(root,"Owl body",new Vector3(0,.50f,0),new Vector3(.70f,.82f,.54f),purple);
                Part(root,"Feather bib",new Vector3(0,.38f,-.22f),new Vector3(.44f,.45f,.15f),new Color(.85f,.82f,.69f));
                Part(root,"Face",new Vector3(0,.75f,-.16f),new Vector3(.59f,.45f,.28f),new Color(.88f,.85f,.72f));
                for(int s=-1;s<=1;s+=2) {
                    var wing=Part(root,"Wing",new Vector3(s*.37f,.5f,.02f),new Vector3(.28f,.51f,.25f),purple*.83f);
                    wing.transform.rotation=Quaternion.Euler(0,0,s*18);
                    Part(root,"Ear tuft",new Vector3(s*.24f,1.0f,0),new Vector3(.19f,.30f,.19f),purple);
                }
                Eyes(root,.78f,-.32f,.14f);
                Part(root,"Beak",new Vector3(0,.64f,-.37f),new Vector3(.115f,.16f,.12f),GoldColor);
            } else if(kind==Kind.Mushroom) {
                Part(root,"Mushroom stem",new Vector3(0,.31f,0),new Vector3(.43f,.57f,.42f),new Color(.96f,.88f,.67f));
                Part(root,"Mushroom cap",new Vector3(0,.70f,0),new Vector3(.96f,.52f,.83f),coral);
                for(int i=0;i<6;i++) {
                    float a=i*Mathf.PI/3;
                    Part(root,"Cap spots",new Vector3(Mathf.Cos(a)*.27f,.89f,Mathf.Sin(a)*.21f),new Vector3(.13f,.052f,.12f),Cream);
                }
                Eyes(root,.34f,-.19f,.10f);
            } else if(kind==Kind.Frog) {
                var jade=new Color(.30f,.68f,.53f);
                Part(root,"Frog body",new Vector3(0,.30f,0),new Vector3(.72f,.45f,.59f),jade);
                Part(root,"Pale belly",new Vector3(0,.23f,-.21f),new Vector3(.48f,.27f,.22f),new Color(.80f,.90f,.62f));
                for(int s=-1;s<=1;s+=2) {
                    Part(root,"Frog eye mount",new Vector3(s*.23f,.57f,-.10f),Vector3.one*.29f,jade);
                    Part(root,"Webbed foot",new Vector3(s*.36f,.10f,-.16f),new Vector3(.35f,.12f,.29f),jade);
                }
                Eyes(root,.6f,-.26f,.23f);
                Part(root,"Water pearl",new Vector3(0,.29f,-.42f),Vector3.one*.15f,new Color(.53f,.85f,.96f));
            } else if(kind==Kind.Crystal) {
                Part(root,"Crystal base",new Vector3(0,.10f,0),new Vector3(.67f,.14f,.56f),new Color(.64f,.66f,.51f),PrimitiveType.Cylinder);
                for(int i=-1;i<=1;i++) {
                    var gem=Part(root,"Magic crystal",new Vector3(i*.22f,.48f,0),new Vector3(.25f,i==0?.88f:.55f,.25f),new Color(.36f,.77f,.78f),PrimitiveType.Cube);
                    gem.transform.rotation=Quaternion.Euler(15,35,i*-17);
                }
            } else if(kind==Kind.Bell) {
                Part(root,"Bell crown",new Vector3(0,.71f,0),new Vector3(.16f,.19f,.16f),new Color(.62f,.42f,.16f));
                Part(root,"Bell",new Vector3(0,.46f,0),new Vector3(.60f,.54f,.58f),new Color(.97f,.76f,.35f));
                Part(root,"Bell rim",new Vector3(0,.24f,0),new Vector3(.67f,.06f,.65f),new Color(.75f,.52f,.20f),PrimitiveType.Cylinder);
                Part(root,"Bell tongue",new Vector3(0,.13f,0),Vector3.one*.17f,DeepGreen);
            } else {
                Part(root,"Backpack",new Vector3(0,.48f,0),new Vector3(.66f,.77f,.37f),new Color(.68f,.49f,.27f),PrimitiveType.Cube);
                Part(root,"Bag flap",new Vector3(0,.72f,-.21f),new Vector3(.64f,.22f,.10f),new Color(.84f,.68f,.40f),PrimitiveType.Cube);
                Part(root,"Bag buckle",new Vector3(0,.54f,-.24f),new Vector3(.12f,.17f,.07f),GoldColor,PrimitiveType.Cube);
            }
        }
        void CreatePortrait(Kind kind)
        {
            var root=new GameObject("Portrait "+kind).transform; root.position=new Vector3(1000+(int)kind*4,0,0);
            BuildPartner(root,kind);
            foreach(Transform child in root.GetComponentsInChildren<Transform>()) child.gameObject.layer=9;
            var camera=new GameObject("Portrait camera").AddComponent<Camera>();
            camera.enabled=false; camera.cullingMask=1<<9; camera.orthographic=true; camera.orthographicSize=.75f;
            camera.clearFlags=CameraClearFlags.SolidColor; camera.backgroundColor=Color.clear;
            camera.transform.position=root.position+new Vector3(0,1.5f,-3);
            camera.transform.LookAt(root.position+Vector3.up*.53f);
            var texture=new RenderTexture(160,160,16,RenderTextureFormat.ARGB32); texture.antiAliasing=4;
            camera.targetTexture=texture;
            StartCoroutine(CapturePortrait(kind,root,camera,texture));
        }
        System.Collections.IEnumerator CapturePortrait(Kind kind,Transform root,Camera camera,RenderTexture texture)
        {
            yield return new WaitForEndOfFrame();
            camera.Render();
            var previous=RenderTexture.active; RenderTexture.active=texture;
            var icon=new Texture2D(160,160,TextureFormat.RGBA32,false);
            icon.ReadPixels(new Rect(0,0,160,160),0,0); icon.Apply(); RenderTexture.active=previous;
            portraits.Add(kind,icon);
            int visible=icon.GetPixels32().Count(c=>c.a>0);
            Debug.Log("FOREST_PORTRAIT "+kind+" visiblePixels="+visible);
            camera.targetTexture=null; texture.Release(); Destroy(texture);
            Destroy(camera.gameObject); Destroy(root.gameObject);
        }
        void Portrait(Rect rect,Kind kind)
        {
            if (portraits.TryGetValue(kind,out var texture)) GUI.DrawTexture(rect,texture,ScaleMode.ScaleToFit,true);
        }
        void Round(Rect rect,Color color,float radius=10)
        {
            GUI.DrawTexture(rect,Texture2D.whiteTexture,ScaleMode.StretchToFill,true,0,color,0,radius);
        }
        void Card(Rect rect,Color color,float radius=12)
        {
            Round(new Rect(rect.x,rect.y+4,rect.width,rect.height),new Color(.12f,.20f,.15f,.14f),radius);
            Round(rect,color,radius);
        }
        void ParticleBurst(Vector3 pos,Color color,int count=8)
        {
            for(int i=0;i<count;i++) {
                float angle=i*Mathf.PI*2/count;
                var obj=Primitive("Magic mote",PrimitiveType.Sphere,pos,Vector3.one*.10f,color,actors);
                sparks.Add(new Spark {obj=obj.transform,velocity=new Vector3(Mathf.Cos(angle),1.8f,Mathf.Sin(angle))*1.3f,life=.6f,maximum=.6f});
            }
        }
        void UpdatePolish()
        {
            if (backdrop) backdrop.localScale=new Vector3(cam.orthographicSize*cam.aspect*2,cam.orthographicSize*2,1);
            if(paused || help || BuffChoosing || cultivationOpen) return;
            float dt=Time.deltaTime;
            UpdateAttackVisuals(dt*(phase==Phase.Battle?speed:1));
            UpdateTerrainFeedback();
            foreach(var s in sparks.ToArray()) {
                if(!s.obj) { sparks.Remove(s); continue; }
                s.life-=dt; s.obj.position+=s.velocity*dt; s.velocity+=Vector3.down*3*dt;
                s.obj.localScale=Vector3.one*.13f*Mathf.Clamp01(s.life/s.maximum);
                if(s.life<=0) { Destroy(s.obj.gameObject); sparks.Remove(s); }
            }
            foreach(var w in words.ToArray()) { w.life-=dt; w.pos+=Vector3.up*dt*.5f; if(w.life<=0) words.Remove(w); }
            foreach(var a in allies) if(a.obj) {
                UpdateHabitatAura(a.aura,dt);
                a.pulse=Mathf.Max(0,a.pulse-dt*(phase==Phase.Battle?speed:1));
                float bob=Mathf.Sin(Time.time*2.5f+a.obj.GetInstanceID())*.035f;
                float kick=a.pulse/.22f;
                a.obj.transform.localScale=new Vector3(1-bob*.4f+kick*.12f,1+bob-kick*.13f,1-bob*.4f+kick*.12f);
            }
        }
        void DrawAtmosphere()
        {
            for(int i=0;i<18;i++) {
                float x=45+Mathf.Repeat(i*137.1f+Time.time*3,1000);
                float y=160+Mathf.Repeat(i*83.7f,430)+Mathf.Sin(Time.time*.6f+i)*12;
                float alpha=.18f+.16f*Mathf.Sin(Time.time*1.4f+i);
                Round(new Rect(x-3,y-3,9,9),new Color(1,.92f,.63f,alpha*.3f),5);
                Round(new Rect(x,y,3,3),new Color(1,.95f,.71f,alpha),2);
            }
            foreach(var w in words) {
                var pos=MapScreen(w.pos);
                Text(new Rect(pos.x-35,pos.y-20,100,30),w.text,18,new Color(w.color.r,w.color.g,w.color.b,w.life),true);
            }
            if(Time.unscaledTime<bannerUntil) {
                float a=Mathf.Clamp01((bannerUntil-Time.unscaledTime)*2);
                Card(new Rect(285,160,520,68),new Color(Cream.r,Cream.g,Cream.b,a));
                Text(new Rect(315,177,475,45),banner,25,Mint,true);
            }
        }
        void DrawHeader()
        {
            Box(new Rect(0,0,1600,124),Ink);
            Box(new Rect(0,122,1600,2),new Color(.79f,.81f,.68f));
            Portrait(new Rect(16,11,92,92),Kind.Sprout);
            Text(new Rect(108,15,290,46),"折叠森林",34,DeepGreen,true);
            Text(new Rect(110,64,300,29),"F O R E S T   F O L D",14,Muted);
            Text(new Rect(441,22,320,35),"微光林地",25,DeepGreen,true);
            Text(new Rect(441,65,350,28),"第一章  /  地图 "+map+" · 波次 "+wave+" / 8",16,Muted);
            for(int i=0;i<3;i++) Round(new Rect(442+i*33,102,25,4),i<map?Mint:new Color(.78f,.82f,.70f),2);
            Card(new Rect(825,21,240,79),new Color(.88f,.91f,.78f));
            Text(new Rect(842,31,215,26),"森林核心",15,Muted);
            Text(new Rect(842,57,215,36),health+" / 20",25,health<7?new Color(.72f,.29f,.22f):DeepGreen,true);
            Round(new Rect(963,63,82,7),new Color(.69f,.76f,.59f),3);
            Round(new Rect(963,63,82*health/20f,7),Mint,3);
            Card(new Rect(1090,21,205,79),new Color(.95f,.88f,.65f));
            Text(new Rect(1108,31,176,25),"旅途金币",15,GoldColor);
            Text(new Rect(1108,57,176,36),gold.ToString("D2"),25,GoldColor,true);
            if(Button(new Rect(1320,24,112,42),"帮助 F1")) help=!help;
            if(Button(new Rect(1443,24,126,42),paused?"继续":"暂停 Esc")) paused=!paused;
            if(Button(new Rect(1320,76,118,30),"局外培育",CanCultivate && !BuffChoosing)) OpenCultivation();
            if(Button(new Rect(1445,76,124,30),muted?"声音：关":"声音：开")) muted=!muted;
        }
        void DecorateTile(Transform root,Vector3 center,int seed)
        {
            var random=new System.Random(seed);
            for(int c=0;c<4;c++) {
                float x=(c%2==0?-1:1),z=(c<2?-1:1);
                for(int n=0;n<5;n++) {
                    float dx=x*(.65f+(float)random.NextDouble()*.35f),dz=z*(.65f+(float)random.NextDouble()*.35f);
                    float size=.06f+(float)random.NextDouble()*.10f;
                    Primitive("Clover",PrimitiveType.Sphere,center+new Vector3(dx,.08f,dz),new Vector3(size,.055f,size),
                        n%2==0?new Color(.60f,.73f,.37f):new Color(.34f,.55f,.31f),root);
                }
                float fx=x*1.05f,fz=z*1.0f;
                Primitive("Flower stem",PrimitiveType.Cylinder,center+new Vector3(fx,.12f,fz),new Vector3(.028f,.12f,.028f),new Color(.32f,.51f,.22f),root);
                for(int n=0;n<5;n++) {
                    float a=n*Mathf.PI*2/5;
                    Primitive("Petal",PrimitiveType.Sphere,center+new Vector3(fx+Mathf.Cos(a)*.052f,.25f,fz+Mathf.Sin(a)*.052f),new Vector3(.075f,.035f,.075f),c%2==0?Cream:new Color(.91f,.67f,.60f),root);
                }
                Primitive("Pollen",PrimitiveType.Sphere,center+new Vector3(fx,.26f,fz),Vector3.one*.052f,new Color(.98f,.75f,.33f),root);
            }
        }
        void BuildTileBase(Vector3 p,int index,Habitat habitat)
        {
            HexPrism("Hex earth",p,HexRadius*.98f,-.34f,-.04f,new Color(.34f,.38f,.24f));
            HexPrism("Hex habitat",p,HexRadius*.96f,-.04f,.07f,EcologyRules.Colors[(int)habitat]);
        }        void BuildSanctuary(Vector3 p)
        {
            Primitive("Sanctuary foundation",PrimitiveType.Cylinder,p+Vector3.up*.18f,new Vector3(1.24f,.12f,1.24f),new Color(.70f,.73f,.52f),terrain);
            Primitive("Ancient trunk",PrimitiveType.Cylinder,p+Vector3.up*.59f,new Vector3(.43f,.43f,.43f),new Color(.57f,.39f,.24f),terrain);
            for(int i=0;i<7;i++) {
                float a=i*Mathf.PI*2/7;
                var leaf=p+new Vector3(Mathf.Cos(a)*.36f,1.07f+(i%2)*.15f,Mathf.Sin(a)*.36f);
                Primitive("Canopy",PrimitiveType.Sphere,leaf,new Vector3(.69f,.63f,.68f),i%2==0?new Color(.59f,.77f,.38f):new Color(.39f,.63f,.36f),terrain);
            }
            Primitive("Heart of forest",PrimitiveType.Sphere,p+Vector3.up*1.42f,Vector3.one*.39f,new Color(.95f,.90f,.48f),terrain);
            for(int i=0;i<8;i++) {
                float a=i*Mathf.PI/4;
                Primitive("Sanctuary stone",PrimitiveType.Sphere,p+new Vector3(Mathf.Cos(a)*.70f,.16f,Mathf.Sin(a)*.70f),new Vector3(.19f,.14f,.19f),new Color(.81f,.78f,.58f),terrain);
            }
        }
    }
}





