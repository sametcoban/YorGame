using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
namespace Ashlight {
    public sealed class Prototype : MonoBehaviour {
        Battle battle = new Battle(true);
        bool atChapters;
        GameObject chapterRoot;
        Text chapterTitle;
        Button[] chapterChoices;
        Button restartCampaignButton;
        Button summonButton;
        readonly System.Random summonRandom = new System.Random();
        Transform battleStage;
        Camera battleCamera;
        CombatEffects effects;
        Button chaptersButton;
        ProgressData restored;
        string saveNotice = "";
        bool progressInitialized, saveFailed;
        Text status, cue;
        Image heroHealth, enemyHealth, timing;
        RectTransform safe;
        readonly Vector3[] homes = { new Vector3(-3, 1, -1.6f), new Vector3(-2.2f, 1, 0), new Vector3(-1.4f, 1, 1.6f) };
        readonly Transform[] allies = new Transform[Battle.MaxPartySize];
        Transform attacker;
        Button reset;
        readonly Vector3 enemyHome = new Vector3(2, 1.3f, 0);
        float attackTime = -10, feedbackUntil;
        bool parried;
        Color enemyColor {
            get {
                switch(battle.ChapterIndex) {
                    case 1: return new Color(.4f,.75f,.95f);
                    case 2: return new Color(.95f,.3f,.1f);
                    case 3: return new Color(.75f,.6f,.95f);
                    case 4: return new Color(.3f,.65f,.2f);
                    case 5: return new Color(.35f,.25f,.4f);
                    default: return new Color(.8f,.45f,.2f);
                }
            }
        }
        Button attack, ability, abilityTwo, dodge, parry, loadout;
        GameObject selectionRoot;
        Text selectionTitle;
        Button[] choices, slots;
        bool selectingSkills;
        int selectedSlot, lastHitDamage;
        HeroClass browsingClass;
        readonly System.Collections.Generic.List<HeroDefinition> candidates = new System.Collections.Generic.List<HeroDefinition>();
        Button[] classButtons;
        Transform hero, enemy;
        EnemyPresentation enemyPresentation;
        float strikeTime;
        string message = "Your turn. Attack the Lantern Warden.";
        Coroutine enemyTurn;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void StartGame() {
            if (FindFirstObjectByType<Prototype>() == null) new GameObject("Ashlight Prototype").AddComponent<Prototype>();
        }
        void Start() {
            Application.targetFrameRate = 60;
            ProgressStore.Load(battle, out restored);
            progressInitialized = true;
            battleStage = new GameObject("Battle Stage").transform;
            var cameraObject = new GameObject("Battle Camera");
            var camera = cameraObject.AddComponent<Camera>();
            battleCamera = camera;
            effects = new GameObject("Combat Effects").AddComponent<CombatEffects>();
            effects.transform.SetParent(battleStage, false); effects.Initialize(camera);
            cameraObject.AddComponent<AudioListener>();
            DarkFantasyStage.Create(battleStage, camera);
            CreateEnemy();
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
            Panel(safe, new Vector2(0,.79f), Vector2.one, new Color(.025f,.03f,.045f,.90f));
            Panel(safe, Vector2.zero, new Vector2(1,.24f), new Color(.025f,.03f,.045f,.94f));
            Panel(safe, new Vector2(.025f,.795f), new Vector2(.975f,.798f), new Color(.40f,.31f,.17f,.8f));
            status = Label(safe, "", new Vector2(.035f, .825f), new Vector2(.965f, .98f), 22);
            status.alignment = TextAnchor.UpperLeft;
            heroHealth = Bar(safe, new Vector2(.035f, .805f), new Vector2(.43f, .818f), new Color(.65f, .27f, .18f));
            enemyHealth = Bar(safe, new Vector2(.57f, .805f), new Vector2(.965f, .818f), new Color(.58f,.43f,.19f));
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
            chaptersButton = MakeButton(safe, "CHAPTERS", .78f, .95f, EnterChapters);
            var chapterRect = chaptersButton.GetComponent<RectTransform>(); chapterRect.anchorMin = new Vector2(.81f,.29f); chapterRect.anchorMax = new Vector2(.97f,.39f);
            CreateChapterMenu();
            CreateSelectionPanel();
            if (FindFirstObjectByType<EventSystem>() == null) new GameObject("Input", typeof(EventSystem), typeof(StandaloneInputModule));
            EnterChapters();
        }
        void CreateEnemy() {
            if (enemy != null) { enemy.gameObject.SetActive(false); Destroy(enemy.gameObject); }
            var prefab = battle.Enemy.Name == "Lantern Warden" ? Resources.Load<GameObject>("Enemies/LanternWarden") : null;
            if (prefab != null) enemy = Instantiate(prefab, enemyHome, Quaternion.identity, battleStage).transform;
            else {
                enemy = MakeShape(battle.Enemy.Name, PrimitiveType.Capsule, enemyHome, Vector3.one*(battle.StageIndex==4?1.7f:1.15f), enemyColor).transform;
                enemy.SetParent(battleStage);
                var material = enemy.GetComponent<Renderer>().material;
                material.EnableKeyword("_EMISSION"); material.SetColor("_EmissionColor",Color.black);
            }
            enemy.name = battle.Enemy.Name;
            enemyPresentation = enemy.GetComponent<EnemyPresentation>() ?? enemy.gameObject.AddComponent<EnemyPresentation>();
        }
        static void Panel(Transform parent, Vector2 min, Vector2 max, Color color) {
            var panel = new GameObject("HUD backdrop",typeof(RectTransform),typeof(Image)); panel.transform.SetParent(parent,false);
            var rect = panel.GetComponent<RectTransform>(); rect.anchorMin = min; rect.anchorMax = max; rect.offsetMin = rect.offsetMax = Vector2.zero;
            var image = panel.GetComponent<Image>(); image.color = color; image.raycastTarget = false;
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
            label.fontSize = size; label.alignment = TextAnchor.MiddleCenter; label.color = new Color(.9f,.86f,.77f); label.raycastTarget = false;
            return label;
        }
        internal static Button MakeButton(Transform parent, string text, float min, float max, UnityEngine.Events.UnityAction action, bool onPress = false) {
            var obj = new GameObject(text, typeof(RectTransform), typeof(Image), typeof(Button)); obj.transform.SetParent(parent, false);
            var rect = obj.GetComponent<RectTransform>(); rect.anchorMin = new Vector2(min, .05f); rect.anchorMax = new Vector2(max, .22f); rect.offsetMin = rect.offsetMax = Vector2.zero;
            obj.GetComponent<Image>().color = new Color(.075f, .085f, .105f, .97f);
            Label(obj.transform, text, Vector2.zero, Vector2.one, 28);
            var border = obj.AddComponent<Outline>(); border.effectColor = new Color(.39f,.30f,.17f,.8f); border.effectDistance = new Vector2(1,-1);
            var button = obj.GetComponent<Button>();
            var colors = button.colors; colors.highlightedColor = new Color(1,.88f,.66f); colors.pressedColor = new Color(.74f,.58f,.35f); button.colors = colors;
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
            enemyPresentation.Warning(true);
            var enemyVisual = enemy.GetComponent<HeroVisual>();
            if (enemyVisual != null) enemyVisual.Play("Attack");
            yield return new WaitForSeconds(.45f);
            if (!battle.Defended) { feedbackUntil = Time.time + .7f; parried = false; }
            int target = battle.ActiveIndex;
            int healthBefore = battle.Party[target].Health;
            battle.FinishStrike();
            lastHitDamage = healthBefore - battle.Party[target].Health;
            if (lastHitDamage > 0) { var visual = allies[target].GetComponent<HeroVisual>(); if (visual != null) visual.Play("Hit"); }
            if (lastHitDamage > 0) effects.Attack(enemy.position, allies[target].position, Element.Physical, lastHitDamage, 1);
            else if (!battle.Defended) effects.Floating(allies[target].position, "BLOCKED", Element.Light);
            enemyPresentation.Warning(false);
            if (battle.Current == Phase.Player) message = battle.Defended ? "Defense succeeded. Your turn." : "Hit! Your turn.";
            enemyTurn = null;
        }
        void Defend(bool isParry) {
            if (battle.Defend(isParry, Time.time - strikeTime)) {
                var visual = allies[battle.ActiveIndex].GetComponent<HeroVisual>(); if (visual != null) visual.Play(isParry ? "Parry" : "Dodge");
                effects.Defense(allies[battle.ActiveIndex].position, isParry);
                if (isParry) {
                    effects.Attack(allies[battle.ActiveIndex].position, enemy.position, battle.LastElement, battle.LastDamage, battle.LastElementMultiplier);
                    var foeVisual = enemy.GetComponent<HeroVisual>(); if (foeVisual != null) foeVisual.Play("Hit");
                }
                parried = isParry; feedbackUntil = Time.time + .7f;
                message = isParry ? "Parried! Counter damage dealt." : "Dodged!";
                if (battle.Current == Phase.Won) SaveProgress();
            }
            else message = "Mistimed defense. React after FLASH.";
        }
        void PerformAction(int skillSlot) {
            if (atChapters || selectionRoot.activeSelf) return;
            var actingHero = allies[battle.ActiveIndex];
            var members = battle.Party;
            int[] previousHealth = new int[members.Count];
            for (int i = 0; i < members.Count; i++) previousHealth[i] = members[i].Health;
            int actingIndex = battle.ActiveIndex;
            int oldGuard = members[actingIndex].Guard;
            string actionName = skillSlot >= 0 ? battle.Party[battle.ActiveIndex].Skill(skillSlot).Name : "Attack";
            bool accepted = skillSlot >= 0 ? battle.UseAbility(skillSlot) : battle.Attack();
            if (!accepted) return;
            var visual = actingHero.GetComponent<HeroVisual>();
            if (visual != null) visual.Play(skillSlot >= 0 && battle.LastElement != Element.Physical ? "Cast" : "Attack");
            effects.Attack(actingHero.position, enemy.position, battle.LastElement, battle.LastDamage, battle.LastElementMultiplier);
            var foeVisual = enemy.GetComponent<HeroVisual>();
            if (foeVisual != null && battle.LastDamage > 0) foeVisual.Play("Hit");
            for (int i = 0; i < members.Count; i++) {
                int restoredHealth = members[i].Health - previousHealth[i];
                if (restoredHealth > 0) effects.Heal(allies[i].position, restoredHealth);
            }
            if (members[actingIndex].Guard > oldGuard) effects.Guard(actingHero.position);
            attacker = actingHero; attackTime = Time.time;
            message = actionName + " — " + battle.LastDamage + " " + battle.LastElement + " damage" + (battle.LastElementMultiplier > 1 ? " (WEAKNESS)" : battle.LastElementMultiplier < 1 ? " (RESISTED)" : "") + ". " + (battle.Current == Phase.EnemyWindup ? "Enemy targets " + battle.Party[battle.ActiveIndex].Identity.Name + "." : "Choose the next hero's action.");
            if (battle.Current == Phase.EnemyWindup) enemyTurn = StartCoroutine(EnemyTurn());
            if (battle.Current == Phase.Won) SaveProgress();
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
                choices[i].GetComponentInChildren<Text>(true).fontSize = 21;
            }
            var close = MakeButton(selectionRoot.transform, "DONE", .35f, .65f, CloseSelection);
            var cr = close.GetComponent<RectTransform>(); cr.anchorMin = new Vector2(.35f, .02f); cr.anchorMax = new Vector2(.65f, .12f);
            selectionRoot.SetActive(false);
        }
        void OpenSelection(bool skills, HeroClass kind) {
            if (atChapters || !battle.CanChangeParty) return;

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
            if (selectingSkills) { if (battle.EquipSkill(selectedSlot, index)) SaveProgress(); RefreshSelection(); return; }
            if (index >= candidates.Count) return;
            string id = candidates[index].Id;
            for (int i = 0; i < battle.Party.Count; i++) if (battle.Party[i].Identity.Id == id) {
                if (battle.SelectHero(i)) { SaveProgress(); CloseSelection(); message = candidates[index].Name + " selected."; }
                return;
            }
            if (battle.EquipHero(id)) {
                RefreshAppearance(); CloseSelection(); SaveProgress();
                message = candidates[index].Name + " joins the active party.";
            }
        }
        void RefreshSelection() {
            var member = battle.Party[battle.ActiveIndex];
            selectionTitle.text = selectingSkills ? member.Identity.Name + " / " + member.Identity.DisplayClass + " / " + member.Identity.Quality + " / " + member.Identity.Affinity + " — equip 2 of 6 skills\nSelect a slot, then a skill. Choices lock during battle." : browsingClass + " heroes\nSelect an ally, or replace the selected party slot before battle.";
            for (int i = 0; i < 2; i++) {
                slots[i].gameObject.SetActive(selectingSkills);
                slots[i].GetComponentInChildren<Text>(true).text = "SLOT " + (i + 1) + ": " + member.Skill(i).Name;
                slots[i].GetComponent<Image>().color = i == selectedSlot ? new Color(.25f,.4f,.55f) : new Color(.12f,.16f,.25f);
            }
            for (int i = 0; i < choices.Length; i++) {
                choices[i].gameObject.SetActive(selectingSkills || i < candidates.Count);
                if (selectingSkills) {
                    var skill = member.Definition.Skills[i];
                    bool equipped = member.SkillIndex(0) == i || member.SkillIndex(1) == i;
                    choices[i].GetComponentInChildren<Text>(true).text = (equipped ? "EQUIPPED: " : "") + skill.Name + " / " + (skill.Affinity ?? member.Identity.Affinity) + "\n" + skill.Description;
                    choices[i].interactable = battle.CanChangeParty && member.SkillIndex(1 - selectedSlot) != i;
                } else if (i < candidates.Count) {
                    var candidate = candidates[i]; int inParty = -1;
                    for (int j = 0; j < battle.Party.Count; j++) if (battle.Party[j].Identity.Id == candidate.Id) inParty = j;
                    choices[i].GetComponentInChildren<Text>(true).text = candidate.Name + " — " + candidate.Gender + " " + candidate.DisplayClass + "\n" + candidate.Quality + " / " + candidate.Affinity + "\nHP " + candidate.Stats.MaxHealth + " | Attack " + candidate.Stats.AttackDamage + "\n" + (Recruited(candidate.Id) ? inParty >= 0 ? "IN PARTY" : "RESERVE" : "NOT RECRUITED");
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
                var identity = battle.Party[i].Identity;
                // Only use a matching authored model; a woman's hero never inherits a man's model.
                var prefab = Resources.Load<GameObject>("Heroes/Named/" + identity.Id) ??
                    Resources.Load<GameObject>("Heroes/" + kind + "_" + identity.Gender);
                if (prefab != null) {
                    hero = Instantiate(prefab, homes[i], Quaternion.identity, battleStage).transform;
                    hero.name = battle.Party[i].Identity.Name;
                    allies[i] = hero;
                    continue;
                }
                Debug.LogWarning("Missing human prefab for " + kind + " " + identity.Gender + ". Choose Ashlight > Prepare Mobile Project after importing the updated assets.");
                hero = MakeShape(identity.Name + " (missing model)", PrimitiveType.Capsule, homes[i], Vector3.one, Color.gray).transform;
                hero.SetParent(battleStage);
                allies[i] = hero;
            }
            hero = allies[battle.ActiveIndex];
        }
        void CloseSelection() {
            selectionRoot.SetActive(false);
        }
        void SaveProgress() {
            if (!progressInitialized) return;
            saveFailed = !ProgressStore.Save(battle.ExportProgress());
            saveNotice = !saveFailed ? "Progress saved." : "Save failed. See Console; keep this session open.";
        }
        void OnApplicationPause(bool paused) { if (paused) SaveProgress(); }
        void OnApplicationQuit() { SaveProgress(); }
        void OnApplicationFocus(bool focused) { if (!focused) SaveProgress(); }
        void CreateChapterMenu() {
            chapterRoot=new GameObject("Chapter Menu",typeof(RectTransform),typeof(Image)); chapterRoot.transform.SetParent(safe,false);
            var rect=chapterRoot.GetComponent<RectTransform>(); rect.anchorMin=Vector2.zero; rect.anchorMax=Vector2.one; rect.offsetMin=rect.offsetMax=Vector2.zero;
            chapterRoot.GetComponent<Image>().color=new Color(.06f,.08f,.13f,.98f);
            chapterTitle=Label(chapterRoot.transform,"",new Vector2(.03f,.8f),new Vector2(.97f,.98f),30);
            chapterChoices=new Button[ChapterDefinition.Catalog.Count];
            for(int i=0;i<chapterChoices.Length;i++) {
                int index=i; float left=i%2==0?.05f:.52f; float bottom=.57f-(i/2)*.2f;
                chapterChoices[i]=MakeButton(chapterRoot.transform,"",left,left+.43f,()=>StartChapter(index));
                var r=chapterChoices[i].GetComponent<RectTransform>(); r.anchorMin=new Vector2(left,bottom); r.anchorMax=new Vector2(left+.43f,bottom+.17f);
            }
            summonButton=MakeButton(chapterRoot.transform,"SUMMON — 100",.05f,.37f,SummonHero);
            var sr=summonButton.GetComponent<RectTransform>(); sr.anchorMin=new Vector2(.05f,.025f); sr.anchorMax=new Vector2(.37f,.135f);
            Label(chapterRoot.transform,Summoning.Odds+"\nRare+ within 5 pulls; Legendary within 15. Six heroes per rarity have equal odds.",new Vector2(.4f,.02f),new Vector2(.96f,.15f),18);
            restartCampaignButton=MakeButton(chapterRoot.transform,"RESTART CAMPAIGN\nKeep heroes, skills & crystals",.24f,.76f,RestartCampaign);
            var replayRect=restartCampaignButton.GetComponent<RectTransform>();
            replayRect.anchorMin=new Vector2(.24f,.43f); replayRect.anchorMax=new Vector2(.76f,.61f);
            restartCampaignButton.gameObject.SetActive(false);
        }
        void RestartCampaign() {
            if(!atChapters || !battle.RestartCampaign()) return;
            RefreshAppearance();
            EnterChapters();
        }
        string SummonWallet() {
            return "Crystals "+battle.Crystals+" | Rare+ in "+(Summoning.RareGuarantee-battle.RareMisses)+" | Legendary in "+(Summoning.LegendaryGuarantee-battle.LegendaryMisses);
        }
        void SummonHero() {
            if(!atChapters) return;
            var result=battle.Summon(summonRandom.NextDouble(),summonRandom.Next(6));
            if(result==null) return;
            SaveProgress();
            var hero=result.Hero;
            chapterTitle.color=hero.Quality==HeroQuality.Legendary?new Color(1,.8f,.25f):hero.Quality==HeroQuality.Epic?new Color(.8f,.5f,1):hero.Quality==HeroQuality.Rare?new Color(.4f,.75f,1):Color.white;
            chapterTitle.text="SUMMON: "+hero.Name+" — "+hero.Quality+" "+hero.DisplayClass+" / "+hero.Affinity+"\n"+
                (result.Duplicate?"Duplicate: +"+result.Refund+" crystals refunded.":"New hero added to reserves! Choose them before battle.")+"\n"+SummonWallet()+" | "+saveNotice;
        }
        void StartChapter(int index) {
            if(battle.CampaignComplete || index!=battle.ChapterIndex) return;
            effects.Clear();
            atChapters=false; chapterRoot.SetActive(false); battleStage.gameObject.SetActive(true);
            DarkFantasyStage.Frame(battleCamera);
            CreateEnemy();
            message="Chapter "+(battle.ChapterIndex+1)+", stage "+(battle.StageIndex+1)+": choose your heroes, then attack.";
        }
        void EnterChapters() {
            if(enemyTurn!=null) StopCoroutine(enemyTurn);
            enemyTurn=null;
            string reward="";
            if(battle.Current==Phase.Won) {
                int previous=battle.Recruits.Count;
                battle.ContinueAfterVictory(); RefreshAppearance();
                if(battle.Recruits.Count>previous) reward=battle.Recruits[battle.Recruits.Count-1].Name+" recruited! ";
            } else if(!battle.CanChangeParty && !battle.CampaignComplete) battle.Reset();
            effects.Clear();
            atChapters=true; attackTime=-10; feedbackUntil=0; parried=false;
            selectionRoot.SetActive(false); battleStage.gameObject.SetActive(false); chapterRoot.SetActive(true);
            SaveProgress();
            chapterTitle.color=Color.white;
            chapterTitle.text="ASHLIGHT — Chapters\n"+reward+(battle.CampaignComplete?"All six chapters completed!":"Continue your journey.")+"\n"+SummonWallet()+" | "+saveNotice;
            restartCampaignButton.gameObject.SetActive(battle.CampaignComplete);
            for(int i=0;i<chapterChoices.Length;i++) {
                chapterChoices[i].gameObject.SetActive(!battle.CampaignComplete);
                var chapter=ChapterDefinition.Catalog[i];
                string state=battle.CampaignComplete || i<battle.ChapterIndex?"COMPLETED":i>battle.ChapterIndex?"LOCKED":"Stage "+(battle.StageIndex+1)+"/5: "+battle.Enemy.Name;
                chapterChoices[i].GetComponentInChildren<Text>(true).text="CHAPTER "+(i+1)+" — "+chapter.Name+"\n"+state;
                chapterChoices[i].interactable=!battle.CampaignComplete && i==battle.ChapterIndex;
            }
        }
        void Restart() {
            if(battle.Current==Phase.Won || battle.Current==Phase.Lost) { EnterChapters(); return; }
            if(enemyTurn!=null) StopCoroutine(enemyTurn);
            effects.Clear();
            enemyTurn=null; battle.Reset(); attackTime=-10; feedbackUntil=0; parried=false;
            enemyPresentation.Warning(false); message="Stage restarted. Choose an action."; SaveProgress();
        }
        void Update() {
            if (status == null) return;
            var area = Screen.safeArea;
            safe.anchorMin = new Vector2(area.xMin / Mathf.Max(1, Screen.width), area.yMin / Mathf.Max(1, Screen.height));
            safe.anchorMax = new Vector2(area.xMax / Mathf.Max(1, Screen.width), area.yMax / Mathf.Max(1, Screen.height));
            var party = battle.Party;
            hero = allies[battle.ActiveIndex];
            string roster = "";
            for (int i = 0; i < party.Count; i++)
                roster += (i == battle.ActiveIndex ? "[" : "") + party[i].Identity.Name + " " + party[i].Health + (i == battle.ActiveIndex ? "] " : " ");
            status.text = "ASHLIGHT — Party " + party.Count + "/3 | " + battle.Enemy.Name + " " + battle.EnemyHealth + "/" + battle.Enemy.MaxHealth + "\nWeak: " + battle.Enemy.Weakness + " | Resists: " + battle.Enemy.Resistance + " | " + roster + " | " + party[battle.ActiveIndex].Identity.DisplayClass + " / " + party[battle.ActiveIndex].Identity.Affinity + "\n" +
                (battle.Current == Phase.Won ? "Victory! Continue saves this clear and recruits a hero." : battle.Current == Phase.Lost ? "Party defeated. Restart to try again." : message);
            if (saveFailed) status.text += "\n" + saveNotice;
            bool preparing = !atChapters && battle.CanChangeParty;
            bool finished = !atChapters && (battle.Current == Phase.Won || battle.Current == Phase.Lost);
            reset.gameObject.SetActive(finished);
            loadout.gameObject.SetActive(preparing);
            if (!preparing) selectionRoot.SetActive(false);
            reset.GetComponentInChildren<Text>(true).text = battle.Current == Phase.Won ? "CONTINUE" : "RESTART";
            attack.gameObject.SetActive(!atChapters); ability.gameObject.SetActive(!atChapters); abilityTwo.gameObject.SetActive(!atChapters);
            dodge.gameObject.SetActive(!atChapters); parry.gameObject.SetActive(!atChapters);
            heroHealth.transform.parent.gameObject.SetActive(!atChapters); enemyHealth.transform.parent.gameObject.SetActive(!atChapters);
            chaptersButton.gameObject.SetActive(preparing || finished);
            chaptersButton.interactable = battle.CanChangeParty || battle.Current == Phase.Won || battle.Current == Phase.Lost;
            attack.interactable = battle.Current == Phase.Player;
            var active = party[battle.ActiveIndex];
            ability.interactable = battle.Current == Phase.Player && active.SkillCharges(0) > 0;
            abilityTwo.interactable = battle.Current == Phase.Player && active.SkillCharges(1) > 0;
            ability.GetComponentInChildren<Text>(true).text = active.Skill(0).Name + "\n" + active.SkillCharges(0) + " uses";
            abilityTwo.GetComponentInChildren<Text>(true).text = active.Skill(1).Name + "\n" + active.SkillCharges(1) + " uses";
            ability.GetComponentInChildren<Text>(true).fontSize = abilityTwo.GetComponentInChildren<Text>(true).fontSize = 23;
            loadout.interactable = battle.CanChangeParty;
            for (int i = 0; i < classButtons.Length; i++) {
                var kind = (HeroClass)i;
                classButtons[i].gameObject.SetActive(preparing);
                classButtons[i].interactable = preparing;
                classButtons[i].GetComponentInChildren<Text>(true).text = (kind == HeroClass.Sorceress ? "SORCERY" : kind.ToString().ToUpperInvariant());
                classButtons[i].GetComponentInChildren<Text>(true).fontSize = 22;
                classButtons[i].GetComponent<Image>().color = kind == battle.Hero.Kind ? new Color(.31f, .25f, .15f) : new Color(.075f, .085f, .105f);
            }
            bool defending = battle.Current == Phase.EnemyWindup || battle.Current == Phase.EnemyStrike;
            dodge.interactable = parry.interactable = defending && !battle.Defended;
            SetMeter(heroHealth, battle.HeroHealth / (float)battle.Hero.MaxHealth);
            SetMeter(enemyHealth, battle.EnemyHealth / (float)battle.Enemy.MaxHealth);
            if(atChapters) { summonButton.interactable=battle.CanSummon; summonButton.GetComponentInChildren<Text>(true).text="SUMMON — 100\nBalance: "+battle.Crystals; timing.transform.parent.gameObject.SetActive(false); cue.text=""; return; }
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
                var visual = allies[i].GetComponent<HeroVisual>();
                if (visual != null) { visual.SetDefeated(party[i].Health == 0); allies[i].localRotation = Quaternion.identity; }
                else allies[i].localRotation = party[i].Health == 0 ? Quaternion.Euler(0, 0, 75) : Quaternion.identity;
            }
            if (attacker != null) attacker.position += Vector3.right * Mathf.Sin(attackProgress * Mathf.PI) * 1.4f;
            if (striking && battle.Defended && !parried) hero.position += Vector3.back * .8f;
            hero.localScale = hero.GetComponent<HeroVisual>() == null && striking && battle.Defended && !parried ? new Vector3(1, .6f, 1) : Vector3.one;
            float lunge = striking ? Mathf.Sin(Mathf.Clamp01(elapsed / .45f) * Mathf.PI) : 0;
            enemy.position = Vector3.Lerp(enemyHome, hero.position + Vector3.right, lunge);
            var foeVisual = enemy.GetComponent<HeroVisual>();
            if (foeVisual != null) { foeVisual.SetDefeated(battle.EnemyHealth == 0); enemy.localRotation = Quaternion.identity; }
            else enemy.localRotation = battle.Current == Phase.Won ? Quaternion.Euler(0,0,-75) : battle.Current == Phase.EnemyWindup ? Quaternion.Euler(0, 0, -12) : Quaternion.identity;
            if (Time.time < feedbackUntil && parried) enemy.position += Vector3.right * .3f;
        }
    }
}
