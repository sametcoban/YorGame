using System;
using System.Collections.Generic;
namespace Ashlight {
    // Presentation only: encounter stats, rewards, and defense windows remain in Battle.
    public sealed class EnemyVisualDefinition {
        public readonly string EnemyName, ModelName, AttackLabel, SourceModel, ClothRole;
        public readonly float Height;
        public readonly Element EffectElement;
        public readonly bool IsBoss, IsRanged;
        public string ResourcePath { get { return "Enemies/" + ModelName; } }
        EnemyVisualDefinition(string enemyName, string modelName, string attackLabel, float height, Element effectElement, bool boss = false, bool ranged = false, string sourceModel = null, string clothRole = null) {
            EnemyName = enemyName; ModelName = modelName; AttackLabel = attackLabel; Height = height;
            EffectElement = effectElement; IsBoss = boss; IsRanged = ranged; SourceModel = sourceModel ?? modelName; ClothRole = clothRole;
        }
        public static readonly IReadOnlyList<EnemyVisualDefinition> ChapterOne = Array.AsReadOnly(new[] {
            new EnemyVisualDefinition("Lantern Warden", "LanternWarden", "HALBERD SWEEP", 2.35f, Element.Physical),
            new EnemyVisualDefinition("Ash Hound", "AshHound", "RAVENOUS BITE", 1.45f, Element.Physical),
            new EnemyVisualDefinition("Cinder Scout", "CinderScout", "FROST HEX", 2.15f, Element.Cold, ranged:true),
            new EnemyVisualDefinition("Iron Watcher", "IronWatcher", "IRON CLEAVE", 2.4f, Element.Physical),
            new EnemyVisualDefinition("Roadkeeper", "Roadkeeper", "LANTERN SLAM", 2.8f, Element.Fire, boss:true)
        });
        public static readonly IReadOnlyList<EnemyVisualDefinition> PairedBosses = Array.AsReadOnly(new[] {
            new EnemyVisualDefinition("Cinder Sovereign", "CinderSovereign", "CINDER SLAM", 2.65f, Element.Fire, true, sourceModel:"Roadkeeper", clothRole:"WineWool"),
            new EnemyVisualDefinition("Ashen Marshal", "AshenMarshal", "ASHEN CLEAVE", 2.35f, Element.Fire, true, sourceModel:"IronWatcher", clothRole:"SootWool"),
            new EnemyVisualDefinition("Storm Regent", "StormRegent", "THUNDER SLAM", 2.65f, Element.Lightning, true, sourceModel:"Roadkeeper", clothRole:"VioletWool"),
            new EnemyVisualDefinition("Stormbound Executioner", "StormboundExecutioner", "STORM HEX", 2.3f, Element.Lightning, true, true, "CinderScout", "VioletWool"),
            new EnemyVisualDefinition("Thorn Queen", "ThornQueen", "THORN HEX", 2.5f, Element.Poison, true, true, "HollowMatron", "MossWool"),
            new EnemyVisualDefinition("Rotbound Consort", "RotboundConsort", "BLIGHT SLAM", 2.65f, Element.Poison, true, sourceModel:"Roadkeeper", clothRole:"MossWool"),
            new EnemyVisualDefinition("Veiled Monarch", "VeiledMonarch", "ECLIPSE HEX", 2.5f, Element.Light, true, true, "CinderScout", "SootWool"),
            new EnemyVisualDefinition("Eclipse Harbinger", "EclipseHarbinger", "ECLIPSE SLAM", 2.65f, Element.Light, true, sourceModel:"Roadkeeper", clothRole:"SootWool")
        });
        public static readonly IReadOnlyList<EnemyVisualDefinition> ChapterEnemies = Array.AsReadOnly(new[] {
            new EnemyVisualDefinition("Frost Sentinel","FrostSentinel","FROST CLEAVE",2.35f,Element.Cold,sourceModel:"LanternWarden",clothRole:"AshWool"),
            new EnemyVisualDefinition("Icefang","Icefang","FROZEN BITE",1.5f,Element.Cold,sourceModel:"AshHound",clothRole:"AshWool"),
            new EnemyVisualDefinition("Snowbound Knight","SnowboundKnight","ICE CLEAVE",2.4f,Element.Cold,sourceModel:"IronWatcher",clothRole:"AshWool"),
            new EnemyVisualDefinition("Glacier Spirit","GlacierSpirit","GLACIER HEX",2.2f,Element.Cold,ranged:true,sourceModel:"CinderScout",clothRole:"AshWool"),
            new EnemyVisualDefinition("Winter Matriarch","WinterMatriarch","WINTER TEMPEST",2.6f,Element.Cold,boss:true,ranged:true,sourceModel:"HollowMatron",clothRole:"AshWool"),
            new EnemyVisualDefinition("Ember Guard","EmberGuard","EMBER CLEAVE",2.35f,Element.Fire,sourceModel:"LanternWarden",clothRole:"WineWool"),
            new EnemyVisualDefinition("Flame Drake","FlameDrake","FLAME BITE",1.65f,Element.Fire,sourceModel:"AshHound",clothRole:"WineWool"),
            new EnemyVisualDefinition("Coal Colossus","CoalColossus","COAL CRUSH",2.7f,Element.Fire,sourceModel:"IronWatcher",clothRole:"SootWool"),
            new EnemyVisualDefinition("Furnace Keeper","FurnaceKeeper","FURNACE HEX",2.35f,Element.Fire,ranged:true,sourceModel:"CinderScout",clothRole:"WineWool"),
            new EnemyVisualDefinition("Storm Revenant","StormRevenant","STORM CLEAVE",2.4f,Element.Lightning,sourceModel:"LanternWarden",clothRole:"VioletWool"),
            new EnemyVisualDefinition("Thunder Hawk","ThunderHawk","THUNDER HEX",2.2f,Element.Lightning,ranged:true,sourceModel:"CinderScout",clothRole:"VioletWool"),
            new EnemyVisualDefinition("Charged Construct","ChargedConstruct","CHARGED CRUSH",2.6f,Element.Lightning,sourceModel:"IronWatcher",clothRole:"VioletWool"),
            new EnemyVisualDefinition("Tempest Herald","TempestHerald","TEMPEST HEX",2.3f,Element.Lightning,ranged:true,sourceModel:"HollowMatron",clothRole:"VioletWool"),
            new EnemyVisualDefinition("Blight Guardian","BlightGuardian","BLIGHT CLEAVE",2.4f,Element.Poison,sourceModel:"LanternWarden",clothRole:"MossWool"),
            new EnemyVisualDefinition("Venom Bloom","VenomBloom","VENOM HEX",2.15f,Element.Poison,ranged:true,sourceModel:"HollowMatron",clothRole:"MossWool"),
            new EnemyVisualDefinition("Rootbound Golem","RootboundGolem","ROOT CRUSH",2.7f,Element.Poison,sourceModel:"IronWatcher",clothRole:"MossWool"),
            new EnemyVisualDefinition("Plague Weaver","PlagueWeaver","PLAGUE HEX",2.3f,Element.Poison,ranged:true,sourceModel:"CinderScout",clothRole:"MossWool"),
            new EnemyVisualDefinition("Dusk Shade","DuskShade","DUSK HEX",2.2f,Element.Light,ranged:true,sourceModel:"CinderScout",clothRole:"SootWool"),
            new EnemyVisualDefinition("Hollow Templar","HollowTemplar","HOLLOW CLEAVE",2.4f,Element.Light,sourceModel:"LanternWarden",clothRole:"SootWool"),
            new EnemyVisualDefinition("Nightbound Oracle","NightboundOracle","NIGHT HEX",2.3f,Element.Light,ranged:true,sourceModel:"HollowMatron",clothRole:"SootWool"),
            new EnemyVisualDefinition("Eclipse Warden","EclipseWarden","ECLIPSE CRUSH",2.6f,Element.Light,sourceModel:"IronWatcher",clothRole:"SootWool")
        });
        public static readonly IReadOnlyList<EnemyVisualDefinition> Catalog = All();
        static IReadOnlyList<EnemyVisualDefinition> All() {
            var all = new List<EnemyVisualDefinition>(ChapterOne); all.AddRange(ChapterEnemies); all.AddRange(PairedBosses); all.AddRange(LaterEnemies()); all.AddRange(FormationAcolytes()); return all.AsReadOnly();
        }
        static IEnumerable<EnemyVisualDefinition> LaterEnemies() {
            string[][] names={
                new[]{"Ashbound Sentinel","Cinder Beast","Pyre Oracle","Ember Ranger","March Tyrant","Pyre Executioner"},
                new[]{"Crypt Guardian","Drowned Hound","Tide Witch","Grave Archer","Drowned King","Tidebound Reaper"},
                new[]{"Blood Templar","Crimson Beast","Scarlet Priest","Sanguine Hunter","Crimson Pontiff","Blood Confessor"},
                new[]{"Obsidian Guard","Glassfang","Ash Prophet","Cinder Stalker","Obsidian Colossus","Glass Warden"},
                new[]{"Pale Sentinel","Rimefang","Winter Seer","Frost Hunter","Pale Sovereign","Rime Herald"},
                new[]{"Abyss Guard","Void Hound","Night Prophet","Dread Archer","Abyssal Emperor","Void Regent"},
                new[]{"Bell Guard","Chimefang","Echo Priest","Silent Hunter","Hollow Bellmaster","Bell Confessor"},
                new[]{"Salt Sentinel","Brine Hound","Tide Cantor","Reef Archer","Saltbound Empress","Brine Regent"},
                new[]{"Web Guardian","Venomfang","Widow Seer","Silk Stalker","Widow Sovereign","Silk Reaper"},
                new[]{"Astral Guard","Starfang","Comet Oracle","Orbit Hunter","Fallen Astronomer","Comet Warden"},
                new[]{"Forge Sentinel","Slag Beast","Furnace Prophet","Ash Marksman","Sunless Forgemaster","Iron Confessor"},
                new[]{"Dawnless Guard","Gloam Hound","Night Cantor","Dusk Archer","Dawn Eater","Gloam Regent"},
                new[]{"Raven Guard","Blackfang","Feather Oracle","Raven Archer","Raven Castellan","Wingless Marshal"},
                new[]{"Cobalt Sentinel","Ore Hound","Crystal Seer","Shard Hunter","Cobalt Overseer","Crystal Warden"},
                new[]{"Harbor Guard","Bilge Hound","Rot Priest","Salt Archer","Rot Admiral","Bilge Regent"},
                new[]{"Sepulchre Guard","Pyre Hound","Ash Cantor","Ember Hunter","Ember Hierophant","Pyre Confessor"},
                new[]{"Dream Guard","Dreamfang","Lucid Oracle","Dream Archer","Dreaming Regent","Lucid Harbinger"},
                new[]{"Gate Sentinel","Dawnfang","Gate Priest","Dawn Hunter","Dawnfall Keeper","Dawnfall Reaper"}
            };
            string[] models={"LanternWarden","AshHound","HollowMatron","CinderScout","Roadkeeper","IronWatcher"};
            string[] cloth={"WineWool","AshWool","WineWool","SootWool","AshWool","VioletWool","VioletWool","AshWool","MossWool","VioletWool","WineWool","SootWool","SootWool","AshWool","MossWool","WineWool","VioletWool","WineWool"};
            Element[] elements={Element.Fire,Element.Cold,Element.Light,Element.Fire,Element.Cold,Element.Poison,Element.Lightning,Element.Cold,Element.Poison,Element.Lightning,Element.Fire,Element.Poison,Element.Poison,Element.Cold,Element.Poison,Element.Fire,Element.Lightning,Element.Light};
            for(int chapter=0;chapter<names.Length;chapter++) for(int enemy=0;enemy<6;enemy++)
                yield return new EnemyVisualDefinition(names[chapter][enemy],names[chapter][enemy].Replace(" ",""),elements[chapter]+" STRIKE",enemy==1?1.6f:enemy>=4?2.65f:2.35f,elements[chapter],enemy>=4,enemy==2 || enemy==3,models[enemy],cloth[chapter]);
        }
        static IEnumerable<EnemyVisualDefinition> FormationAcolytes() {
            for(int chapter=18;chapter<ChapterDefinition.Catalog.Count;chapter++) { var name=ChapterDefinition.Catalog[chapter].Enemies[2].Name+" Acolyte"; yield return new EnemyVisualDefinition(name,name.Replace(" ",""),"WARDING HEX",2.25f,EnemySkills.For(chapter*5,false,1).Element,ranged:true,sourceModel:"HollowMatron",clothRole:"VioletWool"); }
        }
        public static EnemyVisualDefinition For(string name) {
            foreach (var entry in Catalog) if (entry.EnemyName == name) return entry;
            return null;
        }
    }
}
