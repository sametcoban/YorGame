using UnityEngine;
using UnityEngine.UI;
namespace Ashlight {
    public sealed partial class Prototype {
        GameObject homeRoot, summonLobbyRoot, idleRoot, milestonesRoot, milestoneStoryRoot, advancementRoot;
        Text homeTitle,homeWallet,homeNotice,summonLobbyWallet,idleSummary,idleNotice,milestonePageLabel,milestoneStoryTitle,milestoneStoryBody,milestoneStoryRewards,advanceTitle,advanceLevel,advanceAscension,advanceEquipment,advanceNotice;
        Button homeCampaign,homeHeroes,homeSummoning,homeIdle,homeDaily,homeTower,homeMilestones,homeSound,idleClaim,milestoneClaim,advanceConfirm,advanceIdle;
        Image idleMeter,advanceXP;
        readonly Button[] homeParty=new Button[Battle.MaxPartySize];
        readonly PortraitView[] homePortraits=new PortraitView[Battle.MaxPartySize];
        Button[] milestoneCards; Button milestonePrevious,milestoneNext;
        int milestonePage,storyChapter; float nextHomeRefresh;
        string homeToast="", advancementHero, managementHero; bool detailsReturnHome, idleReturnsToUpgrades;
        PartyHero ManagedHero { get { return managementHero==null?battle.Party[battle.ActiveIndex]:battle.HeroProgress(managementHero); } }
        bool CanManageHeroes { get { return battle.CanChangeParty || battle.CampaignComplete; } }
        GameObject ScreenRoot(string name) {
            var root=new GameObject(name,typeof(RectTransform),typeof(Image)); root.transform.SetParent(safe,false);
            var rect=root.GetComponent<RectTransform>(); rect.anchorMin=Vector2.zero; rect.anchorMax=Vector2.one; rect.offsetMin=rect.offsetMax=Vector2.zero;
            root.GetComponent<Image>().color=new Color(.025f,.035f,.055f,.99f); return root;
        }
        Button ScreenButton(Transform root,string text,float left,float bottom,float right,float top,UnityEngine.Events.UnityAction action) {
            var button=MakeButton(root,text,left,right,action); PlaceGearControl(button,left,bottom,right,top); return button;
        }
        void CreateHomeScreens() {
            homeRoot=ScreenRoot("Home Screen");
            homeTitle=Label(homeRoot.transform,"ASHLIGHT",new Vector2(.05f,.86f),new Vector2(.75f,.98f),42); homeTitle.color=new Color(1,.82f,.4f);
            ScreenButton(homeRoot.transform,"ACTIVITIES",.77f,.865f,.95f,.98f,OpenActivities);
            homeWallet=Label(homeRoot.transform,"",new Vector2(.05f,.77f),new Vector2(.95f,.845f),22);
            homeNotice=Label(homeRoot.transform,"",new Vector2(.05f,.715f),new Vector2(.95f,.765f),18);
            Panel(homeRoot.transform,new Vector2(.05f,.85f),new Vector2(.95f,.853f),new Color(.6f,.43f,.18f));
            homeCampaign=ScreenButton(homeRoot.transform,"CAMPAIGN",.05f,.49f,.48f,.70f,OpenCampaign);
            homeHeroes=ScreenButton(homeRoot.transform,"HEROES",.52f,.49f,.95f,.70f,OpenCollection);
            homeSummoning=ScreenButton(homeRoot.transform,"SUMMONING",.05f,.305f,.48f,.48f,OpenSummoning);
            homeIdle=ScreenButton(homeRoot.transform,"IDLE REWARDS",.52f,.305f,.95f,.48f,OpenIdleRewards);
            foreach(var button in new[]{homeCampaign,homeHeroes,homeSummoning,homeIdle}) { button.GetComponentInChildren<Text>().resizeTextMinSize=20; button.GetComponentInChildren<Text>().resizeTextMaxSize=30; }
            homeCampaign.GetComponent<Image>().color=new Color(.23f,.17f,.09f);
            for(int i=0;i<homeParty.Length;i++) {
                int slot=i; float left=.05f+i*.182f;
                homeParty[i]=ScreenButton(homeRoot.transform,"",left,.155f,left+.17f,.285f,()=> { if(slot<battle.Party.Count) { detailsReturnHome=true; OpenHeroDetails(battle.Party[slot].Identity.Id); } });
                var text=homeParty[i].GetComponentInChildren<Text>(); var r=text.GetComponent<RectTransform>(); r.anchorMin=new Vector2(.35f,.03f); r.anchorMax=new Vector2(.98f,.97f); r.offsetMin=r.offsetMax=Vector2.zero; text.resizeTextMinSize=18; text.resizeTextMaxSize=24;
                homePortraits[i]=CreatePortrait(homeParty[i].transform,new Vector2(.025f,.12f),new Vector2(.32f,.95f));
            }
            homeTower=ScreenButton(homeRoot.transform,"TOWER",.05f,.02f,.26f,.145f,()=> { if(battle.EnterTower()) { RefreshAppearance(); StartChapter(battle.ChapterIndex); SaveProgress(); } });
            homeDaily=ScreenButton(homeRoot.transform,"DAILY QUESTS",.28f,.02f,.49f,.145f,()=> { dailyRoot.transform.SetAsLastSibling(); dailyRoot.SetActive(true); RefreshDailyMenu(); });
            homeMilestones=ScreenButton(homeRoot.transform,"MILESTONES",.51f,.02f,.72f,.145f,OpenMilestones);
            homeSound=ScreenButton(homeRoot.transform,"SOUND ON",.74f,.02f,.95f,.145f,()=>sounds.ToggleMute());
            soundButton=homeSound;
            CreateSummonLobby(); CreateWishlistScreens(); CreateIdleScreen(); CreateMilestoneScreens(); CreateAdvancementScreen(); homeRoot.SetActive(false);
        }
        void CloseHubPanels() {
            managementHero=null;
            foreach(var root in new[]{homeRoot,activitiesRoot,guideRoot,guidanceRoot,bossReportRoot,recommendationRoot,storyRoot,betaRoot,feedbackRoot,summonLobbyRoot,wishlistRoot,idleRoot,milestonesRoot,milestoneStoryRoot,advancementRoot,forgeRoot,recruitRoot,heroDetailsRoot,collectionRoot,dailyRoot,equipmentRoot,selectionRoot,formationRoot,progressionRoot,teamPresetRoot}) if(root!=null) root.SetActive(false);
        }
        void OpenHome() {
            if(!atChapters) return;
            chapterRoot.SetActive(false); managementHero=null; homeRoot.SetActive(true); homeRoot.transform.SetAsLastSibling(); RefreshHome();
        }
        void OpenCampaign() {
            if(!atChapters) return;
            homeRoot.SetActive(false); chapterRoot.SetActive(true); chapterRoot.transform.SetAsLastSibling();
            chapterTitle.text="CAMPAIGN\n"+(battle.CampaignComplete?"All 12 chapters cleared · Claim your milestones or challenge the Tower.":"Chapter "+(battle.ChapterIndex+1)+" · Stage "+(battle.StageIndex+1)+" · "+ChapterDefinition.Catalog[battle.ChapterIndex].Name)+"\n"+battle.Crystals+" crystals · "+saveNotice;
            chapterPage=battle.ChapterIndex/6; RefreshChapterPage();
        }
        void RefreshHome() {
            if(homeRoot==null) return;
            int ascensions=0; foreach(var hero in battle.Recruits) { var member=battle.HeroProgress(hero.Id); if(member.UpgradeRank<PartyHero.MaxUpgradeRank && member.Shards>=member.UpgradeCost) ascensions++; }
            homeWallet.text=battle.Crystals+" CRYSTALS · "+battle.ForgeMaterials+" MATERIALS · "+battle.Recruits.Count+"/30 HEROES\nTEAM POWER "+battle.TeamPower+" · "+(battle.CampaignComplete?"CAMPAIGN COMPLETE":"CHAPTER "+(battle.ChapterIndex+1)+" · STAGE "+(battle.StageIndex+1))+" · "+saveNotice;
            homeNotice.text=homeToast!=""?homeToast:battle.ReadyMilestones>0?battle.ReadyMilestones+" chapter milestone rewards ready to claim.":ascensions>0?ascensions+" heroes ready to ascend · Open Heroes to compare upgrades.":"Prepare your heroes, claim idle XP, and continue your journey.";
            homeNotice.color=saveFailed?new Color(1,.4f,.3f):new Color(.85f,.8f,.68f);
            homeCampaign.GetComponentInChildren<Text>().text="CAMPAIGN\n"+(battle.CampaignComplete?"All chapters cleared":"Continue Chapter "+(battle.ChapterIndex+1)+" · "+(battle.StageIndex+1)+"/5")+"\n"+(battle.CampaignComplete?"View your journey":ChapterDefinition.Catalog[battle.ChapterIndex].Name);
            homeHeroes.GetComponentInChildren<Text>().text="HEROES\n"+battle.Recruits.Count+" recruited · "+battle.Party.Count+" in formation\n"+(ascensions>0?ascensions+" ready to ascend":"Skills · equipment · progression");
            homeSummoning.GetComponentInChildren<Text>().text="SUMMONING\n1 HERO · 100     10 HEROES · 1,000\nLegendary guaranteed within "+(Summoning.LegendaryGuarantee-battle.LegendaryMisses)+" pulls";
            homeIdle.GetComponentInChildren<Text>().text="IDLE REWARDS\n+"+battle.IdleCrystals+" crystals · +"+battle.IdleExperience+" XP each\n+"+battle.IdleForgeMaterials+" materials · "+battle.IdleMinutes+"/720 minutes banked";
            int quests=0; for(int q=0;q<4;q++) if(battle.CanClaimDaily(q)) quests++;
            homeDaily.GetComponentInChildren<Text>().text="DAILY QUESTS"+(quests>0?"\n"+quests+" READY":"");
            homeMilestones.GetComponentInChildren<Text>().text="MILESTONES"+(battle.ReadyMilestones>0?"\n"+battle.ReadyMilestones+" READY":"");
            homeTower.interactable=battle.CanEnterTower; homeTower.GetComponentInChildren<Text>().text=battle.TowerComplete?"TOWER CLEARED":battle.Recruits.Count<5?"TOWER\nRecruit 5 to unlock":"TOWER\nFloor "+battle.TowerFloor+"/100";
            homeSound.GetComponentInChildren<Text>().text=sounds.Muted?"SOUND OFF":"SOUND ON";
            for(int i=0;i<homeParty.Length;i++) {
                bool occupied=i<battle.Party.Count; homeParty[i].interactable=occupied;
                ShowPortrait(homePortraits[i],occupied?battle.Party[i]:null);
                homeParty[i].GetComponentInChildren<Text>().text=occupied?battle.Party[i].Identity.Name+"\nLv "+battle.Party[i].CombatLevel+"\n"+(battle.IsFrontRow(i)?"FRONT":"BACK"):"LOCKED\nChapter "+(i+1);
            }
            if(summonLobbyRoot.activeSelf) RefreshSummonLobby(); if(idleRoot.activeSelf) RefreshIdleScreen();
        }
        void CreateSummonLobby() {
            summonLobbyRoot=ScreenRoot("Summoning Lobby");
            Label(summonLobbyRoot.transform,"SUMMONING",new Vector2(.05f,.83f),new Vector2(.95f,.98f),36).color=new Color(1,.82f,.4f);
            summonLobbyWallet=Label(summonLobbyRoot.transform,"",new Vector2(.05f,.69f),new Vector2(.95f,.82f),24);
            Label(summonLobbyRoot.transform,"Base odds · "+Summoning.Odds,new Vector2(.05f,.63f),new Vector2(.95f,.68f),19);
            for(int i=0;i<6;i++) {
                float left=.05f+i*.151f; var hero=HeroDefinition.Common((HeroClass)i);
                var portrait=CreatePortrait(summonLobbyRoot.transform,new Vector2(left,.44f),new Vector2(left+.14f,.61f)); ShowPortrait(portrait,new PartyHero(hero));
                Label(summonLobbyRoot.transform,hero.DisplayClass,new Vector2(left,.39f),new Vector2(left+.14f,.435f),18);
            }
            Label(summonLobbyRoot.transform,"30 named heroes · six classes · Common to Legendary\nDuplicate heroes grant shards and refund crystals.\nRare or better guaranteed within 5 pulls; Legendary within 15.",new Vector2(.05f,.255f),new Vector2(.95f,.385f),22);
            summonButton=ScreenButton(summonLobbyRoot.transform,"SUMMON x1 · 100",.05f,.13f,.48f,.24f,SummonHero);
            summonTenButton=ScreenButton(summonLobbyRoot.transform,"SUMMON x10 · 1,000",.52f,.13f,.95f,.24f,()=>SummonHeroes(10));
            wishlistLobbyButton=ScreenButton(summonLobbyRoot.transform,"WISHLIST",.05f,.02f,.65f,.115f,OpenWishlist);
            ScreenButton(summonLobbyRoot.transform,"BACK",.70f,.02f,.95f,.115f,()=>summonLobbyRoot.SetActive(false)); summonLobbyRoot.SetActive(false);
        }
        void OpenSummoning() { if(!atChapters) return; summonLobbyRoot.transform.SetAsLastSibling(); summonLobbyRoot.SetActive(true); RefreshSummonLobby(); }
        void RefreshSummonLobby() {
            summonLobbyWallet.text=SummonWallet()+"\nWishlist · "+battle.Wishlist.Length+" selected · Rare+ choices have 3× weight within their rarity.";
            wishlistLobbyButton.GetComponentInChildren<Text>().text="WISHLIST · "+battle.Wishlist.Length+" / 6";
            summonButton.interactable=battle.CanSummon; summonTenButton.interactable=battle.CanSummonTen;
            summonButton.GetComponentInChildren<Text>().text="SUMMON x1 · 100";
        }
        void CreateIdleScreen() {
            idleRoot=ScreenRoot("Idle Rewards");
            Label(idleRoot.transform,"IDLE REWARDS",new Vector2(.05f,.83f),new Vector2(.95f,.98f),36).color=new Color(1,.82f,.4f);
            idleSummary=Label(idleRoot.transform,"",new Vector2(.08f,.48f),new Vector2(.92f,.80f),30);
            idleMeter=Bar(idleRoot.transform,new Vector2(.08f,.445f),new Vector2(.92f,.465f),new Color(.8f,.6f,.25f));
            Label(idleRoot.transform,"Rewards accumulate for up to 12 hours.\nXP goes to every recruited hero, including reserves.\nClear campaign stages to increase the reward rate.",new Vector2(.08f,.255f),new Vector2(.92f,.425f),24);
            idleNotice=Label(idleRoot.transform,"",new Vector2(.08f,.16f),new Vector2(.92f,.245f),20);
            idleClaim=ScreenButton(idleRoot.transform,"CLAIM REWARDS",.05f,.025f,.65f,.135f,ClaimIdleRewards);
            ScreenButton(idleRoot.transform,"BACK",.70f,.025f,.95f,.135f,CloseIdleRewards); idleRoot.SetActive(false);
        }
        void OpenIdleRewards() { if(!atChapters) return; idleReturnsToUpgrades=false; idleNotice.text=""; idleRoot.transform.SetAsLastSibling(); idleRoot.SetActive(true); RefreshIdleScreen(); }
        void CloseIdleRewards() {
            idleRoot.SetActive(false); RefreshVisibleHeroMenus();
            if(idleReturnsToUpgrades) { advancementRoot.SetActive(true); advancementRoot.transform.SetAsLastSibling(); RefreshAdvancement(); }
            idleReturnsToUpgrades=false;
        }
        void RefreshVisibleHeroMenus() {
            if(heroDetailsRoot.activeSelf) RefreshHeroDetails(); if(collectionRoot.activeSelf) RefreshCollection();
            if(selectionRoot.activeSelf) RefreshSelection(); if(equipmentRoot.activeSelf) RefreshEquipment(); if(progressionRoot.activeSelf) RefreshProgression();
        }
        void RefreshIdleScreen() {
            int minutes=battle.IdleMinutes;
            idleSummary.text=(minutes/60)+"h "+(minutes%60)+"m / 12h BANKED\n\n+"+battle.IdleCrystals+" CRYSTALS\n+"+battle.IdleExperience+" XP PER HERO\n+"+battle.IdleForgeMaterials+" FORGE MATERIALS";
            SetMeter(idleMeter,minutes/(float)Battle.IdleCapMinutes); idleClaim.interactable=minutes>0 && CanManageHeroes;
            idleClaim.GetComponentInChildren<Text>().text=minutes>0?"CLAIM REWARDS":"REWARDS ARE ACCUMULATING";
        }
        void CreateMilestoneScreens() {
            milestonesRoot=ScreenRoot("Campaign Milestones");
            Label(milestonesRoot.transform,"CAMPAIGN MILESTONES\nClear a chapter to unlock its story and one-time rewards.",new Vector2(.05f,.82f),new Vector2(.95f,.98f),28);
            milestoneCards=new Button[ChapterDefinition.Catalog.Count];
            for(int i=0;i<milestoneCards.Length;i++) {
                int chapter=i; float left=i%2==0?.05f:.52f,bottom=.60f-(i%6)/2*.20f;
                milestoneCards[i]=ScreenButton(milestonesRoot.transform,"",left,bottom,left+.43f,bottom+.18f,()=>OpenMilestoneStory(chapter)); milestoneCards[i].GetComponentInChildren<Text>().resizeTextMaxSize=23;
            }
            milestonePrevious=ScreenButton(milestonesRoot.transform,"<",.05f,.035f,.17f,.145f,()=> { milestonePage--; RefreshMilestones(); });
            milestoneNext=ScreenButton(milestonesRoot.transform,">",.55f,.035f,.67f,.145f,()=> { milestonePage++; RefreshMilestones(); });
            milestonePageLabel=Label(milestonesRoot.transform,"",new Vector2(.20f,.035f),new Vector2(.52f,.145f),20);
            ScreenButton(milestonesRoot.transform,"BACK",.72f,.035f,.95f,.145f,()=>milestonesRoot.SetActive(false)); milestonesRoot.SetActive(false);
            milestoneStoryRoot=ScreenRoot("Chapter Story");
            milestoneStoryTitle=Label(milestoneStoryRoot.transform,"",new Vector2(.08f,.78f),new Vector2(.92f,.97f),32); milestoneStoryTitle.color=new Color(1,.82f,.4f);
            milestoneStoryBody=Label(milestoneStoryRoot.transform,"",new Vector2(.10f,.44f),new Vector2(.90f,.75f),28);
            milestoneStoryRewards=Label(milestoneStoryRoot.transform,"",new Vector2(.10f,.20f),new Vector2(.90f,.415f),25);
            milestoneClaim=ScreenButton(milestoneStoryRoot.transform,"CLAIM REWARD",.05f,.035f,.65f,.16f,ClaimCurrentMilestone);
            ScreenButton(milestoneStoryRoot.transform,"BACK",.70f,.035f,.95f,.16f,()=> { milestoneStoryRoot.SetActive(false); if(milestonesRoot.activeSelf) RefreshMilestones(); }); milestoneStoryRoot.SetActive(false);
        }
        void OpenMilestones() { if(!atChapters) return; milestonePage=0; for(int i=0;i<milestoneCards.Length;i++) if(battle.CanClaimMilestone(i)) { milestonePage=i/6; break; } milestonesRoot.transform.SetAsLastSibling(); milestonesRoot.SetActive(true); RefreshMilestones(); }
        void RefreshMilestones() {
            int pages=(milestoneCards.Length+5)/6; milestonePage=Mathf.Clamp(milestonePage,0,pages-1);
            for(int i=0;i<milestoneCards.Length;i++) {
                milestoneCards[i].gameObject.SetActive(i/6==milestonePage); bool earned=battle.ChapterCleared(i),claimed=battle.MilestoneClaimed(i);
                milestoneCards[i].interactable=earned;
                milestoneCards[i].GetComponentInChildren<Text>().text="CHAPTER "+(i+1)+" · "+(claimed?"CLAIMED":earned?"REWARD READY":"LOCKED")+"\n"+ChapterDefinition.Catalog[i].Name+"\n"+ChapterMilestone.For(i).Rewards;
                milestoneCards[i].GetComponent<Image>().color=earned&&!claimed?new Color(.23f,.17f,.09f):new Color(.06f,.08f,.12f);
            }
            milestonePrevious.interactable=milestonePage>0; milestoneNext.interactable=milestonePage<pages-1; milestonePageLabel.text="PAGE "+(milestonePage+1)+" / "+pages;
        }
        void OpenMilestoneStory(int chapter) {
            if(!atChapters || !battle.ChapterCleared(chapter)) return;
            storyChapter=chapter; milestoneStoryRoot.transform.SetAsLastSibling(); milestoneStoryRoot.SetActive(true); RefreshMilestoneStory();
        }
        void RefreshMilestoneStory() {
            milestoneStoryTitle.text="CHAPTER "+(storyChapter+1)+" CLEARED\n"+ChapterDefinition.Catalog[storyChapter].Name;
            var reward=ChapterMilestone.For(storyChapter); milestoneStoryBody.text=reward.Story;
            milestoneStoryRewards.text=(battle.MilestoneClaimed(storyChapter)?"REWARDS CLAIMED":"CHAPTER REWARDS")+"\n"+reward.Rewards+(storyChapter+1<ChapterDefinition.Catalog.Count?"\nNEXT · "+ChapterDefinition.Catalog[storyChapter+1].Name:"\nTHE CAMPAIGN IS COMPLETE · The Tower awaits.");
            milestoneClaim.interactable=battle.CanClaimMilestone(storyChapter); milestoneClaim.GetComponentInChildren<Text>().text=battle.MilestoneClaimed(storyChapter)?"REWARDS CLAIMED":"CLAIM REWARD";
        }
        void ClaimCurrentMilestone() {
            if(!battle.ClaimMilestone(storyChapter)) return;
            SaveProgress(); RefreshAppearance(); RefreshMilestoneStory(); RefreshVisibleHeroMenus();
            homeToast="Chapter "+(storyChapter+1)+" rewards claimed · "+ChapterMilestone.For(storyChapter).Rewards; RefreshHome(); sounds.Play("Victory",.6f);
        }
        void CloseManagement(GameObject root) {
            root.SetActive(false); var id=managementHero; managementHero=null;
            if(id!=null) OpenHeroDetails(id);
        }
        void CreateAdvancementScreen() {
            advancementRoot=ScreenRoot("Hero Upgrades");
            advanceTitle=Label(advancementRoot.transform,"",new Vector2(.05f,.85f),new Vector2(.95f,.98f),30);
            advanceLevel=Label(advancementRoot.transform,"",new Vector2(.05f,.64f),new Vector2(.75f,.83f),23);
            advanceIdle=ScreenButton(advancementRoot.transform,"IDLE XP",.80f,.65f,.95f,.81f,()=> { advancementRoot.SetActive(false); OpenIdleRewards(); idleReturnsToUpgrades=true; });
            advanceXP=Bar(advancementRoot.transform,new Vector2(.05f,.61f),new Vector2(.95f,.625f),new Color(.35f,.7f,.43f));
            advanceAscension=Label(advancementRoot.transform,"",new Vector2(.05f,.36f),new Vector2(.95f,.59f),25);
            advanceEquipment=Label(advancementRoot.transform,"",new Vector2(.05f,.205f),new Vector2(.95f,.345f),20);
            advanceNotice=Label(advancementRoot.transform,"",new Vector2(.05f,.13f),new Vector2(.95f,.195f),19);
            ScreenButton(advancementRoot.transform,"EQUIPMENT",.05f,.025f,.26f,.115f,()=> { detailsHero=advancementHero; advancementRoot.SetActive(false); OpenDetailsManagement(1); });
            ScreenButton(advancementRoot.transform,"TALENTS",.28f,.025f,.49f,.115f,()=> { detailsHero=advancementHero; advancementRoot.SetActive(false); OpenDetailsManagement(2); });
            advanceConfirm=ScreenButton(advancementRoot.transform,"ASCEND",.51f,.025f,.75f,.115f,()=> {
                var hero=battle.HeroProgress(advancementHero); int power=hero.Power;
                if(!battle.UpgradeHero(advancementHero)) return;
                SaveProgress(); advanceNotice.text=hero.Identity.Name+" ascended · Power "+power+" → "+hero.Power; RefreshAdvancement(); RefreshVisibleHeroMenus(); if(atChapters) RefreshHome();
            });
            ScreenButton(advancementRoot.transform,"BACK",.78f,.025f,.95f,.115f,()=> { advancementRoot.SetActive(false); RefreshVisibleHeroMenus(); }); advancementRoot.SetActive(false);
        }
        void OpenAdvancement(string id) {
            if(!CanManageHeroes || battle.HeroProgress(id)==null) return;
            advancementHero=id; advanceNotice.text="Compare the benefits below before spending shards."; advancementRoot.transform.SetAsLastSibling(); advancementRoot.SetActive(true); RefreshAdvancement();
        }
        static string StatChange(PartyHero before,PartyHero after) { return "HP "+before.Definition.MaxHealth+" → "+after.Definition.MaxHealth+" · ATK "+before.Definition.AttackDamage+" → "+after.Definition.AttackDamage+"\nULT "+before.Ultimate.Damage+" → "+after.Ultimate.Damage+" · POWER "+before.Power+" → "+after.Power; }
        void RefreshAdvancement() {
            var hero=battle.HeroProgress(advancementHero); var nextLevel=battle.PreviewNextLevel(advancementHero); var ascended=battle.PreviewAscension(advancementHero);
            advanceTitle.text=hero.Identity.Name+" · UPGRADES\n"+hero.RarityLabel+" · Combat level "+hero.CombatLevel+(hero.CombatLevel>hero.Level?" · Shared Crystal linked":""); advanceTitle.color=RarityTint(hero.Quality);
            advanceLevel.text=nextLevel==null?"LEVEL · MAX OWN LEVEL "+PartyHero.MaxLevel:"LEVEL "+hero.Level+" → "+nextLevel.Level+" · XP "+hero.Experience+" / "+hero.NextLevelXP+"\n"+StatChange(hero,nextLevel)+"\nEarn XP from victories, idle rewards and chapter milestones.";
            SetMeter(advanceXP,hero.Level==PartyHero.MaxLevel?1:hero.Experience/(float)hero.NextLevelXP);
            advanceAscension.text=ascended==null?"ASCENSION · MAXIMUM REACHED":"ASCENSION · "+hero.RarityLabel+" → "+ascended.RarityLabel+"\n"+StatChange(hero,ascended)+"\nCOST "+hero.UpgradeCost+" hero shards · OWNED "+hero.Shards+"\n"+(hero.Shards>=hero.UpgradeCost?"Ready to ascend.":"Need "+(hero.UpgradeCost-hero.Shards)+" more · Earn shards from duplicates, quests, milestones and Tower.");
            string equipment="EQUIPMENT · Compare items before equipping\n"; for(int i=0;i<3;i++) { var item=hero.EquipmentStats((GearSlot)i); equipment+=(i==0?"":" · ")+((GearSlot)i)+": "+(item==null?"Empty":item.Name); }
            advanceEquipment.text=equipment+"\nTalents unlock at own levels 5, 10 and 15. Shared levels do not unlock talents.";
            advanceConfirm.interactable=CanManageHeroes && ascended!=null && hero.Shards>=hero.UpgradeCost;
            advanceConfirm.GetComponentInChildren<Text>().text=ascended==null?"ASCENSION MAX":"ASCEND · "+hero.UpgradeCost+" SHARDS";
            advanceIdle.interactable=atChapters;
        }
    }
}
