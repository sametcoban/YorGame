using UnityEngine;
using UnityEngine.UI;
namespace Ashlight {
    public sealed partial class Prototype {
        GameObject activitiesRoot,guideRoot,guidanceRoot,bossReportRoot;
        Text activitiesInfo,guideTitle,guideDetail,guidanceTitle,guidanceBody,bossReport;
        Button bossStart,guidePrevious,guideNext;
        readonly Button[] guideCards=new Button[6];
        int guidePage; string guidanceHero;
        Battle campaignBattle;
        bool bossReported;
        Text actionCue;
        readonly Text[] statusBadges=new Text[Battle.MaxPartySize];
        void CreateActivitiesScreens() {
            activitiesRoot=ScreenRoot("Activities");
            Label(activitiesRoot.transform,"ACTIVITIES",new Vector2(.05f,.86f),new Vector2(.95f,.98f),34);
            activitiesInfo=Label(activitiesRoot.transform,"",new Vector2(.05f,.59f),new Vector2(.95f,.83f),21);
            bossStart=ScreenButton(activitiesRoot.transform,"DAILY BOSS",.05f,.25f,.34f,.39f,StartBossChallenge);
            ScreenButton(activitiesRoot.transform,"ENEMY GUIDE",.365f,.25f,.635f,.39f,()=> { guidePage=0; guideRoot.SetActive(true); guideRoot.transform.SetAsLastSibling(); guideDetail.text="Fight enemies to reveal their abilities and counters."; RefreshGuide(); });
            ScreenButton(activitiesRoot.transform,"REPLAY CHAPTER SCENE",.05f,.135f,.48f,.225f,ReplayCurrentStory);
            ScreenButton(activitiesRoot.transform,"FIRST JOURNEY",.52f,.135f,.95f,.225f,OpenBetaOpening);
            ScreenButton(activitiesRoot.transform,"FEEDBACK",.365f,.025f,.635f,.115f,OpenFeedback);
            ScreenButton(activitiesRoot.transform,"HOME",.68f,.025f,.95f,.115f,()=>activitiesRoot.SetActive(false)); activitiesRoot.SetActive(false);
            guideRoot=ScreenRoot("Enemy Guide");
            guideTitle=Label(guideRoot.transform,"",new Vector2(.05f,.88f),new Vector2(.95f,.98f),28);
            for(int i=0;i<6;i++) { int index=i; float left=i%2==0?.05f:.52f, bottom=.69f-i/2*.145f;
                guideCards[i]=ScreenButton(guideRoot.transform,"",left,bottom,left+.43f,bottom+.13f,()=>SelectGuideEntry(guidePage*6+index));
                guideCards[i].GetComponentInChildren<Text>().resizeTextMaxSize=21;
            }
            guideDetail=Label(guideRoot.transform,"",new Vector2(.05f,.145f),new Vector2(.95f,.39f),21);
            guidePrevious=ScreenButton(guideRoot.transform,"<",.05f,.025f,.2f,.12f,()=> { guidePage--; RefreshGuide(); });
            ScreenButton(guideRoot.transform,"BACK",.25f,.025f,.75f,.12f,()=>guideRoot.SetActive(false));
            guideNext=ScreenButton(guideRoot.transform,">",.8f,.025f,.95f,.12f,()=> { guidePage++; RefreshGuide(); }); guideRoot.SetActive(false);
            guidanceRoot=ScreenRoot("Next Hero Upgrades");
            guidanceTitle=Label(guidanceRoot.transform,"",new Vector2(.05f,.84f),new Vector2(.95f,.98f),30);
            guidanceBody=Label(guidanceRoot.transform,"",new Vector2(.06f,.25f),new Vector2(.94f,.81f),25);
            ScreenButton(guidanceRoot.transform,"EQUIPMENT",.05f,.135f,.32f,.225f,()=> { guidanceRoot.SetActive(false); detailsHero=guidanceHero; OpenDetailsManagement(1); });
            ScreenButton(guidanceRoot.transform,"TALENTS",.365f,.135f,.635f,.225f,()=> { guidanceRoot.SetActive(false); detailsHero=guidanceHero; OpenDetailsManagement(2); });
            ScreenButton(guidanceRoot.transform,"UPGRADES",.68f,.135f,.95f,.225f,()=> { guidanceRoot.SetActive(false); OpenAdvancement(guidanceHero); });
            ScreenButton(guidanceRoot.transform,"BACK",.25f,.025f,.75f,.115f,()=>guidanceRoot.SetActive(false)); guidanceRoot.SetActive(false);
            bossReportRoot=ScreenRoot("Daily Boss Results");
            bossReport=Label(bossReportRoot.transform,"",new Vector2(.07f,.22f),new Vector2(.93f,.92f),29);
            ScreenButton(bossReportRoot.transform,"RETURN HOME",.25f,.045f,.75f,.17f,CloseBossChallenge); bossReportRoot.SetActive(false);
        }
        void OpenActivities() {
            if(!atChapters) return;
            activitiesInfo.text="DAILY BOSS · "+battle.DailyBossName+"\nPersonal best · "+battle.DailyBossBest(battle.DailyBossIndex)+" damage\nEight rounds · Uses your current formation\n"+(battle.DailyBossRewardAvailable?"First completed attempt with damage: +35 crystals and +30 materials.":"Today's reward claimed · Replay to improve your score.")+"\nResets at 00:00 UTC · Unlocked on reaching chapter 3\n\nENEMY GUIDE · "+battle.DiscoveredEnemies.Length+" / "+EnemyGuideEntry.Catalog.Count+" enemy variants discovered";
            activitiesWeekly.text=WeeklyHeading(battle.WeeklyIndex)+"\n"+Battle.WeeklyRules(battle.WeeklyIndex)+"\nUnlocks at chapter 4 · One clear reward per UTC week";
            bossStart.interactable=battle.CanStartDailyBoss; activitiesRoot.SetActive(true); activitiesRoot.transform.SetAsLastSibling(); SaveProgress();
        }
        void OpenUpgradeGuidance(string id) {
            if(!CanManageHeroes || battle.HeroProgress(id)==null) return;
            guidanceHero=id; guidanceTitle.text=battle.HeroProgress(id).Identity.Name+" · NEXT UPGRADES";
            guidanceBody.text=battle.UpgradeGuidance(id); guidanceRoot.SetActive(true); guidanceRoot.transform.SetAsLastSibling();
        }
        void RefreshGuide() {
            int pages=(EnemyGuideEntry.Catalog.Count+5)/6; guidePage=Mathf.Clamp(guidePage,0,pages-1);
            guideTitle.text="ENEMY GUIDE · "+battle.DiscoveredEnemies.Length+" / "+EnemyGuideEntry.Catalog.Count+" variants · Page "+(guidePage+1)+" / "+pages;
            guidePrevious.interactable=guidePage>0; guideNext.interactable=guidePage<pages-1;
            for(int i=0;i<6;i++) { int entry=guidePage*6+i; bool exists=entry<EnemyGuideEntry.Catalog.Count;
                guideCards[i].gameObject.SetActive(exists); if(!exists) continue;
                var item=EnemyGuideEntry.Catalog[entry]; bool known=battle.EnemyDiscovered(item.Key); guideCards[i].interactable=known;
                guideCards[i].GetComponentInChildren<Text>().text=known?item.Enemy.Name+"\n"+item.Enemy.Role+" · Weak "+item.Enemy.Weakness+" · Resists "+item.Enemy.Resistance:"UNDISCOVERED\nFight this variant to reveal it";
            }
        }
        void SelectGuideEntry(int index) {
            var entry=EnemyGuideEntry.Catalog[index]; if(!battle.EnemyDiscovered(entry.Key)) return;
            string moves=""; for(int turn=0;turn<3;turn++) { var move=EnemySkills.ForRole(entry.Encounter,entry.Enemy,turn); moves+=(turn==0?"":" → ")+move.Name; }
            if(entry.Enemy.Name=="Cinder Sovereign") moves="Quick Strike → Fire Hex → Inferno Charge → Charged / Disrupted Inferno";
            if(entry.Enemy.Name=="Veiled Monarch") moves="Quick Strike → Eclipse Veil → Light Tempest";
            if(entry.Enemy.Name=="Thorn Queen") moves="Quick Strike → Root Communion → Poison Tempest";
            guideDetail.text=entry.Enemy.Name+" · "+entry.Enemy.Row+" "+entry.Enemy.Role+"\nWeakness "+entry.Enemy.Weakness+" ×1.5 · Resistance "+entry.Enemy.Resistance+" ×0.5 (Physical stays ×1)\nTypical opening: "+moves+"\n"+entry.Advice;
        }
        void StartBossChallenge() {
            var run=battle.StartDailyBoss(); if(run==null) return;
            SaveProgress(); campaignBattle=battle; battle=run; bossReported=false;
            RefreshAppearance(); StartChapter(battle.ChapterIndex); message="DAILY BOSS · Deal as much damage as possible in eight rounds.";
        }
        void ShowBossReport() {
            if(bossReported || campaignBattle==null) return;
            bool rewarded; if(!campaignBattle.FinishDailyBoss(battle,out rewarded)) return;
            bossReported=true; autoPlay=false; Time.timeScale=1; SaveProgress();
            bool weekly=battle.IsWeeklyTrial; string rewardText=rewarded?(weekly?"+150 CRYSTALS · +150 MATERIALS · +5 HERO SHARDS EACH":"+35 CRYSTALS · +30 MATERIALS"):weekly?(!battle.WeeklyGoalMet?"Goal not met · Survive 8 rounds or defeat the boss":"Weekly reward already claimed"):battle.DailyBossScore==0?"No damage · No reward":"Daily reward already claimed";
            bossReport.text=(weekly?(battle.WeeklyGoalMet?"WEEKLY CLEAR":"WEEKLY ATTEMPT COMPLETE"):"DAILY BOSS COMPLETE")+"\n"+battle.Enemy.Name+"\n\nDAMAGE · "+battle.DailyBossScore+"\nPersonal best · "+(weekly?campaignBattle.WeeklyBest(battle.TrialIndex):campaignBattle.DailyBossBest(battle.RunBossIndex))+"\nEnemy rounds completed · "+battle.DailyBossRounds+" / 8\n\n"+rewardText+"\n"+saveNotice;
            foreach(var hero in battle.Party) bossReport.text+="\n"+hero.Identity.Name+" · "+hero.EncounterDamage+" damage · "+hero.EncounterHealing+" healing";
            bossReportRoot.SetActive(true); bossReportRoot.transform.SetAsLastSibling();
        }
        void CloseBossChallenge() {
            if(campaignBattle==null) return;
            if(!bossReported && (battle.Current==Phase.Won || battle.Current==Phase.Lost)) ShowBossReport();
            autoPlay=false;
            CancelActionPresentation(); if(enemyTurn!=null) StopCoroutine(enemyTurn); enemyTurn=null;
            battle=campaignBattle; campaignBattle=null; bossReportRoot.SetActive(false); RefreshAppearance(); EnterChapters();
        }
        void CreateBattleReadability() {
            status.rectTransform.anchorMax=new Vector2(.965f,.915f);
            actionCue=Label(safe,"",new Vector2(.035f,.92f),new Vector2(.965f,.98f),28);
            actionCue.transform.SetSiblingIndex(status.transform.GetSiblingIndex()+1);
            for(int i=0;i<statusBadges.Length;i++) { float left=.025f+i*.194f;
                statusBadges[i]=Label(safe,"",new Vector2(left,.156f),new Vector2(left+.18f,.179f),14);
                statusBadges[i].transform.SetSiblingIndex(status.transform.GetSiblingIndex()+1);
            }
        }
        void RefreshBattleReadability() {
            actionCue.gameObject.SetActive(!atChapters); actionCue.text=message;
            actionCue.color=presentingAction && battle.LastUltimate!=null?new Color(1,.8f,.25f):new Color(.95f,.9f,.8f);
            for(int i=0;i<statusBadges.Length;i++) {
                bool visible=!atChapters && i<battle.Party.Count && battle.Party[i].Health>0; statusBadges[i].gameObject.SetActive(visible); if(!visible) continue;
                var hero=battle.Party[i]; statusBadges[i].text=(hero.Status==Debuff.None?"":"["+hero.Status.ToString().ToUpperInvariant()+" "+hero.StatusRounds+"] ")+(hero.Guard>0?"[WARD "+hero.Guard+"] ":"")+(hero.TauntRounds>0?"[TAUNT]":"");
                statusBadges[i].color=hero.Status==Debuff.None?new Color(.6f,.85f,1):new Color(1,.64f,.4f);
                if(hero.Energy==100) energyBars[i].color=Color.Lerp(new Color(1,.65f,.15f),new Color(1,.92f,.5f),(Mathf.Sin(Time.unscaledTime*5)+1)*.5f);
            }
            if(battle.IsDailyBoss && !presentingAction && enemyTurn==null && (battle.Current==Phase.Lost || battle.Current==Phase.Won)) ShowBossReport();
        }
    }
}
