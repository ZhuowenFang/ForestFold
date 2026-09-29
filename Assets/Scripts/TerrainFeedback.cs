using UnityEngine;

namespace ForestFold
{
    public partial class ForestGame
    {
        float EffectiveSlow(Enemy e) => Mathf.Max(e.terrainSlow,e.slowTime>0?e.slow*StrategyRules.StatusMultiplier(e.kind):0);
        void ThornBurst(Vector3 p)
        {
            p.y=.16f;
            Ring(p,new Color(.90f,.72f,.21f),1.0f,.15f,.65f);
            for(int i=0;i<7;i++) {
                float a=i*Mathf.PI*2/7;
                var foot=p+new Vector3(Mathf.Cos(a),0,Mathf.Sin(a))*.55f;
                var tip=foot+Vector3.up*(.65f+(i%2)*.25f);
                Beam(foot+Vector3.left*.12f,tip,new Color(.57f,.83f,.20f),.13f,.75f);
                Beam(tip,foot+Vector3.right*.12f,new Color(.91f,.87f,.38f),.09f,.75f);
            }
            words.Add(new FloatingWord {pos=p+Vector3.up,text="地刺!",color=new Color(.48f,.37f,.08f)});
        }
        void UpdateTerrainFeedback()
        {
            foreach(var e in enemies) {
                bool slowed=EffectiveSlow(e)>.001f;
                if(slowed && !e.slowRing) {
                    e.slowRing=FxLine("Slowed enemy ring",new Color(.22f,.85f,1),.10f,Vector3.zero,Vector3.zero);
                    e.slowRing.transform.SetParent(e.obj.transform,true);
                    e.slowRing.positionCount=33;
                }
                if(!e.slowRing) continue;
                e.slowRing.enabled=slowed;
                if(!slowed) continue;
                var p=e.obj.transform.position; p.y=.25f;
                for(int i=0;i<=32;i++) {
                    float angle=i*Mathf.PI/16;
                    e.slowRing.SetPosition(i,p+new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle))*(e.boss?.70f:.43f));
                }
            }
        }
        void DrawEnemyStatus(Enemy e,Vector2 p)
        {
            float slow=EffectiveSlow(e);
            string status=(slow>0?"减速 "+Mathf.RoundToInt(slow*100)+"% ":"")+(e.poisonTime>0?"中毒 ":"")+(e.exposedTime>0?"易伤":"");
            if(status.Length==0) return;
            var rect=new Rect(p.x-56,p.y-48,112,22);
            Round(rect,new Color(.10f,.28f,.32f,.94f),5);
            Text(new Rect(rect.x+4,rect.y,108,22),status,12,new Color(.73f,.95f,1),true);
        }
    }
}
