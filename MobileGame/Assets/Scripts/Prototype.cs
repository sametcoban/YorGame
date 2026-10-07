using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
namespace Ashlight {
    public sealed class Prototype : MonoBehaviour {
        Battle battle = new Battle();
        Text status, cue;
        Image heroHealth, enemyHealth, timing;
        RectTransform safe;
        readonly Vector3 heroHome = new Vector3(-2, 1, 0);
        readonly Vector3 enemyHome = new Vector3(2, 1.3f, 0);
        float attackTime = -10, feedbackUntil;
        bool parried;
        readonly Color enemyColor = new Color(.8f, .45f, .2f);
        Button attack, dodge, parry;
        Transform hero, enemy;
        Renderer enemyRenderer;
        float strikeTime;
        string message = "Your turn. Attack the Lantern Warden.";
        Coroutine enemyTurn;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void StartGame() {
            if (FindFirstObjectByType<Prototype>() == null) new GameObject("Ashlight Prototype").AddComponent<Prototype>();
        }
        void Start() {
            Application.targetFrameRate = 60;
            var cameraObject = new GameObject("Battle Camera");
            var camera = cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();
            camera.transform.position = new Vector3(0, 5, -9);
            camera.transform.LookAt(new Vector3(0, 1, 0));
            camera.backgroundColor = new Color(.07f, .08f, .14f);
            var light = new GameObject("Moonlight").AddComponent<Light>();
            light.type = LightType.Directional; light.intensity = 1.3f;
            light.transform.rotation = Quaternion.Euler(40, -30, 0);
            MakeShape("Arena", PrimitiveType.Cube, new Vector3(0, -.2f, 0), new Vector3(12, .4f, 8), new Color(.2f, .22f, .3f));
            hero = MakeShape("Hero", PrimitiveType.Capsule, new Vector3(-2, 1, 0), Vector3.one, new Color(.2f, .7f, .85f)).transform;
            enemy = MakeShape("Lantern Warden", PrimitiveType.Capsule, new Vector3(2, 1.3f, 0), new Vector3(1.3f, 1.3f, 1.3f), new Color(.8f, .45f, .2f)).transform;
            enemyRenderer = enemy.GetComponent<Renderer>();
            var canvas = new GameObject("Touch HUD", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvas.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720); scaler.matchWidthOrHeight = .5f;
            safe = new GameObject("Safe Area", typeof(RectTransform)).GetComponent<RectTransform>();
            safe.SetParent(canvas.transform, false);
            var safeArea = Screen.safeArea;
            safe.anchorMin = new Vector2(safeArea.xMin / Screen.width, safeArea.yMin / Screen.height);
            safe.anchorMax = new Vector2(safeArea.xMax / Screen.width, safeArea.yMax / Screen.height);
            safe.offsetMin = safe.offsetMax = Vector2.zero;
            status = Label(safe, "", new Vector2(.05f, .68f), new Vector2(.95f, .98f), 30);
            heroHealth = Bar(safe, new Vector2(.05f, .64f), new Vector2(.43f, .67f), new Color(.2f, .7f, .85f));
            enemyHealth = Bar(safe, new Vector2(.57f, .64f), new Vector2(.95f, .67f), enemyColor);
            timing = Bar(safe, new Vector2(.3f, .31f), new Vector2(.7f, .35f), Color.yellow);
            cue = Label(safe, "", new Vector2(.2f, .36f), new Vector2(.75f, .49f), 32);
            attack = MakeButton(safe, "ATTACK", .05f, .32f, () => {
                if (!battle.Attack()) return;
                attackTime = Time.time;
                message = "The Warden prepares a strike. Wait for FLASH!";
                if (battle.Current != Phase.Won) enemyTurn = StartCoroutine(EnemyTurn());
            });
            dodge = MakeButton(safe, "DODGE", .35f, .62f, () => Defend(false), true);
            parry = MakeButton(safe, "PARRY", .65f, .92f, () => Defend(true), true);
            var reset = MakeButton(safe, "RESTART", .78f, .95f, Restart);
            var rect = reset.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(.78f, .52f); rect.anchorMax = new Vector2(.95f, .62f);
            if (FindFirstObjectByType<EventSystem>() == null) new GameObject("Input", typeof(EventSystem), typeof(StandaloneInputModule));
        }
        static GameObject MakeShape(string name, PrimitiveType shape, Vector3 position, Vector3 scale, Color color) {
            var obj = GameObject.CreatePrimitive(shape); obj.name = name;
            obj.transform.position = position; obj.transform.localScale = scale;
            obj.GetComponent<Renderer>().material.color = color; return obj;
        }
        static Text Label(Transform parent, string text, Vector2 min, Vector2 max, int size) {
            var obj = new GameObject("Label", typeof(RectTransform), typeof(Text));
            obj.transform.SetParent(parent, false);
            var rect = obj.GetComponent<RectTransform>(); rect.anchorMin = min; rect.anchorMax = max; rect.offsetMin = rect.offsetMax = Vector2.zero;
            var label = obj.GetComponent<Text>(); label.text = text; label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = size; label.alignment = TextAnchor.MiddleCenter; label.color = Color.white; label.raycastTarget = false;
            return label;
        }
        static Button MakeButton(Transform parent, string text, float min, float max, UnityEngine.Events.UnityAction action, bool onPress = false) {
            var obj = new GameObject(text, typeof(RectTransform), typeof(Image), typeof(Button)); obj.transform.SetParent(parent, false);
            var rect = obj.GetComponent<RectTransform>(); rect.anchorMin = new Vector2(min, .05f); rect.anchorMax = new Vector2(max, .22f); rect.offsetMin = rect.offsetMax = Vector2.zero;
            obj.GetComponent<Image>().color = new Color(.12f, .16f, .25f, .95f);
            Label(obj.transform, text, Vector2.zero, Vector2.one, 28);
            var button = obj.GetComponent<Button>();
            if (onPress) obj.AddComponent<PressAction>().Action = action;
            else button.onClick.AddListener(action);
            return button;
        }
        static Image Bar(Transform parent, Vector2 min, Vector2 max, Color color) {
            var track = new GameObject("Meter", typeof(RectTransform), typeof(Image));
            track.transform.SetParent(parent, false);
            var rect = track.GetComponent<RectTransform>(); rect.anchorMin = min; rect.anchorMax = max;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            track.GetComponent<Image>().color = new Color(.08f, .09f, .14f);
            track.GetComponent<Image>().raycastTarget = false;
            var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fill.transform.SetParent(track.transform, false);
            var fillRect = fill.GetComponent<RectTransform>(); fillRect.anchorMin = Vector2.zero; fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = fillRect.offsetMax = Vector2.zero;
            var image = fill.GetComponent<Image>(); image.color = color; image.raycastTarget = false;
            return image;
        }
        static void SetMeter(Image image, float amount) {
            image.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(amount), 1);
        }
        IEnumerator EnemyTurn() {
            yield return new WaitForSeconds(1.2f);
            if (!battle.BeginStrike()) yield break;
            strikeTime = Time.time; message = "FLASH! Dodge now, or parry immediately!";
            enemyRenderer.material.color = Color.yellow;
            yield return new WaitForSeconds(.45f);
            if (!battle.Defended) { feedbackUntil = Time.time + .7f; parried = false; }
            battle.FinishStrike(); enemyRenderer.material.color = new Color(.8f, .45f, .2f);
            if (battle.Current == Phase.Player) message = battle.Defended ? "Defense succeeded. Your turn." : "Hit! Your turn.";
            enemyTurn = null;
        }
        void Defend(bool isParry) {
            if (battle.Defend(isParry, Time.time - strikeTime)) {
                parried = isParry; feedbackUntil = Time.time + .7f;
                message = isParry ? "Parried! Counter damage dealt." : "Dodged!";
            }
            else message = "Mistimed defense. React after FLASH.";
        }
        void Restart() {
            if (enemyTurn != null) StopCoroutine(enemyTurn);
            enemyTurn = null; battle.Reset();
            attackTime = -10; feedbackUntil = 0; parried = false;
            enemyRenderer.material.color = new Color(.8f, .45f, .2f);
            message = "Your turn. Attack the Lantern Warden.";
        }
        void Update() {
            if (status == null) return;
            status.text = "ASHLIGHT — Combat Prototype\nHero " + battle.HeroHealth + " / 100     Warden " + battle.EnemyHealth + " / 100\n" +
                (battle.Current == Phase.Won ? "Victory! Restart to play again." : battle.Current == Phase.Lost ? "Defeated. Restart to try again." : message);
            attack.interactable = battle.Current == Phase.Player;
            bool defending = battle.Current == Phase.EnemyWindup || battle.Current == Phase.EnemyStrike;
            dodge.interactable = parry.interactable = defending && !battle.Defended;
            var area = Screen.safeArea;
            safe.anchorMin = new Vector2(area.xMin / Mathf.Max(1, Screen.width), area.yMin / Mathf.Max(1, Screen.height));
            safe.anchorMax = new Vector2(area.xMax / Mathf.Max(1, Screen.width), area.yMax / Mathf.Max(1, Screen.height));
            SetMeter(heroHealth, battle.HeroHealth / 100f);
            SetMeter(enemyHealth, battle.EnemyHealth / 100f);
            float elapsed = Time.time - strikeTime;
            bool striking = battle.Current == Phase.EnemyStrike;
            timing.transform.parent.gameObject.SetActive(striking && !battle.Defended);
            SetMeter(timing, 1 - elapsed / .4f);
            timing.color = elapsed <= .18f ? Color.yellow : new Color(.2f, .7f, .85f);
            cue.text = battle.Current == Phase.EnemyWindup ? "GET READY" :
                striking && !battle.Defended ? (elapsed <= .18f ? "PARRY OR DODGE!" : elapsed <= .4f ? "DODGE!" : "TOO LATE") :
                Time.time < feedbackUntil ? (battle.Defended ? (parried ? "PARRY + COUNTER" : "DODGED") : "HIT −35") : "";
            cue.color = Time.time < feedbackUntil && battle.Defended ? Color.cyan : Color.yellow;
            float attackProgress = Mathf.Clamp01((Time.time - attackTime) / .35f);
            hero.position = heroHome + Vector3.right * Mathf.Sin(attackProgress * Mathf.PI) * 1.4f;
            if (striking && battle.Defended && !parried) hero.position += Vector3.back * .8f;
            hero.localScale = striking && battle.Defended && !parried ? new Vector3(1, .6f, 1) : Vector3.one;
            float lunge = striking ? Mathf.Sin(Mathf.Clamp01(elapsed / .45f) * Mathf.PI) : 0;
            enemy.position = enemyHome + Vector3.left * lunge * 1.4f;
            enemy.localRotation = battle.Current == Phase.EnemyWindup ? Quaternion.Euler(0, 0, -12) : Quaternion.identity;
            if (Time.time < feedbackUntil && parried) enemy.position += Vector3.right * .3f;
        }
    }
}
