using UnityEngine;
using UnityEngine.UI;
namespace Ashlight {
    // Dynamic combat labels can reset their own sizes. Observe those changes before scaling.
    public sealed class AccessibleTextScale : MonoBehaviour {
        Text label;
        int baseFont,baseMin,baseMax,lastFont,lastMin,lastMax;
        void Awake() { label=GetComponent<Text>(); baseFont=lastFont=label.fontSize; baseMin=lastMin=label.resizeTextMinSize; baseMax=lastMax=label.resizeTextMaxSize; }
        public void Refresh() {
            if(label==null) return;
            if(label.fontSize!=lastFont) baseFont=label.fontSize;
            if(label.resizeTextMinSize!=lastMin) baseMin=label.resizeTextMinSize;
            if(label.resizeTextMaxSize!=lastMax) baseMax=label.resizeTextMaxSize;
            float scale=AccessibilityOptions.LargeText?1.2f:1;
            lastFont=Mathf.RoundToInt(baseFont*scale); lastMin=Mathf.RoundToInt(baseMin*scale); lastMax=Mathf.RoundToInt(baseMax*scale);
            if(label.fontSize!=lastFont) label.fontSize=lastFont;
            if(label.resizeTextMinSize!=lastMin) label.resizeTextMinSize=lastMin;
            if(label.resizeTextMaxSize!=lastMax) label.resizeTextMaxSize=lastMax;
        }
        void LateUpdate() { Refresh(); }
    }
}
