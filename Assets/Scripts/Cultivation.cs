using System;
using UnityEngine;

namespace ForestFold
{
    public partial class ForestGame
    {
        int trainingPoints;
        readonly int[] trainingRanks=new int[4];
        bool cultivationOpen,campWasPaused,isolatedProgress;
        string progressError="";
        bool CanCultivate => phase==Phase.LevelComplete || phase==Phase.Won || phase==Phase.Lost || (phase==Phase.Planning && wave==0 && laid==0);
        void LoadCultivation()
        {
            isolatedProgress=Array.Exists(Environment.GetCommandLineArgs(),a=>a.StartsWith("-forest"));
            if(isolatedProgress) return;
            trainingPoints=Mathf.Max(0,PlayerPrefs.GetInt("ForestFold.Cultivation.Points.v1",0));
            for(int i=0;i<4;i++) trainingRanks[i]=Mathf.Clamp(PlayerPrefs.GetInt("ForestFold.Cultivation.Rank.v1."+i,0),0,5);
        }
        void SaveCultivation()
        {
            if(isolatedProgress) return;
            try {
                PlayerPrefs.SetInt("ForestFold.Cultivation.Points.v1",trainingPoints);
                for(int i=0;i<4;i++) PlayerPrefs.SetInt("ForestFold.Cultivation.Rank.v1."+i,trainingRanks[i]);
                PlayerPrefs.Save(); progressError="";
            } catch(Exception e) { progressError="培养存档未保存，请检查系统写入权限。"; Debug.LogWarning("Cultivation save failed: "+e.Message); }
        }
        float TrainingPower(Kind kind)
        {
            int i=Array.IndexOf(ForestRules.Partners,kind); return i<0?1:1+trainingRanks[i]*.05f;
        }
        void Train(int i)
        {
            if(i<0 || i>=4 || !CanCultivate || trainingRanks[i]>=5 || trainingPoints<trainingRanks[i]+1) return;
            trainingPoints-=trainingRanks[i]+1; trainingRanks[i]++; SaveCultivation();
        }
        void OpenCultivation()
        {
            if(!CanCultivate || BuffChoosing) return;
            campWasPaused=paused; paused=true; cultivationOpen=true;
        }
        void DrawCultivation()
        {
            Box(new Rect(0,0,1600,900),new Color(.04f,.10f,.07f,.88f));
            Card(new Rect(205,170,1190,570),Cream,18);
            Text(new Rect(245,203,1080,45),"伙伴培育 · 永久培养点 "+trainingPoints,30,DeepGreen,true);
            Text(new Rect(245,257,1080,44),"每通关一张8波地图获得3点 · 每级基础伤害 +5% · 最高5级 · 与局内合成等级独立",18,Muted);
            for(int i=0;i<4;i++) {
                float x=245+i*283; Kind kind=ForestRules.Partners[i];
                Card(new Rect(x,327,264,271),new Color(.88f,.92f,.81f),12);
                Portrait(new Rect(x+86,341,86,86),kind);
                Text(new Rect(x+16,431,240,34),ForestRules.Names[(int)kind],23,DeepGreen,true);
                Text(new Rect(x+16,475,240,62),"培育 Lv."+trainingRanks[i]+" / 5\n基础伤害 +"+(trainingRanks[i]*5)+"%",18,Muted);
                if(Button(new Rect(x+16,544,232,41),trainingRanks[i]>=5?"已满级":"培养 · "+(trainingRanks[i]+1)+" 点",trainingRanks[i]<5 && trainingPoints>=trainingRanks[i]+1)) Train(i);
            }
            Text(new Rect(245,613,1080,30),progressError.Length>0?progressError:"培养保留到以后每一关；进入地图时携带两件1星初始芽芽。",17,Muted);
            if(Button(new Rect(535,663,530,48),"返回关卡",true,Mint)) { cultivationOpen=false; paused=campWasPaused; }
        }
    }
}


