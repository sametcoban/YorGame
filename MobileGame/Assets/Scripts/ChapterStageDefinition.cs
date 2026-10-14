using System;
namespace Ashlight {
    public sealed class ChapterStageDefinition {
        public readonly string Name, Sky, Stone, Accent, Moon, Flame;
        public readonly float Fog;
        ChapterStageDefinition(string name,string sky,string stone,string accent,string moon,string flame,float fog) {
            Name=name; Sky=sky; Stone=stone; Accent=accent; Moon=moon; Flame=flame; Fog=fog;
        }
        public static readonly ChapterStageDefinition[] Catalog = {
            new ChapterStageDefinition("Lantern Road","#070A10","#2B2E33","#505158","#B3C9F0","#FF8738",.024f),
            new ChapterStageDefinition("Frozen Pass","#182A3A","#607A8C","#CADCE5","#B8DEFF","#73CCFF",.032f),
            new ChapterStageDefinition("Ember Citadel","#200C0B","#302622","#604538","#EAA878","#FF4B15",.028f),
            new ChapterStageDefinition("Storm Spire","#121424","#34394E","#6B6C8F","#A9B8FF","#A787FF",.03f),
            new ChapterStageDefinition("Blighted Garden","#0D1914","#303E31","#64704E","#B5C7A0","#87CF43",.038f),
            new ChapterStageDefinition("Eclipse Throne","#100D1B","#272334","#665370","#C0AEDB","#DBBFFF",.025f),
            new ChapterStageDefinition("Ashen March","#24100C","#46312B","#916044","#E8B284","#FF602A",.033f),
            new ChapterStageDefinition("Drowned Crypt","#071D24","#283F45","#467E83","#A3D9DA","#40D7D0",.036f),
            new ChapterStageDefinition("Blood Cathedral","#260912","#442A35","#944857","#ECB9C1","#FF335F",.03f),
            new ChapterStageDefinition("Obsidian Wastes","#17120C","#262321","#8E774A","#D8C6A0","#FFC351",.035f),
            new ChapterStageDefinition("Pale Realm","#172535","#516474","#A4B4CF","#DDE5FF","#A2BAFF",.04f),
            new ChapterStageDefinition("Abyssal Crown","#08071A","#24203D","#655E96","#C7BEFF","#895BFF",.038f),
            new ChapterStageDefinition("Hollow Belfry","#121022","#373243","#8E7AB3","#D2C5FF","#A899FF",.034f),
            new ChapterStageDefinition("Saltbound Court","#0B222A","#3F5558","#91AAA5","#B7E9DD","#61DFD8",.034f),
            new ChapterStageDefinition("Widow Woods","#0D1B13","#35412F","#749656","#C5DEAC","#A2DC55",.034f),
            new ChapterStageDefinition("Shattered Observatory","#0C152B","#333C59","#7985BE","#D1DFFF","#8DAAFF",.034f),
            new ChapterStageDefinition("Sunless Forge","#250D08","#3C2B25","#9D6845","#F3C293","#FF7A32",.034f),
            new ChapterStageDefinition("Last Dawn","#140F23","#3C3549","#B69767","#FFEBC5","#FFD58B",.034f),
            new ChapterStageDefinition("Raven Bastion","#111322","#32323D","#71647E","#C8C4ED","#B390FF",.032f),
            new ChapterStageDefinition("Cobalt Mines","#102434","#334E62","#6CABBE","#BEE8FF","#67CFFF",.032f),
            new ChapterStageDefinition("Festered Harbor","#142018","#394436","#768954","#C4D4AE","#A4CA62",.032f),
            new ChapterStageDefinition("Ember Sepulchre","#2B100E","#46352C","#AF784A","#F5D0AC","#FF9644",.032f),
            new ChapterStageDefinition("Dreaming Spire","#1C1533","#49395A","#997ECA","#E1CBFF","#CF9FFF",.032f),
            new ChapterStageDefinition("Dawnfall Gate","#272319","#524635","#BA9A62","#FFEDCC","#FFE7A8",.032f)
        };
        public static ChapterStageDefinition For(int chapter) {
            if(chapter<0 || chapter>=Catalog.Length) throw new ArgumentOutOfRangeException("chapter");
            return Catalog[chapter];
        }
    }
}
