using UnityEngine;
using UnityEngine.UI;
namespace Ashlight {
    public sealed partial class Prototype {
        GameObject recommendationRoot,storyRoot,betaRoot,feedbackRoot;
        Text recommendationTitle,recommendationBody,storyTitle,storySpeaker,storyLine,betaChecklist,feedbackNotice;
        Button recommendationApply,storyNext,feedbackCategory;
        InputField feedbackNotes,feedbackOutput;
        TeamRecommendation reviewedTeam;
        CampaignStory activeStory;
        int storyPage,storyChapterIndex,feedbackKind;
        bool storyBoss,recordStory,replayingTutorial;
        System.Action storyAfter;
        float betaFrameTime; int betaFrameCount;
        readonly PortraitView[] summonPortraits=new PortraitView[10];
        bool summonSingle;
        readonly string[] feedbackKinds={"Bug","Balance","Confusing screen","General feedback"};
        bool BetaModalActive { get { return (recommendationRoot!=null && recommendationRoot.activeSelf) || (storyRoot!=null && storyRoot.activeSelf) || (betaRoot!=null && betaRoot.activeSelf) || (feedbackRoot!=null && feedbackRoot.activeSelf) || SeasonModalActive; } }
        void CreateBetaScreens() {
            recommendationRoot=ScreenRoot("Stage Team Recommendation");
            recommendationTitle=Label(recommendationRoot.transform,"",new Vector2(.05f,.83f),new Vector2(.95f,.98f),28);
            recommendationBody=Label(recommendationRoot.transform,"",new Vector2(.05f,.205f),new Vector2(.95f,.81f),22);
            Label(recommendationRoot.transform,"Uses owned heroes and equipped gear. Applies heroes, formation and two skills only.\nA suggestion, not a guaranteed win. No currency, materials, shards or XP spent.",new Vector2(.05f,.13f),new Vector2(.95f,.195f),17);
            recommendationApply=ScreenButton(recommendationRoot.transform,"APPLY TEAM + SKILLS",.05f,.025f,.68f,.115f,()=> {
                if(!battle.ApplyRecommendation(reviewedTeam)) return;
                RefreshAppearance(); SaveProgress(); recommendationRoot.SetActive(false); previewRoot.SetActive(false); message="Recommended team and skills applied. Review your formation before fighting.";
            });
            ScreenButton(recommendationRoot.transform,"BACK",.73f,.025f,.95f,.115f,()=>recommendationRoot.SetActive(false)); recommendationRoot.SetActive(false);
            storyRoot=ScreenRoot("Campaign Dialogue");
            storyTitle=Label(storyRoot.transform,"",new Vector2(.06f,.82f),new Vector2(.94f,.98f),30);
            storySpeaker=Label(storyRoot.transform,"",new Vector2(.10f,.65f),new Vector2(.90f,.79f),34); storySpeaker.color=new Color(1,.82f,.4f);
            storyLine=Label(storyRoot.transform,"",new Vector2(.12f,.29f),new Vector2(.88f,.64f),30);
            ScreenButton(storyRoot.transform,"SKIP SCENE",.05f,.055f,.35f,.19f,FinishCampaignStory);
            storyNext=ScreenButton(storyRoot.transform,"NEXT",.40f,.055f,.95f,.19f,()=> { if(++storyPage>=activeStory.Lines.Length) FinishCampaignStory(); else RefreshCampaignStory(); }); storyRoot.SetActive(false);
            betaRoot=ScreenRoot("Playtest Opening");
            Label(betaRoot.transform,"FIRST JOURNEY · PLAYTEST",new Vector2(.05f,.86f),new Vector2(.95f,.98f),32);
            betaChecklist=Label(betaRoot.transform,"",new Vector2(.07f,.34f),new Vector2(.93f,.83f),28);
            Label(betaRoot.transform,"No purchases required. Summoning and Wishlist are optional.\nIf something feels unclear, use Feedback to record it.",new Vector2(.07f,.24f),new Vector2(.93f,.33f),20);
            ScreenButton(betaRoot.transform,"CAMPAIGN",.05f,.125f,.32f,.22f,()=> { betaRoot.SetActive(false); OpenCampaign(); });
            ScreenButton(betaRoot.transform,"HEROES",.365f,.125f,.635f,.22f,()=> { betaRoot.SetActive(false); OpenCollection(); });
            ScreenButton(betaRoot.transform,"FEEDBACK",.68f,.125f,.95f,.22f,OpenFeedback);
            ScreenButton(betaRoot.transform,"BACK",.25f,.025f,.75f,.115f,()=>betaRoot.SetActive(false)); betaRoot.SetActive(false);
            feedbackRoot=ScreenRoot("Playtest Feedback");
            Label(feedbackRoot.transform,"PLAYTEST FEEDBACK · "+BetaSupport.BuildTag,new Vector2(.05f,.88f),new Vector2(.95f,.98f),26);
            feedbackCategory=ScreenButton(feedbackRoot.transform,"",.05f,.78f,.95f,.86f,()=> { feedbackKind=(feedbackKind+1)%feedbackKinds.Length; RefreshFeedback(); });
            feedbackNotes=FeedbackField(feedbackRoot.transform,.05f,.51f,.95f,.755f,false);
            feedbackNotes.characterLimit=2000; feedbackNotes.onEndEdit.AddListener(value=> { PlayerPrefs.SetString("Ashlight.FeedbackDraft",value); PlayerPrefs.Save(); });
            feedbackNotes.onValueChanged.AddListener(value=>PlayerPrefs.SetString("Ashlight.FeedbackDraft",value));
            feedbackOutput=FeedbackField(feedbackRoot.transform,.05f,.21f,.95f,.48f,true);
            feedbackNotice=Label(feedbackRoot.transform,"",new Vector2(.05f,.125f),new Vector2(.95f,.195f),17);
            ScreenButton(feedbackRoot.transform,"BUILD REPORT",.05f,.025f,.32f,.115f,BuildFeedback);
            ScreenButton(feedbackRoot.transform,"COPY REPORT",.365f,.025f,.635f,.115f,()=> {
                BuildFeedback(); try { GUIUtility.systemCopyBuffer=feedbackOutput.text; feedbackNotice.text="Copy requested. If clipboard is unavailable, select and copy the report field manually."; } catch(System.Exception) { feedbackNotice.text="Clipboard unavailable. Select and copy the report field manually."; }
            });
            ScreenButton(feedbackRoot.transform,"BACK",.68f,.025f,.95f,.115f,()=>feedbackRoot.SetActive(false)); feedbackRoot.SetActive(false);
        }
        InputField FeedbackField(Transform parent,float left,float bottom,float right,float top,bool readOnly) {
            var root=new GameObject(readOnly?"Report":"Your notes",typeof(RectTransform),typeof(Image),typeof(InputField)); root.transform.SetParent(parent,false);
            var rect=root.GetComponent<RectTransform>(); rect.anchorMin=new Vector2(left,bottom); rect.anchorMax=new Vector2(right,top); rect.offsetMin=rect.offsetMax=Vector2.zero;
            root.GetComponent<Image>().color=new Color(.075f,.085f,.105f); root.AddComponent<RectMask2D>();
            var text=Label(root.transform,"",Vector2.zero,Vector2.one,18); text.alignment=TextAnchor.UpperLeft; text.resizeTextForBestFit=false; text.supportRichText=false;
            text.rectTransform.offsetMin=new Vector2(12,8); text.rectTransform.offsetMax=new Vector2(-12,-8);
            var field=root.GetComponent<InputField>(); field.textComponent=text; field.targetGraphic=root.GetComponent<Image>(); field.lineType=InputField.LineType.MultiLineNewline; field.readOnly=readOnly;
            var placeholder=Label(root.transform,readOnly?"Your report will appear here. Select text to copy manually.":"What happened? What did you expect? List steps to repeat it.",Vector2.zero,Vector2.one,18);
            placeholder.color=new Color(.6f,.65f,.7f); placeholder.alignment=TextAnchor.UpperLeft; placeholder.rectTransform.offsetMin=new Vector2(12,8); placeholder.rectTransform.offsetMax=new Vector2(-12,-8); field.placeholder=placeholder;
            return field;
        }
        void OpenTeamRecommendation() {
            reviewedTeam=battle.RecommendTeam(); if(reviewedTeam==null) return;
            recommendationTitle.text="STAGE TEAM SUGGESTION\nPower "+battle.TeamPower+" → "+reviewedTeam.Power+" · Faction bonus +"+reviewedTeam.FactionBonus+"%";
            recommendationBody.text="";
            for(int i=0;i<reviewedTeam.Heroes.Count;i++) { var row=reviewedTeam.Heroes[i]; var hero=battle.HeroProgress(row.Id);
                recommendationBody.text+=(i==0?"":"\n\n")+(i<Battle.FrontSlots?"FRONT":"BACK")+" · "+hero.Identity.Name+" · "+hero.RarityLabel+" · Lv "+hero.CombatLevel+"\n"+hero.Definition.Skills[row.FirstSkill].Name+" + "+hero.Definition.Skills[row.SecondSkill].Name+"\n"+row.Reason;
            }
            recommendationApply.interactable=battle.CanChangeParty; recommendationRoot.SetActive(true); recommendationRoot.transform.SetAsLastSibling();
        }
        void ShowCampaignStory(int chapter,bool boss,System.Action after,bool replay=false) {
            if(!battle.StoryUnlocked(chapter,boss)) { if(after!=null) after(); return; }
            activeStory=CampaignStory.For(chapter,boss); storyPage=0; storyChapterIndex=chapter; storyBoss=boss; recordStory=!replay; storyAfter=after;
            storyRoot.SetActive(true); storyRoot.transform.SetAsLastSibling(); RefreshCampaignStory();
        }
        void RefreshCampaignStory() { storyTitle.text=activeStory.Title+" · "+(storyPage+1)+"/"+activeStory.Lines.Length; storySpeaker.text=activeStory.Speakers[storyPage]; storyLine.text=activeStory.Lines[storyPage]; storyNext.GetComponentInChildren<Text>().text=storyPage==activeStory.Lines.Length-1?"CONTINUE":"NEXT"; }
        void FinishCampaignStory() {
            storyRoot.SetActive(false); if(recordStory) { battle.MarkStorySeen(storyChapterIndex,storyBoss); SaveProgress(); }
            var after=storyAfter; storyAfter=null; if(after!=null) after();
        }
        void OpenBetaOpening() { if(!atChapters) return; betaChecklist.text=battle.OpeningChecklist; betaRoot.SetActive(true); betaRoot.transform.SetAsLastSibling(); }
        void ReplayCurrentStory() {
            int chapter=battle.ChapterIndex;
            ShowCampaignStory(chapter,false,()=> { if(battle.StoryUnlocked(chapter,true)) ShowCampaignStory(chapter,true,null,true); },true);
        }
        void OpenFeedback() { if(!atChapters) return; feedbackNotes.text=PlayerPrefs.GetString("Ashlight.FeedbackDraft",""); feedbackRoot.SetActive(true); feedbackRoot.transform.SetAsLastSibling(); RefreshFeedback(); }
        void RefreshFeedback() { feedbackCategory.GetComponentInChildren<Text>().text="CATEGORY · "+feedbackKinds[feedbackKind]+" · Tap to change"; feedbackNotice.text="Report stays on this device. Nothing is sent automatically. Your notes are saved as a local draft."; }
        void BuildFeedback() {
            PlayerPrefs.SetString("Ashlight.FeedbackDraft",feedbackNotes.text); PlayerPrefs.Save();
            string runtime=Application.platform+" · Unity "+Application.unityVersion+" · app "+Application.version+"\n"+SystemInfo.deviceModel+" · "+SystemInfo.operatingSystem+"\nScreen "+Screen.width+"×"+Screen.height+" · session average FPS "+(betaFrameTime>0?betaFrameCount/betaFrameTime:0).ToString("0.0")+" · "+saveNotice;
            feedbackOutput.text=BetaSupport.Feedback((campaignBattle??battle).ExportProgress(),feedbackKinds[feedbackKind],feedbackNotes.text,runtime,System.DateTime.UtcNow);
            feedbackNotice.text="Report built locally. Copy it and share it with the developer when you choose.";
        }
        void MonitorBetaFrames() { if(Time.unscaledDeltaTime>0) { betaFrameTime+=Time.unscaledDeltaTime; betaFrameCount++; } }
        void SetupSummonPortraits() {
            for(int i=0;i<10;i++) {
                summonCards[i].transform.SetParent(summonCardPanels[i].transform,false);
                summonPortraits[i]=CreatePortrait(summonCardPanels[i].transform,new Vector2(.025f,.08f),new Vector2(.24f,.92f));
                summonPortraits[i].Root.SetActive(false);
            }
        }
        void LayoutSummonCard(int index,bool single) {
            summonSingle=single;
            var text=summonCards[index].rectTransform; text.anchorMin=new Vector2(single?.43f:.27f,.04f); text.anchorMax=new Vector2(.975f,.96f); text.offsetMin=text.offsetMax=Vector2.zero;
            var face=summonPortraits[index].Root.GetComponent<RectTransform>(); face.anchorMin=new Vector2(.025f,.08f); face.anchorMax=new Vector2(single?.39f:.24f,.92f);
            summonPortraits[index].Root.SetActive(false);
        }
    }
}
