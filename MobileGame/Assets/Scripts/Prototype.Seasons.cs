using UnityEngine;
using UnityEngine.UI;
namespace Ashlight {
    public static class MenuPalette {
        public static readonly Color Surface=new Color(.035f,.04f,.052f,.99f), Control=new Color(.075f,.085f,.10f,.97f), Text=new Color(.91f,.88f,.82f), Gold=new Color(1,.79f,.36f);
        public static Color Rarity(HeroQuality quality) { return quality==HeroQuality.Legendary?Gold:quality==HeroQuality.Epic?new Color(.78f,.60f,.95f):quality==HeroQuality.Rare?new Color(.48f,.72f,.95f):quality==HeroQuality.Uncommon?new Color(.54f,.80f,.61f):Text; }
    }
    public sealed partial class Prototype {
        GameObject setGuideRoot,bondsRoot,weeklyRoot;
        Text setGuideTitle,bondsTitle,bondsStory,bondsNotice,weeklyTitle,weeklyBody,activitiesWeekly;
        PortraitView bondPortrait;
        readonly Button[] bondTabs=new Button[3];
        Button bondCompanion,weeklyStart;
        string bondHero; int bondChapter,reviewedWeekly;
        bool SeasonModalActive { get { return (setGuideRoot!=null && setGuideRoot.activeSelf) || (bondsRoot!=null && bondsRoot.activeSelf) || (weeklyRoot!=null && weeklyRoot.activeSelf); } }
        void CreateSeasonScreens() {
            setGuideRoot=ScreenRoot("Equipment Set Guide");
            setGuideTitle=Label(setGuideRoot.transform,"",new Vector2(.05f,.83f),new Vector2(.95f,.98f),26);
            for(int i=0;i<6;i++) { float left=i%2==0?.05f:.52f,bottom=.62f-i/2*.21f; var element=(Element)i;
                Panel(setGuideRoot.transform,new Vector2(left,bottom),new Vector2(left+.43f,bottom+.19f),MenuPalette.Control);
                var text=Label(setGuideRoot.transform,GearSets.Name(element)+" · "+element+"\n2 PIECES · "+GearSets.Bonus(element,2)+"\n3 PIECES · "+GearSets.Bonus(element,3),new Vector2(left+.01f,bottom),new Vector2(left+.42f,bottom+.19f),21); text.color=CombatEffects.Tint(element);
            }
            Label(setGuideRoot.transform,"Match the gear's element across distinct slots. Mixed rarities and forged copies count.\nOne set can be active; 3-piece bonuses include the 2-piece effect.",new Vector2(.05f,.125f),new Vector2(.95f,.195f),18);
            ScreenButton(setGuideRoot.transform,"BACK",.25f,.025f,.75f,.115f,()=>setGuideRoot.SetActive(false)); setGuideRoot.SetActive(false);
            bondsRoot=ScreenRoot("Hero Bonds");
            bondsTitle=Label(bondsRoot.transform,"",new Vector2(.05f,.84f),new Vector2(.95f,.98f),28);
            for(int i=0;i<3;i++) { int chapter=i; float left=.05f+i*.31f; bondTabs[i]=ScreenButton(bondsRoot.transform,new[]{"ORIGIN","TRUST","COMPANION"}[i],left,.73f,left+.28f,.815f,()=> { bondChapter=chapter; RefreshBonds(); }); }
            bondPortrait=CreatePortrait(bondsRoot.transform,new Vector2(.05f,.31f),new Vector2(.30f,.70f));
            bondsStory=Label(bondsRoot.transform,"",new Vector2(.35f,.31f),new Vector2(.95f,.70f),27);
            bondsNotice=Label(bondsRoot.transform,"",new Vector2(.05f,.155f),new Vector2(.95f,.29f),21);
            bondCompanion=ScreenButton(bondsRoot.transform,"COMPANION",.05f,.025f,.64f,.125f,()=> { var partner=HeroBonds.Companion(battle.HeroProgress(bondHero).Identity); if(battle.HeroProgress(partner.Id)!=null) { bondsRoot.SetActive(false); OpenHeroDetails(partner.Id); } });
            ScreenButton(bondsRoot.transform,"BACK",.69f,.025f,.95f,.125f,()=> { bondsRoot.SetActive(false); OpenHeroDetails(bondHero); }); bondsRoot.SetActive(false);
            weeklyRoot=ScreenRoot("Weekly Challenge");
            weeklyTitle=Label(weeklyRoot.transform,"",new Vector2(.05f,.83f),new Vector2(.95f,.98f),32); weeklyTitle.color=MenuPalette.Gold;
            weeklyBody=Label(weeklyRoot.transform,"",new Vector2(.07f,.24f),new Vector2(.93f,.79f),26);
            weeklyStart=ScreenButton(weeklyRoot.transform,"START CHALLENGE",.05f,.04f,.67f,.18f,StartWeeklyChallenge);
            ScreenButton(weeklyRoot.transform,"BACK",.72f,.04f,.95f,.18f,()=>weeklyRoot.SetActive(false)); weeklyRoot.SetActive(false);
            activitiesWeekly=Label(activitiesRoot.transform,"",new Vector2(.05f,.425f),new Vector2(.95f,.575f),20);
            ScreenButton(activitiesRoot.transform,"WEEKLY",.66f,.25f,.95f,.39f,OpenWeeklyChallenge);
        }
        void OpenSetGuide() { setGuideTitle.text=ManagedHero.Identity.Name+" · SET BUILDS\n"+ManagedHero.SetSummary; setGuideRoot.SetActive(true); setGuideRoot.transform.SetAsLastSibling(); }
        void OpenBonds() { if(battle.HeroProgress(detailsHero)==null) return; bondHero=detailsHero; bondChapter=0; heroDetailsRoot.SetActive(false); bondsRoot.SetActive(true); bondsRoot.transform.SetAsLastSibling(); RefreshBonds(); }
        void RefreshBonds() {
            var hero=battle.HeroProgress(bondHero); var partner=HeroBonds.Companion(hero.Identity); bool unlocked=battle.BondUnlocked(bondHero,bondChapter);
            ShowPortrait(bondPortrait,hero); bondsTitle.text=hero.Identity.Name+" · BONDS\n"+hero.BondBattles+" scored participations · Stories give no stat or reward bonus";
            for(int i=0;i<3;i++) { bondTabs[i].GetComponent<Image>().color=i==bondChapter?new Color(.22f,.18f,.10f):MenuPalette.Control; bondTabs[i].GetComponentInChildren<Text>().text=new[]{"ORIGIN","TRUST","COMPANION"}[i]+(battle.BondUnlocked(bondHero,i)?"":" · LOCKED"); }
            bondsStory.text=unlocked?HeroBonds.Story(hero.Identity,bondChapter):bondChapter==1?"Earn 3 scored participations with this hero to reveal the next story.":"Earn 10 scored participations with "+hero.Identity.Name+", and recruit "+partner.Name+" with at least 3 participations.";
            bondsNotice.text="Claimed campaign/Tower victories and completed positive-score challenges build trust.\nCOMPANION · "+partner.Name+" · "+partner.FactionName+"\nThese stories are optional. Reading them spends nothing.";
            bondCompanion.interactable=battle.HeroProgress(partner.Id)!=null; bondCompanion.GetComponentInChildren<Text>().text="REVIEW "+partner.Name.ToUpperInvariant();
        }
        void OpenWeeklyChallenge() {
            if(!atChapters) return; int index=battle.WeeklyIndex; reviewedWeekly=index; var foe=Battle.WeeklyEnemy(index);
            weeklyTitle.text=WeeklyHeading(index);
            weeklyBody.text=foe.Name+" · Weak "+foe.Weakness+" · Resists "+foe.Resistance+"\n"+Battle.WeeklyRules(index)+"\n\nGOAL · Survive eight enemy rounds or defeat the boss.\nPersonal best · "+battle.WeeklyBest(index)+" damage\n\n"+(battle.WeeklyRewardAvailable?"FIRST CLEAR · 150 crystals, 150 materials and 5 shards per participating hero.":"This week's reward claimed. Replay to improve your score.")+"\n\nUses your current team · Unlocks at chapter 4\nPrepare heroes and skills before starting.\nChanges Monday at 00:00 UTC. Campaign progress stays intact.";
            weeklyStart.interactable=battle.CanStartWeekly; weeklyRoot.SetActive(true); weeklyRoot.transform.SetAsLastSibling(); SaveProgress();
        }
        string WeeklyHeading(int index) { return "WEEKLY · "+Battle.WeeklyName(index); }
        void StartWeeklyChallenge() { if(reviewedWeekly!=battle.WeeklyIndex) { OpenWeeklyChallenge(); return; } var run=battle.StartWeeklyTrial(); if(run==null) return; SaveProgress(); campaignBattle=battle; battle=run; bossReported=false; RefreshAppearance(); StartChapter(battle.ChapterIndex); message=Battle.WeeklyName(run.TrialIndex)+" · "+Battle.WeeklyRules(run.TrialIndex); }
    }
}
