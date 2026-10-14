using UnityEngine;
using UnityEngine.UI;
namespace Ashlight {
    public sealed partial class Prototype {
        GameObject relicRaidRoot;
        Text raidTitle,raidBody;
        Button raidStart,refineButton,lockGearButton,gearSortButton;
        int raidChapterSelection,gearSortMode;
        bool ExpansionModalActive { get { return relicRaidRoot!=null && relicRaidRoot.activeSelf; } }
        void CreateExpansionScreens() {
            relicRaidRoot=ScreenRoot("Relic Challenges");
            raidTitle=Label(relicRaidRoot.transform,"RELIC CHALLENGES",new Vector2(.05f,.85f),new Vector2(.95f,.98f),32);
            raidBody=Label(relicRaidRoot.transform,"",new Vector2(.07f,.25f),new Vector2(.93f,.81f),25);
            ScreenButton(relicRaidRoot.transform,"<",.05f,.135f,.2f,.225f,()=> { raidChapterSelection=System.Math.Max(0,raidChapterSelection-1); RefreshRelicRaid(); });
            ScreenButton(relicRaidRoot.transform,">",.8f,.135f,.95f,.225f,()=> { raidChapterSelection=System.Math.Min(ChapterDefinition.Catalog.Count-1,raidChapterSelection+1); RefreshRelicRaid(); });
            raidStart=ScreenButton(relicRaidRoot.transform,"START",.25f,.135f,.75f,.225f,StartRelicChallenge);
            ScreenButton(relicRaidRoot.transform,"BACK",.25f,.025f,.75f,.115f,()=>relicRaidRoot.SetActive(false)); relicRaidRoot.SetActive(false);
            // Reuse the upper-right header area; the Activities title occupies the left.
            ScreenButton(activitiesRoot.transform,"RELIC RAIDS",.70f,.86f,.95f,.97f,OpenRelicRaid);
            PlaceGearControl(forgeButton,.75f,.915f,.845f,.975f);
            foreach(var button in equipmentRoot.GetComponentsInChildren<Button>(true)) if(button.GetComponentInChildren<Text>().text=="SET BUILDS") { PlaceGearControl(button,.865f,.915f,.96f,.975f); button.GetComponentInChildren<Text>().resizeTextMinSize=10; }
            forgeButton.GetComponentInChildren<Text>().resizeTextMinSize=10;
            lockGearButton=ScreenButton(equipmentRoot.transform,"LOCK",.75f,.845f,.845f,.905f,()=> { if(gearSelected!=null && battle.SetGearLocked(gearSelected,!battle.GearLocked(gearSelected))) { SaveProgress(); RefreshEquipment(); } });
            gearSortButton=ScreenButton(equipmentRoot.transform,"SORT",.865f,.845f,.96f,.905f,()=> { gearSortMode=(gearSortMode+1)%3; gearPage=0; RefreshEquipment(); });
            lockGearButton.GetComponentInChildren<Text>().resizeTextMinSize=10; gearSortButton.GetComponentInChildren<Text>().resizeTextMinSize=10;
            refineButton=ScreenButton(forgeRoot.transform,"REFINE RELIC",.05f,.105f,.95f,.18f,()=> { if(battle.RefineRelic(forgingItem)) { SaveProgress(); RefreshForge(); RefreshVisibleHeroMenus(); } });
        }
        void OpenRelicRaid() { if(!atChapters) return; raidChapterSelection=System.Math.Min(ChapterDefinition.Catalog.Count-1,System.Math.Max(0,battle.Encounter/5-1)); relicRaidRoot.SetActive(true); relicRaidRoot.transform.SetAsLastSibling(); RefreshRelicRaid(); SaveProgress(); }
        void RefreshRelicRaid() {
            var foe=ChapterDefinition.EnemyAt(raidChapterSelection*5+4);
            raidBody.text="CHAPTER "+(raidChapterSelection+1)+" · "+foe.Name+"\nWeak "+foe.Weakness+" · Resists "+foe.Resistance+"\n\nUses your prepared heroes. Survive eight enemy actions or defeat the boss with positive damage.\n\nREWARD · 5 relic essence + 75 materials\n"+battle.RelicRaidRewardsLeft+" rewarded clears left today (shared across bosses).\nUnlimited replays · Daily rewards reset at 00:00 UTC.\n\nEssence owned · "+battle.RelicEssence+"\nRefine owned relics in Equipment → Forge.\nCampaign team and progress are preserved.";
            raidStart.interactable=battle.CanStartDailyBoss && battle.ChapterCleared(raidChapterSelection);
        }
        void StartRelicChallenge() { var run=battle.StartRelicRaid(raidChapterSelection); if(run==null) return; SaveProgress(); campaignBattle=battle; battle=run; bossReported=false; RefreshAppearance(); StartChapter(battle.ChapterIndex); message="RELIC CHALLENGE · Survive eight enemy actions or defeat the boss."; }
        void RefreshGearTools() {
            if(lockGearButton==null) return;
            lockGearButton.interactable=CanManageHeroes && gearSelected!=null;
            lockGearButton.GetComponentInChildren<Text>().text=gearSelected!=null && battle.GearLocked(gearSelected)?"UNLOCK":"LOCK";
            gearSortButton.GetComponentInChildren<Text>().text="SORT · "+new[]{"RARITY","POWER","ELEMENT"}[gearSortMode];
        }
        void RefreshRefinement() {
            if(refineButton==null) return; var item=GearDefinition.Find(forgingItem); bool relic=item!=null && item.RelicChapter>=0;
            refineButton.gameObject.SetActive(relic); refineButton.interactable=relic && battle.CanRefineRelic(forgingItem);
            if(relic) refineButton.GetComponentInChildren<Text>().text="REFINE R"+battle.RelicRefinement(forgingItem)+"/5 · "+battle.RefinementCost(forgingItem)+" ESSENCE · OWNED "+battle.RelicEssence;
        }
    }
}
