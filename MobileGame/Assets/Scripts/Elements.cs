using System;
namespace Ashlight {
    public enum Element { Physical, Fire, Cold, Poison, Lightning, Light }
    public static class ClassElements {
        public static System.Collections.Generic.IReadOnlyList<Element> For(HeroClass kind) {
            Element[] elements;
            switch (kind) {
                case HeroClass.Knight: elements = new[] { Element.Fire, Element.Physical }; break;
                case HeroClass.Paladin: elements = new[] { Element.Light, Element.Fire }; break;
                case HeroClass.Sorceress: elements = new[] { Element.Cold, Element.Lightning, Element.Fire }; break;
                case HeroClass.Ranger: elements = new[] { Element.Physical, Element.Cold }; break;
                case HeroClass.Rogue: elements = new[] { Element.Physical, Element.Poison }; break;
                case HeroClass.Cleric: elements = new[] { Element.Light }; break;
                default: throw new ArgumentOutOfRangeException("kind");
            }
            return Array.AsReadOnly(elements);
        }
    }
    public enum EnemyRole { Defender, Bruiser, Archer, Caster, Healer, Support }
    public enum EnemyRow { Front, Back }
    public static class EnemyRoles {
        public static EnemyRole For(string name) {
            if(name=="Thorn Queen") return EnemyRole.Healer;
            if(name=="Veiled Monarch") return EnemyRole.Support;
            if(name=="Cinder Scout" || name=="Thunder Hawk") return EnemyRole.Archer;
            if(name=="Rotbound Consort" || name=="Eclipse Harbinger") return EnemyRole.Defender;
            var art=EnemyVisualDefinition.For(name);
            if(art==null) return EnemyRole.Bruiser;
            if(art.IsRanged) return EnemyRole.Caster;
            if(art.SourceModel=="LanternWarden" || art.SourceModel=="IronWatcher") return EnemyRole.Defender;
            return EnemyRole.Bruiser;
        }
        public static EnemyRow Row(EnemyRole role) {
            return role==EnemyRole.Defender || role==EnemyRole.Bruiser?EnemyRow.Front:EnemyRow.Back;
        }
    }
    public sealed class EnemyDefinition {
        public readonly string Name;
        public readonly int MaxHealth;
        public readonly bool IsBoss;
        readonly EnemyRole? role;
        public EnemyRole Role { get { return role ?? EnemyRoles.For(Name); } }
        public EnemyRow Row { get { return EnemyRoles.Row(Role); } }
        public readonly Element Weakness, Resistance;
        public EnemyDefinition(string name, int health, Element weakness = Element.Physical, Element resistance = Element.Physical, bool isBoss = false, EnemyRole? role = null) {
            this.role=role; Name = name; MaxHealth = health; Weakness = weakness; Resistance = resistance; IsBoss = isBoss;
        }
        public int Damage(int amount, Element element) {
            if (amount <= 0) return 0;
            return Math.Max(1, (int)Math.Round(amount * Multiplier(element), MidpointRounding.AwayFromZero));
        }
        public float Multiplier(Element element) {
            if (element == Element.Physical) return 1f;
            if (element == Weakness) return 1.5f;
            if (element == Resistance) return .5f;
            return 1f;
        }
        public static EnemyDefinition Encounter(int number) {
            int health = Math.Min(400, 100 + number * 25);
            switch (number % 5) {
                case 0: return new EnemyDefinition("Lantern Warden", health, Element.Fire, Element.Cold);
                case 1: return new EnemyDefinition("Ember Sentinel", health, Element.Cold, Element.Fire);
                case 2: return new EnemyDefinition("Storm Revenant", health, Element.Poison, Element.Lightning);
                case 3: return new EnemyDefinition("Blight Guardian", health, Element.Lightning, Element.Poison);
                default: return new EnemyDefinition("Dusk Shade", health, Element.Light, Element.Fire);
            }
        }
    }
}
