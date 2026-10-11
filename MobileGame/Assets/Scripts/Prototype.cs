using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;
namespace Ashlight {
    public sealed class Prototype : MonoBehaviour {
        Battle battle = new Battle(true,enableWaves:true,enableClassPassives:true,enableFactions:true);
        bool atChapters;
        const string TutorialKey = "Ashlight.Tutorial.v1";
        GameObject tutorialRoot;
        Text tutorialTitle, tutorialBody;
        Button tutorialContinue;
        int tutorialSeen, tutorialPending = -1;

        GameObject chapterRoot;
        GameObject teamPresetRoot; Text teamPresetTitle; Text[] teamPresetLabels; Button[] teamPresetLoads, teamPresetClears; string presetNotice="";
        GameObject dailyRoot; Button[] dailyButtons;
        GameObject collectionRoot; Text collectionTitle; Button[] collectionCards; Button collectionAscend, collectionShared; int collectionPage, collectionTarget, classFilter=-1, factionFilter=-1, rarityFilter=-1; CollectionSort collectionSort; string collectionHero;
        List<PartyHero> collectionItems=new List<PartyHero>(); Button[] collectionFilters; Button collectionTargetButton, collectionAssign, collectionBattleButton; Text collectionCompare;
        Text factionStatus, formationFaction;
        Text chapterTitle;
        Button idleRewardsButton;
        Button[] chapterChoices;
        Button restartCampaignButton;
        Button towerButton;
        Button summonButton, summonTenButton;
        GameObject summonRoot; Text summonTitle; Text[] summonCards; GameObject[] summonCardPanels; Button repeatSingle, repeatTen;
        readonly System.Random summonRandom = new System.Random();
        Transform battleStage;
        Camera battleCamera;
        DarkFantasyStage chapterStage;
        int stageChapter=-1;
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
        readonly Image[] partyHealthBars = new Image[Battle.MaxPartySize];
        readonly Text[] energyLabels = new Text[Battle.MaxPartySize];
        readonly float[] dodgeAt = { -10, -10, -10, -10, -10 };
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
        readonly Vector3[] homes = { new Vector3(-1.5f,1,-1.15f), new Vector3(-1.5f,1,1.15f), new Vector3(-3.5f,1,-2.1f), new Vector3(-3.5f,1,0), new Vector3(-3.5f,1,2.1f) };
        readonly Transform[] allies = new Transform[Battle.MaxPartySize];
        Transform attacker;
        Button reset;
        readonly Vector3 enemyHome = new Vector3(2, 1.3f, 0);
        readonly Vector3[] pairHomes = { new Vector3(1.9f,1.3f,-1.4f), new Vector3(3f,1.3f,1.4f) };
        Vector3 FoeHome(int index) {
            var home=battle.Enemies.Count==1?enemyHome:pairHomes[index];
            home.x=battle.Enemies[index].Definition.Row==EnemyRow.Front?1.8f:3.6f;
            return home;
        }
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
        const string SpeedKey="Ashlight.BattleSpeed";
        int battleSpeed=1;
        Button speedButton;
        float nextAutoAction;
        readonly System.Random combatRandom = new System.Random();
        GameObject selectionRoot;
        Text selectionTitle;
        Button[] choices, slots;
        Button upgradeButton, gearButton, gearPrevious, gearNext;
        GameObject resultsRoot;
        Text resultsTitle,resultsRewards,resultsLoot;
        readonly Text[] resultsHeroes=new Text[Battle.MaxPartySize];
        GameObject equipmentRoot;
        Text equipmentTitle;
        readonly Button[] gearHeroes=new Button[Battle.MaxPartySize],gearSlots=new Button[3],gearChoices=new Button[6];
        readonly System.Collections.Generic.List<GearDefinition> gearCandidates=new System.Collections.Generic.List<GearDefinition>();
        GearSlot browsingSlot;
        GameObject previewRoot;
        Text previewText;
        Button previewButton;
        GameObject progressionRoot;
        Text progressionTitle;
        readonly Button[] progressionHeroes=new Button[Battle.MaxPartySize], progressionChoices=new Button[6];
        GameObject formationRoot;
        Button formationButton;
        readonly Button[] formationSlots=new Button[Battle.MaxPartySize];
        int formationSelected=-1;
        int gearPage;
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
            battleSpeed=Mathf.Clamp(PlayerPrefs.GetInt(SpeedKey,1),1,3);
            Time.timeScale=1;
            ProgressStore.Load(battle, out restored);
            // Existing players are exempt; new players retain tutorial state across restarts.
            int tutorialState = PlayerPrefs.GetInt(TutorialKey, restored == null ? 0 : 31);
            tutorialSeen = tutorialState & 31;
            int pending = tutorialState >> 5;
            tutorialPending = pending >= 1 && pending <= 5 ? pending-1 : -1;
            PersistTutorial();
            battle.FillAvailableSlots();
            progressInitialized = true;
            battleStage = new GameObject("Battle Stage").transform;
            var cameraObject = new GameObject("Battle Camera");
            var camera = cameraObject.AddComponent<Camera>();
            battleCamera = camera;
            effects = new GameObject("Combat Effects").AddComponent<CombatEffects>();
            effects.transform.SetParent(battleStage, false); effects.Initialize(camera);
            // Unity scenes may already contain a listener on their default camera.
            // Disable existing listeners before making the battle camera the audio viewpoint.
            foreach (var listener in FindObjectsByType<AudioListener>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                listener.enabled = false;
            cameraObject.AddComponent<AudioListener>();
            sounds = new GameObject("Combat Audio").AddComponent<CombatAudio>();
            chapterStage=DarkFantasyStage.Create(battleStage,camera,battle.ChapterIndex); stageChapter=battle.ChapterIndex;
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
            Panel(safe, Vector2.zero, new Vector2(1,.285f), new Color(.025f,.03f,.045f,.94f));
            Panel(safe, new Vector2(.025f,.795f), new Vector2(.975f,.798f), new Color(.40f,.31f,.17f,.8f));
            status = Label(safe, "", new Vector2(.035f, .845f), new Vector2(.965f, .98f), 22);
            status.alignment = TextAnchor.UpperLeft;
            factionStatus=Label(safe,"",new Vector2(.035f,.82f),new Vector2(.965f,.845f),17);
            for(int i=0;i<energyBars.Length;i++) {
                float left=.025f+i*.194f;
                energyLabels[i]=Label(safe,"",new Vector2(left,.225f),new Vector2(left+.18f,.28f),20);
                partyHealthBars[i]=Bar(safe,new Vector2(left,.205f),new Vector2(left+.18f,.218f),new Color(.35f,.7f,.43f));
                energyBars[i]=Bar(safe,new Vector2(left,.18f),new Vector2(left+.18f,.193f),new Color(.25f,.65f,.9f));
            }
            heroHealth = Bar(safe, new Vector2(.035f, .805f), new Vector2(.43f, .818f), new Color(.65f, .27f, .18f));
            enemyHealth = Bar(safe, new Vector2(.57f, .805f), new Vector2(.965f, .818f), new Color(.58f,.43f,.19f));
            secondEnemyHealth = Bar(safe,new Vector2(.775f,.805f),new Vector2(.965f,.818f),new Color(.58f,.43f,.19f));
            for (int i = 0; i < enemyTargets.Length; i++) {
                int index = i; float left = .57f+i*.205f;
                enemyTargets[i] = MakeButton(safe,"",left,left+.19f,()=> { if (!presentingAction) battle.SelectEnemy(index); });
                var targetRect = enemyTargets[i].GetComponent<RectTransform>();
                targetRect.anchorMin = new Vector2(left,.725f); targetRect.anchorMax = new Vector2(left+.19f,.797f);
                enemyTargets[i].GetComponentInChildren<Text>().resizeTextMinSize = 10;
                enemyTargets[i].GetComponentInChildren<Text>().resizeTextMaxSize = 16;
            }
            timing = Bar(safe, new Vector2(.3f, .29f), new Vector2(.7f, .305f), Color.yellow);
            cue = Label(safe, "", new Vector2(.2f, .315f), new Vector2(.8f, .395f), 32);
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
            autoButton = MakeButton(safe, "AUTO PLAY: OFF", .59f, .80f, () => { autoPlay = !autoPlay; nextAutoAction = Time.time+.3f; });
            speedButton=MakeButton(safe,"SPEED: "+battleSpeed+"×",.82f,.98f,()=> {
                battleSpeed=battleSpeed%3+1;
                PlayerPrefs.SetInt(SpeedKey,battleSpeed); PlayerPrefs.Save();
                ApplyBattleSpeed();
            });
            // Keep actions separate from the party health and energy meters.
            foreach (var actionButton in new[] { attack, ability, abilityTwo, autoButton, speedButton }) {
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
            CreateSelectionPanel(); CreateEquipmentPanel();
            gearButton=MakeButton(safe,"EQUIPMENT",.78f,.95f,OpenEquipment);
            var gearRect=gearButton.GetComponent<RectTransform>(); gearRect.anchorMin=new Vector2(.78f,.625f); gearRect.anchorMax=new Vector2(.95f,.72f);
            if (FindFirstObjectByType<EventSystem>() == null) new GameObject("Input", typeof(EventSystem), typeof(StandaloneInputModule));
            CreateFormationPanel(); CreateProgressionPanel(); CreateStagePreview(); CreateCollection(); CreateDaily(); CreateSummonResults(); CreateTeamPresets();
            formationButton=MakeButton(safe,"FORMATION",.78f,.95f,OpenFormation);
            PlaceGearControl(formationButton,.78f,.52f,.95f,.62f);
            CreateResultsPanel(); CreateTutorial();
            if(battle.Current==Phase.Won) { atChapters=false; chapterRoot.SetActive(false); battleStage.gameObject.SetActive(true); ShowResults(); } else EnterChapters();
            if (tutorialPending >= 0) ShowTutorial(tutorialPending); else ShowTutorial(0);
        }
        void PersistTutorial() {
            PlayerPrefs.SetInt(TutorialKey,tutorialSeen | ((tutorialPending+1)<<5));
            PlayerPrefs.Save();
        }
        void CreateTutorial() {
            tutorialRoot=new GameObject("First Journey",typeof(RectTransform),typeof(Image));
            tutorialRoot.transform.SetParent(safe,false);
            var rect=tutorialRoot.GetComponent<RectTransform>();
            rect.anchorMin=Vector2.zero; rect.anchorMax=Vector2.one; rect.offsetMin=rect.offsetMax=Vector2.zero;
            tutorialRoot.GetComponent<Image>().color=new Color(.015f,.02f,.03f,.96f);
            Panel(tutorialRoot.transform,new Vector2(.12f,.20f),new Vector2(.88f,.84f),new Color(.055f,.065f,.085f));
            tutorialTitle=Label(tutorialRoot.transform,"",new Vector2(.16f,.68f),new Vector2(.84f,.82f),32);
            tutorialTitle.color=new Color(1,.82f,.4f);
            tutorialBody=Label(tutorialRoot.transform,"",new Vector2(.18f,.33f),new Vector2(.82f,.67f),26);
            tutorialContinue=MakeButton(tutorialRoot.transform,"GOT IT",.44f,.82f,()=> {
                if(tutorialPending>=0) tutorialSeen |= 1<<tutorialPending;
                tutorialPending=-1; PersistTutorial(); tutorialRoot.SetActive(false);
            });
            PlaceGearControl(tutorialContinue,.44f,.22f,.82f,.32f);
            var skip=MakeButton(tutorialRoot.transform,"SKIP TUTORIAL",.18f,.41f,()=> {
                tutorialSeen=31; tutorialPending=-1; PersistTutorial(); tutorialRoot.SetActive(false);
            });
            PlaceGearControl(skip,.18f,.22f,.41f,.32f);
            skip.GetComponentInChildren<Text>().resizeTextMaxSize=20;
            tutorialRoot.SetActive(false);
            var replay=MakeButton(chapterRoot.transform,"TUTORIAL",.80f,.97f,()=> {
                tutorialSeen=0; tutorialPending=-1; PersistTutorial(); ShowTutorial(0);
            });
            PlaceGearControl(replay,.80f,.67f,.97f,.72f);
            replay.GetComponentInChildren<Text>().resizeTextMaxSize=18;
        }
        void ShowTutorial(int step) {
            if(tutorialRoot==null || step<0 || step>4 || (tutorialSeen & (1<<step))!=0) return;
            string[] titles={"WELCOME TO ASHLIGHT","PREPARE YOUR HERO","VICTORY & REWARDS","RECRUIT YOUR NEXT HERO","YOUR JOURNEY BEGINS"};
            string[] instructions={
                "Your journey starts with a Knight.\nChoose Chapter 1 to enter your first battle.\nMore chapters unlock more team slots: eventually two heroes in front and three in back.",
                "Review Enemy Preview, then close it to prepare.\nUse Skills to choose two of your class's six skills.\nAttack or use a skill to fight. Auto Play handles your turns; defense is automatic. Full energy triggers an ultimate.",
                "You've defeated every wave in this stage!\nContinue claims the displayed rewards and saves this victory.\nYour heroes gain XP, and some stages recruit another hero.",
                "Back in Chapters, use Summon x1 to spend 100 crystals.\nCheck Collection to review recruited heroes.\nBefore a fight, assign heroes and arrange their front and back positions in Formation.",
                "New heroes join your collection; duplicates grant shards.\nAssign your team before combat, within the chapter's slot limit.\nKeep clearing stages, claim Idle Rewards, and complete Daily Quests."
            };
            tutorialPending=step; PersistTutorial();
            tutorialTitle.text=titles[step]+" · "+(step+1)+"/5";
            tutorialBody.text=instructions[step];
            tutorialContinue.GetComponentInChildren<Text>().text=step==4?"START YOUR JOURNEY":"GOT IT";
            tutorialRoot.transform.SetAsLastSibling(); tutorialRoot.SetActive(true);
        }
        void CreateEnemy() {
            for (int i = 0; i < foes.Length; i++) {
                if (foes[i] != null) { foes[i].gameObject.SetActive(false); Destroy(foes[i].gameObject); foes[i] = null; }
                if (i >= battle.Enemies.Count) continue;
                var definition = battle.Enemies[i].Definition;
                var art = EnemyVisualDefinition.For(definition.Name);
                var prefab = art == null ? null : Resources.Load<GameObject>(art.ResourcePath);
                if(prefab==null && art!=null) {
                    prefab=Resources.Load<GameObject>("Enemies/"+art.SourceModel);
                    if(prefab==null) foreach(var reference in EnemyVisualDefinition.Catalog) if(reference.SourceModel==art.SourceModel) { prefab=Resources.Load<GameObject>(reference.ResourcePath); if(prefab!=null) break; }
                    if(prefab==null) prefab=Resources.Load<GameObject>("Enemies/"+(art.IsRanged?"CinderScout":"LanternWarden"));
                    Debug.LogWarning("Enemy visual variant missing: "+art.ResourcePath+". Using an animated reference if available. Run Ashlight > Art > Build Enemy Roster.");
                }
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
            var caption = Label(obj.transform, text, Vector2.zero, Vector2.one, 26);
            var captionRect = caption.GetComponent<RectTransform>();
            captionRect.offsetMin = new Vector2(10, 5); captionRect.offsetMax = new Vector2(-10, -5);
            caption.resizeTextMinSize = 16;
            var border = obj.AddComponent<Outline>(); border.effectColor = new Color(.39f,.30f,.17f,.8f); border.effectDistance = new Vector2(1,-1);
            var button = obj.GetComponent<Button>();
            var colors = button.colors; colors.highlightedColor = new Color(1,.88f,.66f); colors.pressedColor = new Color(.74f,.58f,.35f); colors.disabledColor = new Color(.48f,.48f,.48f,.8f); button.colors = colors;
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
        bool transitioningWave;
        IEnumerator NextWave() {
            transitioningWave=true;
            message="WAVE "+(battle.WaveIndex+1)+" CLEARED — next wave incoming!";
            cue.text="WAVE CLEARED";
            yield return new WaitForSeconds(1.2f);
            if(battle.AdvanceWave()) {
                effects.Clear(); CreateEnemy();
                for(int i=0;i<foeAttackAt.Length;i++) foeAttackAt[i]=-10;
                attackTime=-10; attacker=null; feedbackUntil=0;
                message="WAVE "+(battle.WaveIndex+1)+"/"+battle.WaveCount+" — health and energy carried over.";
                nextAutoAction=Time.time+.8f;
            }
            transitioningWave=false;
        }
        void PresentPassiveHealing() {
            for(int i=0;i<battle.Party.Count;i++) {
                if(battle.LastPassiveHealing[i]>0) {
                    effects.Heal(allies[i].position,battle.LastPassiveHealing[i]);
                    effects.Floating(allies[i].position,"PASSIVE HEAL",Element.Light); sounds.Play("Heal",.7f);
                }
                if(battle.LastPassiveCleanse[i]!=Debuff.None) effects.Floating(allies[i].position,"CLEANSED "+battle.LastPassiveCleanse[i].ToString().ToUpperInvariant(),Element.Light);
            }
        }
        IEnumerator EnemyTurn() {
            for(int i=0;i<battle.Party.Count;i++) if(battle.LastPassiveGuard[i]>0) {
                effects.Guard(allies[i].position); effects.Floating(allies[i].position,"SACRED AEGIS",Element.Light);
            }
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
                message = skill.Action!=EnemyAction.Attack ? actor.name+" prepares "+skill.Name+"." : actor.name + " prepares " + skill.Name + " (" + skill.Element + ", " + skill.Damage + " damage" + (skill.Status == Debuff.None ? "" : ", " + skill.Status) + ") on " + (skill.PartyWide ? "ALL HEROES" : battle.Party[battle.ActiveIndex].Identity.Name) + ".";
                yield return new WaitForSeconds(1.2f);
                if (!battle.BeginStrike()) break;
                if(skill.Action!=EnemyAction.Attack) {
                    if(visual!=null) visual.Play("Cast");
                    effects.Ring(actor.position,skill.Element);
                    sounds.Play("Charge",.6f);
                    yield return new WaitForSeconds(.45f);
                    battle.FinishStrike();
                    int recipient=battle.LastMechanicTarget;
                    if(skill.Action==EnemyAction.Charge) {
                        effects.Floating(actor.position,"CHARGING · DEAL 60 DAMAGE",Element.Fire);
                        message="Interrupt Cinder Sovereign: deal 60 damage before his next attack!";
                    } else if(skill.Action==EnemyAction.HealPartner) {
                        effects.Heal(foes[recipient].position,battle.LastEnemyHealing); sounds.Play("Heal");
                        message=battle.Enemies[foeIndex].Definition.Name+" restores "+battle.LastEnemyHealing+" HP to "+battle.Enemies[recipient].Definition.Name+".";
                    } else {
                        effects.Guard(foes[recipient].position); sounds.Play("Guard");
                        effects.Floating(foes[recipient].position,"SHIELD "+battle.Enemies[recipient].Shield,Element.Light);
                        message=skill.Name+" protects "+battle.Enemies[recipient].Definition.Name+".";
                    }
                    PresentPassiveHealing();
                    presentation.Warning(false);
                    continue;
                }
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
                    if(battle.LastPassiveProtection[i]>0) effects.Floating(allies[i].position,"VANGUARD −"+battle.LastPassiveProtection[i],Element.Light);
                    if(damage>0) {
                        var targetVisual=allies[i].GetComponent<HeroVisual>(); if(targetVisual!=null) targetVisual.Play("Hit");
                        effects.Attack(actor.position,allies[i].position,skill.Element,damage,1); sounds.PlayElement(skill.Element);
                        if(visual!=null) visual.ContactPause();
                        if(battle.Party[i].Status!=Debuff.None) effects.Floating(allies[i].position,battle.Party[i].Status.ToString().ToUpperInvariant(),skill.Element);
                    } else if((skill.PartyWide || i==target) && !strikeDefended[i] && battle.Party[i].Health>0) { effects.Floating(allies[i].position,"BLOCKED",Element.Light); sounds.Play("Guard"); }
                    if(battle.LastStatusDamage[i]>0) effects.Floating(allies[i].position,"DEBUFF −"+battle.LastStatusDamage[i],skill.Element);
                }
                if (art != null && art.IsBoss && battle.Enemies[foeIndex].Health > 0) { effects.BossImpact(allies[target].position,skill.Element); sounds.Play("BossSlam",.75f); }
                PresentPassiveHealing();
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
        string EnemyMechanicLabel(int index) {
            var foe=battle.Enemies[index];
            return (battle.EnemyProtected(index)?" · GUARDED":"") + (foe.Shield>0?" · SHIELD "+foe.Shield:"") + (foe.Charged?" · CHARGE "+foe.ChargeDamage+"/60":"");
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
            if ((tutorialRoot!=null && tutorialRoot.activeSelf) || atChapters || selectionRoot.activeSelf || equipmentRoot.activeSelf || formationRoot.activeSelf || progressionRoot.activeSelf || previewRoot.activeSelf || collectionRoot.activeSelf || teamPresetRoot.activeSelf || presentingAction) return;
            int actingIndex = battle.ActiveIndex, target = battle.SelectedEnemyIndex;
            var members = battle.Party; int[] healing = new int[members.Count];
            for (int i = 0; i < healing.Length; i++) healing[i] = members[i].Health;
            int oldGuard = members[actingIndex].Guard;
            for(int i=0;i<battle.Enemies.Count;i++) { actionEnemyBefore[i]=battle.Enemies[i].Health; shownEnemyDamage[i]=0; }
            actionHero=actingIndex; actionEnergy=Mathf.Min(PartyHero.MaxEnergy,members[actingIndex].Energy+members[actingIndex].EnergyGain);
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
            if (casting || kind == HeroClass.Ranger) effects.Windup(actor.position,foes[target].position,element,motion.Impact,kind == HeroClass.Ranger);
            while (Time.time < attackTime+motion.Impact-.10f) yield return null;
            if (!casting) sounds.Play(kind == HeroClass.Ranger ? "BowRelease" : kind == HeroClass.Rogue ? "DaggerSwipe" : kind == HeroClass.Paladin ? "HeavySwing" : "SwordSwing");
            while (Time.time < attackTime+motion.Impact) yield return null;
            if(!string.IsNullOrEmpty(battle.LastOffensivePassive)) effects.Floating(actor.position,battle.LastOffensivePassive,battle.LastElement);
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
                effects.UltimateCharge(actor.position,ultimate.Affinity,ultimateMotion.Impact);
                actionMotion=ultimateMotion; attackTime=ultimateAt; actionTarget=battle.UltimateTarget;
                var actorVisual=actor.GetComponent<HeroVisual>(); if(actorVisual!=null) actorVisual.Play(ultimateCast?"Cast":"Attack");
                while(Time.time<ultimateAt+ultimateMotion.Impact) yield return null;
                for(int i=0;i<battle.Enemies.Count;i++) if(battle.LastUltimateDamage[i]>0) {
                    float mult=battle.Enemies[i].Definition.Multiplier(ultimate.Affinity);
                    HeroImpact(actor,i,battle.LastUltimateDamage[i],ultimate.Affinity,mult,!ultimateCast);
                    effects.UltimateImpact(foes[i].position,ultimate.Affinity,ultimate.AreaDamage);
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
            upgradeButton=MakeButton(selectionRoot.transform,"UPGRADE",.55f,.95f,()=> { if(battle.UpgradeHero(battle.Party[battle.ActiveIndex].Identity.Id)) { SaveProgress(); RefreshSelection(); } });
            var upgradeRect=upgradeButton.GetComponent<RectTransform>(); upgradeRect.anchorMin=new Vector2(.70f,.02f); upgradeRect.anchorMax=new Vector2(.95f,.12f);
            var progression=MakeButton(selectionRoot.transform,"PROGRESSION",.35f,.65f,OpenProgression); PlaceGearControl(progression,.35f,.02f,.65f,.12f);
            var close = MakeButton(selectionRoot.transform, "DONE", .05f, .45f, CloseSelection);
            var cr = close.GetComponent<RectTransform>(); cr.anchorMin = new Vector2(.05f, .02f); cr.anchorMax = new Vector2(.30f, .12f);
            selectionRoot.SetActive(false);
        }
        void OpenSelection(bool skills, HeroClass kind) {
            if (atChapters || !battle.CanChangeParty) return;

            equipmentRoot.SetActive(false);
            selectingSkills = skills; browsingClass = kind; selectedSlot = 0;
            candidates.Clear();
            foreach (var candidate in HeroDefinition.Catalog) if (candidate.Class == kind) candidates.Add(candidate);
            selectionRoot.transform.SetAsLastSibling(); selectionRoot.SetActive(true); RefreshSelection();
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
            selectionTitle.text = selectingSkills ? member.Identity.Name + " / " + member.Identity.DisplayClass + " / " + member.RarityLabel + " / " + member.Identity.FactionName + " / " + member.Identity.Affinity + " — equip 2 of 6 skills\nSelect a slot, then a skill. Choices lock during battle." : browsingClass + " heroes\nSelect an ally, or replace the selected party slot before battle.";
            selectionTitle.text+="\nPASSIVE: "+ClassDefinition.PassiveDescription(member.Identity.Class);
            selectionTitle.text+="\n"+member.Identity.Name+" · Power "+member.Power+" · Lv "+member.LevelLabel+" · Own XP "+member.Experience+"/"+member.NextLevelXP+" · Ascension "+member.UpgradeRank+"/5 · Shards "+member.Shards;
            upgradeButton.interactable=battle.CanChangeParty && member.UpgradeRank<PartyHero.MaxUpgradeRank && member.Shards>=member.UpgradeCost;
            upgradeButton.GetComponentInChildren<Text>().text=member.UpgradeRank==PartyHero.MaxUpgradeRank?"ASCENSION MAX":"ASCEND → "+member.NextAscension+"\n"+member.Shards+"/"+member.UpgradeCost+" shards";
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
                    var progress=battle.HeroProgress(candidate.Id); var stats=progress==null?candidate.Stats:progress.Definition; var ult=progress==null?candidate.Ultimate:progress.Ultimate;
                    for (int j = 0; j < battle.Party.Count; j++) if (battle.Party[j].Identity.Id == candidate.Id) inParty = j;
                    choices[i].GetComponentInChildren<Text>(true).text = candidate.Name + " — " + candidate.Gender + " " + candidate.DisplayClass + "\n" + (progress==null?candidate.Quality.ToString():progress.RarityLabel) + " / " + candidate.FactionName + " / " + candidate.Affinity + "\nHP " + stats.MaxHealth + " | Attack " + stats.AttackDamage + " | Lv " + (progress==null?"1":progress.LevelLabel) + " / +" + (progress==null?0:progress.UpgradeRank) + "\nULT: " + ult.Description + "\n" + (Recruited(candidate.Id) ? inParty >= 0 ? "IN PARTY" : "RESERVE" : "NOT RECRUITED");
                    choices[i].interactable = Recruited(candidate.Id) && (inParty >= 0 ? battle.Current == Phase.Player && battle.Party[inParty].Health > 0 && !battle.Party[inParty].Acted : battle.CanChangeParty);
                }
            }
        }
        void CreateProgressionPanel() {
            progressionRoot=new GameObject("Hero Progression",typeof(RectTransform),typeof(Image));
            progressionRoot.transform.SetParent(safe,false);
            var rect=progressionRoot.GetComponent<RectTransform>(); rect.anchorMin=Vector2.zero; rect.anchorMax=Vector2.one; rect.offsetMin=rect.offsetMax=Vector2.zero;
            progressionRoot.GetComponent<Image>().color=new Color(.035f,.045f,.065f,.98f);
            progressionTitle=Label(progressionRoot.transform,"",new Vector2(.04f,.81f),new Vector2(.96f,.98f),25);
            for(int i=0;i<progressionHeroes.Length;i++) {
                int index=i; float left=.025f+i*.194f;
                progressionHeroes[i]=MakeButton(progressionRoot.transform,"",left,left+.18f,()=> { if(battle.SelectHero(index)) RefreshProgression(); });
                PlaceGearControl(progressionHeroes[i],left,.71f,left+.18f,.79f);
            }
            for(int i=0;i<progressionChoices.Length;i++) {
                int tier=i/2,choice=i%2; float left=choice==0?.05f:.52f; float bottom=.50f-tier*.18f;
                progressionChoices[i]=MakeButton(progressionRoot.transform,"",left,left+.43f,()=> {
                    if(battle.ChooseTalent(battle.Party[battle.ActiveIndex].Identity.Id,tier,choice)) { SaveProgress(); RefreshProgression(); }
                });
                PlaceGearControl(progressionChoices[i],left,bottom,left+.43f,bottom+.16f);
                progressionChoices[i].GetComponentInChildren<Text>().resizeTextMaxSize=22;
            }
            var reset=MakeButton(progressionRoot.transform,"FREE RESET",.05f,.45f,()=> { if(battle.ResetTalents(battle.Party[battle.ActiveIndex].Identity.Id)) { SaveProgress(); RefreshProgression(); } });
            PlaceGearControl(reset,.05f,.025f,.45f,.11f);
            var done=MakeButton(progressionRoot.transform,"DONE",.55f,.95f,()=>progressionRoot.SetActive(false)); PlaceGearControl(done,.55f,.025f,.95f,.11f);
            progressionRoot.SetActive(false);
        }
        void OpenProgression() {
            if(atChapters || !battle.CanChangeParty) return;
            selectionRoot.SetActive(false); equipmentRoot.SetActive(false); formationRoot.SetActive(false);
            progressionRoot.transform.SetAsLastSibling(); progressionRoot.SetActive(true); RefreshProgression();
        }
        void RefreshProgression() {
            var hero=battle.Party[battle.ActiveIndex];
            progressionTitle.text=hero.Identity.Name+" · "+hero.Identity.DisplayClass+" · LEVEL "+hero.Level+"\nChoose one upgrade at levels 5, 10 and 15. Changes and resets are free before combat.\nHP "+hero.Definition.MaxHealth+" · Energy gain "+hero.EnergyGain+" · Ultimate damage "+hero.Ultimate.Damage;
            for(int i=0;i<progressionHeroes.Length;i++) {
                progressionHeroes[i].gameObject.SetActive(i<battle.Party.Count);
                if(i<battle.Party.Count) progressionHeroes[i].GetComponentInChildren<Text>().text=(i==battle.ActiveIndex?"▶ ":"")+battle.Party[i].Identity.Name;
            }
            for(int i=0;i<progressionChoices.Length;i++) {
                int tier=i/2,choice=i%2; bool unlocked=hero.Level>=ClassTalents.UnlockLevel(tier),selected=hero.TalentChoice(tier)==choice;
                progressionChoices[i].interactable=battle.CanChangeParty && unlocked && !selected;
                progressionChoices[i].GetComponentInChildren<Text>().text="LEVEL "+ClassTalents.UnlockLevel(tier)+" · "+ClassTalents.Name(hero.Identity.Class,tier,choice)+"\n"+ClassTalents.Description(hero.Identity.Class,tier,choice)+"\n"+(selected?"SELECTED":unlocked?"CHOOSE":"LOCKED");
                progressionChoices[i].GetComponent<Image>().color=selected?new Color(.31f,.25f,.15f):new Color(.075f,.085f,.105f);
            }
        }
        void CreateFormationPanel() {
            formationRoot=new GameObject("Party Formation",typeof(RectTransform),typeof(Image));
            formationRoot.transform.SetParent(safe,false);
            var rect=formationRoot.GetComponent<RectTransform>(); rect.anchorMin=Vector2.zero; rect.anchorMax=Vector2.one; rect.offsetMin=rect.offsetMax=Vector2.zero;
            formationRoot.GetComponent<Image>().color=new Color(.035f,.045f,.065f,.98f);
            Label(formationRoot.transform,"FORMATION · 2 FRONT / 3 BACK\nTap a hero, then another hero to swap positions.",new Vector2(.05f,.90f),new Vector2(.95f,.98f),24);
            formationFaction=Label(formationRoot.transform,"",new Vector2(.05f,.8f),new Vector2(.95f,.89f),20);
            Label(formationRoot.transform,"FRONT · draws more normal attacks",new Vector2(.1f,.72f),new Vector2(.9f,.79f),20);
            Label(formationRoot.transform,"BACK · protected from most attacks; vulnerable to back-row strikes",new Vector2(.05f,.41f),new Vector2(.95f,.48f),19);
            for(int i=0;i<formationSlots.Length;i++) {
                int index=i; bool front=i<Battle.FrontSlots;
                float left=front?.20f+i*.32f:.045f+(i-Battle.FrontSlots)*.315f;
                float bottom=front?.50f:.18f;
                formationSlots[i]=MakeButton(formationRoot.transform,"",left,left+.28f,()=> {
                    if(!battle.CanChangeParty || index>=battle.Party.Count) return;
                    if(formationSelected<0) { formationSelected=index; battle.SelectHero(index); }
                    else {
                        if(battle.SwapFormation(formationSelected,index)) { RefreshAppearance(); SaveProgress(); }
                        formationSelected=-1;
                    }
                    RefreshFormation();
                });
                PlaceGearControl(formationSlots[i],left,bottom,left+.28f,bottom+.20f);
            }
            var presets=MakeButton(formationRoot.transform,"TEAM PRESETS",.05f,.45f,OpenTeamPresets); PlaceGearControl(presets,.05f,.035f,.45f,.12f);
            var done=MakeButton(formationRoot.transform,"DONE",.55f,.95f,()=>formationRoot.SetActive(false));
            PlaceGearControl(done,.55f,.035f,.95f,.12f);
            formationRoot.SetActive(false);
        }
        void OpenFormation() {
            if(atChapters || !battle.CanChangeParty) return;
            selectionRoot.SetActive(false); equipmentRoot.SetActive(false); formationSelected=-1;
            formationRoot.transform.SetAsLastSibling(); formationRoot.SetActive(true); RefreshFormation();
        }
        void RefreshFormation() {
            formationFaction.text="TEAM POWER "+battle.TeamPower+" · "+battle.FactionSummary+"\n5 matching: +20% HP & damage. Bonuses apply to the whole party.";
            for(int i=0;i<formationSlots.Length;i++) {
                var button=formationSlots[i]; bool occupied=i<battle.Party.Count;
                button.interactable=occupied && battle.CanChangeParty;
                button.GetComponentInChildren<Text>().text=(i<Battle.FrontSlots?"FRONT "+(i+1):"BACK "+(i-Battle.FrontSlots+1))+"\n"+
                    (occupied?battle.Party[i].Identity.Name+" · "+battle.Party[i].Identity.DisplayClass+" · "+battle.Party[i].Identity.FactionName+"\nHP "+battle.Party[i].Definition.MaxHealth+" · Attack "+battle.Party[i].Definition.AttackDamage:"Unlocks in chapter "+(i+1));
                button.GetComponent<Image>().color=formationSelected==i?new Color(.31f,.25f,.15f):new Color(.075f,.085f,.105f);
            }
        }
        void CreateResultsPanel() {
            resultsRoot=new GameObject("Battle Results",typeof(RectTransform),typeof(Image)); resultsRoot.transform.SetParent(safe,false);
            var rect=resultsRoot.GetComponent<RectTransform>(); rect.anchorMin=Vector2.zero; rect.anchorMax=Vector2.one; rect.offsetMin=rect.offsetMax=Vector2.zero;
            resultsRoot.GetComponent<Image>().color=new Color(.035f,.045f,.065f,.98f);
            resultsTitle=Label(resultsRoot.transform,"VICTORY",new Vector2(.06f,.82f),new Vector2(.94f,.97f),38);
            resultsRewards=Label(resultsRoot.transform,"",new Vector2(.06f,.64f),new Vector2(.94f,.81f),24);
            resultsLoot=Label(resultsRoot.transform,"",new Vector2(.06f,.49f),new Vector2(.94f,.63f),24);
            Label(resultsRoot.transform,"HERO CONTRIBUTIONS · DAMAGE / HEALING",new Vector2(.06f,.43f),new Vector2(.94f,.48f),19);
            for(int i=0;i<resultsHeroes.Length;i++) resultsHeroes[i]=Label(resultsRoot.transform,"",new Vector2(.06f,.365f-i*.055f),new Vector2(.94f,.418f-i*.055f),18);
            var claim=MakeButton(resultsRoot.transform,"CONTINUE · CLAIM REWARDS",.2f,.8f,EnterChapters); PlaceGearControl(claim,.2f,.04f,.8f,.145f);
            resultsRoot.SetActive(false);
        }
        void ShowResults() {
            Time.timeScale=1;
            var result=battle.Results; if(result==null) return;
            autoPlay=false; selectionRoot.SetActive(false); equipmentRoot.SetActive(false); formationRoot.SetActive(false); progressionRoot.SetActive(false);
            resultsTitle.text=battle.InTower?"VICTORY · TOWER FLOOR "+battle.TowerFloor:"VICTORY · CHAPTER "+(battle.ChapterIndex+1)+", STAGE "+(battle.StageIndex+1);
            resultsRewards.text="+"+result.Experience+" XP per participating hero · +"+result.Crystals+" crystals"+(result.RecruitName==null?"":"\nNew recruit: "+result.RecruitName)+(battle.InTower?"\n+"+battle.TowerShards+" shards per participating hero":"")+"\nPress Continue to claim these rewards.";
            resultsLoot.text=result.Loot==null?"No loot":"GEAR DROP\n"+result.Loot.Description;
            resultsLoot.color=result.Loot==null?Color.white:result.Loot.Quality==HeroQuality.Legendary?new Color(1,.8f,.25f):result.Loot.Quality==HeroQuality.Epic?new Color(.8f,.5f,1):result.Loot.Quality==HeroQuality.Rare?new Color(.4f,.75f,1):Color.white;
            for(int i=0;i<resultsHeroes.Length;i++) {
                resultsHeroes[i].gameObject.SetActive(i<result.Heroes.Count); if(i>=result.Heroes.Count) continue;
                var row=result.Heroes[i];
                resultsHeroes[i].text=row.Name+" · Damage "+row.Damage+" · Healing "+row.Healing+"\n"+(row.LevelAfter>row.LevelBefore?"LEVEL UP! "+row.LevelBefore+" → "+row.LevelAfter:"Level "+row.LevelAfter)+" · "+(row.LevelAfter==PartyHero.MaxLevel?"MAX LEVEL":"XP "+row.ExperienceAfter+"/"+(100+(row.LevelAfter-1)*25));
                resultsHeroes[i].color=row.LevelAfter>row.LevelBefore?new Color(1,.8f,.25f):Color.white;
            }
            resultsRoot.transform.SetAsLastSibling(); resultsRoot.SetActive(true); SaveProgress();
            ShowTutorial(2);
        }
        void PlaceGearControl(Button button,float left,float bottom,float right,float top) {
            var rect=button.GetComponent<RectTransform>(); rect.anchorMin=new Vector2(left,bottom); rect.anchorMax=new Vector2(right,top);
        }
        void CreateEquipmentPanel() {
            equipmentRoot=new GameObject("Equipment",typeof(RectTransform),typeof(Image)); equipmentRoot.transform.SetParent(safe,false);
            var rootRect=equipmentRoot.GetComponent<RectTransform>(); rootRect.anchorMin=Vector2.zero; rootRect.anchorMax=Vector2.one; rootRect.offsetMin=rootRect.offsetMax=Vector2.zero;
            equipmentRoot.GetComponent<Image>().color=new Color(.06f,.08f,.13f,.98f);
            equipmentTitle=Label(equipmentRoot.transform,"",new Vector2(.03f,.84f),new Vector2(.97f,.99f),22);
            for(int i=0;i<gearHeroes.Length;i++) {
                int index=i; float left=.025f+i*.194f;
                gearHeroes[i]=MakeButton(equipmentRoot.transform,"",left,left+.18f,()=> { if(battle.SelectHero(index)) RefreshEquipment(); });
                PlaceGearControl(gearHeroes[i],left,.755f,left+.18f,.825f);
            }
            for(int i=0;i<3;i++) {
                int index=i; float left=.04f+i*.32f;
                gearSlots[i]=MakeButton(equipmentRoot.transform,"",left,left+.28f,()=> { browsingSlot=(GearSlot)index; gearPage=0; RefreshEquipment(); });
                PlaceGearControl(gearSlots[i],left,.66f,left+.28f,.74f);
            }
            for(int i=0;i<6;i++) {
                int index=i; float left=i%2==0?.04f:.52f; float bottom=.475f-i/2*.16f;
                gearChoices[i]=MakeButton(equipmentRoot.transform,"",left,left+.44f,()=> { int entry=gearPage*6+index; if(entry<gearCandidates.Count && battle.EquipGear(battle.Party[battle.ActiveIndex].Identity.Id,browsingSlot,gearCandidates[entry].Id)) { SaveProgress(); RefreshEquipment(); } });
                PlaceGearControl(gearChoices[i],left,bottom,left+.44f,bottom+.14f);
            }
            gearPrevious=MakeButton(equipmentRoot.transform,"PREVIOUS",.04f,.25f,()=> { gearPage--; RefreshEquipment(); }); PlaceGearControl(gearPrevious,.04f,.035f,.25f,.12f);
            var unequip=MakeButton(equipmentRoot.transform,"UNEQUIP",.27f,.49f,()=> { if(battle.EquipGear(battle.Party[battle.ActiveIndex].Identity.Id,browsingSlot,null)) { SaveProgress(); RefreshEquipment(); } }); PlaceGearControl(unequip,.27f,.035f,.49f,.12f);
            gearNext=MakeButton(equipmentRoot.transform,"NEXT",.51f,.73f,()=> { gearPage++; RefreshEquipment(); }); PlaceGearControl(gearNext,.51f,.035f,.73f,.12f);
            var done=MakeButton(equipmentRoot.transform,"DONE",.75f,.96f,()=>equipmentRoot.SetActive(false)); PlaceGearControl(done,.75f,.035f,.96f,.12f);
            equipmentRoot.SetActive(false);
        }
        void OpenEquipment() {
            if(atChapters || !battle.CanChangeParty) return;
            selectionRoot.SetActive(false); gearPage=0; equipmentRoot.transform.SetAsLastSibling(); equipmentRoot.SetActive(true); RefreshEquipment();
        }
        void RefreshEquipment() {
            var member=battle.Party[battle.ActiveIndex]; gearCandidates.Clear();
            foreach(var item in GearDefinition.Catalog) if(item.Slot==browsingSlot && battle.GearCount(item.Id)>0) gearCandidates.Add(item);
            gearPage=Mathf.Clamp(gearPage,0,Mathf.Max(0,(gearCandidates.Count-1)/6));
            var equipped=member.Gear(browsingSlot);
            equipmentTitle.text=member.Identity.Name+" · EQUIPMENT · HP "+member.Definition.MaxHealth+" · Attack "+member.Definition.AttackDamage+"\n"+browsingSlot+": "+(equipped==null?"Empty":equipped.Description)+"\n"+(gearCandidates.Count==0?"Win battles and press Continue to collect gear.":"Page "+(gearPage+1)+" · Each owned copy can equip one hero, including reserves.");
            for(int i=0;i<gearHeroes.Length;i++) {
                gearHeroes[i].gameObject.SetActive(i<battle.Party.Count);
                if(i<battle.Party.Count) gearHeroes[i].GetComponentInChildren<Text>().text=(i==battle.ActiveIndex?"▶ ":"")+battle.Party[i].Identity.Name;
            }
            for(int i=0;i<3;i++) {
                var slot=(GearSlot)i; var item=member.Gear(slot);
                gearSlots[i].GetComponentInChildren<Text>().text=(slot==browsingSlot?"▶ ":"")+slot+"\n"+(item==null?"Empty":item.Quality+" "+item.Name);
            }
            for(int i=0;i<6;i++) {
                int entry=gearPage*6+i; gearChoices[i].gameObject.SetActive(entry<gearCandidates.Count);
                if(entry>=gearCandidates.Count) continue;
                var item=gearCandidates[entry]; int used=0;
                foreach(var hero in battle.Recruits) if(battle.HeroProgress(hero.Id).Gear(browsingSlot)==item) used++;
                gearChoices[i].GetComponentInChildren<Text>().text=item.Description+"\n"+(item==equipped?"EQUIPPED · ":"")+"Available "+(battle.GearCount(item.Id)-used)+" / Owned "+battle.GearCount(item.Id);
                gearChoices[i].interactable=item!=equipped && used<battle.GearCount(item.Id);
            }
            gearPrevious.interactable=gearPage>0; gearNext.interactable=(gearPage+1)*6<gearCandidates.Count;
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
        void CreateTeamPresets() {
            teamPresetRoot=new GameObject("Team Presets",typeof(RectTransform),typeof(Image)); teamPresetRoot.transform.SetParent(safe,false);
            var rect=teamPresetRoot.GetComponent<RectTransform>(); rect.anchorMin=Vector2.zero; rect.anchorMax=Vector2.one; rect.offsetMin=rect.offsetMax=Vector2.zero; teamPresetRoot.GetComponent<Image>().color=new Color(.035f,.045f,.065f,.98f);
            teamPresetTitle=Label(teamPresetRoot.transform,"",new Vector2(.05f,.83f),new Vector2(.95f,.98f),23); teamPresetLabels=new Text[3]; teamPresetLoads=new Button[3]; teamPresetClears=new Button[3];
            for(int i=0;i<3;i++) { int index=i; float y=.61f-i*.22f; teamPresetLabels[i]=Label(teamPresetRoot.transform,"",new Vector2(.05f,y+.09f),new Vector2(.95f,y+.20f),20);
                var save=MakeButton(teamPresetRoot.transform,"SAVE CURRENT",.05f,.28f,()=> { if(battle.SaveTeamPreset(index)) { presetNotice="Preset "+(index+1)+" saved."; SaveProgress(); RefreshTeamPresets(); } }); PlaceGearControl(save,.05f,y,.28f,y+.08f); save.GetComponentInChildren<Text>().fontSize=18;
                teamPresetLoads[i]=MakeButton(teamPresetRoot.transform,"LOAD",.31f,.65f,()=> { if(battle.LoadTeamPreset(index)) { formationSelected=-1; presetNotice="Preset "+(index+1)+" loaded for "+battle.PartyLimit+" available slots."; RefreshAppearance(); SaveProgress(); RefreshTeamPresets(); } }); PlaceGearControl(teamPresetLoads[i],.31f,y,.65f,y+.08f);
                teamPresetClears[i]=MakeButton(teamPresetRoot.transform,"CLEAR",.68f,.95f,()=> { if(battle.ClearTeamPreset(index)) { presetNotice="Preset "+(index+1)+" cleared."; SaveProgress(); RefreshTeamPresets(); } }); PlaceGearControl(teamPresetClears[i],.68f,y,.95f,y+.08f);
            }
            var done=MakeButton(teamPresetRoot.transform,"BACK TO FORMATION",.25f,.75f,()=> { teamPresetRoot.SetActive(false); formationRoot.transform.SetAsLastSibling(); formationRoot.SetActive(true); RefreshFormation(); }); PlaceGearControl(done,.25f,.025f,.75f,.11f); teamPresetRoot.SetActive(false);
        }
        void OpenTeamPresets() { if(atChapters || !battle.CanChangeParty) return; formationRoot.SetActive(false); presetNotice="Load uses unlocked slots; remaining spaces fill from your roster."; teamPresetRoot.transform.SetAsLastSibling(); teamPresetRoot.SetActive(true); RefreshTeamPresets(); }
        void RefreshTeamPresets() {
            teamPresetTitle.text="TEAM PRESETS · "+battle.PartyLimit+" available slots\n"+presetNotice;
            for(int i=0;i<3;i++) { var ids=battle.TeamPreset(i); string text="PRESET "+(i+1)+" · "+ids.Length+" heroes\n";
                for(int slot=0;slot<ids.Length;slot++) text+=(slot<2?"F: ":"B: ")+battle.HeroProgress(ids[slot]).Identity.Name+(slot<ids.Length-1?" · ":"");
                teamPresetLabels[i].text=text+(ids.Length==0?"Empty · Save your current party here.":""); teamPresetLoads[i].interactable=ids.Length>0 && battle.CanChangeParty; teamPresetClears[i].interactable=ids.Length>0;
            }
        }
        void CreateDaily() {
            dailyRoot=new GameObject("Daily Quests",typeof(RectTransform),typeof(Image)); dailyRoot.transform.SetParent(safe,false);
            var rect=dailyRoot.GetComponent<RectTransform>(); rect.anchorMin=Vector2.zero; rect.anchorMax=Vector2.one; rect.offsetMin=rect.offsetMax=Vector2.zero; dailyRoot.GetComponent<Image>().color=new Color(.035f,.045f,.065f,.98f);
            Label(dailyRoot.transform,"DAILY QUESTS\nResets at 00:00 UTC · Claim rewards before the next day.",new Vector2(.05f,.82f),new Vector2(.95f,.98f),25);
            dailyButtons=new Button[4];
            for(int i=0;i<4;i++) { int quest=i; float y=.64f-i*.16f; dailyButtons[i]=MakeButton(dailyRoot.transform,"",.05f,.95f,()=> { if(battle.ClaimDaily(quest)) { SaveProgress(); RefreshDailyMenu(); } }); PlaceGearControl(dailyButtons[i],.05f,y,.95f,y+.14f); dailyButtons[i].GetComponentInChildren<Text>().fontSize=22; }
            var done=MakeButton(dailyRoot.transform,"DONE",.7f,.95f,()=> { dailyRoot.SetActive(false); chapterTitle.text="ASHLIGHT — Chapters\n"+SummonWallet()+" | "+saveNotice; }); PlaceGearControl(done,.7f,.025f,.95f,.115f); dailyRoot.SetActive(false);
            var open=MakeButton(chapterRoot.transform,"DAILY QUESTS",.76f,.98f,()=> { dailyRoot.transform.SetAsLastSibling(); dailyRoot.SetActive(true); RefreshDailyMenu(); }); PlaceGearControl(open,.76f,.025f,.98f,.135f);
        }
        void RefreshDailyMenu() {
            string[] names={"WIN 3 BATTLES","CLAIM IDLE REWARDS","SUMMON A HERO","COMPLETE ALL THREE"}; string[] rewards={"40 crystals + 10 Rowan shards","20 crystals + 5 Rowan shards","30 crystals + 10 Rowan shards","100 crystals + 10 shards for every recruited hero"};
            for(int i=0;i<4;i++) { bool claimed=battle.DailyClaimed(i), ready=battle.CanClaimDaily(i); dailyButtons[i].interactable=ready; dailyButtons[i].GetComponentInChildren<Text>().text=names[i]+(i<3?" · "+battle.DailyProgress(i)+"/"+(i==0?3:1):"")+"\n"+rewards[i]+"\n"+(claimed?"CLAIMED":ready?"CLAIM":"IN PROGRESS"); }
        }
        void CreateCollection() {
            collectionRoot=new GameObject("Hero Collection",typeof(RectTransform),typeof(Image)); collectionRoot.transform.SetParent(safe,false);
            var rect=collectionRoot.GetComponent<RectTransform>(); rect.anchorMin=Vector2.zero; rect.anchorMax=Vector2.one; rect.offsetMin=rect.offsetMax=Vector2.zero; collectionRoot.GetComponent<Image>().color=new Color(.035f,.045f,.065f,.98f);
            collectionTitle=Label(collectionRoot.transform,"",new Vector2(.04f,.82f),new Vector2(.96f,.98f),20);
            collectionFilters=new Button[4];
            for(int i=0;i<4;i++) { int filter=i; float x=.04f+i*.235f; collectionFilters[i]=MakeButton(collectionRoot.transform,"",x,x+.215f,()=> { if(filter==0) classFilter=(classFilter+2)%7-1; else if(filter==1) factionFilter=(factionFilter+2)%4-1; else if(filter==2) rarityFilter=(rarityFilter+2)%6-1; else collectionSort=(CollectionSort)(((int)collectionSort+1)%3); collectionPage=0; collectionHero=null; RefreshCollection(); }); PlaceGearControl(collectionFilters[i],x,.735f,x+.215f,.80f); collectionFilters[i].GetComponentInChildren<Text>().fontSize=17; }
            collectionCompare=Label(collectionRoot.transform,"",new Vector2(.04f,.62f),new Vector2(.96f,.72f),19);
            collectionCards=new Button[4];
            for(int i=0;i<4;i++) { int index=i; float x=i%2==0?.05f:.52f,y=.45f-(i/2)*.17f; collectionCards[i]=MakeButton(collectionRoot.transform,"",x,x+.43f,()=> { int entry=collectionPage*4+index; if(entry<collectionItems.Count) { collectionHero=collectionItems[entry].Identity.Id; RefreshCollection(); } }); PlaceGearControl(collectionCards[i],x,y,x+.43f,y+.16f); collectionCards[i].GetComponentInChildren<Text>().fontSize=18; }
            collectionTargetButton=MakeButton(collectionRoot.transform,"",.04f,.36f,()=> { collectionTarget=(collectionTarget+1)%battle.Party.Count; RefreshCollection(); }); PlaceGearControl(collectionTargetButton,.04f,.18f,.36f,.26f); collectionTargetButton.GetComponentInChildren<Text>().fontSize=18;
            collectionAssign=MakeButton(collectionRoot.transform,"ASSIGN",.40f,.72f,()=> { if(battle.EquipHeroAt(collectionHero,collectionTarget)) { RefreshAppearance(); SaveProgress(); RefreshCollection(); } }); PlaceGearControl(collectionAssign,.40f,.18f,.72f,.26f); collectionAssign.GetComponentInChildren<Text>().fontSize=18;
            var prev=MakeButton(collectionRoot.transform,"<",.76f,.85f,()=> { collectionPage=System.Math.Max(0,collectionPage-1); collectionHero=null; RefreshCollection(); }); PlaceGearControl(prev,.76f,.18f,.85f,.26f);
            var next=MakeButton(collectionRoot.transform,">",.87f,.96f,()=> { collectionPage=System.Math.Min(System.Math.Max(0,(collectionItems.Count-1)/4),collectionPage+1); collectionHero=null; RefreshCollection(); }); PlaceGearControl(next,.87f,.18f,.96f,.26f);
            collectionShared=MakeButton(collectionRoot.transform,"LINK",.04f,.30f,()=> { if(battle.SetShared(collectionHero,!battle.IsShared(collectionHero))) { SaveProgress(); RefreshCollection(); } }); PlaceGearControl(collectionShared,.04f,.025f,.30f,.12f); collectionShared.GetComponentInChildren<Text>().fontSize=17;
            collectionAscend=MakeButton(collectionRoot.transform,"ASCEND",.33f,.70f,()=> { if(battle.UpgradeHero(collectionHero)) { SaveProgress(); RefreshCollection(); } }); PlaceGearControl(collectionAscend,.33f,.025f,.70f,.12f); collectionAscend.GetComponentInChildren<Text>().fontSize=18;
            var done=MakeButton(collectionRoot.transform,"DONE",.74f,.96f,()=>collectionRoot.SetActive(false)); PlaceGearControl(done,.74f,.025f,.96f,.12f); collectionRoot.SetActive(false);
            var open=MakeButton(chapterRoot.transform,"COLLECTION",.4f,.73f,OpenCollection); PlaceGearControl(open,.4f,.025f,.73f,.135f);
            collectionBattleButton=MakeButton(safe,"COLLECTION",.25f,.46f,OpenCollection); PlaceGearControl(collectionBattleButton,.25f,.625f,.46f,.72f);
        }
        void OpenCollection() {
            if(!atChapters && !battle.CanChangeParty) return;
            selectionRoot.SetActive(false); equipmentRoot.SetActive(false); formationRoot.SetActive(false); progressionRoot.SetActive(false); previewRoot.SetActive(false);
            collectionPage=0; collectionHero=null; collectionTarget=System.Math.Min(battle.ActiveIndex,battle.Party.Count-1); collectionRoot.transform.SetAsLastSibling(); collectionRoot.SetActive(true); RefreshCollection();
        }
        static string Signed(int value) { return value>0?"+"+value:value.ToString(); }
        void RefreshCollection() {
            collectionItems=battle.Collection(classFilter<0?(HeroClass?)null:(HeroClass)classFilter,factionFilter<0?(HeroFaction?)null:(HeroFaction)factionFilter,rarityFilter<0?(HeroQuality?)null:(HeroQuality)rarityFilter,collectionSort);
            collectionPage=System.Math.Min(collectionPage,System.Math.Max(0,(collectionItems.Count-1)/4));
            bool onPage=false; for(int i=collectionPage*4;i<System.Math.Min(collectionItems.Count,collectionPage*4+4);i++) if(collectionItems[i].Identity.Id==collectionHero) onPage=true;
            if(!onPage) collectionHero=collectionItems.Count>0?collectionItems[collectionPage*4].Identity.Id:null;
            collectionFilters[0].GetComponentInChildren<Text>().text="CLASS · "+(classFilter<0?"ALL":((HeroClass)classFilter).ToString()); collectionFilters[1].GetComponentInChildren<Text>().text="FACTION · "+(factionFilter<0?"ALL":HeroDefinition.FactionDisplay((HeroFaction)factionFilter)); collectionFilters[2].GetComponentInChildren<Text>().text="RARITY · "+(rarityFilter<0?"ALL":((HeroQuality)rarityFilter).ToString()); collectionFilters[3].GetComponentInChildren<Text>().text="SORT · "+collectionSort;
            var hero=collectionHero==null?null:battle.HeroProgress(collectionHero); var preview=hero==null?null:battle.PreviewReplacement(collectionHero,collectionTarget); var target=battle.Party[collectionTarget];
            collectionTitle.text="COLLECTION · "+collectionItems.Count+" matches · Page "+(collectionPage+1)+" · Shared Lv "+battle.CrystalLevel+" · "+battle.SharedCount+"/5 slots"+(hero==null?"\nNo heroes match. Cycle a filter to ALL.":"\n"+hero.Identity.Name+" · "+hero.RarityLabel+" · "+hero.Identity.FactionName+" · "+hero.Identity.DisplayClass+"\nLv "+hero.LevelLabel+" · HP "+hero.Definition.MaxHealth+" · ATK "+hero.Definition.AttackDamage+" · Power "+hero.Power+"\nULT: "+hero.Ultimate.Description);
            collectionTargetButton.GetComponentInChildren<Text>().text="TARGET "+(collectionTarget+1)+" · "+(battle.IsFrontRow(collectionTarget)?"FRONT":"BACK")+"\n"+target.Identity.Name;
            collectionCompare.text=hero==null?"":preview==null?"Already in party · use Formation to swap positions.":"Replace "+target.Identity.Name+" → "+hero.Identity.Name+" · HP "+Signed(preview.HealthDelta)+" · ATK "+Signed(preview.AttackDelta)+"\nTeam Power "+battle.TeamPower+" → "+preview.TeamPower+" ("+Signed(preview.PowerDelta)+") · Includes faction changes.";
            collectionAssign.interactable=preview!=null && battle.CanChangeParty; collectionAssign.GetComponentInChildren<Text>().text=!battle.CanChangeParty?"START A FIGHT TO PREPARE":"ASSIGN TO SLOT "+(collectionTarget+1);
            collectionShared.interactable=hero!=null && (battle.IsShared(collectionHero) || (battle.CrystalUnlocked && battle.SharedCount<5 && hero.Level<battle.CrystalLevel)); collectionShared.GetComponentInChildren<Text>().text=battle.IsShared(collectionHero)?"UNLINK":battle.CrystalUnlocked?"LINK · LV "+battle.CrystalLevel:"OWN 5 TO UNLOCK";
            collectionAscend.interactable=hero!=null && hero.UpgradeRank<5 && hero.Shards>=hero.UpgradeCost; collectionAscend.GetComponentInChildren<Text>().text=hero==null?"ASCEND":hero.UpgradeRank==5?"ASCENSION MAX":"ASCEND → "+hero.NextAscension+"\n"+hero.Shards+"/"+hero.UpgradeCost+" shards";
            for(int i=0;i<4;i++) { int entry=collectionPage*4+i; collectionCards[i].gameObject.SetActive(entry<collectionItems.Count); if(entry>=collectionItems.Count) continue; var member=collectionItems[entry]; bool active=false; foreach(var ally in battle.Party) if(ally==member) active=true;
                collectionCards[i].GetComponentInChildren<Text>().text=member.Identity.Name+" · "+member.RarityLabel+"\n"+member.Identity.DisplayClass+" · "+member.Identity.FactionName+"\nLv "+member.LevelLabel+" · Power "+member.Power+"\n"+(active?"PARTY":"RESERVE")+(battle.IsShared(member.Identity.Id)?" · LINKED":"")+(member.UpgradeRank<5 && member.Shards>=member.UpgradeCost?" · CAN ASCEND":""); collectionCards[i].GetComponent<Image>().color=member.Identity.Id==collectionHero?new Color(.28f,.23f,.14f):new Color(.08f,.1f,.15f);
            }
        }
        void CreateStagePreview() {
            previewRoot=new GameObject("Stage Preview",typeof(RectTransform),typeof(Image));
            previewRoot.transform.SetParent(safe,false);
            var rect=previewRoot.GetComponent<RectTransform>(); rect.anchorMin=Vector2.zero; rect.anchorMax=Vector2.one; rect.offsetMin=rect.offsetMax=Vector2.zero;
            previewRoot.GetComponent<Image>().color=new Color(.035f,.045f,.065f,.98f);
            Label(previewRoot.transform,"UPCOMING ENEMIES",new Vector2(.05f,.88f),new Vector2(.95f,.98f),28);
            previewText=Label(previewRoot.transform,"",new Vector2(.04f,.28f),new Vector2(.96f,.86f),21);
            Label(previewRoot.transform,"Defender: protects rear allies · Archer: targets your back row\nCaster: elemental spells · Healer: restores allies · Support: shields allies",new Vector2(.04f,.14f),new Vector2(.96f,.26f),18);
            var done=MakeButton(previewRoot.transform,"PREPARE PARTY",.25f,.75f,()=>previewRoot.SetActive(false)); PlaceGearControl(done,.25f,.025f,.75f,.12f);
            previewRoot.SetActive(false);
            previewButton=MakeButton(safe,"ENEMY PREVIEW",.02f,.23f,OpenStagePreview); PlaceGearControl(previewButton,.02f,.625f,.23f,.72f);
        }
        void OpenStagePreview() {
            if(atChapters || !battle.CanChangeParty) return;
            selectionRoot.SetActive(false); equipmentRoot.SetActive(false); formationRoot.SetActive(false); progressionRoot.SetActive(false);
            previewText.text=(battle.InTower?"TOWER FLOOR "+battle.TowerFloor+" · "+ChapterDefinition.Catalog[battle.ChapterIndex].Name:"Chapter "+(battle.ChapterIndex+1)+" · Stage "+(battle.StageIndex+1))+"\nTeam "+battle.TeamPower+" vs Stage "+battle.StagePower+" · "+battle.Difficulty+"\nEstimate only · elements and formation matter.\n\n"+ChapterDefinition.Preview(battle.EnemyEncounter,battle.TowerPowerScale);
            previewRoot.transform.SetAsLastSibling(); previewRoot.SetActive(true);
        }
        void CreateChapterMenu() {
            chapterRoot=new GameObject("Chapter Menu",typeof(RectTransform),typeof(Image)); chapterRoot.transform.SetParent(safe,false);
            var rect=chapterRoot.GetComponent<RectTransform>(); rect.anchorMin=Vector2.zero; rect.anchorMax=Vector2.one; rect.offsetMin=rect.offsetMax=Vector2.zero;
            chapterRoot.GetComponent<Image>().color=new Color(.06f,.08f,.13f,.98f);
            chapterTitle=Label(chapterRoot.transform,"",new Vector2(.03f,.8f),new Vector2(.97f,.98f),30);
            towerButton=MakeButton(chapterRoot.transform,"TOWER",.05f,.78f,()=> { if(battle.EnterTower()) { RefreshAppearance(); StartChapter(battle.ChapterIndex); SaveProgress(); } }); PlaceGearControl(towerButton,.05f,.67f,.78f,.72f); towerButton.GetComponentInChildren<Text>().fontSize=18;
            soundButton=MakeButton(chapterRoot.transform,"SOUND ON",.80f,.97f,()=>sounds.ToggleMute());
            var soundRect=soundButton.GetComponent<RectTransform>(); soundRect.anchorMin=new Vector2(.80f,.747f); soundRect.anchorMax=new Vector2(.97f,.795f);
            idleRewardsButton=MakeButton(chapterRoot.transform,"IDLE REWARDS",.05f,.78f,ClaimIdleRewards); PlaceGearControl(idleRewardsButton,.05f,.735f,.78f,.795f); idleRewardsButton.GetComponentInChildren<Text>(true).fontSize=18;
            chapterChoices=new Button[ChapterDefinition.Catalog.Count];
            for(int i=0;i<chapterChoices.Length;i++) {
                int index=i; float left=i%2==0?.05f:.52f; float bottom=.49f-(i/2)*.16f;
                chapterChoices[i]=MakeButton(chapterRoot.transform,"",left,left+.43f,()=>StartChapter(index));
                var r=chapterChoices[i].GetComponent<RectTransform>(); r.anchorMin=new Vector2(left,bottom); r.anchorMax=new Vector2(left+.43f,bottom+.14f);
            }
            summonButton=MakeButton(chapterRoot.transform,"SUMMON x1",.05f,.20f,SummonHero);
            var sr=summonButton.GetComponent<RectTransform>(); sr.anchorMin=new Vector2(.05f,.025f); sr.anchorMax=new Vector2(.20f,.135f);
            summonButton.GetComponentInChildren<Text>().fontSize=18;
            summonTenButton=MakeButton(chapterRoot.transform,"SUMMON x10\n1,000",.22f,.37f,()=>SummonHeroes(10)); PlaceGearControl(summonTenButton,.22f,.025f,.37f,.135f); summonTenButton.GetComponentInChildren<Text>().fontSize=18;
            restartCampaignButton=MakeButton(chapterRoot.transform,"RESTART CAMPAIGN\nKeep heroes, skills & crystals",.24f,.76f,RestartCampaign);
            var replayRect=restartCampaignButton.GetComponent<RectTransform>();
            replayRect.anchorMin=new Vector2(.24f,.43f); replayRect.anchorMax=new Vector2(.76f,.61f);
            restartCampaignButton.gameObject.SetActive(false);
        }
        void ClaimIdleRewards() {
            if(!atChapters) return;
            int crystals=battle.IdleCrystals, xp=battle.IdleExperience;
            if(!battle.ClaimIdleRewards()) return;
            RefreshAppearance(); SaveProgress();
            chapterTitle.color=Color.white;
            chapterTitle.text="IDLE REWARDS CLAIMED\n+"+crystals+" crystals · +"+xp+" XP to each recruited hero\n"+SummonWallet()+" | "+saveNotice;
        }
        void RestartCampaign() {
            if(!atChapters || !battle.RestartCampaign()) return;
            RefreshAppearance();
            EnterChapters();
        }
        string SummonWallet() {
            return "Crystals "+battle.Crystals+" | Rare+ in "+(Summoning.RareGuarantee-battle.RareMisses)+" | Legendary in "+(Summoning.LegendaryGuarantee-battle.LegendaryMisses);
        }
        void CreateSummonResults() {
            summonRoot=new GameObject("Summon Results",typeof(RectTransform),typeof(Image)); summonRoot.transform.SetParent(safe,false);
            var rect=summonRoot.GetComponent<RectTransform>(); rect.anchorMin=Vector2.zero; rect.anchorMax=Vector2.one; rect.offsetMin=rect.offsetMax=Vector2.zero; summonRoot.GetComponent<Image>().color=new Color(.035f,.045f,.065f,.98f);
            summonTitle=Label(summonRoot.transform,"",new Vector2(.04f,.83f),new Vector2(.96f,.98f),23); summonCards=new Text[10]; summonCardPanels=new GameObject[10];
            for(int i=0;i<10;i++) { float x=i%2==0?.05f:.52f, y=.70f-(i/2)*.115f; Panel(summonRoot.transform,new Vector2(x,y),new Vector2(x+.43f,y+.10f),new Color(.08f,.10f,.15f)); summonCardPanels[i]=summonRoot.transform.GetChild(summonRoot.transform.childCount-1).gameObject; summonCards[i]=Label(summonRoot.transform,"",new Vector2(x+.01f,y),new Vector2(x+.42f,y+.10f),18); }
            Label(summonRoot.transform,Summoning.Odds+"\nRare+ within 5 pulls · Legendary within 15 · Guarantees carry between batches.",new Vector2(.04f,.14f),new Vector2(.96f,.22f),17);
            repeatSingle=MakeButton(summonRoot.transform,"SUMMON x1 · 100",.05f,.35f,()=>SummonHeroes(1)); PlaceGearControl(repeatSingle,.05f,.025f,.35f,.12f);
            repeatTen=MakeButton(summonRoot.transform,"SUMMON x10 · 1,000",.38f,.68f,()=>SummonHeroes(10)); PlaceGearControl(repeatTen,.38f,.025f,.68f,.12f);
            var done=MakeButton(summonRoot.transform,"DONE",.73f,.95f,()=> { summonRoot.SetActive(false); chapterTitle.color=Color.white; chapterTitle.text="ASHLIGHT — Chapters\n"+SummonWallet()+" | "+saveNotice; }); PlaceGearControl(done,.73f,.025f,.95f,.12f); summonRoot.SetActive(false);
        }
        void SummonHero() { SummonHeroes(1); }
        void SummonHeroes(int count) {
            if(!atChapters || (count==10?!battle.CanSummonTen:!battle.CanSummon)) return;
            SummonResult[] results;
            if(count==10) { var rolls=new double[10]; var classes=new int[10]; for(int i=0;i<10;i++) { rolls[i]=summonRandom.NextDouble(); classes[i]=summonRandom.Next(6); } results=battle.SummonTen(rolls,classes); }
            else { var result=battle.Summon(summonRandom.NextDouble(),summonRandom.Next(6)); if(result==null) return; results=new[]{result}; }
            if(results==null) return; SaveProgress(); int refunds=0,added=0;
            for(int i=0;i<10;i++) { summonCards[i].gameObject.SetActive(i<results.Length); summonCardPanels[i].SetActive(i<results.Length); if(i>=results.Length) continue;
                float x=i%2==0?.05f:.52f, y=.70f-(i/2)*.115f; var cardRect=summonCardPanels[i].GetComponent<RectTransform>(); var textRect=summonCards[i].GetComponent<RectTransform>(); cardRect.anchorMin=new Vector2(count==1?.2f:x,count==1?.35f:y); cardRect.anchorMax=new Vector2(count==1?.8f:x+.43f,count==1?.70f:y+.10f); textRect.anchorMin=cardRect.anchorMin+Vector2.right*.01f; textRect.anchorMax=cardRect.anchorMax-Vector2.right*.01f; summonCards[i].fontSize=count==1?28:18; var result=results[i]; var hero=result.Hero; refunds+=result.Refund; if(!result.Duplicate) added++;
                summonCards[i].text=hero.Name+" · "+hero.Quality+"\n"+hero.DisplayClass+" · "+hero.FactionName+"\n"+(result.Duplicate?"DUPLICATE · +"+result.Refund+" crystals · +"+result.Shards+" shards":"NEW HERO · added to reserves");
                summonCards[i].color=hero.Quality==HeroQuality.Legendary?new Color(1,.8f,.25f):hero.Quality==HeroQuality.Epic?new Color(.8f,.5f,1):hero.Quality==HeroQuality.Rare?new Color(.4f,.75f,1):Color.white;
            }
            summonTitle.text="SUMMON RESULTS · "+count+" pulls · "+added+" new heroes\nSpent "+(count*Summoning.Cost)+" · Refunded "+refunds+"\n"+SummonWallet()+" | "+saveNotice;
            summonRoot.transform.SetAsLastSibling(); summonRoot.SetActive(true);
            ShowTutorial(4);
        }
        void StartChapter(int index) {
            if(battle.CampaignComplete || index!=battle.ChapterIndex) return;
            collectionRoot.SetActive(false); dailyRoot.SetActive(false); summonRoot.SetActive(false);
            CancelActionPresentation(); sounds.Clear(); effects.Clear();
            atChapters=false; chapterRoot.SetActive(false); battleStage.gameObject.SetActive(true);
            if(stageChapter!=battle.ChapterIndex) {
                if(chapterStage!=null) { chapterStage.gameObject.SetActive(false); Destroy(chapterStage.gameObject); }
                chapterStage=DarkFantasyStage.Create(battleStage,battleCamera,battle.ChapterIndex); stageChapter=battle.ChapterIndex;
            }
            DarkFantasyStage.Frame(battleCamera);
            CreateEnemy();
            message=(battle.InTower?"Tower floor "+battle.TowerFloor:"Chapter "+(battle.ChapterIndex+1)+", stage "+(battle.StageIndex+1))+": choose your heroes, then attack.";
            OpenStagePreview();
            ShowTutorial(1);
        }
        void EnterChapters() {
            Time.timeScale=1;
            resultsRoot.SetActive(false);
            CancelActionPresentation();
            if(enemyTurn!=null) StopCoroutine(enemyTurn);
            enemyTurn=null;
            string reward="";
            bool claimedVictory=battle.Current==Phase.Won;
            if(battle.Current==Phase.Won) {
                reward="Party gained "+battle.Results.Experience+" XP each! ";
                int previous=battle.Recruits.Count;
                battle.ContinueAfterVictory(); RefreshAppearance();
                if(battle.LastLoot!=null) reward+="Loot: "+battle.LastLoot.Description+". ";
                if(battle.Recruits.Count>previous) reward+=battle.Recruits[battle.Recruits.Count-1].Name+" recruited! ";
            } else if(!battle.CanChangeParty && !battle.CampaignComplete) battle.Reset();
            if(battle.InTower) { battle.LeaveTower(); RefreshAppearance(); }
            effects.Clear();
            atChapters=true; attackTime=-10; feedbackUntil=0; parried=false;
            selectionRoot.SetActive(false); formationRoot.SetActive(false); progressionRoot.SetActive(false); battleStage.gameObject.SetActive(false); chapterRoot.SetActive(true);
            SaveProgress();
            chapterTitle.color=Color.white;
            chapterTitle.text="ASHLIGHT — Chapters\n"+reward+(battle.CampaignComplete?"All six chapters completed!":"Continue your journey.")+"\n"+SummonWallet()+" | "+saveNotice;
            restartCampaignButton.gameObject.SetActive(battle.CampaignComplete);
            for(int i=0;i<chapterChoices.Length;i++) {
                chapterChoices[i].gameObject.SetActive(!battle.CampaignComplete);
                var chapter=ChapterDefinition.Catalog[i];
                string state=battle.CampaignComplete || i<battle.ChapterIndex?"COMPLETED":i>battle.ChapterIndex?"LOCKED":"Stage "+(battle.StageIndex+1)+"/5 · 3 waves: "+ChapterDefinition.EnemyAt(battle.Encounter).Name;
                chapterChoices[i].GetComponentInChildren<Text>(true).text="CHAPTER "+(i+1)+" — "+chapter.Name+" · "+Mathf.Min(Battle.MaxPartySize,i+1)+" hero slots\n"+state;
                chapterChoices[i].interactable=!battle.CampaignComplete && i==battle.ChapterIndex;
            }
            if(claimedVictory) ShowTutorial(3);
        }
        void Restart() {
            CancelActionPresentation();
            if(battle.Current==Phase.Won || battle.Current==Phase.Lost) { EnterChapters(); return; }
            if(enemyTurn!=null) StopCoroutine(enemyTurn);
            effects.Clear();
            enemyTurn=null; battle.Reset(); attackTime=-10; feedbackUntil=0; parried=false;
            foreach (var foe in foes) if (foe != null) foe.GetComponent<EnemyPresentation>().Warning(false); message="Stage restarted. Choose an action."; SaveProgress();
        }
        void ApplyBattleSpeed() {
            bool combat=!atChapters && (presentingAction || enemyTurn!=null ||
                battle.Current==Phase.Player || battle.Current==Phase.EnemyWindup || battle.Current==Phase.EnemyStrike || battle.Current==Phase.WaveCleared);
            bool menu=(tutorialRoot!=null && tutorialRoot.activeSelf) || (selectionRoot!=null && selectionRoot.activeSelf) || (equipmentRoot!=null && equipmentRoot.activeSelf) || (resultsRoot!=null && resultsRoot.activeSelf) || (formationRoot!=null && formationRoot.activeSelf) || (progressionRoot!=null && progressionRoot.activeSelf) || (previewRoot!=null && previewRoot.activeSelf) || (collectionRoot!=null && collectionRoot.activeSelf) || (teamPresetRoot!=null && teamPresetRoot.activeSelf);
            Time.timeScale=combat && !menu?battleSpeed:1;
        }
        void OnDisable() { Time.timeScale=1; }
        void Update() {
            if (status == null) return;
            ApplyBattleSpeed();
            var area = Screen.safeArea;
            safe.anchorMin = new Vector2(area.xMin / Mathf.Max(1, Screen.width), area.yMin / Mathf.Max(1, Screen.height));
            safe.anchorMax = new Vector2(area.xMax / Mathf.Max(1, Screen.width), area.yMax / Mathf.Max(1, Screen.height));
            var party = battle.Party;
            hero = allies[battle.ActiveIndex];
            int visibleTarget = presentingAction ? actionTarget : battle.SelectedEnemyIndex;
            var visibleEnemy = battle.Enemies[visibleTarget].Definition;
            status.text = (battle.InTower ? "TOWER " + battle.TowerFloor : "ASHLIGHT") + " · WAVE " + (battle.WaveIndex+1) + "/" + battle.WaveCount +
                " · " + (visibleEnemy.IsBoss ? "BOSS: " : "") + visibleEnemy.Name + " " + VisibleEnemyHealth(visibleTarget) + "/" + visibleEnemy.MaxHealth + EnemyMechanicLabel(visibleTarget) +
                "\nWeak: " + visibleEnemy.Weakness + " · Resists: " + visibleEnemy.Resistance + " · Acting: " + party[battle.ActiveIndex].Identity.Name +
                "\n" + (battle.Current == Phase.Won ? "Victory! Continue to claim your rewards." : battle.Current == Phase.Lost ? "Party defeated. Restart to try again." : message);
            if (saveFailed) status.text += "\n" + saveNotice;
            for(int i=0;i<energyBars.Length;i++) {
                bool visible=!atChapters && i<party.Count;
                energyBars[i].transform.parent.gameObject.SetActive(visible); energyLabels[i].gameObject.SetActive(visible);
                partyHealthBars[i].transform.parent.gameObject.SetActive(visible);
                if(!visible) continue;
                int energy=presentingAction && i==actionHero?actionEnergy:party[i].Energy;
                SetMeter(partyHealthBars[i],party[i].Health/(float)party[i].Definition.MaxHealth);
                partyHealthBars[i].color=party[i].Health <= party[i].Definition.MaxHealth*.25f ? new Color(.85f,.3f,.25f) : new Color(.35f,.7f,.43f);
                energyLabels[i].color=i==battle.ActiveIndex ? new Color(1,.82f,.4f) : new Color(.9f,.86f,.77f);
                SetMeter(energyBars[i],energy/(float)PartyHero.MaxEnergy);
                energyBars[i].color=energy==PartyHero.MaxEnergy?new Color(1,.75f,.2f):new Color(.25f,.65f,.9f);
                energyLabels[i].text=party[i].Identity.Name+" · "+(battle.IsFrontRow(i)?"FRONT":"BACK")+"\n"+(party[i].Health<=0?"FALLEN":"HP "+party[i].Health+" · "+(energy==100?"ULT READY":"EN "+energy))+ (party[i].Status==Debuff.None?"":" · "+party[i].Status);
            }
            factionStatus.gameObject.SetActive(!atChapters); factionStatus.text="TEAM POWER "+battle.TeamPower+" · "+battle.FactionSummary;
            bool preparing = !atChapters && battle.CanChangeParty;
            if(!atChapters && !presentingAction && enemyTurn==null && battle.Current==Phase.WaveCleared && !transitioningWave) StartCoroutine(NextWave());
            bool finished = !atChapters && !presentingAction && (battle.Current == Phase.Won || battle.Current == Phase.Lost);
            reset.gameObject.SetActive(finished && battle.Current!=Phase.Won);
            collectionBattleButton.gameObject.SetActive(preparing); if(!preparing && !atChapters) collectionRoot.SetActive(false);
            previewButton.gameObject.SetActive(preparing);
            if(!preparing) previewRoot.SetActive(false);
            loadout.gameObject.SetActive(preparing); gearButton.gameObject.SetActive(preparing); formationButton.gameObject.SetActive(preparing);
            if(!preparing) { teamPresetRoot.SetActive(false); formationRoot.SetActive(false); progressionRoot.SetActive(false); }
            if(!preparing) equipmentRoot.SetActive(false);
            if (!preparing) selectionRoot.SetActive(false);
            reset.GetComponentInChildren<Text>(true).text = battle.Current == Phase.Won ? "CONTINUE" : "RESTART";
            attack.gameObject.SetActive(!atChapters); ability.gameObject.SetActive(!atChapters); abilityTwo.gameObject.SetActive(!atChapters);
            autoButton.gameObject.SetActive(!atChapters);
            speedButton.gameObject.SetActive(!atChapters && !finished);
            speedButton.GetComponentInChildren<Text>().text="SPEED: "+battleSpeed+"×";
            if (finished || atChapters) autoPlay = false;
            if(finished && battle.Current==Phase.Won && enemyTurn==null && !resultsRoot.activeSelf) ShowResults();
            autoButton.interactable = !finished;
            autoButton.GetComponentInChildren<Text>().text = autoPlay ? "AUTO PLAY: ON" : "AUTO PLAY: OFF";
            heroHealth.transform.parent.gameObject.SetActive(!atChapters); enemyHealth.transform.parent.gameObject.SetActive(!atChapters);
            chaptersButton.gameObject.SetActive(preparing || (finished && battle.Current!=Phase.Won));
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
                enemyTargets[i].GetComponentInChildren<Text>().text = (i == visibleTarget ? "▶ " : "") + foe.Definition.Name + "\n"+foe.Definition.Row+" "+foe.Definition.Role+" · " + VisibleEnemyHealth(i) + "/" + foe.Definition.MaxHealth + EnemyMechanicLabel(i);
                enemyTargets[i].GetComponent<Image>().color = i == visibleTarget ? new Color(.31f,.25f,.15f) : new Color(.075f,.085f,.105f);
            }
            soundButton.GetComponentInChildren<Text>().text=sounds.Muted?"SOUND OFF":"SOUND ON";
            if (!presentingAction && lastSoundPhase != battle.Current) {
                if (battle.Current == Phase.Won) sounds.Play("Victory",.7f);
                else if (battle.Current == Phase.Lost) sounds.Play("Defeat",.8f);
                lastSoundPhase = battle.Current;
            }
            if(atChapters) { towerButton.interactable=battle.CanEnterTower; towerButton.GetComponentInChildren<Text>().text=battle.TowerComplete?"TOWER COMPLETE · 100 floors":battle.Recruits.Count<5?"TOWER · Recruit 5 heroes to unlock":"TOWER · FLOOR "+battle.TowerFloor+"/100 · Up to 5 heroes"; summonTenButton.interactable=battle.CanSummonTen; repeatSingle.interactable=battle.CanSummon; repeatTen.interactable=battle.CanSummonTen; if(dailyRoot.activeSelf) RefreshDailyMenu(); idleRewardsButton.interactable=battle.IdleMinutes>0; idleRewardsButton.GetComponentInChildren<Text>(true).text="IDLE · "+battle.IdleMinutes+"/720 min · +"+battle.IdleCrystals+" crystals · +"+battle.IdleExperience+" XP each"; summonButton.interactable=battle.CanSummon; summonButton.GetComponentInChildren<Text>(true).text="SUMMON x1\n100"; timing.transform.parent.gameObject.SetActive(false); cue.text=""; return; }
            if (autoPlay && !tutorialRoot.activeSelf && !presentingAction && !selectionRoot.activeSelf && !equipmentRoot.activeSelf && !formationRoot.activeSelf && !progressionRoot.activeSelf && !previewRoot.activeSelf && !collectionRoot.activeSelf && !teamPresetRoot.activeSelf && battle.Current == Phase.Player && Time.time >= nextAutoAction) {
                battle.SelectEnemy(AutoBattlePlanner.ChooseTarget(battle));
                PerformAction(AutoBattlePlanner.ChooseSkill(battle));
                nextAutoAction = Time.time+1.3f;
            }
            float elapsed = Time.time - strikeTime;
            bool striking = !presentingAction && battle.Current == Phase.EnemyStrike && battle.IncomingSkill.Action==EnemyAction.Attack;
            timing.transform.parent.gameObject.SetActive(striking && !battle.Defended);
            SetMeter(timing, 1 - elapsed / .4f);
            timing.color = elapsed <= .18f ? Color.yellow : new Color(.2f, .7f, .85f);
            cue.text = battle.Current==Phase.WaveCleared ? "WAVE CLEARED — NEXT WAVE INCOMING" : presentingAction ? "" : battle.Current == Phase.EnemyWindup ? battle.IncomingSkill.Name.ToUpperInvariant() + (battle.IncomingSkill.Action!=EnemyAction.Attack ? "" : battle.IncomingSkill.PartyWide ? " — ALL HEROES" : " — " + battle.Party[battle.ActiveIndex].Identity.Name) :
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
