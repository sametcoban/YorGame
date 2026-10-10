using System;
using System.Collections.Generic;
namespace Ashlight {
    // Presentation only: encounter stats, rewards, and defense windows remain in Battle.
    public sealed class EnemyVisualDefinition {
        public readonly string EnemyName, ModelName, AttackLabel, SourceModel, ClothRole;
        public readonly float Height;
        public readonly Element EffectElement;
        public readonly bool IsBoss, IsRanged;
        public string ResourcePath { get { return "Enemies/" + ModelName; } }
        EnemyVisualDefinition(string enemyName, string modelName, string attackLabel, float height, Element effectElement, bool boss = false, bool ranged = false, string sourceModel = null, string clothRole = null) {
            EnemyName = enemyName; ModelName = modelName; AttackLabel = attackLabel; Height = height;
            EffectElement = effectElement; IsBoss = boss; IsRanged = ranged; SourceModel = sourceModel ?? modelName; ClothRole = clothRole;
        }
        public static readonly IReadOnlyList<EnemyVisualDefinition> ChapterOne = Array.AsReadOnly(new[] {
            new EnemyVisualDefinition("Lantern Warden", "LanternWarden", "HALBERD SWEEP", 2.35f, Element.Physical),
            new EnemyVisualDefinition("Ash Hound", "AshHound", "RAVENOUS BITE", 1.45f, Element.Physical),
            new EnemyVisualDefinition("Cinder Scout", "CinderScout", "FROST HEX", 2.15f, Element.Cold, ranged:true),
            new EnemyVisualDefinition("Iron Watcher", "IronWatcher", "IRON CLEAVE", 2.4f, Element.Physical),
            new EnemyVisualDefinition("Roadkeeper", "Roadkeeper", "LANTERN SLAM", 2.8f, Element.Fire, boss:true)
        });
        public static readonly IReadOnlyList<EnemyVisualDefinition> PairedBosses = Array.AsReadOnly(new[] {
            new EnemyVisualDefinition("Cinder Sovereign", "CinderSovereign", "CINDER SLAM", 2.65f, Element.Fire, true, sourceModel:"Roadkeeper", clothRole:"WineWool"),
            new EnemyVisualDefinition("Ashen Marshal", "AshenMarshal", "ASHEN CLEAVE", 2.35f, Element.Fire, true, sourceModel:"IronWatcher", clothRole:"SootWool"),
            new EnemyVisualDefinition("Storm Regent", "StormRegent", "THUNDER SLAM", 2.65f, Element.Lightning, true, sourceModel:"Roadkeeper", clothRole:"VioletWool"),
            new EnemyVisualDefinition("Stormbound Executioner", "StormboundExecutioner", "STORM HEX", 2.3f, Element.Lightning, true, true, "CinderScout", "VioletWool"),
            new EnemyVisualDefinition("Thorn Queen", "ThornQueen", "THORN HEX", 2.5f, Element.Poison, true, true, "HollowMatron", "MossWool"),
            new EnemyVisualDefinition("Rotbound Consort", "RotboundConsort", "BLIGHT SLAM", 2.65f, Element.Poison, true, sourceModel:"Roadkeeper", clothRole:"MossWool"),
            new EnemyVisualDefinition("Veiled Monarch", "VeiledMonarch", "ECLIPSE HEX", 2.5f, Element.Light, true, true, "CinderScout", "SootWool"),
            new EnemyVisualDefinition("Eclipse Harbinger", "EclipseHarbinger", "ECLIPSE SLAM", 2.65f, Element.Light, true, sourceModel:"Roadkeeper", clothRole:"SootWool")
        });
        public static readonly IReadOnlyList<EnemyVisualDefinition> Catalog = All();
        static IReadOnlyList<EnemyVisualDefinition> All() {
            var all = new List<EnemyVisualDefinition>(ChapterOne); all.AddRange(PairedBosses); return all.AsReadOnly();
        }
        public static EnemyVisualDefinition For(string name) {
            foreach (var entry in Catalog) if (entry.EnemyName == name) return entry;
            return null;
        }
    }
}
