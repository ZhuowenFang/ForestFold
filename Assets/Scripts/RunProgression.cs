using System;
using System.Linq;
using UnityEngine;

namespace ForestFold
{
    public partial class ForestGame
    {
        int runLevel=1,runXp,pendingBuffs;
        readonly int[] buffs=new int[4];
        readonly int[] buffOffers=new int[3];
        readonly int[] offerLevels=new int[4];
        static readonly string[] BuffNames={"齐心协力","精英招募","疾风节拍","远望之眼"};
        static readonly string[] BuffDescriptions={"全体伙伴伤害 +15%（含毒伤）","商店伙伴为 Lv.2 的概率 +20%\n最高100%，价格按实际等级翻倍","全体伙伴攻击速度 +12%","全体伙伴圆形射程 +8%"};
        int XpThreshold => 8+(runLevel-1)*6;
        bool BuffChoosing => pendingBuffs>0;
        float RunPower => 1+buffs[0]*.15f;
        float RunHaste => 1+buffs[2]*.12f;
        float RunRange => 1+buffs[3]*.08f;
        int ShopCost(int i) => offers[i]==Kind.Expansion?expansionSizes[i]*3:ForestRules.Cost(offers[i])*offerLevels[i];
        void ResetRunProgress()
        {
            runLevel=1; runXp=0; pendingBuffs=0; Array.Clear(buffs,0,buffs.Length);
        }
        void GainXp(int amount)
        {
            runXp+=amount; bool choosing=BuffChoosing;
            while(runXp>=XpThreshold) { runXp-=XpThreshold; runLevel++; pendingBuffs++; }
            if(!choosing && BuffChoosing) RollBuffs();
        }
        void RollBuffs()
        {
            var pool=Enumerable.Range(0,4).Where(i=>i!=1 || buffs[1]<5).OrderBy(_=>rng.Next()).ToArray();
            Array.Copy(pool,buffOffers,3);
        }
        void ChooseBuff(int choice)
        {
            if(!BuffChoosing || choice<0 || choice>=3) return;
            buffs[buffOffers[choice]]++; pendingBuffs--;
            if(BuffChoosing) RollBuffs();
            Chime(winSound,.5f);
        }
        int RollPartnerLevel(Kind kind) => ForestRules.IsPartner(kind) && rng.NextDouble()<Mathf.Min(1,buffs[1]*.20f)?2:1;
        AttackProfile PartnerAttack(Ally ally)
        {
            var attack=EcologyRules.Attack(ally.relic.kind,ally.habitat,ally.relic.level);
            attack.range*=RunRange; return attack;
        }
        void DrawRunProgress()
        {
            Text(new Rect(1124,799,450,25),"本关等级 "+runLevel+"  ·  经验 "+runXp+" / "+XpThreshold,16,DeepGreen,true);
            Box(new Rect(1124,827,440,5),new Color(.78f,.82f,.70f));
            Box(new Rect(1124,827,440*runXp/XpThreshold,5),Mint);
            Text(new Rect(1124,839,450,50),"局内增伤 +"+(buffs[0]*15)+"% · 攻速 +"+(buffs[2]*12)+"% · 射程 +"+(buffs[3]*8)+"%\n商店二级伙伴概率 "+Mathf.Min(100,buffs[1]*20)+"% · 换关清空",15,Muted);
        }
        void DrawBuffChoice()
        {
            Box(new Rect(0,0,1600,900),new Color(.04f,.10f,.07f,.84f));
            Card(new Rect(225,210,1150,465),Cream,18);
            Text(new Rect(265,246,1050,46),"本关升级！Lv."+runLevel+" · 选择一项强化",30,DeepGreen,true);
            Text(new Rect(265,300,1050,35),"战斗已暂停 · 本关有效 · 同类加成可叠加"+(pendingBuffs>1?" · 还有 "+pendingBuffs+" 次选择":""),18,Muted);
            for(int i=0;i<3;i++) {
                int index=buffOffers[i]; float x=265+i*352;
                Card(new Rect(x,361,330,235),new Color(.90f,.93f,.82f),12);
                Text(new Rect(x+18,382,296,38),BuffNames[index],25,DeepGreen,true);
                Text(new Rect(x+18,431,296,95),BuffDescriptions[index],18,TextColor);
                if(Button(new Rect(x+18,538,294,43),"选择 · 已有 "+buffs[index]+" 层")) { ChooseBuff(i); break; }
            }
        }
        System.Collections.IEnumerator ProgressPreview()
        {
            muted=true; NewRun(); SmokePick(); SmokePick(); StartWave();
            yield return new WaitForSeconds(1);
            GainXp(XpThreshold); float timer=spawnTimer; int count=enemies.Count;
            yield return new WaitForSeconds(1);
            if(spawnTimer!=timer || enemies.Count!=count) throw new Exception("Buff choice did not pause combat");
            ScreenCapture.CaptureScreenshot(Application.dataPath+"/../progress-buff.png");
            yield return new WaitForSeconds(1); ChooseBuff(0);
            NewRun(); trainingPoints=3; OpenCultivation(); Train(0);
            yield return new WaitForSeconds(1); ScreenCapture.CaptureScreenshot(Application.dataPath+"/../progress-training.png");
            yield return new WaitForSeconds(1); cultivationOpen=false; paused=false;
            buffs[1]=5; wave=1; phase=Phase.Shop; RollShop();
            yield return new WaitForSeconds(1); ScreenCapture.CaptureScreenshot(Application.dataPath+"/../progress-shop.png");
            yield return new WaitForSeconds(1);
            Debug.Log("FOREST_PROGRESS_PREVIEW_OK: modal pauses combat, training and elite shop rendered"); Application.Quit();
        }
    }
}

