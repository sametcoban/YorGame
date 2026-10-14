using System;
using System.Collections.Generic;
namespace Ashlight {
    public sealed class ChapterMilestone {
        public readonly string Story;
        public readonly int Crystals, Experience, Shards;
        ChapterMilestone(int chapter,string story) { Story=story; Crystals=100+chapter*25; Experience=100+chapter*25; Shards=5; }
        public string Rewards { get { return "+"+Crystals+" crystals · +"+Experience+" XP and +"+Shards+" shards for every recruited hero"; } }
        static readonly string[] Stories={
            "The Roadkeeper's lantern fades. Rowan carries its last ember toward the mountains, where the frozen gates have begun to open.",
            "The Winter Matriarch falls, and a path thaws through the pass. Beyond it, smoke rises from the Ember Citadel.",
            "The Cinder Sovereign and his marshal lie silent. A stolen furnace seal points the party toward the lightning above Storm Spire.",
            "The storm loses its voice. Beneath the spire, roots split the stone and lead into a garden that should have died centuries ago.",
            "The Thorn Queen's communion breaks. Between the dying vines, the party finds a black road leading to the Eclipse Throne.",
            "The Monarch's veil tears. Dawn reveals an army of ashes marching beyond the throne; the journey is far from over.",
            "The Pyre Legion's banners burn out. Its retreat leaves a drowned staircase descending into the crypt beneath the battlefield.",
            "The Drowned King sinks beneath the last dark tide. A crimson bell rings far above: the Blood Cathedral is calling.",
            "The Crimson Covenant's bell falls silent. Its broken altar hides a map through the Obsidian Wastes.",
            "The Glass Legion shatters. The party crosses the wastes toward a pale horizon where winter has stolen the sun.",
            "The Pale Sovereign's crown cracks. Beyond the returning light waits a final gate, leading into the Abyssal Crown.",
            "The Abyssal Emperor's crown is broken. The heroes return with a living ember, and the world begins to remember the dawn. But six distant lanterns remain dark.",
            "The hollow bell cracks. Its echoes carry a warning from the saltbound coast.",
            "The court releases its drowned captives. A silk road leads inland toward the Widow Woods.",
            "The widow's web loosens. Between its threads hangs a fallen map of the stars.",
            "The shattered lenses reveal a furnace beneath the horizon. Its smoke is swallowing the morning.",
            "The sunless forge falls quiet. Its last chain points toward the creature devouring the dawn.",
            "The Dawn Eater falls. The company lights the road home, carrying the names of everyone who followed. Beyond the valley, another war is beginning.",
            "The raven banners fall. Below the bastion, blue light spills from an abandoned mine.",
            "The mine's overseer breaks. A buried waterway carries its poisoned waste toward the harbor.",
            "The rot admiral loses his fleet. A funeral ship points toward the Ember Sepulchre.",
            "The sepulchre releases its captives. Their dreams gather around a distant spire.",
            "The dreaming regent wakes for the last time. Beyond the spire lies the Dawnfall Gate.",
            "The gate opens to a living horizon. The company carries its hard-won dawn onward."
        };
        public static ChapterMilestone For(int chapter) {
            if(chapter<0 || chapter>=ChapterDefinition.Catalog.Count) throw new ArgumentOutOfRangeException("chapter");
            return new ChapterMilestone(chapter,Stories[chapter]);
        }
    }
    public sealed class ChapterDefinition {
        public readonly string Name;
        public readonly IReadOnlyList<EnemyDefinition> Enemies;
        ChapterDefinition(string name, string[] names, int chapter, Element weakness, Element resistance) {
            Name = name;
            var enemies = new EnemyDefinition[names.Length];
            for (int stage=0;stage<names.Length;stage++) {
                int health=100+(chapter*5+stage)*25;
                // Some stages change the elemental puzzle within each chapter.
                Element weak=stage==2 ? resistance : weakness;
                Element resist=stage==2 ? weakness : resistance;
                enemies[stage]=new EnemyDefinition(names[stage],health,weak,resist,stage==4);
            }
            Enemies=Array.AsReadOnly(enemies);
        }
        public const int StagesPerChapter=5;
        public static readonly IReadOnlyList<ChapterDefinition> Catalog=Array.AsReadOnly(new[] {
            new ChapterDefinition("The Lantern Road",new[]{"Lantern Warden","Ash Hound","Cinder Scout","Iron Watcher","Roadkeeper"},0,Element.Fire,Element.Cold),
            new ChapterDefinition("The Frozen Pass",new[]{"Frost Sentinel","Icefang","Snowbound Knight","Glacier Spirit","Winter Matriarch"},1,Element.Fire,Element.Cold),
            new ChapterDefinition("The Ember Citadel",new[]{"Ember Guard","Flame Drake","Coal Colossus","Furnace Keeper","Cinder Sovereign"},2,Element.Cold,Element.Fire),
            new ChapterDefinition("The Storm Spire",new[]{"Storm Revenant","Thunder Hawk","Charged Construct","Tempest Herald","Storm Regent"},3,Element.Poison,Element.Lightning),
            new ChapterDefinition("The Blighted Garden",new[]{"Blight Guardian","Venom Bloom","Rootbound Golem","Plague Weaver","Thorn Queen"},4,Element.Lightning,Element.Poison),
            new ChapterDefinition("The Eclipse Throne",new[]{"Dusk Shade","Hollow Templar","Nightbound Oracle","Eclipse Warden","Veiled Monarch"},5,Element.Light,Element.Fire),
            new ChapterDefinition("The Ashen March",new[]{"Ashbound Sentinel","Cinder Beast","Pyre Oracle","Ember Ranger","March Tyrant"},6,Element.Cold,Element.Fire),
            new ChapterDefinition("The Drowned Crypt",new[]{"Crypt Guardian","Drowned Hound","Tide Witch","Grave Archer","Drowned King"},7,Element.Lightning,Element.Cold),
            new ChapterDefinition("The Blood Cathedral",new[]{"Blood Templar","Crimson Beast","Scarlet Priest","Sanguine Hunter","Crimson Pontiff"},8,Element.Poison,Element.Light),
            new ChapterDefinition("The Obsidian Wastes",new[]{"Obsidian Guard","Glassfang","Ash Prophet","Cinder Stalker","Obsidian Colossus"},9,Element.Cold,Element.Fire),
            new ChapterDefinition("The Pale Realm",new[]{"Pale Sentinel","Rimefang","Winter Seer","Frost Hunter","Pale Sovereign"},10,Element.Fire,Element.Cold),
            new ChapterDefinition("The Abyssal Crown",new[]{"Abyss Guard","Void Hound","Night Prophet","Dread Archer","Abyssal Emperor"},11,Element.Light,Element.Poison),
            new ChapterDefinition("The Hollow Belfry",new[]{"Bell Guard","Chimefang","Echo Priest","Silent Hunter","Hollow Bellmaster"},12,Element.Light,Element.Lightning),
            new ChapterDefinition("The Saltbound Court",new[]{"Salt Sentinel","Brine Hound","Tide Cantor","Reef Archer","Saltbound Empress"},13,Element.Fire,Element.Cold),
            new ChapterDefinition("The Widow Woods",new[]{"Web Guardian","Venomfang","Widow Seer","Silk Stalker","Widow Sovereign"},14,Element.Cold,Element.Poison),
            new ChapterDefinition("The Shattered Observatory",new[]{"Astral Guard","Starfang","Comet Oracle","Orbit Hunter","Fallen Astronomer"},15,Element.Poison,Element.Lightning),
            new ChapterDefinition("The Sunless Forge",new[]{"Forge Sentinel","Slag Beast","Furnace Prophet","Ash Marksman","Sunless Forgemaster"},16,Element.Lightning,Element.Fire),
            new ChapterDefinition("The Last Dawn",new[]{"Dawnless Guard","Gloam Hound","Night Cantor","Dusk Archer","Dawn Eater"},17,Element.Light,Element.Poison),
            new ChapterDefinition("The Raven Bastion",new[]{"Raven Guard","Blackfang","Feather Oracle","Raven Archer","Raven Castellan"},18,Element.Light,Element.Poison),
            new ChapterDefinition("The Cobalt Mines",new[]{"Cobalt Sentinel","Ore Hound","Crystal Seer","Shard Hunter","Cobalt Overseer"},19,Element.Fire,Element.Cold),
            new ChapterDefinition("The Festered Harbor",new[]{"Harbor Guard","Bilge Hound","Rot Priest","Salt Archer","Rot Admiral"},20,Element.Cold,Element.Poison),
            new ChapterDefinition("The Ember Sepulchre",new[]{"Sepulchre Guard","Pyre Hound","Ash Cantor","Ember Hunter","Ember Hierophant"},21,Element.Lightning,Element.Fire),
            new ChapterDefinition("The Dreaming Spire",new[]{"Dream Guard","Dreamfang","Lucid Oracle","Dream Archer","Dreaming Regent"},22,Element.Poison,Element.Lightning),
            new ChapterDefinition("The Dawnfall Gate",new[]{"Gate Sentinel","Dawnfang","Gate Priest","Dawn Hunter","Dawnfall Keeper"},23,Element.Light,Element.Fire)
        });
        public static int TotalStages { get { return Catalog.Count*StagesPerChapter; } }
        // Chapter 3 onward ends with two bosses in the same encounter. Save indices stay stable.
        static readonly string[] Partners = { "Ashen Marshal", "Stormbound Executioner", "Rotbound Consort", "Eclipse Harbinger", "Pyre Executioner", "Tidebound Reaper", "Blood Confessor", "Glass Warden", "Rime Herald", "Void Regent", "Bell Confessor", "Brine Regent", "Silk Reaper", "Comet Warden", "Iron Confessor", "Gloam Regent","Wingless Marshal","Crystal Warden","Bilge Regent","Pyre Confessor","Lucid Harbinger","Dawnfall Reaper" };
        public static IReadOnlyList<EnemyDefinition> EnemiesAt(int encounter) {
            var primary = EnemyAt(encounter);
            if (encounter / StagesPerChapter < 2 || encounter % StagesPerChapter != 4)
                return Array.AsReadOnly(new[] { primary });
            var partner = new EnemyDefinition(Partners[encounter / StagesPerChapter - 2], (int)(primary.MaxHealth*.7f), primary.Resistance, primary.Weakness, true);
            return Array.AsReadOnly(new[] { primary, partner });
        }
        public static IReadOnlyList<EnemyDefinition> WaveAt(int encounter,int wave,int waveCount) {
            if(waveCount<1 || wave<0 || wave>=waveCount) throw new ArgumentOutOfRangeException("wave");
            if(encounter/StagesPerChapter>=18) {
                if(wave==waveCount-1 && encounter%5==4) return EnemiesAt(encounter);
                var roster=Catalog[encounter/5].Enemies; int formationChapter=encounter/5;
                var roles=new[]{EnemyRole.Defender,EnemyRole.Bruiser,EnemyRole.Caster,formationChapter==20 || formationChapter==23?EnemyRole.Healer:EnemyRole.Support,EnemyRole.Archer};
                var sources=new[]{0,1,2,2,3}; var formation=new EnemyDefinition[5];
                for(int i=0;i<5;i++) { var formationSource=roster[sources[i]]; formation[i]=new EnemyDefinition(formationSource.Name+(i==3?" Acolyte":""),Math.Max(20,(int)Math.Ceiling(formationSource.MaxHealth*(wave==waveCount-1?.32:.14))),formationSource.Weakness,formationSource.Resistance,role:roles[i]); }
                return Array.AsReadOnly(formation);
            }
            if(encounter/StagesPerChapter>=6) {
                if(wave==waveCount-1 && encounter%StagesPerChapter==4) return EnemiesAt(encounter);
                var chapterRoster=Catalog[encounter/StagesPerChapter];
                int count=2+(encounter%StagesPerChapter+wave)%2;
                var group=new EnemyDefinition[count];
                int chapterIndex=encounter/StagesPerChapter;
                var rearRole=chapterIndex==8 || chapterIndex==12 || chapterIndex==17?EnemyRole.Healer:chapterIndex==9 || chapterIndex==16?EnemyRole.Support:chapterIndex==7 || chapterIndex==13?EnemyRole.Archer:chapterIndex==10?EnemyRole.Caster:new[]{EnemyRole.Caster,EnemyRole.Archer}[(encounter+wave)%2];
                var roles=new[]{EnemyRole.Defender,rearRole,EnemyRole.Bruiser};
                for(int i=0;i<count;i++) {
                    // Match silhouettes to roles: guardian, priest/archer, beast.
                    var sourceEnemy=chapterRoster.Enemies[i==0?0:i==2?1:rearRole==EnemyRole.Archer?3:2];
                    group[i]=new EnemyDefinition(sourceEnemy.Name,Math.Max(20,(int)Math.Ceiling(sourceEnemy.MaxHealth*(wave==waveCount-1?.55:.22))),sourceEnemy.Weakness,sourceEnemy.Resistance,role:roles[i]);
                }
                return Array.AsReadOnly(group);
            }
            if(wave==waveCount-1) return EnemiesAt(encounter);
            var chapter=Catalog[encounter/StagesPerChapter];
            var source=chapter.Enemies[(encounter%StagesPerChapter+wave)%4];
            var first=new EnemyDefinition(source.Name,Math.Max(20,(int)Math.Ceiling(source.MaxHealth*.3)),source.Weakness,source.Resistance);
            if(encounter/StagesPerChapter==0 || (wave==0 && (encounter/StagesPerChapter<2 || encounter%StagesPerChapter%2==0))) return Array.AsReadOnly(new[]{first});
            // Mixed waves use a frontline enemy and a rear threat from the same chapter.
            var front=source; var next=source;
            for(int offset=0;offset<4;offset++) {
                var candidate=chapter.Enemies[(encounter%StagesPerChapter+wave+offset)%4];
                if(candidate.Role==(wave==0?EnemyRole.Bruiser:EnemyRole.Defender)) { front=candidate; break; }
            }
            for(int offset=0;offset<4;offset++) {
                var candidate=chapter.Enemies[(encounter%StagesPerChapter+wave+1+offset)%4];
                if(candidate.Row==EnemyRow.Back) { next=candidate; break; }
            }
            first=new EnemyDefinition(front.Name,Math.Max(20,(int)Math.Ceiling(front.MaxHealth*.3)),front.Weakness,front.Resistance);
            var second=new EnemyDefinition(next.Name,Math.Max(20,(int)Math.Ceiling(next.MaxHealth*.3)),next.Weakness,next.Resistance,role:new[]{EnemyRole.Caster,EnemyRole.Archer,EnemyRole.Healer,EnemyRole.Support,EnemyRole.Caster}[encounter%StagesPerChapter]);
            return Array.AsReadOnly(new[]{first,second});
        }
        public static double DamageScale(int encounter) {
            if(encounter<0 || encounter>=TotalStages) throw new ArgumentOutOfRangeException("encounter");
            // Keep the first twelve chapters stable; health keeps growing after the level cap.
            return 1+Math.Max(0,Math.Min(encounter,59)-StagesPerChapter+1)*.08+Math.Max(0,Math.Min(encounter,89)-59)*.04+Math.Max(0,encounter-89)*.015;
        }
        public static int WavePower(int encounter,int wave,int waveCount=3,bool scaleCampaignDamage=true) {
            int partyLimit=Math.Min(5,encounter/StagesPerChapter+1); double total=0;
            foreach(var enemy in WaveAt(encounter,wave,waveCount)) {
                int health=(int)Math.Ceiling(enemy.MaxHealth*Math.Max(1,partyLimit/3.0));
                double attacks=0;
                for(int move=0;move<3;move++) {
                    var skill=EnemySkills.ForRole(encounter,enemy,move);
                    int damage=wave<waveCount-1?(int)Math.Ceiling(skill.Damage*.6):skill.Damage;
                    if(waveCount>1 && scaleCampaignDamage) damage=(int)Math.Ceiling(damage*DamageScale(encounter));
                    attacks+=damage*(skill.PartyWide?partyLimit:1);
                    if(skill.Action==EnemyAction.HealPartner) attacks+=health*.2;
                    if(skill.Action==EnemyAction.Shield) attacks+=enemy.IsBoss?60:25;
                }
                total+=health+attacks*4/3+(enemy.Role==EnemyRole.Defender?health*.1:0);
                if(enemy.Name=="Cinder Sovereign") total+=80;
                if(enemy.Name=="Thorn Queen" || enemy.Name=="Veiled Monarch") total+=100;
            }
            return Math.Max(1,(int)Math.Ceiling(total));
        }
        public static int StagePower(int encounter,int waveCount=3,bool scaleCampaignDamage=true) {
            if(waveCount<1) throw new ArgumentOutOfRangeException("waveCount");
            int sum=0,peak=0;
            for(int wave=0;wave<waveCount;wave++) { int value=WavePower(encounter,wave,waveCount,scaleCampaignDamage); sum+=value; peak=Math.Max(peak,value); }
            return peak+(int)Math.Ceiling((sum-peak)*.25);
        }
        public static string Difficulty(int teamPower,int stagePower) {
            if(stagePower<=0 || teamPower<0) throw new ArgumentOutOfRangeException("power");
            return teamPower>=stagePower*1.25?"EASY":teamPower<=stagePower*.85?"HARD":"EVEN";
        }
        public static string Preview(int encounter,double powerScale=1,bool scaleCampaignDamage=true) {
            var lines=new List<string>();
            lines.Add(EnemySkills.Family(encounter)+" · "+EnemySkills.Tactics(encounter));
            for(int wave=0;wave<3;wave++) {
                lines.Add("WAVE "+(wave+1)+(wave==2?" · FINAL":"")+" · Power "+(int)Math.Ceiling(WavePower(encounter,wave,3,scaleCampaignDamage)*powerScale));
                foreach(var enemy in WaveAt(encounter,wave,3))
                    lines.Add(enemy.Name+" · "+enemy.Role+" / "+enemy.Row+" · Weak: "+enemy.Weakness+" · Resists: "+enemy.Resistance);
            }
            return string.Join("\n",lines.ToArray());
        }
        public static EnemyDefinition EnemyAt(int encounter) {
            if(encounter<0 || encounter>=TotalStages) throw new ArgumentOutOfRangeException("encounter");
            return Catalog[encounter/StagesPerChapter].Enemies[encounter%StagesPerChapter];
        }
    }
}
