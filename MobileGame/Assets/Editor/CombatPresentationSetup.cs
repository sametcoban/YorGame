using UnityEditor;
using UnityEngine;
public sealed class CombatAudioImporter : AssetPostprocessor {
    void OnPreprocessAudio() {
        if (!assetPath.StartsWith("Assets/Resources/Audio/Combat/",System.StringComparison.Ordinal)) return;
        var importer = (AudioImporter)assetImporter;
        importer.forceToMono = true;
        var settings = importer.defaultSampleSettings;
        settings.loadType = AudioClipLoadType.DecompressOnLoad;
        settings.compressionFormat = AudioCompressionFormat.PCM;
        settings.sampleRateSetting = AudioSampleRateSetting.OverrideSampleRate;
        settings.sampleRateOverride = 22050;
        importer.defaultSampleSettings = settings;
    }
}
public static class CombatPresentationSetup {
    [MenuItem("Ashlight/Art/Refresh Combat Presentation")]
    public static void Build() { DarkFantasySetup.Ensure(true,true,true); }
}
