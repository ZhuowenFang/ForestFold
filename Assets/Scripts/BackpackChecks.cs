using System;
using System.Linq;
using UnityEngine;

namespace ForestFold
{
    public partial class ForestGame
    {
        System.Collections.IEnumerator BackpackChecks()
        {
            muted=true; NewRun(); yield return null;
            if(bag.Count!=2 || bag.Any(r=>r.kind!=Kind.Sprout || r.level!=1)) throw new Exception("Two starter partners missing");
            if(bagCells.Count!=16 || bagCells.Any(p=>p.x<2 || p.x>5 || p.y<1 || p.y>4)) throw new Exception("Initial 4x4 footprint failed");
            bag.Clear(); gold=50;
            var a=new Relic(Kind.Sprout) {anchor=new Vector2Int(2,1)};
            var b=new Relic(Kind.Sprout) {anchor=new Vector2Int(4,1)};
            var c=new Relic(Kind.Sprout) {anchor=new Vector2Int(4,3)};
            bag.AddRange(new [] {a,b,c});
            if(ForestRules.MergeSet(bag,a).Count!=3) throw new Exception("Remote merge missing");
            BeginBagDrag(a,Vector2Int.zero);
            if(DropAt(new Vector2Int(4,2)) || dragging!=a || a.anchor!=new Vector2Int(2,1)) throw new Exception("Two-item collision mutated bag");
            if(!DropAt(new Vector2Int(4,1)) || dragging!=b || a.anchor!=new Vector2Int(4,1)) throw new Exception("Pickup swap failed");
            if(!DropAt(new Vector2Int(2,1)) || dragging!=null || b.anchor!=new Vector2Int(2,1)) throw new Exception("Swap completion failed");
            BeginBagDrag(a,Vector2Int.zero); DropAt(b.anchor); CancelBagDrag();
            if(a.anchor!=new Vector2Int(4,1) || b.anchor!=new Vector2Int(2,1)) throw new Exception("Chain rollback failed");
            selected=b; int before=gold; RotateSelected();
            if(gold!=before-1 || b.rotation!=1) throw new Exception("Paid rotation failed");
            selected=c; c.anchor=new Vector2Int(5,3); before=gold; RotateSelected();
            if(gold!=before || c.rotation!=0) throw new Exception("Invalid rotation charged");
            c.anchor=new Vector2Int(4,3); selected=a; MergeConnected();
            if(bag.Count!=1 || a.level!=2) throw new Exception("Remote merge transaction failed");
            phase=Phase.Shop; gold=100; offers[0]=Kind.Expansion; expansionSizes[0]=4; expansionShapes[0]=new [] {new Vector2Int(0,0),new Vector2Int(0,1),new Vector2Int(0,2),new Vector2Int(1,2)}; sold[0]=false; Buy(0);
            if(expansionPending!=4 || gold!=88 || !sold[0]) throw new Exception("Expansion purchase failed");
            var expected=expansionShapes[0].Select(p=>p+new Vector2Int(0,1)).ToArray();
            if(ExpansionFits(new [] {Vector2Int.zero},new Vector2Int(1,0))) throw new Exception("Diagonal expansion accepted");
            if(ExpandAt(new Vector2Int(2,1)) || expansionPending!=4 || bagCells.Count!=16) throw new Exception("Overlapping shape mutated capacity");
            CancelExpansion(); if(gold!=100 || sold[0] || expansionPending!=0) throw new Exception("Expansion refund failed");
            Buy(0);
            if(!ExpandAt(new Vector2Int(0,1)) || expansionPending!=0 || bagCells.Count!=20 || expected.Any(p=>!bagCells.Contains(p))) throw new Exception("Fixed shape placement failed");
            var sample=new Relic(Kind.Sprout) {rotation=1};
            if(ForestRules.CanPlace(bag,sample,new Vector2Int(0,0),width,height,area:bagCells)) throw new Exception("Item placed in locked cells");
            var seen=new System.Collections.Generic.HashSet<int>();
            for(int i=0;i<100;i++) { RollShop(); foreach(int n in expansionSizes) seen.Add(n); }
            if(seen.Count!=4) throw new Exception("Expansion sizes not randomized 1-4");
            Debug.Log("FOREST_BACKPACK_CHECKS_OK: 4x4, remote merge, paid rotation and rejection, single swap, double overlap rejection, rollback, 1-4 expansion, fixed shape, atomic rejection, cancel refund, edge connectivity, two starters, locked cells");
            NewRun(); phase=Phase.Shop; gold=40; RollShop();
            for(int i=0;i<4;i++) { offers[i]=Kind.Expansion; expansionSizes[i]=i+1; expansionShapes[i]=RollExpansionShape(i+1); offerLevels[i]=1; sold[i]=false; }
            expansionShapes[3]=new [] {new Vector2Int(0,0),new Vector2Int(0,1),new Vector2Int(0,2),new Vector2Int(1,2)};
            yield return new WaitForSeconds(1); ScreenCapture.CaptureScreenshot(Application.dataPath+"/../backpack-shop.png");
            yield return new WaitForSeconds(1); Buy(3); expansionPreviewAnchor=new Vector2Int(0,1);
            yield return new WaitForSeconds(1); ScreenCapture.CaptureScreenshot(Application.dataPath+"/../backpack-expand.png");
            yield return new WaitForSeconds(1); ExpandAt(new Vector2Int(0,1)); expansionPreviewAnchor=null;
            yield return new WaitForSeconds(1); ScreenCapture.CaptureScreenshot(Application.dataPath+"/../backpack-shape-placed.png");
            yield return new WaitForSeconds(1); Application.Quit();
        }
    }
}

