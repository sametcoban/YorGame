using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
namespace Ashlight {
    public sealed class Prototype : MonoBehaviour {
        Battle battle = new Battle();
        Text status;
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
            var safe = new GameObject("Safe Area", typeof(RectTransform)).GetComponent<RectTransform>();
            safe.SetParent(canvas.transform, false);
            var safeArea = Screen.safeArea;
            safe.anchorMin = new Vector2(safeArea.xMin / Screen.width, safeArea.yMin / Screen.height);
            safe.anchorMax = new Vector2(safeArea.xMax / Screen.width, safeArea.yMax / Screen.height);
            safe.offsetMin = safe.offsetMax = Vector2.zero;
            status = Label(safe, "", new Vector2(.05f, .68f), new Vector2(.95f, .98f), 30);
            attack = MakeButton(safe, "ATTACK", .05f, .32f, () => {
                if (!battle.Attack()) return;
                message = "The Warden prepares a strike. Wait for FLASH!";
                if (battle.Current != Phase.Won) enemyTurn = StartCoroutine(EnemyTurn());
            });
            dodge = MakeButton(safe, "DODGE", .35f, .62f, () => Defend(false));
            parry = MakeButton(safe, "PARRY", .65f, .92f, () => Defend(true));
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
        static Button MakeButton(Transform parent, string text, float min, float max, UnityEngine.Events.UnityAction action) {
            var obj = new GameObject(text, typeof(RectTransform), typeof(Image), typeof(Button)); obj.transform.SetParent(parent, false);
            var rect = obj.GetComponent<RectTransform>(); rect.anchorMin = new Vector2(min, .05f); rect.anchorMax = new Vector2(max, .22f); rect.offsetMin = rect.offsetMax = Vector2.zero;
            obj.GetComponent<Image>().color = new Color(.12f, .16f, .25f, .95f);
            Label(obj.transform, text, Vector2.zero, Vector2.one, 28);
            var button = obj.GetComponent<Button>(); button.onClick.AddListener(action); return button;
        }
        IEnumerator EnemyTurn() {
            yield return new WaitForSeconds(1.2f);
            if (!battle.BeginStrike()) yield break;
            strikeTime = Time.time; message = "FLASH! Dodge now, or parry immediately!";
            enemyRenderer.material.color = Color.yellow;
            yield return new WaitForSeconds(.45f);
            battle.FinishStrike(); enemyRenderer.material.color = new Color(.8f, .45f, .2f);
            if (battle.Current == Phase.Player) message = battle.Defended ? "Defense succeeded. Your turn." : "Hit! Your turn.";
            enemyTurn = null;
        }
        void Defend(bool isParry) {
            if (battle.Defend(isParry, Time.time - strikeTime)) message = isParry ? "Parried! Counter damage dealt." : "Dodged!";
            else message = "Mistimed defense. React after FLASH.";
        }
        void Restart() {
            if (enemyTurn != null) StopCoroutine(enemyTurn);
            enemyTurn = null; battle.Reset();
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
            hero.localScale = battle.Defended && battle.Current == Phase.EnemyStrike ? new Vector3(1, .6f, 1) : Vector3.one;
        }
    }
}
