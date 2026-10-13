using System;
using System.Collections.Generic;
namespace Ashlight {
    public enum HeroFaction { DawnOrder, Wilderkin, Veilborn }
    public enum HeroGender { Woman, Man }
    public enum HeroQuality { Common, Uncommon, Rare, Epic, Legendary }
    public sealed class StoryRecruit {
        public readonly int Encounter;
        public readonly string HeroId, Introduction;
        StoryRecruit(int encounter,string id,string introduction) { Encounter=encounter; HeroId=id; Introduction=introduction; }
        public HeroDefinition Hero { get { foreach(var hero in HeroDefinition.Catalog) if(hero.Id==HeroId) return hero; throw new InvalidOperationException("Unknown story hero."); } }
        public static readonly IReadOnlyList<StoryRecruit> Catalog=Array.AsReadOnly(new[]{
            new StoryRecruit(4,"Paladin_Common","Lucan joins at the road's end, offering his oath and a shield against the coming frost."),
            new StoryRecruit(6,"Sorceress_Common","Elara emerges from a broken ice seal. Her spells reveal the path through the Frozen Pass."),
            new StoryRecruit(9,"Ranger_Common","Finn tracks the fleeing frost spirits and offers to guide the party to the citadel."),
            new StoryRecruit(12,"Cleric_Common","Tessa tends the survivors of the furnace raids. She joins to keep the party alive."),
            new StoryRecruit(14,"Rogue_Common","Wren steals the citadel's storm map and joins the heroes before the gates close."),
            new StoryRecruit(29,"Knight_Rare","Brenna rises from the Eclipse battlefield, ready to hold the line against the Pyre Legion."),
            new StoryRecruit(39,"Ranger_Rare","Tarin scouts a route out of the drowned crypt and brings his hunter's skill to the party."),
            new StoryRecruit(49,"Paladin_Rare","Garrick refuses to abandon the wastes. His oath becomes the party's shelter against the pale winter."),
            new StoryRecruit(59,"Sorceress_Rare","Selene returns from the broken abyss, bearing the magic needed for the battles still to come.")
        });
        public static StoryRecruit At(int encounter) { foreach(var recruit in Catalog) if(recruit.Encounter==encounter) return recruit; return null; }
    }
    public sealed class HeroDefinition {
        public readonly string Id, Name;
        public readonly HeroClass Class;
        public readonly HeroQuality Quality;
        public readonly HeroFaction Faction;
        public string FactionName { get { return FactionDisplay(Faction); } }
        public static string FactionDisplay(HeroFaction faction) { return faction==HeroFaction.DawnOrder?"Dawn Order":faction==HeroFaction.Wilderkin?"Wilderkin":"Veilborn"; }
        public readonly HeroGender Gender;
        public readonly int Age;
        public string DisplayClass { get { return Class == HeroClass.Sorceress && Gender == HeroGender.Man ? "Sorcerer" : Stats.Name; } }
        public readonly Element Affinity;
        public readonly ClassDefinition Stats;
        public readonly UltimateDefinition Ultimate;
        HeroDefinition(string id, string name, HeroClass kind, HeroQuality quality, HeroGender gender) {
            var affinities = ClassElements.For(kind);
            Affinity = affinities[(int)quality % affinities.Count];
            Faction=Factions[(int)kind,(int)quality];
            Age = Ages[(int)kind,(int)quality];
            Gender = gender;
            Id = id; Name = name; Class = kind; Quality = quality;
            Stats = ClassDefinition.For(kind).Scaled(Multiplier(quality));
            Ultimate = UltimateDefinition.For(this);
        }
        public static float Multiplier(HeroQuality quality) {
            switch (quality) {
                case HeroQuality.Common: return 1f;
                case HeroQuality.Uncommon: return 1.1f;
                case HeroQuality.Rare: return 1.25f;
                case HeroQuality.Epic: return 1.45f;
                case HeroQuality.Legendary: return 1.7f;
                default: throw new ArgumentOutOfRangeException("quality");
            }
        }
        static readonly string[,] Names = {
            { "Rowan", "Aldric", "Brenna", "Kaelan", "Seraphine" },
            { "Lucan", "Mira", "Garrick", "Aurelia", "Solenne" },
            { "Elara", "Nyra", "Selene", "Vesper", "Astra" },
            { "Finn", "Sylva", "Tarin", "Liora", "Cael" },
            { "Wren", "Kestrel", "Silas", "Raven", "Nyx" },
            { "Tessa", "Jonas", "Amara", "Elyse", "Ilyra" }
        };
        // Authored independently of elemental affinity; each faction spans all six classes.
        static readonly HeroFaction[,] Factions = {
            {HeroFaction.DawnOrder,HeroFaction.Wilderkin,HeroFaction.Veilborn,HeroFaction.DawnOrder,HeroFaction.Wilderkin},
            {HeroFaction.DawnOrder,HeroFaction.Wilderkin,HeroFaction.Veilborn,HeroFaction.DawnOrder,HeroFaction.Wilderkin},
            {HeroFaction.Wilderkin,HeroFaction.Veilborn,HeroFaction.DawnOrder,HeroFaction.Wilderkin,HeroFaction.Veilborn},
            {HeroFaction.Wilderkin,HeroFaction.Veilborn,HeroFaction.DawnOrder,HeroFaction.Wilderkin,HeroFaction.Veilborn},
            {HeroFaction.Veilborn,HeroFaction.DawnOrder,HeroFaction.Wilderkin,HeroFaction.Veilborn,HeroFaction.DawnOrder},
            {HeroFaction.Veilborn,HeroFaction.DawnOrder,HeroFaction.Wilderkin,HeroFaction.Veilborn,HeroFaction.DawnOrder}
        };
        static readonly int[,] Ages = {
            { 28, 42, 31, 35, 44 }, { 33, 29, 46, 38, 51 },
            { 27, 34, 41, 39, 52 }, { 25, 32, 37, 30, 45 },
            { 26, 31, 36, 40, 29 }, { 24, 43, 35, 47, 50 }
        };
        // Gender is authored per named hero, independently of class, rarity, stats, and skills.
        static readonly HeroGender[,] Genders = {
            { HeroGender.Man, HeroGender.Man, HeroGender.Woman, HeroGender.Man, HeroGender.Woman },
            { HeroGender.Man, HeroGender.Woman, HeroGender.Man, HeroGender.Woman, HeroGender.Woman },
            { HeroGender.Woman, HeroGender.Woman, HeroGender.Woman, HeroGender.Man, HeroGender.Woman },
            { HeroGender.Man, HeroGender.Woman, HeroGender.Man, HeroGender.Woman, HeroGender.Man },
            { HeroGender.Woman, HeroGender.Man, HeroGender.Man, HeroGender.Woman, HeroGender.Woman },
            { HeroGender.Woman, HeroGender.Man, HeroGender.Woman, HeroGender.Woman, HeroGender.Woman }
        };
        static readonly IReadOnlyList<HeroDefinition> catalog = CreateCatalog();
        public static IReadOnlyList<HeroDefinition> Catalog { get { return catalog; } }
        static IReadOnlyList<HeroDefinition> CreateCatalog() {
            var heroes = new List<HeroDefinition>();
            // Stable catalog order; story recruitment is scheduled separately.
            for (int tier = 0; tier < 5; tier++)
                for (int kind = 0; kind < 6; kind++)
                    heroes.Add(new HeroDefinition(((HeroClass)kind) + "_" + ((HeroQuality)tier), Names[kind,tier], (HeroClass)kind, (HeroQuality)tier, Genders[kind,tier]));
            return heroes.AsReadOnly();
        }
        public static HeroDefinition Common(HeroClass kind) {
            foreach (var hero in Catalog) if (hero.Class == kind && hero.Quality == HeroQuality.Common) return hero;
            throw new ArgumentOutOfRangeException("kind");
        }
    }
}
