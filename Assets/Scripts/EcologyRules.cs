using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ForestFold
{
    public enum Habitat { Meadow, Woodland, Water, Swamp }
    public struct AttackProfile
    {
        public int star;
        public float range,damage,interval,radius,poisonDps,poisonDuration,slow,slowDuration;
        public bool enhanced, weakened;
    }
    public class HabitatSlot
    {
        public Vector2Int cell;
        public Vector2 offset;
        public Habitat habitat;
    }
    public class Deployment
    {
        public Relic relic;
        public HabitatSlot slot;
    }
    public static class EcologyRules
    {
        public static readonly string[] Names={"草甸","林地","水域","沼泽"};
        public static readonly Color[] Colors={new Color(.57f,.71f,.39f),new Color(.30f,.53f,.35f),new Color(.37f,.68f,.76f),new Color(.47f,.48f,.32f)};
        public static readonly Vector2[] Corners=Enumerable.Range(0,6).Select(i=>new Vector2(Mathf.Cos((30+i*60)*Mathf.Deg2Rad),Mathf.Sin((30+i*60)*Mathf.Deg2Rad))*1.12f).ToArray();
        public static Habitat Preference(Kind kind)
        {
            switch(kind) { case Kind.Owl:return Habitat.Woodland; case Kind.Mushroom:return Habitat.Swamp; case Kind.Frog:return Habitat.Water; default:return Habitat.Meadow; }
        }
        public static Habitat Dislike(Kind kind)
        {
            switch(kind) { case Kind.Owl:return Habitat.Swamp; case Kind.Mushroom:return Habitat.Water; case Kind.Frog:return Habitat.Meadow; default:return Habitat.Swamp; }
        }
        public static int Affinity(Kind kind,Habitat habitat) => habitat==Preference(kind)?1:habitat==Dislike(kind)?-1:0;
        public static Vector2[] Slots(int ports)
        {
            int count=Enumerable.Range(0,6).Count(d=>RoadMap.Has(ports,d));
            if(count<3) return new Vector2[0];
            return Enumerable.Range(0,6).Where(d=>RoadMap.Has(ports,d)).Select(d=>Corners[d]).ToArray();
        }        public static List<HabitatSlot> MapSlots(List<RoadTile> tiles)
        {
            return tiles.Where(t=>t.cell!=Vector2Int.zero).SelectMany(t=>Slots(t.ports).Select(p=>new HabitatSlot {cell=t.cell,offset=p,habitat=t.habitat})).ToList();
        }
        public static List<Deployment> Deploy(List<Relic> bag,List<RoadTile> tiles,System.Random random)
        {
            var units=bag.Where(r=>r.Partner).SelectMany(r=>Enumerable.Repeat(r,Mathf.Clamp(r.level,1,3))).OrderBy(_=>random.Next()).ToList();
            var slots=MapSlots(tiles).OrderBy(_=>random.Next()).ToList();
            var result=new List<Deployment>();
            // Match all preferred habitats before filling remaining slots, so an indifferent unit
            // cannot consume a water slot while a water-loving unit is still waiting.
            foreach(var item in units.ToArray()) {
                var slot=slots.FirstOrDefault(s=>s.habitat==Preference(item.kind));
                if(slot==null) continue;
                result.Add(new Deployment {relic=item,slot=slot}); slots.Remove(slot); units.Remove(item);
            }
            foreach(var item in units) {
                if(slots.Count==0) break;
                var slot=slots.FirstOrDefault(s=>Affinity(item.kind,s.habitat)==0)??slots[0];
                result.Add(new Deployment {relic=item,slot=slot}); slots.Remove(slot);
            }
            return result;
        }
        public static AttackProfile Attack(Kind kind,Habitat habitat,int star=1)
        {
            AttackProfile attack;
            switch(kind) {
                case Kind.Owl: attack=new AttackProfile {range=8.5f,damage=30,interval=1.8f}; break;
                case Kind.Mushroom: attack=new AttackProfile {range=3.4f,damage=7,interval=1.5f,radius=1,poisonDps=3,poisonDuration=3}; break;
                case Kind.Frog: attack=new AttackProfile {range=5.1f,damage=10,interval=1.2f,radius=.5f,slow=.3f,slowDuration=2}; break;
                default: attack=new AttackProfile {range=4.1f,damage=7,interval=.42f}; break;
            }
            attack.star=Mathf.Clamp(star,1,3);
            attack.enhanced=Affinity(kind,habitat)>0; attack.weakened=Affinity(kind,habitat)<0;
            if(kind==Kind.Mushroom || kind==Kind.Frog) attack.radius+=.3f*(attack.star-1);
            if(attack.weakened) { attack.damage*=.65f; attack.interval*=1.25f; attack.poisonDuration=0; attack.slowDuration=0; }
            return attack;
        }
        public static string StarDetail(Kind kind,int star)
        {
            string form=kind==Kind.Sprout?(star==1?"单叶弹":star==2?"双叶齐射":"三叶齐射"):
                kind==Kind.Owl?(star==1?"单体狙击":star==2?"光束弹射1次":"光束弹射2次"):
                kind==Kind.Mushroom?(star==1?"孢子爆炸":star==2?"爆炸留下毒池":"三簇扩散毒池"):
                (star==1?"小范围水弹":star==2?"扩散冲击波":"双重扩散冲击波");
            return star+"星 · 本件最多出战 "+star+" 只\n"+form+"；生态偏好另行强化，厌恶仍会削弱。";
        }
    }
}

