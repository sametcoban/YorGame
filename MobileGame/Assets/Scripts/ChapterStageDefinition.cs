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
            new ChapterStageDefinition("Eclipse Throne","#100D1B","#272334","#665370","#C0AEDB","#DBBFFF",.025f)
        };
        public static ChapterStageDefinition For(int chapter) {
            if(chapter<0 || chapter>=Catalog.Length) throw new ArgumentOutOfRangeException("chapter");
            return Catalog[chapter];
        }
    }
}
