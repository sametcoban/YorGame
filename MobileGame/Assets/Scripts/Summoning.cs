using System;
namespace Ashlight {
    public sealed class SummonResult {
        public readonly HeroDefinition Hero;
        public readonly bool Duplicate;
        public readonly int Refund, Shards;
        public SummonResult(HeroDefinition hero, bool duplicate, int refund, int shards=0) { Hero=hero; Duplicate=duplicate; Refund=refund; Shards=shards; }
    }
    public static class Summoning {
        public const int Cost=100, StarterCrystals=300, VictoryCrystals=50;
        public const int RareGuarantee=5, LegendaryGuarantee=15;
        public const string Odds="Common 60% | Uncommon 25% | Rare 10% | Epic 4% | Legendary 1%";
        public const int WishlistPerFaction=2, WishlistWeight=3;
        public const string WishlistRules="Choose up to 2 named heroes per faction. Selected Rare+ heroes have 3× selection weight within their own rarity. Other heroes have weight 1. Rarity odds and guarantees stay the same.";
        public static int Weight(HeroDefinition hero, System.Collections.Generic.ICollection<string> wishlist) {
            return hero.Quality>=HeroQuality.Rare && wishlist.Contains(hero.Id)?WishlistWeight:1;
        }
        public static double HeroChance(HeroDefinition hero, System.Collections.Generic.ICollection<string> wishlist) {
            int total=0; foreach(var candidate in HeroDefinition.Catalog) if(candidate.Quality==hero.Quality) total+=Weight(candidate,wishlist);
            return (double)Weight(hero,wishlist)/total;
        }
        public static HeroDefinition Pick(HeroQuality quality,double roll,System.Collections.Generic.ICollection<string> wishlist) {
            if(double.IsNaN(roll) || roll<0 || roll>=1) throw new ArgumentOutOfRangeException("roll");
            int total=0; foreach(var hero in HeroDefinition.Catalog) if(hero.Quality==quality) total+=Weight(hero,wishlist);
            double position=roll*total;
            foreach(var hero in HeroDefinition.Catalog) if(hero.Quality==quality) { position-=Weight(hero,wishlist); if(position<0) return hero; }
            throw new ArgumentOutOfRangeException("quality");
        }
        public static HeroQuality Quality(double roll, int rareMisses, int legendaryMisses) {
            if(double.IsNaN(roll) || double.IsInfinity(roll) || roll<0 || roll>=1) throw new ArgumentOutOfRangeException("roll");
            if(rareMisses<0 || rareMisses>=RareGuarantee || legendaryMisses<0 || legendaryMisses>=LegendaryGuarantee) throw new ArgumentOutOfRangeException("pity");
            HeroQuality quality=roll<.60?HeroQuality.Common:roll<.85?HeroQuality.Uncommon:roll<.95?HeroQuality.Rare:roll<.99?HeroQuality.Epic:HeroQuality.Legendary;
            if(legendaryMisses==LegendaryGuarantee-1) return HeroQuality.Legendary;
            if(rareMisses==RareGuarantee-1 && quality<HeroQuality.Rare) return HeroQuality.Rare;
            return quality;
        }
        public static int DuplicateRefund(HeroQuality quality) {
            switch(quality) {
                case HeroQuality.Common:return 20;
                case HeroQuality.Uncommon:return 30;
                case HeroQuality.Rare:return 50;
                case HeroQuality.Epic:return 70;
                case HeroQuality.Legendary:return 90;
                default:throw new ArgumentOutOfRangeException("quality");
            }
        }
    }
}
