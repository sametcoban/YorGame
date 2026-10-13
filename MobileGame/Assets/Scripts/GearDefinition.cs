using System;
using System.Collections.Generic;
namespace Ashlight {
    public enum GearSlot { Weapon, Armor, Accessory }
    public sealed class GearDefinition {
        public readonly string Id, Name;
        public readonly GearSlot Slot;
        public readonly HeroQuality Quality;
        public readonly Element Element;
        public readonly int Health, Attack, SkillDamage, Protection, Enhancement;
        public const int MaxEnhancement=10;
        GearDefinition[] enhanced;
        public readonly float ElementBoost;
        GearDefinition(GearSlot slot,HeroQuality quality,Element element,int enhancement=0) {
            Slot=slot; Quality=quality; Element=element; Enhancement=enhancement;
            Id=slot+"_"+quality+"_"+element;
            Name=element+" "+new[]{"Runeblade","Cuirass","Sigil"}[(int)slot]+(enhancement>0?" +"+enhancement:"");
            int tier=(int)quality+1; double scale=1+enhancement*.12;
            Attack=slot==GearSlot.Weapon?(int)Math.Round(6*tier*scale):0; SkillDamage=slot==GearSlot.Weapon?(int)Math.Round(3*tier*scale):0;
            Health=slot==GearSlot.Armor?(int)Math.Round(20*tier*scale):0; Protection=slot==GearSlot.Armor?(int)Math.Round(2*tier*scale):0;
            ElementBoost=slot==GearSlot.Accessory?.06f*tier+enhancement*.01f:0;
        }
        public int EnhanceCost { get { return Enhancement>=MaxEnhancement?0:(10+Enhancement*5)*((int)Quality+1); } }
        public GearDefinition Enhanced(int level) {
            if(level<0 || level>MaxEnhancement) throw new ArgumentOutOfRangeException("level");
            if(level==Enhancement) return this;
            if(enhanced==null) enhanced=new GearDefinition[MaxEnhancement+1];
            return enhanced[level]??(enhanced[level]=new GearDefinition(Slot,Quality,Element,level));
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
