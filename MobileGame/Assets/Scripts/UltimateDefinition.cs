using System;
namespace Ashlight {
    public sealed class UltimateDefinition {
        public readonly string Name;
        public readonly int Damage, Healing, Guard;
        public readonly bool AreaDamage;
        public readonly Element Affinity;
        UltimateDefinition(string name,int damage,bool area,Element affinity,int healing,int guard) {
            Name=name; Damage=damage; AreaDamage=area; Affinity=affinity; Healing=healing; Guard=guard;
        }
        public string Description { get { return Name+" · "+Damage+" "+Affinity+" damage "+(AreaDamage?"to all enemies":"to one enemy")+(Healing>0?" · Party heal "+Healing:"")+(Guard>0?" · Party guard "+Guard:""); } }
        static readonly string[,] Names = {
            {"Last Ember","Iron Reckoning","Cinderfall","Siegebreaker","Crown of Ash"},
            {"Dawn Verdict","Sunlit Requiem","Oathbreaker’s Bane","Sanctified Storm","Solar Judgment"},
            {"Winter’s Grasp","Thunderheart","Astral Inferno","Nightfire Nova","Starfall Cataclysm"},
            {"Heartseeker","Briar Volley","Falcon’s Execution","Moonrain","Horizon’s End"},
            {"Widow’s Kiss","Viper Dance","Silent Execution","Murder of Crows","Black Lotus"},
            {"Bell of Mercy","Martyr’s Rebuke","Seraph’s Embrace","Hallowed Ruin","Final Benediction"}
        };
        static readonly bool[,] Area = {
            {false,false,true,false,true}, {false,true,false,true,false},
            {false,false,true,true,true}, {false,true,false,true,false},
            {false,true,false,true,false}, {true,false,true,false,true}
        };
        public static UltimateDefinition For(HeroDefinition hero) {
            int kind=(int)hero.Class,tier=(int)hero.Quality;
            bool area=Area[kind,tier]; float scale=HeroDefinition.Multiplier(hero.Quality);
            // Every single-target base exceeds every area base at the same rarity.
            int damage=(int)Math.Round((area?60+kind*2+tier:100+kind*3+tier)*scale);
            int healing=hero.Class==HeroClass.Cleric?(int)Math.Round((22+tier*3)*scale):0;
            int guard=hero.Class==HeroClass.Paladin?(int)Math.Round((12+tier*2)*scale):0;
            return new UltimateDefinition(Names[kind,tier],damage,area,hero.Affinity,healing,guard);
        }
    }
}
