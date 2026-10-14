using UnityEngine;
using UnityEngine.UI;
namespace Ashlight {
    public sealed partial class Prototype {
        readonly Text[] routeStages=new Text[5];
        Text waveBanner;
        void CreateCampaignRoute() {
            for(int i=0;i<5;i++) { float left=.05f+i*.182f;
                Panel(chapterRoot.transform,new Vector2(left,.66f),new Vector2(left+.17f,.725f),MenuPalette.Control);
                routeStages[i]=Label(chapterRoot.transform,"",new Vector2(left,.66f),new Vector2(left+.17f,.725f),19);
            }
        }
        void RefreshCampaignRoute() {
            if(routeStages[0]==null) return;
            for(int i=0;i<5;i++) { bool cleared=battle.CampaignComplete || i<battle.StageIndex; routeStages[i].text=cleared?"CLEARED "+(i+1):i==battle.StageIndex?"NEXT · "+(i==4?"BOSS":(i+1).ToString()):i==4?"BOSS":"STAGE "+(i+1); routeStages[i].color=cleared?new Color(.54f,.8f,.61f):i==battle.StageIndex?MenuPalette.Gold:MenuPalette.Text; }
        }
        void CreateWaveBanner() { waveBanner=Label(safe,"",new Vector2(.12f,.43f),new Vector2(.88f,.59f),36); waveBanner.color=MenuPalette.Gold; waveBanner.gameObject.SetActive(false); }
        void ShowWaveBanner(bool active) { if(waveBanner==null) return; waveBanner.text="WAVE "+(battle.WaveIndex+1)+" CLEARED\n"+(battle.WaveIndex+2==battle.WaveCount?"FINAL WAVE APPROACHES":"THE NEXT WAVE APPROACHES"); waveBanner.gameObject.SetActive(active); }
    }
}
