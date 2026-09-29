using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ForestFold
{
    public partial class ForestGame
    {
        class AttackVisual
        {
            public LineRenderer line;
            public Vector3 center;
            public Color color;
            public float life,duration,width,radius;
            public bool ring;
        }
        readonly List<AttackVisual> attackVisuals=new List<AttackVisual>();
        readonly Dictionary<Kind,int> attacksFired=new Dictionary<Kind,int>();
        Material fxMaterial;
        AudioClip leafSound,owlSound,sporeSound,waterSound;
        int confirmedImpacts;
        class GroundSkill { public Vector3 center; public float age,tick,damage,multiplier; public Kind kind; public AttackProfile attack; public HashSet<Enemy> hit=new HashSet<Enemy>(); }
        readonly List<GroundSkill> groundSkills=new List<GroundSkill>();
        static Color AttackColor(Kind kind)
        {
            switch(kind) {
                case Kind.Owl:return new Color(.77f,.49f,1);
                case Kind.Mushroom:return new Color(.81f,.95f,.25f);
                case Kind.Frog:return new Color(.20f,.84f,1);
                default:return new Color(.59f,1,.31f);
            }
        }
        void InitializeAttackVisuals()
        {
            fxMaterial=new Material(Resources.Load<Shader>("AttackFX"));
            leafSound=Tone(1046,.075f); owlSound=Tone(392,.26f); sporeSound=Tone(196,.22f); waterSound=Tone(740,.20f);
        }
        LineRenderer FxLine(string name,Color color,float width,params Vector3[] points)
        {
            var obj=new GameObject(name); obj.transform.SetParent(actors,false);
            var line=obj.AddComponent<LineRenderer>(); line.sharedMaterial=fxMaterial; line.useWorldSpace=true;
            line.positionCount=points.Length; line.SetPositions(points);
            line.startWidth=width; line.endWidth=width; line.startColor=color; line.endColor=color;
            line.numCapVertices=4; line.numCornerVertices=3;
            return line;
        }
        void Beam(Vector3 from,Vector3 to,Color color,float width,float duration)
        {
            attackVisuals.Add(new AttackVisual {line=FxLine("Sniper beam",color,width,from,to),color=color,width=width,life=duration,duration=duration});
        }
        void Ring(Vector3 center,Color color,float radius,float width,float duration)
        {
            var line=FxLine("Impact ring",color,width,center,center); line.positionCount=33;
            attackVisuals.Add(new AttackVisual {line=line,center=center,color=color,width=width,radius=radius,ring=true,life=duration,duration=duration});
        }
        void LaunchAttack(Ally ally,Enemy enemy,AttackProfile attack,float damage,float multiplier)
        {
            Kind kind=ally.relic.kind; Color color=AttackColor(kind);
            if((attack.enhanced || attack.star>1) && kind==Kind.Sprout) {
                int count=attack.star+(attack.enhanced?2:0);
                attack.enhanced=false;
                attack.star=1;
                var targets=enemies.Where(e=>e.hp>0 && StrategyRules.CanReach(kind,ally.home,ally.obj.transform.position,e.obj.transform.position,attack.range)).OrderBy(e=>Vector3.Distance(e.obj.transform.position,enemy.obj.transform.position)).Take(count).ToArray();
                for(int i=0;i<count;i++) { LaunchAttack(ally,targets[i%targets.Length],attack,damage,multiplier); shots[shots.Count-1].fan=(i-(count-1)*.5f)*.65f; }
                return;
            }
            if(!attacksFired.ContainsKey(kind)) attacksFired[kind]=0;
            attacksFired[kind]++;
            ally.pulse=.22f;
            var from=ally.obj.transform.position+Vector3.up*.76f;
            var to=enemy.obj.transform.position;
            if(kind==Kind.Owl) {
                // A hitscan attack: the beam and damage happen on the same frame.
                Beam(from,to,color,.26f,.38f);
                Beam(from+Vector3.up*.025f,to+Vector3.up*.025f,Color.white,.065f,.22f);
                Ring(from,color,.48f,.09f,.3f);
                ApplyImpact(kind,enemy,to,attack,damage,multiplier);
                var chained=new HashSet<Enemy> {enemy}; var bounceFrom=to;
                for(int n=1;n<attack.star;n++) {
                    var next=enemies.Where(e=>e.hp>0 && !chained.Contains(e) && Vector3.Distance(e.obj.transform.position,bounceFrom)<4.5f && StrategyRules.CanReach(kind,ally.home,from,e.obj.transform.position,attack.range)).OrderBy(e=>Vector3.Distance(e.obj.transform.position,bounceFrom)).FirstOrDefault();
                    if(next==null) break;
                    Beam(bounceFrom,next.obj.transform.position,color,.22f,.55f);
                    ApplyImpact(kind,next,next.obj.transform.position,attack,damage*.75f,multiplier);
                    chained.Add(next); bounceFrom=next.obj.transform.position;
                }
                if(attack.enhanced) {
                    Vector3 direction=(to-from).normalized, end=from+direction*attack.range;
                    Beam(from,end,color,.40f,.55f); Beam(from,end,Color.white,.10f,.4f);
                    foreach(var extra in enemies.Where(e=>e!=enemy && e.hp>0).ToArray()) {
                        Vector3 delta=extra.obj.transform.position-from; float along=Vector3.Dot(delta,direction);
                        if(along>0 && along<attack.range && (delta-direction*along).magnitude<.7f) ApplyImpact(kind,extra,extra.obj.transform.position,attack,damage,multiplier);
                    }
                }
            } else {
                var obj=new GameObject(kind+" projectile"); obj.transform.SetParent(actors); obj.transform.position=from;
                if(kind==Kind.Sprout) {
                    Part(obj.transform,"Bright leaf",Vector3.zero,new Vector3(.29f,.10f,.56f),color);
                    Part(obj.transform,"Leaf vein",Vector3.zero,new Vector3(.045f,.13f,.48f),Cream);
                } else if(kind==Kind.Mushroom) {
                    Part(obj.transform,"Spore pod",Vector3.zero,Vector3.one*.38f,new Color(.86f,.48f,.80f));
                    for(int i=0;i<4;i++) {
                        float a=i*Mathf.PI*.5f;
                        Part(obj.transform,"Spore satellite",new Vector3(Mathf.Cos(a)*.24f,0,Mathf.Sin(a)*.24f),Vector3.one*.14f,color);
                    }
                } else {
                    Part(obj.transform,"Water globule",Vector3.zero,new Vector3(.42f,.43f,.52f),color);
                    Part(obj.transform,"Water highlight",new Vector3(-.09f,.15f,-.04f),Vector3.one*.13f,Color.white);
                }
                float duration=kind==Kind.Sprout?Mathf.Clamp(Vector3.Distance(from,to)/13,.15f,.42f):Mathf.Clamp(Vector3.Distance(from,to)/8,.3f,.65f);
                shots.Add(new Shot {obj=obj,start=from,target=to,enemy=enemy,kind=kind,attack=attack,damage=damage,multiplier=multiplier,
                    duration=duration,trail=FxLine("Projectile ribbon",color,kind==Kind.Sprout?.11f:.16f,from,from)});
                Ring(from,color,kind==Kind.Sprout?.22f:.38f,.07f,.18f);
            }
            if(Time.unscaledTime>nextHitSound) {
                Chime(kind==Kind.Owl?owlSound:kind==Kind.Mushroom?sporeSound:kind==Kind.Frog?waterSound:leafSound,.26f);
                nextHitSound=Time.unscaledTime+.07f;
            }
        }
        Vector3 ProjectilePosition(Shot shot,float progress)
        {
            var p=Vector3.Lerp(shot.start,shot.target,progress);
            float arc=shot.kind==Kind.Mushroom?1.15f:shot.kind==Kind.Frog?.65f:.12f;
            return p+Vector3.up*Mathf.Sin(progress*Mathf.PI)*arc+Vector3.Cross((shot.target-shot.start).normalized,Vector3.up)*Mathf.Sin(progress*Mathf.PI)*shot.fan;
        }
        void UpdateProjectiles(float dt)
        {
            foreach(var shot in shots.ToArray()) {
                shot.elapsed+=dt;
                if(shot.enemy.obj && enemies.Contains(shot.enemy)) shot.target=shot.enemy.obj.transform.position;
                float t=Mathf.Clamp01(shot.elapsed/shot.duration);
                var position=ProjectilePosition(shot,t); shot.obj.transform.position=position;
                if(shot.kind==Kind.Sprout && (shot.target-position).sqrMagnitude>.001f) shot.obj.transform.rotation=Quaternion.LookRotation(shot.target-position);
                else shot.obj.transform.Rotate(0,dt*240,0);
                shot.trail.positionCount=6;
                for(int i=0;i<6;i++) shot.trail.SetPosition(i,ProjectilePosition(shot,Mathf.Max(0,t-i*.055f)));
                shot.trail.startColor=AttackColor(shot.kind); shot.trail.endColor=new Color(1,1,1,0);
                if(t>=1) {
                    ApplyImpact(shot.kind,shot.enemy,shot.target,shot.attack,shot.damage,shot.multiplier);
                    Destroy(shot.obj); Destroy(shot.trail.gameObject); shots.Remove(shot);
                }
            }
        }
        void ApplyImpact(Kind kind,Enemy primary,Vector3 position,AttackProfile attack,float damage,float multiplier)
        {
            if((attack.enhanced || attack.star>1) && (kind==Kind.Frog || (kind==Kind.Mushroom && !attack.weakened))) {
                groundSkills.Add(new GroundSkill {center=position,kind=kind,attack=attack,damage=damage,multiplier=multiplier});
                if(kind==Kind.Mushroom && attack.star==3) for(int side=-1;side<=1;side+=2)
                    groundSkills.Add(new GroundSkill {center=position+Vector3.right*side*1.5f,kind=kind,attack=attack,damage=damage,multiplier=multiplier});
                if(kind==Kind.Mushroom && attack.star==2 && attack.enhanced)
                    groundSkills.Add(new GroundSkill {center=position+Vector3.right*1.5f,kind=kind,attack=attack,damage=damage,multiplier=multiplier});
                if(kind==Kind.Frog) { for(int i=0;i<3;i++) Ring(new Vector3(position.x,.27f+i*.025f,position.z),AttackColor(kind),attack.enhanced?3.2f:2.4f,.15f,1+i*.12f); return; }
            }
            confirmedImpacts++;
            foreach(var enemy in enemies.Where(e=>e.hp>0 && (e==primary || (attack.radius>0 && Vector3.Distance(e.obj.transform.position,position)<=attack.radius)))) {
                DirectDamage(enemy,damage);
                if(attack.poisonDuration>0) { poisonApplications++; enemy.poisonDps=Mathf.Max(enemy.poisonDps,attack.poisonDps*multiplier); enemy.poisonTime=Mathf.Max(enemy.poisonTime,attack.poisonDuration); }
                if(attack.slowDuration>0) { slowApplications++; enemy.slow=enemy.slowTime>0?Mathf.Max(enemy.slow,attack.slow):attack.slow; enemy.slowTime=Mathf.Max(enemy.slowTime,attack.slowDuration); }
            }
            var color=AttackColor(kind);
            words.Add(new FloatingWord {pos=position+Vector3.up*.5f,text=Mathf.RoundToInt(damage).ToString(),color=kind==Kind.Owl?new Color(.49f,.24f,.72f):DeepGreen});
            if(kind==Kind.Sprout) {
                ParticleBurst(position,color,5); Ring(position,color,.28f,.06f,.2f);
            } else if(kind==Kind.Owl) {
                Ring(position,color,.65f,.11f,.42f);
                Beam(position+new Vector3(-.44f,.05f,0),position+new Vector3(.44f,.05f,0),Color.white,.08f,.3f);
                Beam(position+new Vector3(0,.05f,-.44f),position+new Vector3(0,.05f,.44f),Color.white,.08f,.3f);
                ParticleBurst(position,color,10);
            } else if(kind==Kind.Mushroom) {
                Ring(new Vector3(position.x,.23f,position.z),color,attack.radius,.18f,.9f);
                Ring(new Vector3(position.x,.27f,position.z),new Color(.71f,.42f,.87f,.7f),attack.radius*.78f,.30f,1.0f);
                ParticleBurst(position,color,12);
            } else {
                for(int i=0;i<3;i++) Ring(new Vector3(position.x,.24f+i*.025f,position.z),i==1?new Color(.91f,1,1,.9f):color,attack.radius*(1-i*.18f),.095f,.55f+i*.17f);
                ParticleBurst(position,color,10);
            }
        }
        void UpdateAttackVisuals(float dt)
        {
            foreach(var skill in groundSkills.ToArray()) {
                float previousAge=skill.age; skill.age+=dt; skill.tick-=dt;
                int waveCount=Mathf.Max(1,skill.attack.star-1)+(skill.attack.enhanced && skill.attack.star>1?1:0);
                if(skill.kind==Kind.Frog && Mathf.FloorToInt(previousAge/1.2f)!=Mathf.FloorToInt(skill.age/1.2f) && skill.age<1.2f*waveCount) {
                    skill.hit.Clear(); Ring(new Vector3(skill.center.x,.29f,skill.center.z),AttackColor(Kind.Frog),skill.attack.enhanced?3.2f:2.4f,.19f,1.2f);
                }
                float radius=skill.kind==Kind.Frog?(skill.attack.enhanced?3.2f:2.4f)*Mathf.Clamp01(Mathf.Repeat(skill.age,1.2f)):(skill.attack.enhanced?1.9f:1.3f);
                if(skill.tick<=0) {
                    skill.tick=skill.kind==Kind.Frog?0:.5f;
                    if(skill.kind==Kind.Mushroom) Ring(new Vector3(skill.center.x,.25f,skill.center.z),new Color(.64f,.85f,.22f,.8f),radius,.30f,.65f);
                    foreach(var enemy in enemies.Where(e=>e.hp>0 && Vector3.Distance(e.obj.transform.position,skill.center)<=radius).ToArray()) {
                        if(skill.kind==Kind.Frog) { if(!skill.hit.Add(enemy)) continue; DirectDamage(enemy,skill.damage); if(!skill.attack.weakened) { enemy.slow=Mathf.Max(enemy.slowTime>0?enemy.slow:0,skill.attack.enhanced?.5f:.3f); enemy.slowTime=3; slowApplications++; } ParticleBurst(enemy.obj.transform.position,AttackColor(skill.kind),6); }
                        else { enemy.hp-=2*skill.multiplier*StrategyRules.StatusMultiplier(enemy.kind); poisonDamageDealt+=2*skill.multiplier; poisonApplications++; enemy.poisonDps=Mathf.Max(enemy.poisonDps,3*skill.multiplier); enemy.poisonTime=Mathf.Max(enemy.poisonTime,3); }
                    }
                }
                if(skill.age>=(skill.kind==Kind.Frog?waveCount*1.2f:4)) groundSkills.Remove(skill);
            }
            foreach(var effect in attackVisuals.ToArray()) {
                if(!effect.line) { attackVisuals.Remove(effect); continue; }
                effect.life-=dt;
                if(effect.life<=0) { Destroy(effect.line.gameObject); attackVisuals.Remove(effect); continue; }
                float t=1-effect.life/effect.duration;
                Color color=effect.color; color.a*=1-t;
                effect.line.startColor=color; effect.line.endColor=color;
                effect.line.startWidth=effect.line.endWidth=effect.width*(1-t*.5f);
                if(effect.ring) {
                    float radius=effect.radius*(.22f+.78f*Mathf.Sqrt(t));
                    for(int i=0;i<=32;i++) {
                        float angle=i*Mathf.PI*2/32;
                        effect.line.SetPosition(i,effect.center+new Vector3(Mathf.Cos(angle)*radius,0,Mathf.Sin(angle)*radius));
                    }
                }
            }
        }
        void ClearAttackVisuals()
        {
            groundSkills.Clear();
            foreach(var v in attackVisuals) if(v.line) Destroy(v.line.gameObject);
            attackVisuals.Clear();
        }
        void DrawAttackLegend()
        {
            string[] labels={"芽芽 · 连发叶弹","猫头鹰 · 狙击光束","蘑菇 · 孢子毒雾","青蛙 · 减速水波"};
            for(int i=0;i<4;i++) {
                float x=26+i*265;
                Round(new Rect(x,843,13,13),AttackColor(ForestRules.Partners[i]),6);
                Text(new Rect(x+21,836,243,34),labels[i],16,DeepGreen);
            }
        }
    }
}

