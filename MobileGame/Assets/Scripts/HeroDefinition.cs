using System;
using System.Collections.Generic;
namespace Ashlight {
    public enum HeroQuality { Common, Uncommon, Rare, Epic, Legendary }
    public sealed class HeroDefinition {
        public readonly string Id, Name;
        public readonly HeroClass Class;
        public readonly HeroQuality Quality;
        public readonly Element Affinity;
        public readonly ClassDefinition Stats;
        HeroDefinition(string id, string name, HeroClass kind, HeroQuality quality) {
            var affinities = ClassElements.For(kind);
            Affinity = affinities[(int)quality % affinities.Count];
            Id = id; Name = name; Class = kind; Quality = quality;
            Stats = ClassDefinition.For(kind).Scaled(Multiplier(quality));
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
        static readonly IReadOnlyList<HeroDefinition> catalog = CreateCatalog();
        public static IReadOnlyList<HeroDefinition> Catalog { get { return catalog; } }
        static IReadOnlyList<HeroDefinition> CreateCatalog() {
            var heroes = new List<HeroDefinition>();
            // Recruit common heroes first, then progress through the rarity tiers.
            for (int tier = 0; tier < 5; tier++)
                for (int kind = 0; kind < 6; kind++)
                    heroes.Add(new HeroDefinition(((HeroClass)kind) + "_" + ((HeroQuality)tier), Names[kind,tier], (HeroClass)kind, (HeroQuality)tier));
            return heroes.AsReadOnly();
        }
        public static HeroDefinition Common(HeroClass kind) {
            foreach (var hero in Catalog) if (hero.Class == kind && hero.Quality == HeroQuality.Common) return hero;
            throw new ArgumentOutOfRangeException("kind");
        }
    }
}
