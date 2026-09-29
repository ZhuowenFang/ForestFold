using System;
using System.Linq;
using UnityEngine;

namespace ForestFold
{
    public partial class ForestGame
    {
        class HabitatAura
        {
            public int affinity;
            public float age;
            public LineRenderer[] rings, motes;
        }
        HabitatAura CreateHabitatAura(Ally ally)
        {
            int affinity=EcologyRules.Affinity(ally.relic.kind,ally.habitat);
            if(affinity==0) return null;
            var aura=new HabitatAura {affinity=affinity,age=(float)rng.NextDouble(),rings=new LineRenderer[affinity>0?2:3],motes=new LineRenderer[4]};
            for(int i=0;i<aura.rings.Length+aura.motes.Length;i++) {
                var line=FxLine(affinity>0?"Habitat blessing":"Habitat discomfort",Color.white,.06f,Vector3.zero,Vector3.zero);
                line.transform.SetParent(ally.obj.transform,false); line.useWorldSpace=false;
                if(i<aura.rings.Length) aura.rings[i]=line; else aura.motes[i-aura.rings.Length]=line;
            }
            UpdateHabitatAura(aura,0);
            return aura;
        }
        void UpdateHabitatAura(HabitatAura aura,float dt)
        {
            if(aura==null) return;
            aura.age+=dt;
            bool liked=aura.affinity>0;
            Color color=liked?new Color(1,.84f,.22f):new Color(.69f,.43f,.76f);
            for(int i=0;i<aura.rings.Length;i++) {
                var line=aura.rings[i]; line.positionCount=33;
                float radius=(liked?.62f:.58f)+i*.10f+Mathf.Sin(aura.age*3)*.035f;
                float start=liked?aura.age*.65f:aura.age*-.5f+i*Mathf.PI*2/3;
                float span=liked?Mathf.PI*2:Mathf.PI*.43f;
                for(int j=0;j<=32;j++) {
                    float a=start+span*j/32;
                    line.SetPosition(j,new Vector3(Mathf.Cos(a)*radius,-.08f+i*.02f,Mathf.Sin(a)*radius));
                }
                Color c=i==1 && liked?new Color(.75f,1,.43f,.75f):color;
                c.a=.65f+Mathf.Sin(aura.age*3+i)*.2f;
                line.startColor=line.endColor=c;
                line.startWidth=line.endWidth=liked?.055f:.075f;
            }
            for(int i=0;i<aura.motes.Length;i++) {
                var line=aura.motes[i]; float t=Mathf.Repeat(aura.age*.55f+i*.25f,1);
                float a=i*Mathf.PI*.5f+aura.age*(liked?.65f:-.35f);
                Vector3 p=new Vector3(Mathf.Cos(a)*.53f,.12f+(liked?t:1-t)*1.1f,Mathf.Sin(a)*.53f);
                float sign=liked?1:-1;
                line.positionCount=3;
                line.SetPosition(0,p+new Vector3(-.10f,-sign*.08f,0));
                line.SetPosition(1,p+new Vector3(0,sign*.05f,0));
                line.SetPosition(2,p+new Vector3(.10f,-sign*.08f,0));
                Color c=color; c.a=Mathf.Sin(t*Mathf.PI)*.95f;
                line.startColor=line.endColor=c; line.startWidth=line.endWidth=.065f;
            }
        }
        System.Collections.IEnumerator AuraPreview()
        {
            muted=true;
            foreach(var habitat in new [] {Habitat.Meadow,Habitat.Woodland,Habitat.Swamp}) {
                NewRun(); tiles.Add(new RoadTile {cell=Vector2Int.up,ports=63,habitat=habitat}); laid=PicksPerWave; StartWave();
                int affinity=EcologyRules.Affinity(Kind.Sprout,habitat);
                if(allies.Count!=2 || allies.Any(a=>(a.aura==null)!=(affinity==0))) throw new Exception("Habitat aura deployment mismatch");
                if(allies.Any(a=>a.aura!=null && a.aura.affinity!=affinity)) throw new Exception("Wrong habitat aura");
                ClearActors(); yield return null;
                if(actors.childCount!=0) throw new Exception("Habitat aura leaked after cleanup");
            }
            NewRun(); tiles.Clear(); tiles.Add(new RoadTile {cell=Vector2Int.zero,ports=63});
            var habitats=new [] {Habitat.Meadow,Habitat.Woodland,Habitat.Swamp};
            for(int i=0;i<3;i++) {
                var cell=new Vector2Int(i-1,1); tiles.Add(new RoadTile {cell=cell,ports=63,habitat=habitats[i]});
                var root=new GameObject("Habitat state preview").transform; root.SetParent(actors); root.position=Position(cell)+new Vector3(-.65f,.25f,.65f); BuildPartner(root,Kind.Sprout);
                var ally=new Ally {obj=root.gameObject,relic=bag[0],habitat=habitats[i]}; ally.aura=CreateHabitatAura(ally); allies.Add(ally);
            }
            RebuildTerrain(); phase=Phase.Battle; remainingSpawn=1; spawnTimer=99999;
            Notify("生态状态对照：左侧喜爱强化 · 中间正常无特效 · 右侧厌恶削弱");
            yield return new WaitForSeconds(2);
            ScreenCapture.CaptureScreenshot(Application.dataPath+"/../habitat-aura-preview.png");
            Debug.Log("FOREST_AURA_OK: preferred aura, disliked aura, neutral none, deployment and cleanup");
            yield return new WaitForSeconds(1); Application.Quit();
        }
    }
}



