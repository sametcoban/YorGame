using System;
using System.Collections.Generic;
namespace Ashlight {
    public enum GearSlot { Weapon, Armor, Accessory }
    public static class GearSets {
        public static string Name(Element element) { return new[]{"Bastion","Ember","Rime","Venom","Tempest","Radiance"}[(int)element]; }
        public static string Bonus(Element element,int pieces) {
            if(pieces<2) return "2 matching pieces unlock a bonus";
            switch(element) {
                case Element.Physical:return "+5% max HP"+(pieces==3?" · +2 armor":"");
                case Element.Fire:return "+"+(pieces==3?15:5)+"% Fire damage";
                case Element.Cold:return "+5% max HP"+(pieces==3?" · +10% Cold damage":"");
                case Element.Poison:return "+5% Poison damage"+(pieces==3?" · Rogue critical chance +5 percentage points":"");
                case Element.Lightning:return "+5% Lightning damage"+(pieces==3?" · +10 energy per action":"");
                default:return "+5% max HP"+(pieces==3?" · +10% skill healing":"");
            }
        }
    }
    public sealed class GearDefinition {
        public readonly string Id, Name;
        public readonly int RelicChapter;
        public readonly GearSlot Slot;
        public readonly HeroQuality Quality;
        public readonly Element Element;
        public readonly int Health, Attack, SkillDamage, Protection, Enhancement;
        public const int MaxEnhancement=10;
        GearDefinition[] enhanced;
        public readonly float ElementBoost;
        GearDefinition(GearSlot slot,HeroQuality quality,Element element,int enhancement=0,int relicChapter=-1) {
            Slot=slot; Quality=quality; Element=element; Enhancement=enhancement;
            RelicChapter=relicChapter; Id=relicChapter<0?slot+"_"+quality+"_"+element:"BossRelic_"+relicChapter;
            Name=element+" "+new[]{"Runeblade","Cuirass","Sigil"}[(int)slot]+(enhancement>0?" +"+enhancement:"");
            if(relicChapter>=0) Name=RelicNames[relicChapter]+(enhancement>0?" +"+enhancement:"");
            int tier=(int)quality+1; double scale=(1+enhancement*.12)*(relicChapter>=0?1.1:1);
            Attack=slot==GearSlot.Weapon?(int)Math.Round(6*tier*scale):0; SkillDamage=slot==GearSlot.Weapon?(int)Math.Round(3*tier*scale):0;
            Health=slot==GearSlot.Armor?(int)Math.Round(20*tier*scale):0; Protection=slot==GearSlot.Armor?(int)Math.Round(2*tier*scale):0;
            ElementBoost=slot==GearSlot.Accessory?(.06f*tier+enhancement*.01f)*(relicChapter>=0?1.1f:1):0;
        }
        public int EnhanceCost { get { return Enhancement>=MaxEnhancement?0:(10+Enhancement*5)*((int)Quality+1); } }
        public GearDefinition Enhanced(int level) {
            if(level<0 || level>MaxEnhancement) throw new ArgumentOutOfRangeException("level");
            if(level==Enhancement) return this;
            if(enhanced==null) enhanced=new GearDefinition[MaxEnhancement+1];
            return enhanced[level]??(enhanced[level]=new GearDefinition(Slot,Quality,Element,level,RelicChapter));
        }
        public string Description { get { return Quality+" "+Name+" · "+(Slot==GearSlot.Weapon?"Attack +"+Attack+", skill/ultimate damage +"+SkillDamage:Slot==GearSlot.Armor?"HP +"+Health+", protection "+Protection:Element+" damage +"+Math.Round(ElementBoost*100)+"%")+" · "+GearSets.Name(Element)+" set"; } }
        static readonly string[] RelicNames={"Roadkeeper's Lantern","Matriarch's Mantle","Sovereign's Brand","Regent's Stormseal","Thorn Crown","Monarch's Veil","March Standard","Drowned King's Mail","Crimson Bell","Obsidian Heart","Pale Crown","Abyssal Signet","Hollow Chime","Saltbound Mantle","Widow's Fang","Fallen Lens","Sunless Hammer","Dawn Ember"};
        static readonly IReadOnlyList<GearDefinition> catalog=Build();
        public static GearDefinition BossRelic(int chapter) { if(chapter<0 || chapter>=ChapterDefinition.Catalog.Count) throw new ArgumentOutOfRangeException("chapter"); return Find("BossRelic_"+chapter); }
        public static IReadOnlyList<GearDefinition> Catalog { get { return catalog; } }
        static IReadOnlyList<GearDefinition> Build() {
            var items=new List<GearDefinition>();
            foreach(GearSlot slot in Enum.GetValues(typeof(GearSlot))) foreach(HeroQuality quality in Enum.GetValues(typeof(HeroQuality))) foreach(Element element in Enum.GetValues(typeof(Element))) items.Add(new GearDefinition(slot,quality,element));
            for(int chapter=0;chapter<ChapterDefinition.Catalog.Count;chapter++) items.Add(new GearDefinition((GearSlot)(chapter%3),chapter<12?HeroQuality.Rare:HeroQuality.Epic,chapter==0?Element.Fire:EnemySkills.For(chapter*5+4,true,1).Element,0,chapter));
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
