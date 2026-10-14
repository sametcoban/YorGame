using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace Ashlight {
    public sealed partial class Prototype {
        GameObject inventoryRoot,recycleRoot,heroBuildRoot;
        Text inventoryTitle,inventoryDetail,recycleDetail,buildTitle,buildDetail;
        readonly Button[] inventoryCards=new Button[6],inventoryFilters=new Button[4];
        Button inventoryPrevious,inventoryNext,inventoryLock,inventoryRecycle,recycleConfirm,buildSave,buildLoad,buildClear;
        InputField buildName;
        List<InventoryEntry> inventoryRows=new List<InventoryEntry>();
        int inventoryPage,inventorySlot,inventoryQuality,inventoryElement,inventoryView,recycleQuantity,buildSlot;
        string inventorySelection,recycleSelection,buildHero,inventoryNotice="",buildNotice="";
        bool InventoryModalActive { get { return (inventoryRoot!=null && inventoryRoot.activeSelf) || (recycleRoot!=null && recycleRoot.activeSelf) || (heroBuildRoot!=null && heroBuildRoot.activeSelf); } }
        void CreateInventoryScreens() {
            inventoryRoot=ScreenRoot("Full Gear Inventory");
            inventoryTitle=Label(inventoryRoot.transform,"",new Vector2(.05f,.90f),new Vector2(.95f,.99f),28);
            for(int i=0;i<4;i++) { int filter=i; float x=.05f+i*.23f; inventoryFilters[i]=ScreenButton(inventoryRoot.transform,"",x,.805f,x+.21f,.88f,()=> { if(filter==0) inventorySlot=(inventorySlot+1)%4; if(filter==1) inventoryQuality=(inventoryQuality+1)%6; if(filter==2) inventoryElement=(inventoryElement+1)%7; if(filter==3) inventoryView=(inventoryView+1)%4; inventoryPage=0; inventorySelection=null; RefreshInventory(); }); }
            for(int i=0;i<6;i++) { int row=i; float x=i%2==0?.05f:.515f,y=.64f-i/2*.15f; inventoryCards[i]=ScreenButton(inventoryRoot.transform,"",x,y,x+.435f,y+.135f,()=> { int index=inventoryPage*6+row; if(index<inventoryRows.Count) { inventorySelection=inventoryRows[index].Gear.Id; inventoryNotice=""; RefreshInventory(); } }); }
            inventoryDetail=ScrollingText(inventoryRoot.transform,new Vector2(.05f,.19f),new Vector2(.95f,.325f),21);
            inventoryPrevious=ScreenButton(inventoryRoot.transform,"<",.05f,.095f,.14f,.175f,()=> { inventoryPage--; inventorySelection=null; RefreshInventory(); });
            inventoryLock=ScreenButton(inventoryRoot.transform,"LOCK",.16f,.095f,.36f,.175f,()=> { if(battle.SetGearLocked(inventorySelection,!battle.GearLocked(inventorySelection))) { SaveProgress(); inventoryNotice=saveNotice; RefreshInventory(); } });
            inventoryRecycle=ScreenButton(inventoryRoot.transform,"RECYCLE",.38f,.095f,.82f,.175f,OpenRecycle);
            inventoryNext=ScreenButton(inventoryRoot.transform,">",.85f,.095f,.95f,.175f,()=> { inventoryPage++; inventorySelection=null; RefreshInventory(); });
            ScreenButton(inventoryRoot.transform,"BACK",.25f,.015f,.75f,.08f,()=>inventoryRoot.SetActive(false)); inventoryRoot.SetActive(false);
            recycleRoot=ScreenRoot("Confirm Gear Recycling");
            Label(recycleRoot.transform,"RECYCLE UNEQUIPPED COPIES",new Vector2(.05f,.86f),new Vector2(.95f,.98f),30);
            recycleDetail=ScrollingText(recycleRoot.transform,new Vector2(.06f,.37f),new Vector2(.94f,.82f),26);
            ScreenButton(recycleRoot.transform,"−",.05f,.225f,.23f,.325f,()=> { recycleQuantity=Math.Max(1,recycleQuantity-1); RefreshRecycle(); });
            ScreenButton(recycleRoot.transform,"+",.255f,.225f,.435f,.325f,()=> { recycleQuantity=Math.Min(battle.AvailableGear(recycleSelection),recycleQuantity+1); RefreshRecycle(); });
            ScreenButton(recycleRoot.transform,"MAX",.46f,.225f,.64f,.325f,()=> { recycleQuantity=Math.Min(battle.AvailableGear(recycleSelection),(1000000-battle.ForgeMaterials)/Math.Max(1,battle.RecycleValue(recycleSelection))); RefreshRecycle(); });
            ScreenButton(recycleRoot.transform,"CANCEL",.665f,.225f,.95f,.325f,()=>recycleRoot.SetActive(false));
            recycleConfirm=ScreenButton(recycleRoot.transform,"CONFIRM RECYCLE",.15f,.075f,.85f,.175f,ConfirmRecycle); recycleRoot.SetActive(false);
            heroBuildRoot=ScreenRoot("Hero Loadout Profiles");
            buildTitle=Label(heroBuildRoot.transform,"",new Vector2(.05f,.86f),new Vector2(.95f,.98f),30);
            buildName=CreatePresetInput(heroBuildRoot.transform,new Vector2(.05f,.75f),new Vector2(.95f,.83f));
            buildDetail=ScrollingText(heroBuildRoot.transform,new Vector2(.06f,.30f),new Vector2(.94f,.72f),23);
            ScreenButton(heroBuildRoot.transform,"< BUILD",.05f,.185f,.245f,.275f,()=> { buildSlot=(buildSlot+2)%3; buildNotice=""; RefreshHeroBuilds(); });
            buildSave=ScreenButton(heroBuildRoot.transform,"SAVE CURRENT",.265f,.185f,.485f,.275f,()=> { if(battle.SaveHeroBuild(buildHero,buildSlot,buildName.text)) { SaveProgress(); buildNotice="Current two skills and three equipment slots saved. "+saveNotice; RefreshHeroBuilds(); } else { buildNotice="Name must be at most 24 characters; avoid control characters and < >."; RefreshHeroBuilds(false); } });
            buildLoad=ScreenButton(heroBuildRoot.transform,"LOAD",.505f,.185f,.735f,.275f,()=> { if(battle.LoadHeroBuild(buildHero,buildSlot)) { SaveProgress(); RefreshAppearance(); RefreshVisibleHeroMenus(); if(heroDetailsRoot.activeSelf) RefreshHeroDetails(); buildNotice="Build applied. "+saveNotice; } else buildNotice=battle.HeroBuildBlock(buildHero,buildSlot); RefreshHeroBuilds(); });
            ScreenButton(heroBuildRoot.transform,"BUILD >",.755f,.185f,.95f,.275f,()=> { buildSlot=(buildSlot+1)%3; buildNotice=""; RefreshHeroBuilds(); });
            buildClear=ScreenButton(heroBuildRoot.transform,"CLEAR PROFILE",.05f,.06f,.48f,.15f,()=> { if(battle.ClearHeroBuild(buildHero,buildSlot)) { SaveProgress(); buildNotice="Profile cleared; equipped items are unchanged. "+saveNotice; RefreshHeroBuilds(); } });
            ScreenButton(heroBuildRoot.transform,"BACK",.52f,.06f,.95f,.15f,()=>heroBuildRoot.SetActive(false)); heroBuildRoot.SetActive(false);
            ScreenButton(polishRoot.transform,"INVENTORY",.52f,.015f,.95f,.08f,OpenInventory);
            foreach(var button in polishRoot.GetComponentsInChildren<Button>(true)) if(button.GetComponentInChildren<Text>().text=="BACK") PlaceGearControl(button,.05f,.015f,.48f,.08f);
            ScreenButton(heroDetailsRoot.transform,"LOADOUTS",.04f,.645f,.32f,.72f,()=>OpenHeroBuilds(detailsHero));
        }
        void OpenInventory() {
            if(!atChapters || battle.IsDailyBoss) return;
            inventoryPage=inventorySlot=inventoryQuality=inventoryElement=inventoryView=0; inventorySelection=null; inventoryNotice="";
            RefreshInventory(); inventoryRoot.SetActive(true); inventoryRoot.transform.SetAsLastSibling();
        }
        void RefreshInventory() {
            inventoryRows=battle.InventoryItems(inventorySlot==0?(GearSlot?)null:(GearSlot)(inventorySlot-1),inventoryQuality==0?(HeroQuality?)null:(HeroQuality)(inventoryQuality-1),inventoryElement==0?(Element?)null:(Element)(inventoryElement-1),(InventoryView)inventoryView);
            int pages=Math.Max(1,(inventoryRows.Count+5)/6); inventoryPage=Mathf.Clamp(inventoryPage,0,pages-1);
            inventoryTitle.text="GEAR INVENTORY · "+inventoryRows.Count+" types · Page "+(inventoryPage+1)+"/"+pages+"\nForge materials · "+battle.ForgeMaterials+" · Sorted by rarity";
            inventoryFilters[0].GetComponentInChildren<Text>().text=inventorySlot==0?"ALL SLOTS":((GearSlot)(inventorySlot-1)).ToString();
            inventoryFilters[1].GetComponentInChildren<Text>().text=inventoryQuality==0?"ALL RARITIES":((HeroQuality)(inventoryQuality-1)).ToString();
            inventoryFilters[2].GetComponentInChildren<Text>().text=inventoryElement==0?"ALL ELEMENTS":((Element)(inventoryElement-1)).ToString();
            inventoryFilters[3].GetComponentInChildren<Text>().text=((InventoryView)inventoryView).ToString();
            for(int i=0;i<6;i++) { int index=inventoryPage*6+i; bool present=index<inventoryRows.Count; inventoryCards[i].gameObject.SetActive(present); if(!present) continue; var row=inventoryRows[index]; inventoryCards[i].GetComponentInChildren<Text>().text=row.Gear.Name+" · "+row.Gear.Quality+"\nOwned "+row.Owned+" · Equipped "+row.Equipped+" · Free "+row.Available+(row.Locked?" · LOCKED":""); inventoryCards[i].GetComponent<Image>().color=row.Gear.Id==inventorySelection?new Color(.26f,.20f,.12f):MenuPalette.Control; }
            inventoryPrevious.interactable=inventoryPage>0; inventoryNext.interactable=inventoryPage<pages-1;
            var item=GearDefinition.Find(inventorySelection); bool selected=item!=null && battle.GearCount(item.Id)>0;
            inventoryLock.interactable=selected && !battle.IsDailyBoss && CanManageHeroes; inventoryLock.GetComponentInChildren<Text>().text=selected && battle.GearLocked(item.Id)?"UNLOCK":"LOCK";
            string block=selected?battle.RecycleBlock(item.Id,1):"Select an item to inspect it.";
            inventoryRecycle.interactable=selected && block=="";
            inventoryDetail.text=inventoryNotice+"\n"+(selected?battle.EnhancedGear(item.Id).Description+"\n"+(block==""?"Recycle value · "+battle.RecycleValue(item.Id)+" materials per free copy. Saved profiles do not reserve copies.":block):inventoryRows.Count==0?"No gear matches these filters. Earn gear from campaign victories.":block);
        }
        void OpenRecycle() { if(inventorySelection==null || battle.RecycleBlock(inventorySelection,1)!="") return; recycleSelection=inventorySelection; recycleQuantity=1; RefreshRecycle(); recycleRoot.SetActive(true); recycleRoot.transform.SetAsLastSibling(); }
        void RefreshRecycle() {
            var item=GearDefinition.Find(recycleSelection); string block=battle.RecycleBlock(recycleSelection,recycleQuantity);
            recycleDetail.text=(item==null?"Unavailable item":item.Quality+" "+item.Name)+"\nCopies · "+recycleQuantity+" / "+battle.AvailableGear(recycleSelection)+" unequipped\nReturn · "+((long)recycleQuantity*battle.RecycleValue(recycleSelection))+" Forge materials\n\nEquipped copies, locks, upgraded gear and boss relics are protected. Recycling cannot be undone. Profiles referencing removed copies remain saved but cannot load until a copy is available.\n\n"+block;
            recycleConfirm.interactable=block=="";
        }
        void ConfirmRecycle() {
            int value=recycleQuantity*battle.RecycleValue(recycleSelection);
            if(!battle.RecycleGear(recycleSelection,recycleQuantity)) { RefreshRecycle(); return; }
            SaveProgress(); recycleRoot.SetActive(false); inventoryNotice="Recycled "+recycleQuantity+" copies for "+value+" materials. "+saveNotice; RefreshInventory();
        }
        void OpenHeroBuilds(string id) { if(!CanManageHeroes || battle.HeroProgress(id)==null) return; buildHero=id; buildSlot=0; buildNotice=""; RefreshHeroBuilds(); heroBuildRoot.SetActive(true); heroBuildRoot.transform.SetAsLastSibling(); }
        void RefreshHeroBuilds(bool refreshName=true) {
            var hero=battle.HeroProgress(buildHero); var build=battle.HeroBuild(buildHero,buildSlot);
            buildTitle.text=hero.Identity.Name+" · LOADOUT "+(buildSlot+1)+" / 3";
            if(refreshName) buildName.SetTextWithoutNotify(build==null?"Build "+(buildSlot+1):build.name);
            string text=build==null?"EMPTY · Save this hero's current two skills and three equipment slots.":"SAVED · "+build.name+"\n"+hero.Definition.Skills[build.firstSkill].Name+" + "+hero.Definition.Skills[build.secondSkill].Name;
            if(build!=null) for(int i=0;i<3;i++) { var gear=GearDefinition.Find(build.gear[i]); text+="\n"+(GearSlot)i+" · "+(gear==null?"Empty":battle.EnhancedGear(gear.Id).Name); }
            string block=battle.HeroBuildBlock(buildHero,buildSlot);
            buildDetail.text=text+"\n\n"+(build!=null && block!=""?block+"\n\n":"")+"Loads all slots together, including empty slots. Never takes another hero's gear. Uses current shared Forge/refinement ranks. Explicit loads may replace locked equipment, like manual equips. Talents, levels and formation are unchanged.\n\n"+buildNotice;
            buildSave.interactable=!battle.IsDailyBoss && CanManageHeroes; buildClear.interactable=build!=null && !battle.IsDailyBoss && CanManageHeroes; buildLoad.interactable=build!=null && block==""; buildName.interactable=!battle.IsDailyBoss;
        }
    }
}
