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
        CombatAudio sounds;
        Button soundButton;
        Phase lastSoundPhase;
        bool presentingAction;
        Coroutine actionPresentation;
        ActionMotion actionMotion;
        int actionTarget, actionHero, actionEnergy;
        readonly int[] actionEnemyBefore = new int[2], shownEnemyDamage = new int[2];
        readonly Image[] energyBars = new Image[Battle.MaxPartySize];
        readonly Text[] energyLabels = new Text[Battle.MaxPartySize];
        readonly float[] dodgeAt = { -10, -10, -10 };
        readonly float[] foeAttackAt = { -10, -10 };
        readonly Vector3[] foeAttackTarget = new Vector3[2];
        Button chaptersButton;
        ProgressData restored;
        string saveNotice = "";
        bool progressInitialized, saveFailed;
        Text status, cue;
        Image heroHealth, enemyHealth, secondEnemyHealth, timing;
        readonly Button[] enemyTargets = new Button[2];
        readonly Transform[] foes = new Transform[2];
        RectTransform safe;
        readonly Vector3[] homes = { new Vector3(-3, 1, -1.6f), new Vector3(-2.2f, 1, 0), new Vector3(-1.4f, 1, 1.6f) };
        readonly Transform[] allies = new Transform[Battle.MaxPartySize];
        Transform attacker;
        Button reset;
        readonly Vector3 enemyHome = new Vector3(2, 1.3f, 0);
        readonly Vector3[] pairHomes = { new Vector3(1.9f,1.3f,-1.4f), new Vector3(3f,1.3f,1.4f) };
        Vector3 FoeHome(int index) { return battle.Enemies.Count == 1 ? enemyHome : pairHomes[index]; }
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
        Button attack, ability, abilityTwo, autoButton, loadout;
        bool autoPlay;
        float nextAutoAction;
        readonly System.Random combatRandom = new System.Random();
        GameObject selectionRoot;
        Text selectionTitle;
        Button[] choices, slots;
        bool selectingSkills;
        int selectedSlot, lastHitDamage;
        HeroClass browsingClass;
        readonly System.Collections.Generic.List<HeroDefinition> candidates = new System.Collections.Generic.List<HeroDefinition>();
        Button[] classButtons;
        Transform hero;
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
            sounds = new GameObject("Combat Audio").AddComponent<CombatAudio>();
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
            for(int i=0;i<energyBars.Length;i++) {
                float left=.035f+i*.315f;
                energyLabels[i]=Label(safe,"",new Vector2(left,.195f),new Vector2(left+.29f,.235f),17);
                energyBars[i]=Bar(safe,new Vector2(left,.175f),new Vector2(left+.29f,.188f),new Color(.25f,.65f,.9f));
            }
            heroHealth = Bar(safe, new Vector2(.035f, .805f), new Vector2(.43f, .818f), new Color(.65f, .27f, .18f));
            enemyHealth = Bar(safe, new Vector2(.57f, .805f), new Vector2(.965f, .818f), new Color(.58f,.43f,.19f));
            secondEnemyHealth = Bar(safe,new Vector2(.775f,.805f),new Vector2(.965f,.818f),new Color(.58f,.43f,.19f));
            for (int i = 0; i < enemyTargets.Length; i++) {
                int index = i; float left = .57f+i*.205f;
                enemyTargets[i] = MakeButton(safe,"",left,left+.19f,()=> { if (!presentingAction) battle.SelectEnemy(index); });
                var targetRect = enemyTargets[i].GetComponent<RectTransform>();
                targetRect.anchorMin = new Vector2(left,.752f); targetRect.anchorMax = new Vector2(left+.19f,.797f);
                enemyTargets[i].GetComponentInChildren<Text>().resizeTextMinSize = 10;
                enemyTargets[i].GetComponentInChildren<Text>().resizeTextMaxSize = 16;
            }
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
            autoButton = MakeButton(safe, "AUTO PLAY: OFF", .59f, .95f, () => { autoPlay = !autoPlay; nextAutoAction = Time.time+.3f; });
            // Keep battle actions below the energy row (.175 to .235).
            foreach (var actionButton in new[] { attack, ability, abilityTwo, autoButton }) {
                var actionRect = actionButton.GetComponent<RectTransform>();
                actionRect.anchorMin = new Vector2(actionRect.anchorMin.x, .025f);
                actionRect.anchorMax = new Vector2(actionRect.anchorMax.x, .155f);
            }
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
            for (int i = 0; i < foes.Length; i++) {
                if (foes[i] != null) { foes[i].gameObject.SetActive(false); Destroy(foes[i].gameObject); foes[i] = null; }
                if (i >= battle.Enemies.Count) continue;
                var definition = battle.Enemies[i].Definition;
                var art = EnemyVisualDefinition.For(definition.Name);
                var prefab = art == null ? null : Resources.Load<GameObject>(art.ResourcePath);
                if (prefab != null) foes[i] = Instantiate(prefab,FoeHome(i),Quaternion.identity,battleStage).transform;
                else {
                    if (art != null) Debug.LogWarning("Missing enemy model: " + art.ResourcePath + ". Run Ashlight > Art > Build Enemy Roster.");
                    foes[i] = MakeShape(definition.Name,PrimitiveType.Capsule,FoeHome(i),Vector3.one*(definition.IsBoss?1.7f:1.15f),enemyColor).transform;
                    foes[i].SetParent(battleStage);
                    var material = foes[i].GetComponent<Renderer>().material;
                    material.EnableKeyword("_EMISSION"); material.SetColor("_EmissionColor",Color.black);
                }
                foes[i].name = definition.Name;
                if (foes[i].GetComponent<EnemyPresentation>() == null) foes[i].gameObject.AddComponent<EnemyPresentation>();
            }
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
            // Both bosses remain on the field; each living boss attacks once per enemy round.
            while (battle.Current == Phase.EnemyWindup) {
                int foeIndex = battle.AttackingEnemyIndex;
                var skill = battle.IncomingSkill;
                var actor = foes[foeIndex];
                var art = EnemyVisualDefinition.For(battle.Enemies[foeIndex].Definition.Name);
                var presentation = actor.GetComponent<EnemyPresentation>();
                var visual = actor.GetComponent<HeroVisual>();
                if (visual != null) visual.Play("Windup");
                if (art != null && art.SourceModel == "AshHound") sounds.Play("HoundGrowl",.7f);
                else if (art != null && art.IsRanged) sounds.Play("Charge",.6f);
                message = actor.name + " prepares " + skill.Name + " (" + skill.Element + ", " + skill.Damage + " damage" + (skill.Status == Debuff.None ? "" : ", " + skill.Status) + ") on " + (skill.PartyWide ? "ALL HEROES" : battle.Party[battle.ActiveIndex].Identity.Name) + ".";
                yield return new WaitForSeconds(1.2f);
                if (!battle.BeginStrike()) break;
                strikeTime = Time.time; foeAttackAt[foeIndex] = Time.time; foeAttackTarget[foeIndex] = homes[battle.ActiveIndex]+Vector3.right*.85f; message = "FLASH! " + actor.name + " uses " + skill.Name + " on " + (skill.PartyWide ? "ALL HEROES" : battle.Party[battle.ActiveIndex].Identity.Name) + "!";
                presentation.Warning(true);
                if (visual != null) visual.Play("Attack");
                if (skill.Element != Element.Physical || skill.PartyWide) effects.Ring(actor.position,skill.Element);
                var defenses = new DefenseOutcome[battle.Party.Count];
                for(int i=0;i<defenses.Length;i++) defenses[i] = battle.Party[i].Health>0 && (skill.PartyWide || i==battle.ActiveIndex) ? AutoBattlePlanner.RollDefense(battle.Party[i].Identity.Class,combatRandom.NextDouble()) : DefenseOutcome.Miss;
                while (Time.time < strikeTime+.12f) yield return null;
                for(int i=0;i<defenses.Length;i++) if(defenses[i]==DefenseOutcome.Parry) Defend(true,.12f,i);
                while (Time.time < strikeTime+.26f) yield return null;
                for(int i=0;i<defenses.Length;i++) if(defenses[i]==DefenseOutcome.Dodge) Defend(false,.26f,i);
                while (Time.time < strikeTime+.30f) yield return null;
                if (battle.Enemies[foeIndex].Health > 0)
                    sounds.Play(skill.Element != Element.Physical ? skill.Element.ToString() : art != null && art.SourceModel == "AshHound" ? "HoundBite" : art != null && art.IsBoss ? "HeavySwing" : "SwordSwing");
                while (Time.time < strikeTime+.45f) yield return null;
                bool defended = battle.Defended;
                if (!defended) { feedbackUntil = Time.time+.7f; parried = false; }
                int target = battle.ActiveIndex;
                bool[] strikeDefended = new bool[battle.Party.Count];
                for(int i=0;i<strikeDefended.Length;i++) strikeDefended[i]=battle.HeroDefended(i);
                battle.FinishStrike();
                lastHitDamage = battle.LastIncomingDamage[target];
                for(int i=0;i<battle.Party.Count;i++) {
                    int damage=battle.LastIncomingDamage[i];
                    if(damage>0) {
                        var targetVisual=allies[i].GetComponent<HeroVisual>(); if(targetVisual!=null) targetVisual.Play("Hit");
                        effects.Attack(actor.position,allies[i].position,skill.Element,damage,1); sounds.PlayElement(skill.Element);
                        if(visual!=null) visual.ContactPause();
                        if(battle.Party[i].Status!=Debuff.None) effects.Floating(allies[i].position,battle.Party[i].Status.ToString().ToUpperInvariant(),skill.Element);
                    } else if((skill.PartyWide || i==target) && !strikeDefended[i] && battle.Party[i].Health>0) { effects.Floating(allies[i].position,"BLOCKED",Element.Light); sounds.Play("Guard"); }
                    if(battle.LastStatusDamage[i]>0) effects.Floating(allies[i].position,"DEBUFF −"+battle.LastStatusDamage[i],skill.Element);
                }
                if (art != null && art.IsBoss && battle.Enemies[foeIndex].Health > 0) { effects.BossImpact(allies[target].position,skill.Element); sounds.Play("BossSlam",.75f); }
                presentation.Warning(false);
                if (battle.Current == Phase.Player) message = defended ? "Defense succeeded. Your turn." : "Hit! Your turn.";
            }
            enemyTurn = null;
        }
        void Defend(bool isParry, float resolvedElapsed, int heroIndex) {
            if (presentingAction || atChapters) return;
            if (battle.DefendHero(heroIndex,isParry, resolvedElapsed)) {
                var visual = allies[heroIndex].GetComponent<HeroVisual>(); if (visual != null) visual.Play(isParry ? "Parry" : "Dodge");
                effects.Defense(allies[heroIndex].position, isParry);
                dodgeAt[heroIndex] = isParry ? -10 : Time.time; sounds.Play(isParry ? "Parry" : "Dodge");
                if (isParry) {
                    effects.Attack(allies[heroIndex].position, foes[battle.AttackingEnemyIndex].position, battle.LastElement, battle.LastDamage, battle.LastElementMultiplier);
                    var foeVisual = foes[battle.AttackingEnemyIndex].GetComponent<HeroVisual>(); if (foeVisual != null) foeVisual.Play("Hit");
                }
                parried = isParry; feedbackUntil = Time.time + .7f;
                message = isParry ? "Parried! Counter damage dealt." : "Dodged!";
                if (battle.Current == Phase.Won) SaveProgress();
            }
            else message = "Mistimed defense. React after FLASH.";
        }
        int VisibleEnemyHealth(int index) {
            return presentingAction ? Mathf.Max(0,actionEnemyBefore[index]-shownEnemyDamage[index]) : battle.Enemies[index].Health;
        }
        void CancelActionPresentation() {
            if (actionPresentation != null) StopCoroutine(actionPresentation);
            actionPresentation = null; presentingAction = false; attacker = null;
            for(int i=0;i<dodgeAt.Length;i++) dodgeAt[i]=-10;
            for (int i = 0; i < foeAttackAt.Length; i++) foeAttackAt[i] = -10;
        }
        void HeroImpact(Transform acting, int target, int damage, Element element, float multiplier, bool pause) {
            shownEnemyDamage[target] += damage;
            effects.Attack(acting.position,foes[target].position,element,damage,multiplier);
            if (damage <= 0) return;
            sounds.PlayElement(element,.85f);
            var victim = foes[target].GetComponent<HeroVisual>(); if (victim != null) victim.Play("Hit");
            var actor = acting.GetComponent<HeroVisual>(); if (actor != null && pause) actor.ContactPause();
        }
        void PerformAction(int skillSlot) {
            if (atChapters || selectionRoot.activeSelf || presentingAction) return;
            int actingIndex = battle.ActiveIndex, target = battle.SelectedEnemyIndex;
            var members = battle.Party; int[] healing = new int[members.Count];
            for (int i = 0; i < healing.Length; i++) healing[i] = members[i].Health;
            int oldGuard = members[actingIndex].Guard;
            for(int i=0;i<battle.Enemies.Count;i++) { actionEnemyBefore[i]=battle.Enemies[i].Health; shownEnemyDamage[i]=0; }
            actionHero=actingIndex; actionEnergy=Mathf.Min(PartyHero.MaxEnergy,members[actingIndex].Energy+PartyHero.ActionEnergy);
            string name = skillSlot >= 0 ? members[actingIndex].Skill(skillSlot).Name : "Attack";
            if (!(skillSlot >= 0 ? battle.UseAbility(skillSlot) : battle.Attack())) return;
            for (int i = 0; i < healing.Length; i++) healing[i] = members[i].Health-healing[i]-battle.LastUltimateHealing[i];
            bool casting = CombatMotion.UsesCast(members[actingIndex].Identity.Class,skillSlot >= 0 && battle.LastElement != Element.Physical);
            actionMotion = CombatMotion.For(members[actingIndex].Identity.Class,casting);
            presentingAction = true; actionTarget = target;
            attacker = allies[actingIndex]; attackTime = Time.time;
            var visual = attacker.GetComponent<HeroVisual>(); if (visual != null) visual.Play(casting ? "Cast" : "Attack");
            actionPresentation = StartCoroutine(PresentAction(actingIndex,target,casting,healing,members[actingIndex].Guard-oldGuard-battle.LastUltimateGuard[actingIndex],name));
        }
        IEnumerator PresentAction(int actingIndex, int target, bool casting, int[] healing, int guard, string name) {
            var kind = battle.Party[actingIndex].Identity.Class;
            var actor = allies[actingIndex]; var motion = actionMotion;
            int damage = battle.LastDamage; Element element = battle.LastElement; float multiplier = battle.LastElementMultiplier;
            if (casting) sounds.Play("Charge",.7f);
            else if (kind == HeroClass.Ranger) sounds.Play("BowDraw",.75f);
            message = battle.Party[actingIndex].Identity.Name+" — "+name+".";
            while (Time.time < attackTime+motion.Impact-.10f) yield return null;
            if (!casting) sounds.Play(kind == HeroClass.Ranger ? "BowRelease" : kind == HeroClass.Rogue ? "DaggerSwipe" : kind == HeroClass.Paladin ? "HeavySwing" : "SwordSwing");
            while (Time.time < attackTime+motion.Impact) yield return null;
            bool combo = motion.SecondImpact > 0;
            int firstDamage = combo ? (damage+1)/2 : damage;
            HeroImpact(actor,target,firstDamage,element,multiplier,!casting && kind != HeroClass.Ranger && !combo);
            if (casting && damage == 0) sounds.PlayElement(element,.65f);
            bool healed = false;
            for (int i = 0; i < healing.Length; i++) if (healing[i] > 0) { effects.Heal(allies[i].position,healing[i]); healed = true; }
            if (healed) sounds.Play("Heal");
            if (guard > 0) { effects.Guard(actor.position); sounds.Play("Guard"); }
            if (combo) {
                while (Time.time < attackTime+motion.SecondImpact-.08f) yield return null;
                sounds.Play("DaggerSwipe",.9f,1.08f);
                while (Time.time < attackTime+motion.SecondImpact) yield return null;
                HeroImpact(actor,target,damage-firstDamage,element,multiplier,true);
            }
            while (Time.time < attackTime+motion.Duration+.04f) yield return null;
            var ultimate=battle.LastUltimate;
            if(ultimate!=null) {
                message=battle.Party[actingIndex].Identity.Name+" — ULTIMATE: "+ultimate.Name+"!";
                effects.Floating(actor.position,"ULTIMATE · "+ultimate.Name,ultimate.Affinity);
                effects.Ring(actor.position,ultimate.Affinity,.15f); sounds.Play("Charge");
                bool ultimateCast=CombatMotion.UsesCast(kind,ultimate.Affinity!=Element.Physical);
                var ultimateMotion=CombatMotion.For(kind,ultimateCast); float ultimateAt=Time.time;
                actionMotion=ultimateMotion; attackTime=ultimateAt; actionTarget=battle.UltimateTarget;
                var actorVisual=actor.GetComponent<HeroVisual>(); if(actorVisual!=null) actorVisual.Play(ultimateCast?"Cast":"Attack");
                while(Time.time<ultimateAt+ultimateMotion.Impact) yield return null;
                for(int i=0;i<battle.Enemies.Count;i++) if(battle.LastUltimateDamage[i]>0) {
                    float mult=battle.Enemies[i].Definition.Multiplier(ultimate.Affinity);
                    HeroImpact(actor,i,battle.LastUltimateDamage[i],ultimate.Affinity,mult,!ultimateCast);
                    effects.Ring(foes[i].position,ultimate.Affinity,.15f);
                }
                actionEnergy=battle.Party[actingIndex].Energy;
                for(int i=0;i<battle.Party.Count;i++) {
                    if(battle.LastUltimateHealing[i]>0) effects.Heal(allies[i].position,battle.LastUltimateHealing[i]);
                    if(battle.LastUltimateGuard[i]>0) effects.Guard(allies[i].position);
                }
                if(ultimate.Healing>0) sounds.Play("Heal"); if(ultimate.Guard>0) sounds.Play("Guard");
                while(Time.time<ultimateAt+ultimateMotion.Duration+.04f) yield return null;
            }
            presentingAction = false; actionPresentation = null;
            message = (ultimate==null?name:ultimate.Name+" (ULTIMATE)")+" — "+(ultimate==null?damage:SumUltimateDamage())+" "+(ultimate==null?element:ultimate.Affinity)+" damage"+(ultimate!=null?"":multiplier>1?" (WEAKNESS)":multiplier<1?" (RESISTED)":"")+". Choose the next hero's action.";
            if (battle.Current == Phase.EnemyWindup) enemyTurn = StartCoroutine(EnemyTurn());
            if (battle.Current == Phase.Won) SaveProgress();
        }
        int SumUltimateDamage() { int total=0; foreach(int amount in battle.LastUltimateDamage) total+=amount; return total; }
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
                    choices[i].GetComponentInChildren<Text>(true).text = candidate.Name + " — " + candidate.Gender + " " + candidate.DisplayClass + "\n" + candidate.Quality + " / " + candidate.Affinity + "\nHP " + candidate.Stats.MaxHealth + " | Attack " + candidate.Stats.AttackDamage + "\nULT: " + candidate.Ultimate.Description + "\n" + (Recruited(candidate.Id) ? inParty >= 0 ? "IN PARTY" : "RESERVE" : "NOT RECRUITED");
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
                    Resources.Load<GameObject>("Heroes/DarkFantasy/" + kind + "_" + identity.Gender) ??
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
            soundButton=MakeButton(chapterRoot.transform,"SOUND ON",.80f,.97f,()=>sounds.ToggleMute());
            var soundRect=soundButton.GetComponent<RectTransform>(); soundRect.anchorMin=new Vector2(.80f,.747f); soundRect.anchorMax=new Vector2(.97f,.795f);
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
            CancelActionPresentation(); sounds.Clear(); effects.Clear();
            atChapters=false; chapterRoot.SetActive(false); battleStage.gameObject.SetActive(true);
            DarkFantasyStage.Frame(battleCamera);
            CreateEnemy();
            message="Chapter "+(battle.ChapterIndex+1)+", stage "+(battle.StageIndex+1)+": choose your heroes, then attack.";
        }
        void EnterChapters() {
            CancelActionPresentation();
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
                string state=battle.CampaignComplete || i<battle.ChapterIndex?"COMPLETED":i>battle.ChapterIndex?"LOCKED":"Stage "+(battle.StageIndex+1)+"/5: "+battle.Enemies[0].Definition.Name+(battle.Enemies.Count==2?" + "+battle.Enemies[1].Definition.Name:"");
                chapterChoices[i].GetComponentInChildren<Text>(true).text="CHAPTER "+(i+1)+" — "+chapter.Name+"\n"+state;
                chapterChoices[i].interactable=!battle.CampaignComplete && i==battle.ChapterIndex;
            }
        }
        void Restart() {
            CancelActionPresentation();
            if(battle.Current==Phase.Won || battle.Current==Phase.Lost) { EnterChapters(); return; }
            if(enemyTurn!=null) StopCoroutine(enemyTurn);
            effects.Clear();
            enemyTurn=null; battle.Reset(); attackTime=-10; feedbackUntil=0; parried=false;
            foreach (var foe in foes) if (foe != null) foe.GetComponent<EnemyPresentation>().Warning(false); message="Stage restarted. Choose an action."; SaveProgress();
        }
        void Update() {
            if (status == null) return;
            var area = Screen.safeArea;
            safe.anchorMin = new Vector2(area.xMin / Mathf.Max(1, Screen.width), area.yMin / Mathf.Max(1, Screen.height));
            safe.anchorMax = new Vector2(area.xMax / Mathf.Max(1, Screen.width), area.yMax / Mathf.Max(1, Screen.height));
            var party = battle.Party;
            hero = allies[battle.ActiveIndex];
            int visibleTarget = presentingAction ? actionTarget : battle.SelectedEnemyIndex;
            var visibleEnemy = battle.Enemies[visibleTarget].Definition;
            string roster = "";
            for (int i = 0; i < party.Count; i++)
                roster += (i == battle.ActiveIndex ? "[" : "") + party[i].Identity.Name + " " + party[i].Health + (party[i].Status == Debuff.None ? "" : " · " + party[i].Status) + (i == battle.ActiveIndex ? "] " : " ");
            status.text = "ASHLIGHT — Party " + party.Count + "/3 | " + (visibleEnemy.IsBoss ? "BOSS · " : "") + visibleEnemy.Name + " " + VisibleEnemyHealth(visibleTarget) + "/" + visibleEnemy.MaxHealth + "\nWeak: " + visibleEnemy.Weakness + " | Resists: " + visibleEnemy.Resistance + " | " + roster + " | " + party[battle.ActiveIndex].Identity.DisplayClass + " / " + party[battle.ActiveIndex].Identity.Affinity + "\n" +
                (battle.Current == Phase.Won ? "Victory! Continue saves this clear and recruits a hero." : battle.Current == Phase.Lost ? "Party defeated. Restart to try again." : message);
            if (saveFailed) status.text += "\n" + saveNotice;
            for(int i=0;i<energyBars.Length;i++) {
                bool visible=!atChapters && i<party.Count;
                energyBars[i].transform.parent.gameObject.SetActive(visible); energyLabels[i].gameObject.SetActive(visible);
                if(!visible) continue;
                int energy=presentingAction && i==actionHero?actionEnergy:party[i].Energy;
                SetMeter(energyBars[i],energy/(float)PartyHero.MaxEnergy);
                energyBars[i].color=energy==PartyHero.MaxEnergy?new Color(1,.75f,.2f):new Color(.25f,.65f,.9f);
                energyLabels[i].text=party[i].Identity.Name+" · "+energy+"/100"+(energy==100?" · ULTIMATE":"");
            }
            bool preparing = !atChapters && battle.CanChangeParty;
            bool finished = !atChapters && !presentingAction && (battle.Current == Phase.Won || battle.Current == Phase.Lost);
            reset.gameObject.SetActive(finished);
            loadout.gameObject.SetActive(preparing);
            if (!preparing) selectionRoot.SetActive(false);
            reset.GetComponentInChildren<Text>(true).text = battle.Current == Phase.Won ? "CONTINUE" : "RESTART";
            attack.gameObject.SetActive(!atChapters); ability.gameObject.SetActive(!atChapters); abilityTwo.gameObject.SetActive(!atChapters);
            autoButton.gameObject.SetActive(!atChapters);
            if (finished || atChapters) autoPlay = false;
            autoButton.interactable = !finished;
            autoButton.GetComponentInChildren<Text>().text = autoPlay ? "AUTO PLAY: ON" : "AUTO PLAY: OFF";
            heroHealth.transform.parent.gameObject.SetActive(!atChapters); enemyHealth.transform.parent.gameObject.SetActive(!atChapters);
            chaptersButton.gameObject.SetActive(preparing || finished);
            chaptersButton.interactable = battle.CanChangeParty || battle.Current == Phase.Won || battle.Current == Phase.Lost;
            attack.interactable = battle.Current == Phase.Player && !presentingAction;
            var active = party[battle.ActiveIndex];
            ability.interactable = battle.Current == Phase.Player && !presentingAction && active.SkillCharges(0) > 0;
            abilityTwo.interactable = battle.Current == Phase.Player && !presentingAction && active.SkillCharges(1) > 0;
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

            SetMeter(heroHealth, battle.HeroHealth / (float)battle.Hero.MaxHealth);
            bool paired = battle.Enemies.Count == 2;
            var enemyBarRect = enemyHealth.transform.parent.GetComponent<RectTransform>();
            enemyBarRect.anchorMax = new Vector2(paired ? .755f : .965f,.818f);
            SetMeter(enemyHealth,VisibleEnemyHealth(0)/(float)battle.Enemies[0].Definition.MaxHealth);
            secondEnemyHealth.transform.parent.gameObject.SetActive(paired && !atChapters);
            if (paired) SetMeter(secondEnemyHealth,VisibleEnemyHealth(1)/(float)battle.Enemies[1].Definition.MaxHealth);
            for (int i = 0; i < enemyTargets.Length; i++) {
                enemyTargets[i].gameObject.SetActive(paired && !atChapters);
                if (!paired) continue;
                var foe = battle.Enemies[i];
                enemyTargets[i].interactable = battle.Current == Phase.Player && !presentingAction && foe.Health > 0;
                enemyTargets[i].GetComponentInChildren<Text>().text = (i == visibleTarget ? "▶ " : "") + foe.Definition.Name + " " + VisibleEnemyHealth(i) + "/" + foe.Definition.MaxHealth;
                enemyTargets[i].GetComponent<Image>().color = i == visibleTarget ? new Color(.31f,.25f,.15f) : new Color(.075f,.085f,.105f);
            }
            soundButton.GetComponentInChildren<Text>().text=sounds.Muted?"SOUND OFF":"SOUND ON";
            if (!presentingAction && lastSoundPhase != battle.Current) {
                if (battle.Current == Phase.Won) sounds.Play("Victory",.7f);
                else if (battle.Current == Phase.Lost) sounds.Play("Defeat",.8f);
                lastSoundPhase = battle.Current;
            }
            if(atChapters) { summonButton.interactable=battle.CanSummon; summonButton.GetComponentInChildren<Text>(true).text="SUMMON — 100\nBalance: "+battle.Crystals; timing.transform.parent.gameObject.SetActive(false); cue.text=""; return; }
            if (autoPlay && !presentingAction && !selectionRoot.activeSelf && battle.Current == Phase.Player && Time.time >= nextAutoAction) {
                battle.SelectEnemy(AutoBattlePlanner.ChooseTarget(battle));
                PerformAction(AutoBattlePlanner.ChooseSkill(battle));
                nextAutoAction = Time.time+1.3f;
            }
            float elapsed = Time.time - strikeTime;
            bool striking = !presentingAction && battle.Current == Phase.EnemyStrike;
            timing.transform.parent.gameObject.SetActive(striking && !battle.Defended);
            SetMeter(timing, 1 - elapsed / .4f);
            timing.color = elapsed <= .18f ? Color.yellow : new Color(.2f, .7f, .85f);
            cue.text = presentingAction ? "" : battle.Current == Phase.EnemyWindup ? battle.IncomingSkill.Name.ToUpperInvariant() + (battle.IncomingSkill.PartyWide ? " — ALL HEROES" : " — " + battle.Party[battle.ActiveIndex].Identity.Name) :
                striking && !battle.Defended ? (elapsed <= .18f ? "RESOLVING DEFENSE" : elapsed <= .4f ? "RESOLVING DEFENSE" : "DEFENSE MISSED") :
                Time.time < feedbackUntil ? (battle.Defended ? (parried ? "PARRY + COUNTER" : "DODGED") : "HIT −" + lastHitDamage) : "";
            cue.color = Time.time < feedbackUntil && battle.Defended ? Color.cyan : Color.yellow;
            float advance = actionMotion == null ? 0 : CombatMotion.Advance(Time.time-attackTime,actionMotion.Impact,actionMotion.Duration)*actionMotion.Advance;
            for (int i = 0; i < party.Count; i++) {
                allies[i].position = homes[i];
                allies[i].localScale = Vector3.one;
                var visual = allies[i].GetComponent<HeroVisual>();
                if (visual != null) { visual.SetDefeated(party[i].Health == 0); allies[i].localRotation = Quaternion.identity; }
                else allies[i].localRotation = party[i].Health == 0 ? Quaternion.Euler(0, 0, 75) : Quaternion.identity;
            }
            if (attacker != null && advance > 0 && foes[actionTarget] != null) {
                var direction = foes[actionTarget].position-attacker.position; direction.y=0;
                attacker.position += direction.normalized*advance;
            }
            for(int i=0;i<party.Count;i++) if(party[i].Health>0) allies[i].position += Vector3.back*.8f*CombatMotion.Dodge(Time.time-dodgeAt[i]);
            hero.localScale = hero.GetComponent<HeroVisual>() == null && striking && battle.Defended && !parried ? new Vector3(1, .6f, 1) : Vector3.one;

            for (int i = 0; i < battle.Enemies.Count; i++) {
                var actor = foes[i]; var art = EnemyVisualDefinition.For(battle.Enemies[i].Definition.Name);
                float lunge = VisibleEnemyHealth(i) > 0 ? CombatMotion.EnemyAdvance(Time.time-foeAttackAt[i]) : 0;
                actor.position = art == null || !art.IsRanged ? Vector3.Lerp(FoeHome(i),foeAttackTarget[i],lunge) : FoeHome(i);
                var visual = actor.GetComponent<HeroVisual>();
                bool dead = VisibleEnemyHealth(i) == 0;
                if (visual != null) { visual.SetDefeated(dead); actor.localRotation = Quaternion.identity; }
                else actor.localRotation = dead ? Quaternion.Euler(0,0,-75) : i == battle.AttackingEnemyIndex && battle.Current == Phase.EnemyWindup ? Quaternion.Euler(0,0,-12) : Quaternion.identity;
                if (Time.time < feedbackUntil && parried && i == battle.AttackingEnemyIndex) actor.position += Vector3.right*.3f;
            }
        }
    }
}
