using UnityEngine;

namespace ForestFold
{
    public partial class ForestGame
    {
        Rect hoverAnchor;
        Vector2? tooltipPreviewPointer;
        Vector2 HoverPointer => tooltipPreviewPointer??Event.current.mousePosition;
        void ShowHover(string text,Rect anchor)
        {
            hover=text; hoverAnchor=anchor;
        }
        void DrawHoverBubble()
        {
            if(string.IsNullOrEmpty(hover) || paused || help || BuffChoosing || cultivationOpen || expansionPending>0 || dragging!=null) return;
            int split=hover.IndexOf('\n');
            string title=split<0?hover:hover.Substring(0,split),body=split<0?"":hover.Substring(split+1);
            var titleStyle=new GUIStyle(bold) {fontSize=18,wordWrap=true};
            var bodyStyle=new GUIStyle(label) {fontSize=16,wordWrap=true};
            titleStyle.normal.textColor=DeepGreen; bodyStyle.normal.textColor=TextColor;
            const float width=382,padding=16;
            float titleHeight=titleStyle.CalcHeight(new GUIContent(title),width-padding*2);
            float bodyHeight=body.Length==0?0:bodyStyle.CalcHeight(new GUIContent(body),width-padding*2);
            float height=padding*2+titleHeight+(bodyHeight>0?10+bodyHeight:0);
            bool below=hoverAnchor.y-height-14<14;
            float x=Mathf.Clamp(hoverAnchor.center.x-width*.5f,14,1600-width-14);
            float y=Mathf.Clamp(below?hoverAnchor.yMax+14:hoverAnchor.y-height-14,14,900-height-14);
            var rect=new Rect(x,y,width,height);
            Round(new Rect(x+3,y+5,width,height),new Color(.08f,.16f,.12f,.25f),13);
            Round(rect,DeepGreen,13);
            Round(new Rect(x+2,y+2,width-4,height-4),new Color(1,.985f,.92f),11);
            float tipX=Mathf.Clamp(hoverAnchor.center.x,x+20,x+width-20);
            float edge=below?y:y+height,sign=below?-1:1;
            for(int i=0;i<10;i++) {
                Box(new Rect(tipX-10+i,edge+sign*i,20-i*2,1.5f),DeepGreen);
                if(i<8) Box(new Rect(tipX-8+i,edge+sign*i,16-i*2,1.5f),new Color(1,.985f,.92f));
            }
            GUI.Label(new Rect(x+padding,y+padding,width-padding*2,titleHeight),title,titleStyle);
            if(bodyHeight>0) GUI.Label(new Rect(x+padding,y+padding+titleHeight+10,width-padding*2,bodyHeight),body,bodyStyle);
        }
        System.Collections.IEnumerator TooltipPreview()
        {
            muted=true; NewRun(); noticeUntil=0;
            tooltipPreviewPointer=new Vector2(140,808);
            yield return new WaitForSeconds(2);
            ScreenCapture.CaptureScreenshot(Application.dataPath+"/../tooltip-tile.png");
            yield return new WaitForSeconds(1);
            tooltipPreviewPointer=new Vector2(1145,250);
            yield return new WaitForSeconds(1);
            ScreenCapture.CaptureScreenshot(Application.dataPath+"/../tooltip-backpack.png");
            yield return new WaitForSeconds(1);
            tooltipPreviewPointer=new Vector2(120,193);
            yield return new WaitForSeconds(1);
            ScreenCapture.CaptureScreenshot(Application.dataPath+"/../tooltip-enemy.png");
            yield return new WaitForSeconds(1); Application.Quit();
        }
    }
}


