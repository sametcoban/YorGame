using UnityEngine;
using UnityEngine.UI;
namespace Ashlight {
    public sealed partial class Prototype {
        GameObject puzzlesRoot,goalsRoot;
        Text puzzlesTitle,puzzleHint,goalsBody,goalsPage;
        readonly Button[] puzzleHeroes=new Button[5];
        readonly Button[,] puzzleSkills=new Button[5,2];
        readonly Button[] puzzleTabs=new Button[3];
        Button puzzleStart;
        string[] puzzleFormation=FormationPuzzles.Heroes();
        readonly int[,] puzzleLoadout={{3,1},{3,2},{4,0},{0,1},{0,2}};
        int chosenPuzzle,swapPuzzleSlot=-1,goalTab,goalPage;
        bool JourneyModalActive { get { return puzzlesRoot!=null && puzzlesRoot.activeSelf || goalsRoot!=null && goalsRoot.activeSelf; } }
        void CreateJourneyScreens() {
            puzzlesRoot=ScreenRoot("Formation Puzzles");
            puzzlesTitle=Label(puzzlesRoot.transform,"",new Vector2(.05f,.90f),new Vector2(.95f,.99f),28);
            for(int i=0;i<3;i++) { int index=i; float left=.05f+i*.31f; puzzleTabs[i]=ScreenButton(puzzlesRoot.transform,"PUZZLE "+(i+1),left,.82f,left+.28f,.89f,()=> { chosenPuzzle=index; RefreshPuzzles(); }); }
            puzzleHint=Label(puzzlesRoot.transform,"",new Vector2(.05f,.64f),new Vector2(.95f,.81f),20);
            for(int i=0;i<5;i++) { int slot=i; float y=.52f-i*.095f;
                puzzleHeroes[i]=ScreenButton(puzzlesRoot.transform,"",.05f,y,.35f,y+.085f,()=> { if(swapPuzzleSlot<0) swapPuzzleSlot=slot; else { var id=puzzleFormation[slot]; puzzleFormation[slot]=puzzleFormation[swapPuzzleSlot]; puzzleFormation[swapPuzzleSlot]=id; for(int skill=0;skill<2;skill++) { int old=puzzleLoadout[slot,skill]; puzzleLoadout[slot,skill]=puzzleLoadout[swapPuzzleSlot,skill]; puzzleLoadout[swapPuzzleSlot,skill]=old; } swapPuzzleSlot=-1; } RefreshPuzzles(); });
                for(int j=0;j<2;j++) { int skill=j; float left=j==0?.38f:.68f; puzzleSkills[i,j]=ScreenButton(puzzlesRoot.transform,"",left,y,left+.27f,y+.085f,()=> { do { puzzleLoadout[slot,skill]=(puzzleLoadout[slot,skill]+1)%6; } while(puzzleLoadout[slot,skill]==puzzleLoadout[slot,1-skill]); RefreshPuzzles(); }); }
            }
            puzzleStart=ScreenButton(puzzlesRoot.transform,"START",.05f,.025f,.65f,.115f,StartPuzzle);
            ScreenButton(puzzlesRoot.transform,"BACK",.7f,.025f,.95f,.115f,()=>puzzlesRoot.SetActive(false)); puzzlesRoot.SetActive(false);
            goalsRoot=ScreenRoot("Collection Goals"); Label(goalsRoot.transform,"COLLECTION GOALS",new Vector2(.05f,.86f),new Vector2(.95f,.98f),32);
            for(int i=0;i<3;i++) { int tab=i; float left=.05f+i*.31f; ScreenButton(goalsRoot.transform,new[]{"MISSING HEROES","COMPANIONS","GEAR SETS"}[i],left,.75f,left+.28f,.84f,()=> { goalTab=tab; goalPage=0; RefreshGoals(); }); }
            goalsBody=Label(goalsRoot.transform,"",new Vector2(.06f,.21f),new Vector2(.94f,.71f),26);
            goalsPage=Label(goalsRoot.transform,"",new Vector2(.25f,.125f),new Vector2(.75f,.195f),20);
            ScreenButton(goalsRoot.transform,"<",.05f,.125f,.2f,.195f,()=> { goalPage--; RefreshGoals(); });
            ScreenButton(goalsRoot.transform,">",.8f,.125f,.95f,.195f,()=> { goalPage++; RefreshGoals(); });
            ScreenButton(goalsRoot.transform,"HEROES",.05f,.025f,.32f,.115f,()=> { goalsRoot.SetActive(false); OpenCollection(); });
            ScreenButton(goalsRoot.transform,"SUMMONING",.365f,.025f,.635f,.115f,()=> { goalsRoot.SetActive(false); OpenSummoning(); });
            ScreenButton(goalsRoot.transform,"BACK",.68f,.025f,.95f,.115f,()=>goalsRoot.SetActive(false)); goalsRoot.SetActive(false);
            ScreenButton(activitiesRoot.transform,"PUZZLES",.51f,.135f,.72f,.225f,OpenPuzzles);
            ScreenButton(activitiesRoot.transform,"GOALS",.74f,.135f,.95f,.225f,OpenGoals);
        }
        void OpenPuzzles() { if(!atChapters) return; swapPuzzleSlot=-1; puzzlesRoot.SetActive(true); puzzlesRoot.transform.SetAsLastSibling(); RefreshPuzzles(); }
        void RefreshPuzzles() {
            puzzlesTitle.text="FORMATION PUZZLES · "+FormationPuzzles.Name(chosenPuzzle)+(battle.PuzzleCleared(chosenPuzzle)?" · CLEARED":"");
            puzzleHint.text=FormationPuzzles.Hint(chosenPuzzle)+"\nLoan heroes: Rare, Lv 10 · No gear · Tap two heroes to swap rows; tap a skill to cycle six choices. Defeat all enemies within 12 enemy actions.\n"+(battle.PuzzleCleared(chosenPuzzle)?"Replay freely · First-clear reward already claimed.":"FIRST CLEAR · 50 crystals + 50 materials · Unlocks at chapter 3.");
            for(int i=0;i<3;i++) puzzleTabs[i].GetComponent<Image>().color=i==chosenPuzzle?new Color(.22f,.18f,.10f):MenuPalette.Control;
            for(int i=0;i<5;i++) { var hero=HeroDefinition.Find(puzzleFormation[i]); puzzleHeroes[i].GetComponentInChildren<Text>().text=(i<2?"FRONT · ":"BACK · ")+hero.Name; puzzleHeroes[i].GetComponent<Image>().color=i==swapPuzzleSlot?new Color(.22f,.18f,.10f):MenuPalette.Control;
                for(int j=0;j<2;j++) { var skill=SkillDefinition.For(hero.Class)[puzzleLoadout[i,j]]; puzzleSkills[i,j].GetComponentInChildren<Text>().text=skill.Name+"\n"+skill.Affinity+(skill.Utility==SkillUtility.None?"":" · "+skill.Utility.ToString().Replace("PartyShield","Party shield")); }
            }
            puzzleStart.interactable=battle.CanStartPuzzle;
        }
        void StartPuzzle() {
            var run=battle.StartFormationPuzzle(chosenPuzzle,puzzleFormation,puzzleLoadout); if(run==null) return;
            SaveProgress(); campaignBattle=battle; battle=run; bossReported=false; autoPlay=false;
            RefreshAppearance(); StartChapter(battle.ChapterIndex); message=FormationPuzzles.Hint(chosenPuzzle);
        }
        void OpenGoals() { if(!atChapters) return; goalPage=0; goalsRoot.SetActive(true); goalsRoot.transform.SetAsLastSibling(); RefreshGoals(); }
        void RefreshGoals() {
            var lines=battle.CollectionGoals(goalTab).TrimEnd().Split('\n'); int pages=System.Math.Max(1,(lines.Length-1+5)/6); goalPage=Mathf.Clamp(goalPage,0,pages-1); string body=lines[0]+"\n\n";
            for(int i=1+goalPage*6;i<System.Math.Min(lines.Length,1+(goalPage+1)*6);i++) body+=lines[i]+"\n\n";
            goalsBody.text=body; goalsPage.text="Page "+(goalPage+1)+" / "+pages;
        }
    }
}
