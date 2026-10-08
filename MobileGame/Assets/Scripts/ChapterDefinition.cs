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
                int health=Math.Min(400,100+(chapter*5+stage)*25);
                // Some stages change the elemental puzzle within each chapter.
                Element weak=stage==2 ? resistance : weakness;
                Element resist=stage==2 ? weakness : resistance;
                enemies[stage]=new EnemyDefinition(names[stage],health,weak,resist);
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
        public static EnemyDefinition EnemyAt(int encounter) {
            if(encounter<0 || encounter>=TotalStages) throw new ArgumentOutOfRangeException("encounter");
            return Catalog[encounter/StagesPerChapter].Enemies[encounter%StagesPerChapter];
        }
    }
}
