using System;
using UnityEngine;
using UnityEngine.UI;
namespace Ashlight {
    public sealed partial class Prototype {
        GameObject polishRoot,compareRoot,trainingRoot,supportReportRoot;
        Text detailedReport;
        Text compareLeft,compareRight,trainingInfo,polishInfo;
        Button practiceStart,lossReportButton;
        readonly InputField[] presetInputs=new InputField[3];
        int presetMode,compareIndex,trainingChapter;
        bool rotatingTraining;
        string compareAnchor;
        float bossEntranceAt=-10;
        bool PolishModalActive { get { return (polishRoot!=null && polishRoot.activeSelf) || (compareRoot!=null && compareRoot.activeSelf) || (trainingRoot!=null && trainingRoot.activeSelf) || (supportReportRoot!=null && supportReportRoot.activeSelf); } }
        void CreatePolishScreens() {
            polishRoot=ScreenRoot("Company Tools");
            Label(polishRoot.transform,"COMPANY TOOLS",new Vector2(.05f,.86f),new Vector2(.95f,.98f),32);
            polishInfo=Label(polishRoot.transform,"",new Vector2(.06f,.66f),new Vector2(.94f,.83f),22);
            ScreenButton(polishRoot.transform,"COMPARE HEROES",.05f,.49f,.48f,.62f,()=>OpenComparison(battle.Party[0].Identity.Id));
            ScreenButton(polishRoot.transform,"TRAINING",.52f,.49f,.95f,.62f,()=> { trainingChapter=0; rotatingTraining=false; RefreshTraining(); trainingRoot.SetActive(true); trainingRoot.transform.SetAsLastSibling(); });
            ScreenButton(polishRoot.transform,"TEXT SIZE",.05f,.32f,.48f,.45f,()=> { AccessibilityOptions.LargeText=!AccessibilityOptions.LargeText; ApplyAccessibleText(); RefreshPolishInfo(); });
            ScreenButton(polishRoot.transform,"REDUCED EFFECTS",.52f,.32f,.95f,.45f,()=> { AccessibilityOptions.ReducedEffects=!AccessibilityOptions.ReducedEffects; RefreshPolishInfo(); });
            ScreenButton(polishRoot.transform,"CAMERA MOTION",.05f,.15f,.48f,.28f,()=> { AccessibilityOptions.ReducedMotion=!AccessibilityOptions.ReducedMotion; RefreshPolishInfo(); });
            ScreenButton(polishRoot.transform,"FORMATION",.52f,.15f,.95f,.28f,()=> { if(!battle.CanChangeParty) return; polishRoot.SetActive(false); formationRoot.SetActive(true); formationRoot.transform.SetAsLastSibling(); RefreshFormation(); });
            ScreenButton(polishRoot.transform,"BACK",.25f,.025f,.75f,.115f,()=>polishRoot.SetActive(false)); polishRoot.SetActive(false);
            // The Home title keeps its left half; the new control shares the existing header row.
            homeTitle.rectTransform.anchorMax=new Vector2(.48f,.98f);
            ScreenButton(homeRoot.transform,"TOOLS",.51f,.865f,.73f,.98f,()=> { RefreshPolishInfo(); polishRoot.SetActive(true); polishRoot.transform.SetAsLastSibling(); });
            ScreenButton(heroDetailsRoot.transform,"COMPARE",.04f,.735f,.32f,.81f,()=>OpenComparison(detailsHero));
            compareRoot=ScreenRoot("Hero Comparison");
            Label(compareRoot.transform,"HERO COMPARISON",new Vector2(.05f,.88f),new Vector2(.95f,.98f),30);
            compareLeft=ScrollingText(compareRoot.transform,new Vector2(.05f,.18f),new Vector2(.485f,.85f),20);
            compareRight=ScrollingText(compareRoot.transform,new Vector2(.515f,.18f),new Vector2(.95f,.85f),20);
            ScreenButton(compareRoot.transform,"< HERO",.05f,.025f,.29f,.13f,()=>CycleComparison(-1));
            ScreenButton(compareRoot.transform,"BACK",.34f,.025f,.66f,.13f,()=>compareRoot.SetActive(false));
            ScreenButton(compareRoot.transform,"HERO >",.71f,.025f,.95f,.13f,()=>CycleComparison(1)); compareRoot.SetActive(false);
            trainingRoot=ScreenRoot("Training Battles");
            Label(trainingRoot.transform,"TRAINING · NO COST OR REWARDS",new Vector2(.05f,.86f),new Vector2(.95f,.98f),28);
            trainingInfo=ScrollingText(trainingRoot.transform,new Vector2(.06f,.35f),new Vector2(.94f,.82f),24);
            ScreenButton(trainingRoot.transform,"<",.05f,.215f,.2f,.315f,()=> { trainingChapter=Math.Max(0,trainingChapter-1); RefreshTraining(); });
            ScreenButton(trainingRoot.transform,">",.8f,.215f,.95f,.315f,()=> { trainingChapter=Math.Min(Math.Min(battle.ChapterIndex,ChapterDefinition.Catalog.Count-1),trainingChapter+1); RefreshTraining(); });
            ScreenButton(trainingRoot.transform,"RULES",.25f,.215f,.75f,.315f,()=> { rotatingTraining=!rotatingTraining; RefreshTraining(); });
            practiceStart=ScreenButton(trainingRoot.transform,"START",.05f,.075f,.48f,.175f,StartPractice);
            ScreenButton(trainingRoot.transform,"BACK",.52f,.075f,.95f,.175f,()=>trainingRoot.SetActive(false)); trainingRoot.SetActive(false);
            ScreenButton(teamPresetRoot.transform,"MODE",.75f,.925f,.95f,.975f,()=> { presetMode=(presetMode+1)%3; RefreshTeamPresets(); });
            for(int i=0;i<3;i++) { int index=i; float y=.61f-i*.22f;
                teamPresetTitle.rectTransform.anchorMax=new Vector2(.70f,.98f);
                teamPresetLabels[i].rectTransform.anchorMax=new Vector2(.70f,y+.20f);
                presetInputs[i]=CreatePresetInput(teamPresetRoot.transform,new Vector2(.73f,y+.105f),new Vector2(.95f,y+.19f));
                presetInputs[i].onEndEdit.AddListener(name=> { if(battle.RenamePreset(presetMode*3+index,name)) SaveProgress(); RefreshTeamPresets(); });
            }
            supportReportRoot=ScreenRoot("Battle Support Report");
            Label(supportReportRoot.transform,"BATTLE REPORT",new Vector2(.05f,.86f),new Vector2(.95f,.98f),30);
            detailedReport=ScrollingText(supportReportRoot.transform,new Vector2(.06f,.18f),new Vector2(.94f,.82f),24);
            ScreenButton(supportReportRoot.transform,"BACK",.25f,.025f,.75f,.115f,()=>supportReportRoot.SetActive(false)); supportReportRoot.SetActive(false);
            ScreenButton(resultsRoot.transform,"FULL REPORT",.73f,.485f,.94f,.545f,OpenSupportReport);
            lossReportButton=ScreenButton(safe,"BATTLE REPORT",.02f,.625f,.23f,.72f,OpenSupportReport); lossReportButton.gameObject.SetActive(false);
            ApplyAccessibleText();
        }
        InputField CreatePresetInput(Transform parent,Vector2 min,Vector2 max) {
            var root=new GameObject("Preset Name",typeof(RectTransform),typeof(Image),typeof(InputField)); root.transform.SetParent(parent,false);
            var rect=root.GetComponent<RectTransform>(); rect.anchorMin=min; rect.anchorMax=max; rect.offsetMin=rect.offsetMax=Vector2.zero;
            root.GetComponent<Image>().color=MenuPalette.Control;
            root.AddComponent<RectMask2D>();
            var input=root.GetComponent<InputField>(); input.targetGraphic=root.GetComponent<Image>(); input.textComponent=Label(root.transform,"",new Vector2(.04f,.05f),new Vector2(.96f,.95f),18); input.characterLimit=24; return input;
        }
        Text ScrollingText(Transform parent,Vector2 min,Vector2 max,int size) {
            var root=new GameObject("Scrollable Reading",typeof(RectTransform),typeof(Image),typeof(Mask),typeof(ScrollRect)); root.transform.SetParent(parent,false);
            var rect=root.GetComponent<RectTransform>(); rect.anchorMin=min; rect.anchorMax=max; rect.offsetMin=rect.offsetMax=Vector2.zero;
            root.GetComponent<Image>().color=MenuPalette.Surface; root.GetComponent<Mask>().showMaskGraphic=false;
            var text=Label(root.transform,"",new Vector2(0,1),new Vector2(1,1),size); text.alignment=TextAnchor.UpperLeft; text.resizeTextForBestFit=false; text.horizontalOverflow=HorizontalWrapMode.Wrap; text.verticalOverflow=VerticalWrapMode.Overflow;
            text.rectTransform.pivot=new Vector2(.5f,1); text.gameObject.AddComponent<ContentSizeFitter>().verticalFit=ContentSizeFitter.FitMode.PreferredSize;
            var scroll=root.GetComponent<ScrollRect>(); scroll.content=text.rectTransform; scroll.viewport=rect; scroll.horizontal=false; scroll.movementType=ScrollRect.MovementType.Clamped;
            // Mask image supplies the scroll hit area while text retains selectable controls elsewhere.
            text.raycastTarget=true; return text;
        }
        void OpenSupportReport() {
            detailedReport.text="Ward given counts new shield strength; absorbed counts prevented damage on its recipient. Opening formation wards are credited to their recipient. Cleanses count removed statuses. Interrupts count successful disruptions.\n";
            foreach(var hero in battle.Party) detailedReport.text+="\n"+hero.Identity.Name+" · Damage "+hero.EncounterDamage+" · Heal "+hero.EncounterHealing+"\n"+Battle.SupportReport(hero);
            detailedReport.rectTransform.anchoredPosition=Vector2.zero; supportReportRoot.SetActive(true); supportReportRoot.transform.SetAsLastSibling();
        }
        void RefreshPolishInfo() { polishInfo.text="Text · "+(AccessibilityOptions.LargeText?"Large":"Standard")+"\nEffects · "+(AccessibilityOptions.ReducedEffects?"Reduced":"Full")+"\nCamera · "+(AccessibilityOptions.ReducedMotion?"Still":"Animated")+"\nTraining uses your current party and equipped skills."; }
        void ApplyAccessibleText() {
            foreach(var text in safe.GetComponentsInChildren<Text>(true)) {
                var scaling=text.GetComponent<AccessibleTextScale>(); if(scaling==null) scaling=text.gameObject.AddComponent<AccessibleTextScale>(); scaling.Refresh();
            }
        }
        void OpenComparison(string id) { if(!CanManageHeroes || battle.HeroProgress(id)==null) return; compareAnchor=id; compareIndex=0; if(battle.Recruits.Count>1 && battle.Recruits[0].Id==id) compareIndex=1; RefreshComparison(); compareRoot.SetActive(true); compareRoot.transform.SetAsLastSibling(); }
        void CycleComparison(int offset) { compareIndex=(compareIndex+offset+battle.Recruits.Count)%battle.Recruits.Count; RefreshComparison(); }
        void RefreshComparison() { compareLeft.text=battle.CompareHero(compareAnchor); compareRight.text=battle.CompareHero(battle.Recruits[compareIndex].Id); compareLeft.rectTransform.anchoredPosition=compareRight.rectTransform.anchoredPosition=Vector2.zero; }
        void RefreshTraining() { practiceStart.interactable=battle.CanTrain; trainingInfo.text="CHAPTER "+(trainingChapter+1)+" · "+ChapterStageDefinition.For(trainingChapter).Name+"\nOpponent · "+ChapterDefinition.Catalog[trainingChapter].Enemies[4].Name+"\n\n"+(rotatingTraining?"TODAY'S RULE · "+Battle.PracticeName(battle.RotatingPracticeRule)+"\n"+Battle.PracticeRules(battle.RotatingPracticeRule):"STANDARD · Practice targeting, formation and skills.")+"\n\nEight enemy actions, or defeat the boss. Cloned heroes recover after the battle. No XP, currency, quests, bonds or campaign progress.\n\nChange formation and skills before attacking. Changes inside training are temporary. Rules rotate at 00:00 UTC."; }
        void StartPractice() {
            var run=battle.StartTraining(trainingChapter,rotatingTraining); if(run==null) return;
            SaveProgress(); campaignBattle=battle; battle=run; bossReported=false; RefreshAppearance(); StartChapter(battle.ChapterIndex); message="TRAINING · No cost or rewards";
        }
        System.Collections.IEnumerator BossEntrance() {
            var run=battle; int wave=battle.WaveIndex;
            // Let StartChapter finish opening its story/preview before checking visibility.
            yield return null;
            while(!atChapters && battle==run && battle.WaveIndex==wave && (BetaModalActive || previewRoot.activeSelf || formationRoot.activeSelf || presentingAction || enemyTurn!=null)) yield return null;
            if(atChapters || battle!=run || battle.WaveIndex!=wave) yield break;
            string names=""; foreach(var foe in battle.Enemies) if(foe.Definition.IsBoss) names+=(names.Length==0?"":" & ")+foe.Definition.Name;
            if(names.Length==0 || waveBanner==null) yield break;
            bossEntranceAt=Time.time;
            foreach(var actor in foes) if(actor!=null && !AccessibilityOptions.ReducedEffects) effects.Ring(actor.position,Element.Light,.09f);
            waveBanner.text="BOSS ENCOUNTER\n"+names; waveBanner.gameObject.SetActive(true);
            yield return new WaitForSeconds(2); if(battle==run && battle.WaveIndex==wave && waveBanner.text=="BOSS ENCOUNTER\n"+names) waveBanner.gameObject.SetActive(false);
        }
    }
}
