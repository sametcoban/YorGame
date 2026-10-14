using System;
using System.Text;
namespace Ashlight {
    public sealed class CampaignStory {
        public readonly string Title;
        public readonly string[] Speakers, Lines;
        CampaignStory(string title,string[] speakers,string[] lines) { Title=title; Speakers=speakers; Lines=lines; }
        static readonly string[] Openings={
            "The lanterns along the old road have stopped answering the dawn. A lone knight follows the last one still burning.",
            "Beyond the road lies a frozen pass. Shapes stand beneath the ice, watching every footstep toward the gate.",
            "A stolen furnace seal draws the company into the Ember Citadel. The fire inside is feeding on something other than coal.",
            "The citadel's smoke points toward Storm Spire. Its bells ring without hands, calling lightning into the stone.",
            "Roots split the spire's foundations. Beneath them grows a garden whose dead refuse to stay buried.",
            "A black road leaves the garden. At its end, the Eclipse Throne waits beneath a sun that never rises.",
            "The broken throne reveals an army marching through ash. Every banner bears the mark of the stolen flame.",
            "The army's retreat exposes a drowned stair. In the crypt below, footsteps continue long after their owners have stopped.",
            "A crimson bell calls from above the crypt. The Blood Cathedral has begun collecting names instead of prayers.",
            "The cathedral's altar holds a map through the Obsidian Wastes. Glass figures repeat the words of travellers who vanished there.",
            "Beyond the wastes, winter has stolen the horizon. A pale crown hangs where the sun should be.",
            "One gate remains. Behind the Abyssal Crown, the first stolen ember still remembers the world before its darkness.",
            "Six lanterns remain dark beyond the broken crown. The first hangs in a belfry whose bells repeat the voices of the lost.",
            "The bells send the company toward a coast crusted with salt. A court beneath the tide demands a living heir.",
            "Silk covers the inland road. Every thread leads toward a widow who remembers the company before they arrived.",
            "Beyond the webs stands an observatory in ruins. Its fallen stars still obey someone inside.",
            "The lenses point toward a hidden forge. Its smiths hammer stolen sunlight into chains.",
            "The last chain stretches into a sunless valley. Something vast has learned to swallow the dawn."
        };
        static readonly string[] Replies={
            "Then we keep this flame alive.","We cross together. Nobody stays beneath that ice.","Find the furnace. Break its chains.","Let the bells ring. We know where to go.","We will give this place a quieter ending.","A throne can fall. Keep moving.","Their march ends here.","Follow my lantern. Stay close.","We take our names back.","Listen to each other, not the glass.","There is still light beyond that crown.","One last gate. One living ember.",
            "We will answer with our own voices.",
            "Nobody is tribute. We break the court.",
            "Cut a path. Keep each other in sight.",
            "Stars can fall. So can their master.",
            "Break the chains before morning disappears.",
            "We carry the dawn together."
        };
        static readonly string[] BossLines={
            "This road ends where my lantern falls.","Warm blood has no place beyond my gate.","The furnace has room for all of you.","The spire will speak through your bones.","Lay down your weapons. My roots will carry you.","Kneel, and you may forget the dawn.","The march does not stop for the living.","Every kingdom sinks. Yours is next.","Your names belong to the bell now.","Break one reflection. A thousand remain.","Nothing survives the winter I wear.","Bring me your ember. Let the world go quiet.",
            "Every voice becomes my bell.",
            "The tide has already chosen your heir.",
            "Your paths were woven long ago.",
            "Look up. Your stars are falling.",
            "The sun is only another metal to bend.",
            "Your little lantern cannot feed the sky."
        };
        public static CampaignStory For(int chapter,bool boss) {
            if(chapter<0 || chapter>=ChapterDefinition.Catalog.Count) throw new ArgumentOutOfRangeException("chapter");
            string title=ChapterDefinition.Catalog[chapter].Name;
            if(!boss) return new CampaignStory(title,new[]{"Narrator","Rowan","Narrator"},new[]{Openings[chapter],Replies[chapter],"The company prepares its formation before entering the next battle."});
            var enemies=ChapterDefinition.EnemiesAt(chapter*5+4); string names=enemies[0].Name;
            if(enemies.Count>1) names+=" and "+enemies[1].Name;
            return new CampaignStory("BOSS ENCOUNTER · "+title,new[]{"Narrator",enemies[0].Name,"Rowan"},new[]{names+" block the chapter's final passage. The company must fight through the approach before facing them.",BossLines[chapter],Replies[chapter]});
        }
    }
    public static class BetaSupport {
        public const string BuildTag="ashlight-beta-prep-4";
        public static string Feedback(ProgressData progress,string category,string notes,string runtime,DateTime time) {
            var report=new StringBuilder();
            report.AppendLine("ASHLIGHT PLAYTEST REPORT · "+BuildTag).AppendLine("UTC: "+time.ToUniversalTime().ToString("u"));
            report.AppendLine("Category: "+category).AppendLine("Runtime: "+runtime);
            report.AppendLine("Save version: "+progress.version+" · Stage index: "+progress.encounter+" · Tower: "+progress.inTower+" / "+progress.towerFloor);
            report.AppendLine("Crystals: "+progress.crystals+" · Materials: "+progress.forgeMaterials+" · Rare/Legendary misses: "+progress.rareMisses+" / "+progress.legendaryMisses);
            report.AppendLine("Daily claim UTC ticks: "+progress.bossClaimDay+" · Weekly claim UTC ticks: "+progress.weeklyClaim);
            report.AppendLine("Wishlist: "+string.Join(", ",progress.wishlist));
            report.AppendLine("Formation: "+string.Join(", ",progress.party));
            foreach(var id in progress.party) foreach(var hero in progress.loadouts) if(hero.id==id) {
                report.AppendLine(id+" · own Lv "+hero.level+" · rank "+hero.upgradeRank+" · mastery "+hero.masteryRank+" · bond participations "+hero.bondBattles+" · skills "+hero.firstSkill+", "+hero.secondSkill+" · talents "+string.Join(", ",Array.ConvertAll(hero.talents,n=>n.ToString())));
                report.AppendLine("Gear: "+string.Join(", ",hero.gear??new string[0]));
            }
            foreach(var gear in progress.enhancements) report.AppendLine("Forged: "+gear.id+" +"+gear.level);
            report.AppendLine("NOTES / WHAT HAPPENED / STEPS TO REPEAT:");
            report.AppendLine(string.IsNullOrWhiteSpace(notes)?"(Add your observations here.)":notes.Substring(0,Math.Min(2000,notes.Length)));
            return report.ToString();
        }
    }
    public sealed partial class Battle {
        int seenStories, seenLaterStories;
        public bool StoryUnlocked(int chapter,bool boss) { return chapter>=0 && chapter<ChapterDefinition.Catalog.Count && !InTower && encounter>=chapter*5+(boss?4:0); }
        public bool StorySeen(int chapter,bool boss) { return chapter>=0 && chapter<ChapterDefinition.Catalog.Count && (chapter<12?(seenStories&(1<<(chapter+(boss?12:0))))!=0:(seenLaterStories&(1<<(chapter-12+(boss?6:0))))!=0); }
        public bool MarkStorySeen(int chapter,bool boss) {
            if(IsDailyBoss || !campaign || (!CanChangeParty && !CampaignComplete) || !StoryUnlocked(chapter,boss)) return false;
            if(chapter<12) seenStories|=1<<(chapter+(boss?12:0)); else seenLaterStories|=1<<(chapter-12+(boss?6:0)); return true;
        }
        internal static int AllowedLaterStories(int stage) {
            int intros=Math.Max(0,Math.Min(6,stage/5-11)),bosses=Math.Max(0,Math.Min(6,(stage+1)/5-12));
            return ((1<<intros)-1)|(((1<<bosses)-1)<<6);
        }
        internal static int AllowedStories(int stage) {
            int intros=Math.Min(12,stage/5+1),bosses=Math.Min(12,(stage+1)/5);
            return ((1<<intros)-1)|(((1<<bosses)-1)<<12);
        }
        public string OpeningChecklist {
            get {
                bool talent=false; foreach(var hero in recruits) for(int tier=0;tier<3;tier++) if(HeroProgress(hero.Id).HasTalent(tier,0) || HeroProgress(hero.Id).HasTalent(tier,1)) talent=true;
                return (encounter>=1?"[DONE]":"[NEXT]")+" Claim your first stage victory.\n"+(encounter>=5?"[DONE]":"[NEXT]")+" Clear chapter 1 and meet Lucan.\n"+(encounter>=5 && recruits.Count>=2?"[DONE]":"[NEXT]")+" Build a two-hero team for chapter 2.\n"+(talent?"[DONE]":"[NEXT]")+" Choose a free talent at own level 5.\n"+(encounter>=10?"[DONE]":"[NEXT]")+" Reach chapter 3 to unlock the Daily Boss.";
            }
        }
    }
}
