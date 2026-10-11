using System;
using System.Collections.Generic;
namespace Ashlight {
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
            new ChapterDefinition("The Eclipse Throne",new[]{"Dusk Shade","Hollow Templar","Nightbound Oracle","Eclipse Warden","Veiled Monarch"},5,Element.Light,Element.Fire)
        });
        public static int TotalStages { get { return Catalog.Count*StagesPerChapter; } }
        // Chapter 3 onward ends with two bosses in the same encounter. Save indices stay stable.
        static readonly string[] Partners = { "Ashen Marshal", "Stormbound Executioner", "Rotbound Consort", "Eclipse Harbinger" };
        public static IReadOnlyList<EnemyDefinition> EnemiesAt(int encounter) {
            var primary = EnemyAt(encounter);
            if (encounter / StagesPerChapter < 2 || encounter % StagesPerChapter != 4)
                return Array.AsReadOnly(new[] { primary });
            var partner = new EnemyDefinition(Partners[encounter / StagesPerChapter - 2], (int)(primary.MaxHealth*.7f), primary.Resistance, primary.Weakness, true);
            return Array.AsReadOnly(new[] { primary, partner });
        }
        public static IReadOnlyList<EnemyDefinition> WaveAt(int encounter,int wave,int waveCount) {
            if(waveCount<1 || wave<0 || wave>=waveCount) throw new ArgumentOutOfRangeException("wave");
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
            return 1+Math.Max(0,encounter-StagesPerChapter+1)*.08;
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
