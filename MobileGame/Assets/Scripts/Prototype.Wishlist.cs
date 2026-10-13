using UnityEngine;
using UnityEngine.UI;
namespace Ashlight {
    public sealed partial class Prototype {
        GameObject wishlistRoot;
        Text wishlistTitle;
        Button wishlistLobbyButton;
        readonly Button[] wishlistFactions=new Button[3], wishlistCards=new Button[6];
        readonly PortraitView[] wishlistPortraits=new PortraitView[6];
        readonly HeroDefinition[] wishlistChoices=new HeroDefinition[6];
        HeroFaction wishlistFaction;
        void CreateWishlistScreens() {
            wishlistRoot=ScreenRoot("Summon Wishlist");
            wishlistTitle=Label(wishlistRoot.transform,"",new Vector2(.05f,.89f),new Vector2(.95f,.98f),28);
            Label(wishlistRoot.transform,Summoning.WishlistRules+"\nCard percentages show chance AFTER that rarity is rolled, including guarantees.",new Vector2(.05f,.755f),new Vector2(.95f,.885f),20);
            for(int i=0;i<3;i++) { int index=i; float left=.05f+i*.31f;
                wishlistFactions[i]=ScreenButton(wishlistRoot.transform,"",left,.67f,left+.28f,.745f,()=> { wishlistFaction=(HeroFaction)index; RefreshWishlist(); });
            }
            for(int i=0;i<6;i++) { int index=i; float left=i%2==0?.05f:.52f, bottom=.49f-(i/2)*.18f;
                wishlistCards[i]=ScreenButton(wishlistRoot.transform,"",left,bottom,left+.43f,bottom+.16f,()=> {
                    if(battle.ToggleWishlist(wishlistChoices[index].Id)) { SaveProgress(); RefreshWishlist(); RefreshSummonLobby(); }
                });
                var caption=wishlistCards[i].GetComponentInChildren<Text>(); var rect=caption.GetComponent<RectTransform>();
                rect.anchorMin=new Vector2(.24f,.03f); rect.anchorMax=new Vector2(.98f,.97f); rect.offsetMin=rect.offsetMax=Vector2.zero; caption.resizeTextMinSize=16; caption.resizeTextMaxSize=23;
                wishlistPortraits[i]=CreatePortrait(wishlistCards[i].transform,new Vector2(.025f,.12f),new Vector2(.22f,.90f));
            }
            ScreenButton(wishlistRoot.transform,"BACK TO SUMMONING",.25f,.02f,.75f,.11f,()=> { wishlistRoot.SetActive(false); RefreshSummonLobby(); });
            wishlistRoot.SetActive(false);
        }
        void OpenWishlist() {
            if(!atChapters || revealingSummons) return;
            wishlistRoot.transform.SetAsLastSibling(); wishlistRoot.SetActive(true); RefreshWishlist();
        }
        void RefreshWishlist() {
            wishlistTitle.text="SUMMON WISHLIST · "+battle.Wishlist.Length+" / 6\nTap a hero to select or remove · No crystals spent";
            for(int i=0;i<3;i++) {
                wishlistFactions[i].GetComponentInChildren<Text>().text=HeroDefinition.FactionDisplay((HeroFaction)i)+" · "+battle.WishlistCount((HeroFaction)i)+"/2";
                wishlistFactions[i].GetComponent<Image>().color=(HeroFaction)i==wishlistFaction?new Color(.25f,.19f,.10f):new Color(.075f,.085f,.105f);
            }
            int slot=0; foreach(var hero in HeroDefinition.Catalog) if(hero.Faction==wishlistFaction && hero.Quality>=HeroQuality.Rare) wishlistChoices[slot++]=hero;
            for(int i=0;i<6;i++) {
                var hero=wishlistChoices[i]; bool selected=battle.IsWishlisted(hero.Id);
                ShowPortrait(wishlistPortraits[i],new PartyHero(hero));
                wishlistCards[i].GetComponentInChildren<Text>().text=(selected?"SELECTED · ":"")+hero.Name+" · "+hero.Quality+"\n"+hero.DisplayClass+" · "+hero.Affinity+"\n"+(Summoning.HeroChance(hero,battle.Wishlist)*100).ToString("0.0")+"% within "+hero.Quality+(selected?" · weight 3":" · weight 1");
                wishlistCards[i].GetComponent<Image>().color=selected?new Color(.25f,.19f,.10f):new Color(.075f,.085f,.105f);
                wishlistCards[i].interactable=selected || battle.WishlistCount(wishlistFaction)<Summoning.WishlistPerFaction;
            }
        }
    }
}
