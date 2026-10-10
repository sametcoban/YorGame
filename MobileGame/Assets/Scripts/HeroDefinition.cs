using System;
using System.Collections.Generic;
namespace Ashlight {
    public enum HeroGender { Woman, Man }
    public enum HeroQuality { Common, Uncommon, Rare, Epic, Legendary }
    public sealed class HeroDefinition {
        public readonly string Id, Name;
        public readonly HeroClass Class;
        public readonly HeroQuality Quality;
        public readonly HeroGender Gender;
        public readonly int Age;
        public string DisplayClass { get { return Class == HeroClass.Sorceress && Gender == HeroGender.Man ? "Sorcerer" : Stats.Name; } }
        public readonly Element Affinity;
        public readonly ClassDefinition Stats;
        public readonly UltimateDefinition Ultimate;
        HeroDefinition(string id, string name, HeroClass kind, HeroQuality quality, HeroGender gender) {
            var affinities = ClassElements.For(kind);
            Affinity = affinities[(int)quality % affinities.Count];
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
            // Recruit common heroes first, then progress through the rarity tiers.
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
