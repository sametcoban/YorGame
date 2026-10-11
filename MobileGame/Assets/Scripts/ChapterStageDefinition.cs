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
            new ChapterStageDefinition("Abyssal Crown","#08071A","#24203D","#655E96","#C7BEFF","#895BFF",.038f)
        };
        public static ChapterStageDefinition For(int chapter) {
            if(chapter<0 || chapter>=Catalog.Length) throw new ArgumentOutOfRangeException("chapter");
            return Catalog[chapter];
        }
    }
}
