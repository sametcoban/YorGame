using System;
using System.Collections.Generic;
namespace Ashlight {
    public enum GearSlot { Weapon, Armor, Accessory }
    public sealed class GearDefinition {
        public readonly string Id, Name;
        public readonly GearSlot Slot;
        public readonly HeroQuality Quality;
        public readonly Element Element;
        public readonly int Health, Attack, SkillDamage, Protection;
        public readonly float ElementBoost;
        GearDefinition(GearSlot slot,HeroQuality quality,Element element) {
            Slot=slot; Quality=quality; Element=element;
            Id=slot+"_"+quality+"_"+element;
            Name=element+" "+new[]{"Runeblade","Cuirass","Sigil"}[(int)slot];
            int tier=(int)quality+1;
            Attack=slot==GearSlot.Weapon?6*tier:0; SkillDamage=slot==GearSlot.Weapon?3*tier:0;
            Health=slot==GearSlot.Armor?20*tier:0; Protection=slot==GearSlot.Armor?2*tier:0;
            ElementBoost=slot==GearSlot.Accessory?.06f*tier:0;
        }
        public string Description { get { return Quality+" "+Name+" · "+(Slot==GearSlot.Weapon?"Attack +"+Attack+", skill/ultimate damage +"+SkillDamage:Slot==GearSlot.Armor?"HP +"+Health+", protection "+Protection:Element+" damage +"+Math.Round(ElementBoost*100)+"%"); } }
        static readonly IReadOnlyList<GearDefinition> catalog=Build();
        public static IReadOnlyList<GearDefinition> Catalog { get { return catalog; } }
        static IReadOnlyList<GearDefinition> Build() {
            var items=new List<GearDefinition>();
            foreach(GearSlot slot in Enum.GetValues(typeof(GearSlot))) foreach(HeroQuality quality in Enum.GetValues(typeof(HeroQuality))) foreach(Element element in Enum.GetValues(typeof(Element))) items.Add(new GearDefinition(slot,quality,element));
            return items.AsReadOnly();
        }
        public static GearDefinition Find(string id) { foreach(var item in Catalog) if(item.Id==id) return item; return null; }
        public static GearDefinition Drop(bool boss,double roll,int variant) {
            if(double.IsNaN(roll) || roll<0 || roll>=1 || variant<0 || variant>=18) throw new ArgumentOutOfRangeException("loot roll");
            var quality=boss?(roll<.55?HeroQuality.Rare:roll<.90?HeroQuality.Epic:HeroQuality.Legendary):
                (roll<.55?HeroQuality.Common:roll<.85?HeroQuality.Uncommon:roll<.97?HeroQuality.Rare:roll<.995?HeroQuality.Epic:HeroQuality.Legendary);
            return Find(((GearSlot)(variant/6))+"_"+quality+"_"+((Element)(variant%6)));
        }
    }
}
