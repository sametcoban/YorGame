using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
namespace Ashlight {
    public sealed class Prototype : MonoBehaviour {
        Battle battle = new Battle(true);
        Text status, cue;
        Image heroHealth, enemyHealth, timing;
        RectTransform safe;
        readonly Vector3[] homes = { new Vector3(-2, 1, -1.8f), new Vector3(-2, 1, 0), new Vector3(-2, 1, 1.8f) };
        readonly Transform[] allies = new Transform[Battle.MaxPartySize];
        Transform attacker;
        Button reset;
        readonly Vector3 enemyHome = new Vector3(2, 1.3f, 0);
        float attackTime = -10, feedbackUntil;
        bool parried;
        readonly Color enemyColor = new Color(.8f, .45f, .2f);
        Button attack, ability, abilityTwo, dodge, parry, loadout;
        GameObject selectionRoot;
        Text selectionTitle;
        Button[] choices, slots;
        bool selectingSkills;
        int selectedSlot, lastHitDamage;
        HeroClass browsingClass;
        readonly System.Collections.Generic.List<HeroDefinition> candidates = new System.Collections.Generic.List<HeroDefinition>();
        Button[] classButtons;
        Transform equipment;
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
            enemy = MakeShape("Enemy", PrimitiveType.Capsule, new Vector3(2, 1.3f, 0), new Vector3(1.3f, 1.3f, 1.3f), new Color(.8f, .45f, .2f)).transform;
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
            timing = Bar(safe, new Vector2(.3f, .255f), new Vector2(.7f, .28f), Color.yellow);
            cue = Label(safe, "", new Vector2(.2f, .29f), new Vector2(.8f, .39f), 32);
            attack = MakeButton(safe, "ATTACK", .02f, .19f, () => PerformAction(-1));
            ability = MakeButton(safe, "", .21f, .38f, () => PerformAction(0));
            abilityTwo = MakeButton(safe, "", .4f, .57f, () => PerformAction(1));
            classButtons = new Button[System.Enum.GetValues(typeof(HeroClass)).Length];
            for (int i = 0; i < classButtons.Length; i++) {
                var kind = (HeroClass)i;
                float left = .05f + (i % 3) * .23f;
                classButtons[i] = MakeButton(safe, kind.ToString().ToUpperInvariant(), left, left + .21f, () => SelectClass(kind));
                var classRect = classButtons[i].GetComponent<RectTransform>();
                classRect.anchorMin = new Vector2(left, i < 3 ? .52f : .405f); classRect.anchorMax = new Vector2(left + .21f, i < 3 ? .62f : .505f);
            }
            RefreshAppearance();
            dodge = MakeButton(safe, "DODGE", .59f, .76f, () => Defend(false), true);
            parry = MakeButton(safe, "PARRY", .78f, .95f, () => Defend(true), true);
            reset = MakeButton(safe, "RESTART", .78f, .95f, Restart);
            var rect = reset.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(.78f, .52f); rect.anchorMax = new Vector2(.95f, .62f);
            loadout = MakeButton(safe, "SKILLS", .78f, .95f, () => OpenSelection(true, battle.Hero.Kind));
            var loadoutRect = loadout.GetComponent<RectTransform>();
            loadoutRect.anchorMin = new Vector2(.78f, .405f); loadoutRect.anchorMax = new Vector2(.95f, .505f);
            CreateSelectionPanel();
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
            label.resizeTextForBestFit = true; label.resizeTextMinSize = 14; label.resizeTextMaxSize = size;
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
            strikeTime = Time.time; message = "FLASH! " + battle.Party[battle.ActiveIndex].Identity.Name + " must dodge or parry!";
            enemyRenderer.material.color = Color.yellow;
            yield return new WaitForSeconds(.45f);
            if (!battle.Defended) { feedbackUntil = Time.time + .7f; parried = false; }
            int target = battle.ActiveIndex;
            int healthBefore = battle.Party[target].Health;
            battle.FinishStrike();
            lastHitDamage = healthBefore - battle.Party[target].Health;
            enemyRenderer.material.color = new Color(.8f, .45f, .2f);
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
        void PerformAction(int skillSlot) {
            if (selectionRoot.activeSelf) return;
            var actingHero = allies[battle.ActiveIndex];
            string actionName = skillSlot >= 0 ? battle.Party[battle.ActiveIndex].Skill(skillSlot).Name : "Attack";
            bool accepted = skillSlot >= 0 ? battle.UseAbility(skillSlot) : battle.Attack();
            if (!accepted) return;
            attacker = actingHero; attackTime = Time.time;
            message = actionName + " — " + battle.LastDamage + " " + battle.LastElement + " damage" + (battle.LastElementMultiplier > 1 ? " (WEAKNESS)" : battle.LastElementMultiplier < 1 ? " (RESISTED)" : "") + ". " + (battle.Current == Phase.EnemyWindup ? "Enemy targets " + battle.Party[battle.ActiveIndex].Identity.Name + "." : "Choose the next hero's action.");
            if (battle.Current == Phase.EnemyWindup) enemyTurn = StartCoroutine(EnemyTurn());
        }
        void SelectClass(HeroClass kind) { OpenSelection(false, kind); }
        void CreateSelectionPanel() {
            selectionRoot = new GameObject("Hero and Skill Selection", typeof(RectTransform), typeof(Image));
            selectionRoot.transform.SetParent(safe, false);
            var rect = selectionRoot.GetComponent<RectTransform>(); rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
            selectionRoot.GetComponent<Image>().color = new Color(.06f, .08f, .13f, .98f);
            selectionTitle = Label(selectionRoot.transform, "", new Vector2(.03f, .81f), new Vector2(.97f, .98f), 26);
            slots = new Button[2];
            for (int i = 0; i < 2; i++) {
                int index = i;
                slots[i] = MakeButton(selectionRoot.transform, "", .08f + i * .46f, .46f + i * .46f, () => { selectedSlot = index; RefreshSelection(); });
                var r = slots[i].GetComponent<RectTransform>(); r.anchorMin = new Vector2(.08f + i * .46f, .69f); r.anchorMax = new Vector2(.46f + i * .46f, .79f);
            }
            choices = new Button[6];
            for (int i = 0; i < choices.Length; i++) {
                int index = i; float left = i % 2 == 0 ? .05f : .52f; float bottom = .5f - (i / 2) * .18f;
                choices[i] = MakeButton(selectionRoot.transform, "", left, left + .43f, () => Choose(index));
                var r = choices[i].GetComponent<RectTransform>(); r.anchorMin = new Vector2(left, bottom); r.anchorMax = new Vector2(left + .43f, bottom + .16f);
                choices[i].GetComponentInChildren<Text>().fontSize = 21;
            }
            var close = MakeButton(selectionRoot.transform, "DONE", .35f, .65f, () => selectionRoot.SetActive(false));
            var cr = close.GetComponent<RectTransform>(); cr.anchorMin = new Vector2(.35f, .02f); cr.anchorMax = new Vector2(.65f, .12f);
            selectionRoot.SetActive(false);
        }
        void OpenSelection(bool skills, HeroClass kind) {
            if (battle.Current != Phase.Player) return;
            selectingSkills = skills; browsingClass = kind; selectedSlot = 0;
            candidates.Clear();
            foreach (var candidate in HeroDefinition.Catalog) if (candidate.Class == kind) candidates.Add(candidate);
            selectionRoot.SetActive(true); RefreshSelection();
        }
        bool Recruited(string id) {
            foreach (var known in battle.Recruits) if (known.Id == id) return true;
            return false;
        }
        void Choose(int index) {
            if (selectingSkills) { battle.EquipSkill(selectedSlot, index); RefreshSelection(); return; }
            if (index >= candidates.Count) return;
            string id = candidates[index].Id;
            for (int i = 0; i < battle.Party.Count; i++) if (battle.Party[i].Identity.Id == id) {
                if (battle.SelectHero(i)) { selectionRoot.SetActive(false); message = candidates[index].Name + " selected."; }
                return;
            }
            if (battle.EquipHero(id)) {
                RefreshAppearance(); selectionRoot.SetActive(false);
                message = candidates[index].Name + " joins the active party.";
            }
        }
        void RefreshSelection() {
            var member = battle.Party[battle.ActiveIndex];
            selectionTitle.text = selectingSkills ? member.Identity.Name + " / " + member.Identity.Quality + " / " + member.Identity.Affinity + " — equip 2 of 6 skills\nSelect a slot, then a skill. Choices lock during battle." : browsingClass + " heroes\nSelect an ally, or replace the selected party slot before battle.";
            for (int i = 0; i < 2; i++) {
                slots[i].gameObject.SetActive(selectingSkills);
                slots[i].GetComponentInChildren<Text>().text = "SLOT " + (i + 1) + ": " + member.Skill(i).Name;
                slots[i].GetComponent<Image>().color = i == selectedSlot ? new Color(.25f,.4f,.55f) : new Color(.12f,.16f,.25f);
            }
            for (int i = 0; i < choices.Length; i++) {
                choices[i].gameObject.SetActive(selectingSkills || i < candidates.Count);
                if (selectingSkills) {
                    var skill = member.Definition.Skills[i];
                    bool equipped = member.SkillIndex(0) == i || member.SkillIndex(1) == i;
                    choices[i].GetComponentInChildren<Text>().text = (equipped ? "EQUIPPED: " : "") + skill.Name + " / " + (skill.Affinity ?? member.Identity.Affinity) + "\n" + skill.Description;
                    choices[i].interactable = battle.CanChangeParty && member.SkillIndex(1 - selectedSlot) != i;
                } else if (i < candidates.Count) {
                    var candidate = candidates[i]; int inParty = -1;
                    for (int j = 0; j < battle.Party.Count; j++) if (battle.Party[j].Identity.Id == candidate.Id) inParty = j;
                    choices[i].GetComponentInChildren<Text>().text = candidate.Name + " — " + candidate.Quality + " / " + candidate.Affinity + "\nHP " + candidate.Stats.MaxHealth + " | Attack " + candidate.Stats.AttackDamage + "\n" + (Recruited(candidate.Id) ? inParty >= 0 ? "IN PARTY" : "RESERVE" : "NOT RECRUITED");
                    choices[i].interactable = Recruited(candidate.Id) && (inParty >= 0 ? battle.Current == Phase.Player && battle.Party[inParty].Health > 0 && !battle.Party[inParty].Acted : battle.CanChangeParty);
                }
            }
        }
        void RefreshAppearance() {
            attacker = null; attackTime = -10;
            for (int i = 0; i < allies.Length; i++) {
                if (allies[i] != null) Destroy(allies[i].gameObject);
                allies[i] = null;
                if (i >= battle.Party.Count) continue;
                var kind = battle.Party[i].Definition.Kind;
                Color color = kind == HeroClass.Knight ? new Color(.3f, .5f, .75f) :
                    kind == HeroClass.Paladin ? new Color(.95f, .78f, .3f) :
                    kind == HeroClass.Sorceress ? new Color(.6f, .3f, .8f) :
                    kind == HeroClass.Ranger ? new Color(.25f, .6f, .3f) :
                    kind == HeroClass.Rogue ? new Color(.35f, .25f, .4f) : new Color(.85f, .85f, .8f);
                hero = MakeShape(battle.Party[i].Identity.Name, PrimitiveType.Capsule, homes[i], Vector3.one, color).transform;
                allies[i] = hero;
                equipment = new GameObject("Class Equipment").transform;
                equipment.SetParent(hero, false);
                if (kind == HeroClass.Sorceress || kind == HeroClass.Cleric) {
                    Accessory("Staff", PrimitiveType.Cylinder, new Vector3(.65f, 0, 0), new Vector3(.08f, 1.2f, .08f), new Color(.3f, .2f, .15f));
                    Accessory("Magic Orb", PrimitiveType.Sphere, new Vector3(.65f, 1.25f, 0), Vector3.one * .35f, kind == HeroClass.Cleric ? Color.yellow : Color.cyan);
                    Accessory("Mantle", PrimitiveType.Cube, new Vector3(0, .35f, .3f), new Vector3(1.1f, 1.2f, .15f), color * .65f);
                } else if (kind == HeroClass.Ranger) {
                    Accessory("Bow", PrimitiveType.Cube, new Vector3(.65f, .1f, 0), new Vector3(.12f, 1.5f, .2f), new Color(.5f, .3f, .15f));
                    Accessory("Bow String", PrimitiveType.Cube, new Vector3(.85f, .1f, 0), new Vector3(.03f, 1.5f, .03f), Color.white);
                    Accessory("Quiver", PrimitiveType.Cylinder, new Vector3(0, .2f, .45f), new Vector3(.3f, .55f, .3f), new Color(.3f, .2f, .1f));
                } else if (kind == HeroClass.Rogue) {
                    Accessory("Right Dagger", PrimitiveType.Cube, new Vector3(.65f, -.15f, 0), new Vector3(.1f, .7f, .15f), Color.gray);
                    Accessory("Left Dagger", PrimitiveType.Cube, new Vector3(-.65f, -.15f, 0), new Vector3(.1f, .7f, .15f), Color.gray);
                } else {
                    Accessory("Sword", PrimitiveType.Cube, new Vector3(.65f, .2f, 0), new Vector3(.12f, 1.4f, .18f), Color.gray);
                    Accessory("Sword Guard", PrimitiveType.Cube, new Vector3(.65f, -.25f, 0), new Vector3(.45f, .12f, .2f), color);
                    Accessory("Shield", PrimitiveType.Cube, new Vector3(-.65f, .05f, -.15f), new Vector3(.6f, .9f, .18f), color);
                }
            }
            hero = allies[battle.ActiveIndex];
        }
        void Accessory(string name, PrimitiveType shape, Vector3 position, Vector3 scale, Color color) {
            var obj = MakeShape(name, shape, Vector3.zero, scale, color);
            obj.transform.SetParent(equipment, false); obj.transform.localPosition = position;
            var collider = obj.GetComponent<Collider>(); if (collider != null) Destroy(collider);
        }
        void Restart() {
            if (enemyTurn != null) StopCoroutine(enemyTurn);
            enemyTurn = null;
            if (battle.Current == Phase.Won) {
                int previous = battle.Recruits.Count;
                battle.ContinueAfterVictory();
                message = battle.Recruits.Count > previous ? battle.Recruits[battle.Recruits.Count - 1].Name + " recruited! Choose your party before attacking." : "All named heroes recruited. Choose up to three heroes.";
                RefreshAppearance();
            } else { battle.Reset(); message = "Choose your heroes, then attack."; }
            attackTime = -10; feedbackUntil = 0; parried = false;
            enemyRenderer.material.color = new Color(.8f, .45f, .2f);
        }
        void Update() {
            if (status == null) return;
            var party = battle.Party;
            hero = allies[battle.ActiveIndex];
            string roster = "";
            for (int i = 0; i < party.Count; i++)
                roster += (i == battle.ActiveIndex ? "[" : "") + party[i].Identity.Name + " " + party[i].Health + (i == battle.ActiveIndex ? "] " : " ");
            status.text = "ASHLIGHT — Party " + party.Count + "/3 | " + battle.Enemy.Name + " " + battle.EnemyHealth + "/" + battle.Enemy.MaxHealth + "\nWeak: " + battle.Enemy.Weakness + " | Resists: " + battle.Enemy.Resistance + " | " + roster + " | " + battle.Hero.Name + " / " + party[battle.ActiveIndex].Identity.Affinity + "\n" +
                (battle.Current == Phase.Won ? "Victory! Continue to recruit the next hero." : battle.Current == Phase.Lost ? "Party defeated. Restart to try again." : message);
            reset.GetComponentInChildren<Text>().text = battle.Current == Phase.Won ? "CONTINUE" : "RESTART";
            attack.interactable = battle.Current == Phase.Player;
            var active = party[battle.ActiveIndex];
            ability.interactable = battle.Current == Phase.Player && active.SkillCharges(0) > 0;
            abilityTwo.interactable = battle.Current == Phase.Player && active.SkillCharges(1) > 0;
            ability.GetComponentInChildren<Text>().text = active.Skill(0).Name + "\n" + active.SkillCharges(0) + " uses";
            abilityTwo.GetComponentInChildren<Text>().text = active.Skill(1).Name + "\n" + active.SkillCharges(1) + " uses";
            ability.GetComponentInChildren<Text>().fontSize = abilityTwo.GetComponentInChildren<Text>().fontSize = 23;
            loadout.interactable = battle.CanChangeParty;
            for (int i = 0; i < classButtons.Length; i++) {
                var kind = (HeroClass)i;
                classButtons[i].interactable = battle.Current == Phase.Player;
                classButtons[i].GetComponentInChildren<Text>().text = kind.ToString().ToUpperInvariant();
                classButtons[i].GetComponentInChildren<Text>().fontSize = 22;
                classButtons[i].GetComponent<Image>().color = kind == battle.Hero.Kind ? new Color(.25f, .4f, .55f) : new Color(.12f, .16f, .25f);
            }
            bool defending = battle.Current == Phase.EnemyWindup || battle.Current == Phase.EnemyStrike;
            dodge.interactable = parry.interactable = defending && !battle.Defended;
            var area = Screen.safeArea;
            safe.anchorMin = new Vector2(area.xMin / Mathf.Max(1, Screen.width), area.yMin / Mathf.Max(1, Screen.height));
            safe.anchorMax = new Vector2(area.xMax / Mathf.Max(1, Screen.width), area.yMax / Mathf.Max(1, Screen.height));
            SetMeter(heroHealth, battle.HeroHealth / (float)battle.Hero.MaxHealth);
            SetMeter(enemyHealth, battle.EnemyHealth / (float)battle.Enemy.MaxHealth);
            float elapsed = Time.time - strikeTime;
            bool striking = battle.Current == Phase.EnemyStrike;
            timing.transform.parent.gameObject.SetActive(striking && !battle.Defended);
            SetMeter(timing, 1 - elapsed / .4f);
            timing.color = elapsed <= .18f ? Color.yellow : new Color(.2f, .7f, .85f);
            cue.text = battle.Current == Phase.EnemyWindup ? "GET READY" :
                striking && !battle.Defended ? (elapsed <= .18f ? "PARRY OR DODGE!" : elapsed <= .4f ? "DODGE!" : "TOO LATE") :
                Time.time < feedbackUntil ? (battle.Defended ? (parried ? "PARRY + COUNTER" : "DODGED") : "HIT −" + lastHitDamage) : "";
            cue.color = Time.time < feedbackUntil && battle.Defended ? Color.cyan : Color.yellow;
            float attackProgress = Mathf.Clamp01((Time.time - attackTime) / .35f);
            for (int i = 0; i < party.Count; i++) {
                allies[i].position = homes[i];
                allies[i].localScale = Vector3.one;
                allies[i].localRotation = party[i].Health == 0 ? Quaternion.Euler(0, 0, 75) : Quaternion.identity;
            }
            if (attacker != null) attacker.position += Vector3.right * Mathf.Sin(attackProgress * Mathf.PI) * 1.4f;
            if (striking && battle.Defended && !parried) hero.position += Vector3.back * .8f;
            hero.localScale = striking && battle.Defended && !parried ? new Vector3(1, .6f, 1) : Vector3.one;
            float lunge = striking ? Mathf.Sin(Mathf.Clamp01(elapsed / .45f) * Mathf.PI) : 0;
            enemy.position = Vector3.Lerp(enemyHome, hero.position + Vector3.right, lunge);
            enemy.localRotation = battle.Current == Phase.EnemyWindup ? Quaternion.Euler(0, 0, -12) : Quaternion.identity;
            if (Time.time < feedbackUntil && parried) enemy.position += Vector3.right * .3f;
        }
    }
}
