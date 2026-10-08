using System;
namespace Ashlight {
    public sealed class SummonResult {
        public readonly HeroDefinition Hero;
        public readonly bool Duplicate;
        public readonly int Refund;
        public SummonResult(HeroDefinition hero, bool duplicate, int refund) { Hero=hero; Duplicate=duplicate; Refund=refund; }
    }
    public static class Summoning {
        public const int Cost=100, StarterCrystals=300, VictoryCrystals=50;
        public const int RareGuarantee=5, LegendaryGuarantee=15;
        public const string Odds="Common 60% | Uncommon 25% | Rare 10% | Epic 4% | Legendary 1%";
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
