using UnityEngine;
namespace Ashlight {
    // Device preferences are separate from account saves and never change battle rules.
    public static class AccessibilityOptions {
        static bool largeText=PlayerPrefs.GetInt("Ashlight.LargeText",0)!=0;
        static bool reducedEffects=PlayerPrefs.GetInt("Ashlight.ReducedEffects",0)!=0;
        static bool reducedMotion=PlayerPrefs.GetInt("Ashlight.ReducedMotion",0)!=0;
        public static bool LargeText { get { return largeText; } set { largeText=value; Set("Ashlight.LargeText",value); } }
        public static bool ReducedEffects { get { return reducedEffects; } set { reducedEffects=value; Set("Ashlight.ReducedEffects",value); } }
        public static bool ReducedMotion { get { return reducedMotion; } set { reducedMotion=value; Set("Ashlight.ReducedMotion",value); } }
        static void Set(string key,bool value) { PlayerPrefs.SetInt(key,value?1:0); PlayerPrefs.Save(); }
    }
}
