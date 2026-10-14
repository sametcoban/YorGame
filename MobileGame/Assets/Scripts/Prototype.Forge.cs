using UnityEngine;
using UnityEngine.UI;
namespace Ashlight {
    public sealed partial class Prototype {
        GameObject forgeRoot,recruitRoot;
        Button forgeButton,forgeConfirm;
        readonly Button[] forgeSlots=new Button[3];
        Text forgeTitle,forgeItem,forgeComparison,forgeNotice,recruitTitle,recruitStory,recruitRole;
        PortraitView recruitPortrait;
        string forgeHero,forgingItem,recruitHero;
        void CreateForgeScreens() {
            forgeRoot=ScreenRoot("Equipment Forge");
            forgeTitle=Label(forgeRoot.transform,"",new Vector2(.05f,.83f),new Vector2(.95f,.98f),30);
            for(int i=0;i<3;i++) { int slot=i; float left=.05f+i*.31f;
                forgeSlots[i]=ScreenButton(forgeRoot.transform,((GearSlot)i).ToString(),left,.69f,left+.28f,.81f,()=> { browsingSlot=(GearSlot)slot; ChooseForgeItem(); forgeNotice.text=""; RefreshForge(); });
            }
            forgeItem=Label(forgeRoot.transform,"",new Vector2(.06f,.48f),new Vector2(.94f,.67f),27);
            forgeComparison=Label(forgeRoot.transform,"",new Vector2(.06f,.28f),new Vector2(.94f,.46f),25);
            Label(forgeRoot.transform,"Upgrades strengthen all owned and future copies of this gear type.\nEarn forging materials from battle victories and idle rewards.\nSelect another item in Equipment to upgrade a different gear type.",new Vector2(.06f,.20f),new Vector2(.94f,.265f),21);
            forgeNotice=Label(forgeRoot.transform,"",new Vector2(.06f,.18f),new Vector2(.94f,.20f),18);
            forgeConfirm=ScreenButton(forgeRoot.transform,"UPGRADE",.05f,.02f,.68f,.105f,()=> {
                if(!battle.EnhanceGear(forgingItem)) return;
                SaveProgress(); forgeNotice.text="Upgraded "+battle.EnhancedGear(forgingItem).Name+" · "+saveNotice; RefreshForge(); RefreshVisibleHeroMenus(); if(atChapters) RefreshHome();
            });
            ScreenButton(forgeRoot.transform,"BACK",.73f,.02f,.95f,.105f,()=> { forgeRoot.SetActive(false); RefreshVisibleHeroMenus(); }); forgeRoot.SetActive(false);
            recruitRoot=ScreenRoot("Story Ally");
            recruitTitle=Label(recruitRoot.transform,"",new Vector2(.06f,.82f),new Vector2(.94f,.98f),34);
            recruitPortrait=CreatePortrait(recruitRoot.transform,new Vector2(.07f,.29f),new Vector2(.33f,.78f));
            recruitStory=Label(recruitRoot.transform,"",new Vector2(.38f,.54f),new Vector2(.93f,.78f),27);
            recruitRole=Label(recruitRoot.transform,"",new Vector2(.38f,.23f),new Vector2(.93f,.52f),23);
            ScreenButton(recruitRoot.transform,"REVIEW HERO",.05f,.035f,.55f,.17f,()=> { recruitRoot.SetActive(false); milestoneStoryRoot.SetActive(false); detailsReturnHome=true; OpenHeroDetails(recruitHero); });
            ScreenButton(recruitRoot.transform,"CONTINUE",.60f,.035f,.95f,.17f,()=>recruitRoot.SetActive(false)); recruitRoot.SetActive(false);
        }
        void ChooseForgeItem() {
            forgingItem=null; var hero=battle.HeroProgress(forgeHero); var equipped=hero.Gear(browsingSlot);
            if(equipped!=null) forgingItem=equipped.Id;
            else foreach(var item in GearDefinition.Catalog) if(item.Slot==browsingSlot && battle.GearCount(item.Id)>0) { forgingItem=item.Id; break; }
        }
        void OpenForge() {
            if(!CanManageHeroes) return;
            forgeHero=ManagedHero.Identity.Id; ChooseForgeItem();
            var selected=GearDefinition.Find(gearSelected); if(selected!=null && selected.Slot==browsingSlot && battle.GearCount(selected.Id)>0) forgingItem=selected.Id;
            forgeNotice.text="Compare the upgrade before spending materials."; forgeRoot.transform.SetAsLastSibling(); forgeRoot.SetActive(true); RefreshForge();
        }
        void RefreshForge() {
            RefreshRefinement();
            var hero=battle.HeroProgress(forgeHero); var item=battle.EnhancedGear(forgingItem);
            forgeTitle.text=hero.Identity.Name+" · EQUIPMENT FORGE\n"+battle.ForgeMaterials+" MATERIALS";
            for(int i=0;i<forgeSlots.Length;i++) forgeSlots[i].GetComponent<Image>().color=i==(int)browsingSlot?new Color(.25f,.19f,.10f):new Color(.075f,.085f,.105f);
            if(item==null) { forgeItem.text="No "+browsingSlot+" owned yet.\nWin battles to earn gear."; forgeComparison.text=""; forgeConfirm.interactable=false; forgeConfirm.GetComponentInChildren<Text>().text="NO GEAR"; return; }
            var next=item.Enhancement<GearDefinition.MaxEnhancement?item.Enhanced(item.Enhancement+1):null;
            forgeItem.text="CURRENT · "+item.Description+"\n"+(next==null?"MAX UPGRADE +10":"NEXT · "+next.Description)+"\n"+battle.GearCount(item.Id)+" owned copies · "+(next==null?"":item.EnhanceCost+" materials required");
            var before=battle.PreviewGear(forgeHero,browsingSlot,item.Id); var after=battle.PreviewEnhancement(forgeHero,browsingSlot,item.Id);
            forgeComparison.text=after==null?"This gear type is fully upgraded.":"ON "+hero.Identity.Name+" · "+(hero.Gear(browsingSlot)!=null && hero.Gear(browsingSlot).Id==item.Id?"CURRENTLY EQUIPPED":"IF EQUIPPED")+"\n"+StatChange(before,after)+"\nArmor "+before.Protection+" → "+after.Protection+(browsingSlot==GearSlot.Accessory?" · "+item.Element+" boost "+Mathf.RoundToInt(item.ElementBoost*100)+"% → "+Mathf.RoundToInt(next.ElementBoost*100)+"%":"");
            forgeConfirm.interactable=battle.CanEnhanceGear(item.Id); forgeConfirm.GetComponentInChildren<Text>().text=next==null?"UPGRADE MAX":battle.ForgeMaterials<item.EnhanceCost?"NEED "+(item.EnhanceCost-battle.ForgeMaterials)+" MORE MATERIALS":"UPGRADE · "+item.EnhanceCost+" MATERIALS";
        }
        void ShowStoryRecruit(StoryRecruit story,bool duplicate) {
            var hero=battle.HeroProgress(story.HeroId); if(hero==null) return;
            recruitHero=story.HeroId; recruitTitle.text=(duplicate?"STORY ALLY · +20 HERO SHARDS":"A NEW ALLY JOINS")+"\n"+hero.Identity.Name+" · "+hero.Identity.DisplayClass+" · "+hero.RarityLabel;
            recruitTitle.color=RarityTint(hero.Quality); ShowPortrait(recruitPortrait,hero); recruitStory.text=story.Introduction;
            bool inParty=false; foreach(var member in battle.Party) if(member.Identity.Id==story.HeroId) inParty=true;
            recruitRole.text=ClassDefinition.PassiveDescription(hero.Identity.Class)+"\n\n"+(duplicate?"You already own this hero. The story reward grants 20 shards.":inParty?"Added to your formation.":"Available in Heroes · Assign this ally to your formation.")+"\nChoose two of this class's six skills before fighting.";
            recruitRoot.transform.SetAsLastSibling(); recruitRoot.SetActive(true);
        }
    }
}
