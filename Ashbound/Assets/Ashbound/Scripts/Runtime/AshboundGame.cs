using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Ashbound.Core;

namespace Ashbound
{
    public sealed class AshboundGame : MonoBehaviour
    {
        internal enum GameScreen { Title, Playing, Paused, Defeat }
        private sealed class Mote
        {
            internal Transform Transform;
            internal Vector3 Velocity;
            internal float Life;
            internal float Duration;
            internal Vector3 Scale;
            internal bool Soul;
        }

        private readonly Dictionary<int, CharacterView> enemies = new Dictionary<int, CharacterView>();
        private readonly List<int> staleEnemies = new List<int>();
        private readonly List<Mote> motes = new List<Mote>();
        private readonly HashSet<int> activeIds = new HashSet<int>();
        private CharacterView knight;
        private Camera arenaCamera;
        private Vector3 cameraPosition;
        private Transform actors;
        private AudioDirector audioDirector;
        private GameHud hud;
        private CombatSession combat;
        private GameScreen screen;
        private float visualTime;
        private float shake;
        private float damageFlash;
        private float messageLife;
        private string message = "";
        private Color messageColor;
        private Side lastStrike = Side.Right;
        private bool shakeEnabled;
        private int bestScore;
        private float screenAge;
        private bool initialized;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (SceneManager.GetActiveScene().name != "Arena") return;
            if (FindFirstObjectByType<AshboundGame>() != null) return;
            new GameObject("ASHBOUND").AddComponent<AshboundGame>();
        }

        private void Awake()
        {
            Application.targetFrameRate = 60;
            Screen.orientation = ScreenOrientation.LandscapeLeft;
            // IMGUI buttons need touch-to-mouse translation. ReadAttacks ignores
            // mouse input while touches exist, preventing a duplicate strike.
            Input.simulateMouseWithTouches = true;
            bestScore = PlayerPrefs.GetInt("Ashbound.Best", 0);
            shakeEnabled = PlayerPrefs.GetInt("Ashbound.Shake", 1) != 0;
            combat = new CombatSession(Environment.TickCount);
            arenaCamera = ArenaBuilder.Build(transform);
            cameraPosition = arenaCamera.transform.position;
            actors = new GameObject("Combatants").transform;
            actors.SetParent(transform, false);
            knight = new CharacterView(actors, true);
            audioDirector = new AudioDirector(gameObject);
            hud = new GameHud();
            screen = GameScreen.Title;
            initialized = true;
        }

        private void Update()
        {
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.1f);
            screenAge += dt;
            visualTime += dt;
            arenaCamera.orthographicSize = Mathf.Max(6.6f, 12.5f / Mathf.Max(0.5f, arenaCamera.aspect));
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P)) TogglePause();
            if (Input.GetKeyDown(KeyCode.M)) ToggleMute();
            if ((screen == GameScreen.Title || screen == GameScreen.Defeat) &&
                screenAge > 0.3f && (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space)))
                StartRun();

            if (screen == GameScreen.Playing)
            {
                // Advance before taking input, so the displayed last-frame position and
                // this-frame attack window differ by at most one simulation step.
                combat.Tick(dt);
                if (!combat.IsOver) ReadAttacks();
                HandleEvents();
                SyncEnemies(dt);
            }
            else if (screen == GameScreen.Title)
                knight.Animate(visualTime, dt, 0f, 1, false);

            if (screen == GameScreen.Playing)
                knight.Animate(visualTime, dt, 0f, (int)lastStrike, false);
            UpdateMotes(dt);
            damageFlash = Mathf.Max(0f, damageFlash - dt * 1.6f);
            messageLife = Mathf.Max(0f, messageLife - dt);
            shake = Mathf.Max(0f, shake - dt * 2.4f);
            Vector3 offset = shakeEnabled ? new Vector3(Mathf.Sin(visualTime * 91f), Mathf.Cos(visualTime * 113f), 0f) * shake * 0.16f : Vector3.zero;
            arenaCamera.transform.position = cameraPosition + offset;
        }

        private void ReadAttacks()
        {
            bool left = Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A);
            bool right = Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D);
            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch touch = Input.GetTouch(i);
                if (touch.phase != TouchPhase.Began || hud.IsToolbar(touch.position)) continue;
                if (touch.position.x < Screen.width * 0.5f) left = true;
                else right = true;
            }
            if (Input.touchCount == 0 && Input.GetMouseButtonDown(0) && !hud.IsToolbar(Input.mousePosition))
            {
                if (Input.mousePosition.x < Screen.width * 0.5f) left = true;
                else right = true;
            }
            // Opposite simultaneous inputs resolve to one strike; alternating presses
            // still obey the cooldown, so holding or multi-touch cannot auto-win.
            if (left) combat.Attack(Side.Left);
            else if (right) combat.Attack(Side.Right);
        }

        private void HandleEvents()
        {
            foreach (var e in combat.DrainEvents())
            {
                switch (e.Kind)
                {
                    case CombatEventKind.Strike:
                        lastStrike = e.Side;
                        knight.Strike(e.Side);
                        audioDirector.Swing();
                        break;
                    case CombatEventKind.Kill:
                        Burst(new Vector3((int)e.Side * e.Distance, 1.2f, 0f), e.Perfect ? ArenaBuilder.Gold : ArenaBuilder.Soul, 15, true);
                        shake = e.Perfect ? 0.48f : 0.26f;
                        audioDirector.Impact(e.Perfect);
                        ShowMessage(e.Perfect ? "PERFECT" : "SOUL CLAIMED", e.Perfect ? ArenaBuilder.Gold : ArenaBuilder.Soul);
                        break;
                    case CombatEventKind.Miss:
                        ShowMessage("TOO EARLY", new Color(0.62f, 0.62f, 0.61f));
                        break;
                    case CombatEventKind.Hurt:
                        damageFlash = 0.65f;
                        shake = 0.9f;
                        Burst(new Vector3(0f, 1.2f, 0f), new Color(0.65f, 0.14f, 0.12f), 12, false);
                        audioDirector.Hurt();
                        ShowMessage("OATH WOUNDED", new Color(0.91f, 0.35f, 0.26f));
                        break;
                    case CombatEventKind.GameOver:
                        SetScreen(GameScreen.Defeat);
                        audioDirector.End();
                        if (combat.Score > bestScore)
                        {
                            bestScore = combat.Score;
                            PlayerPrefs.SetInt("Ashbound.Best", bestScore);
                            PlayerPrefs.Save();
                        }
                        break;
                }
            }
        }

        private void SyncEnemies(float dt)
        {
            activeIds.Clear();
            foreach (var enemy in combat.Enemies)
            {
                activeIds.Add(enemy.Id);
                CharacterView view;
                if (!enemies.TryGetValue(enemy.Id, out view))
                {
                    view = new CharacterView(actors, false, enemy.Kind == EnemyKind.Wraith);
                    enemies.Add(enemy.Id, view);
                }
                view.Animate(visualTime, dt, (int)enemy.Side * enemy.Distance, -(int)enemy.Side, true);
            }
            staleEnemies.Clear();
            foreach (var pair in enemies)
                if (!activeIds.Contains(pair.Key)) staleEnemies.Add(pair.Key);
            foreach (int id in staleEnemies)
            {
                enemies[id].Destroy();
                enemies.Remove(id);
            }
        }

        private void StartRun()
        {
            foreach (var view in enemies.Values) view.Destroy();
            enemies.Clear();
            foreach (var mote in motes) Destroy(mote.Transform.gameObject);
            motes.Clear();
            combat.Reset(Environment.TickCount);
            messageLife = damageFlash = shake = 0f;
            lastStrike = Side.Right;
            knight.Strike(lastStrike);
            SetScreen(GameScreen.Playing);
        }

        private void SetScreen(GameScreen next) { screen = next; screenAge = 0f; }

        private void TogglePause()
        {
            if (screen == GameScreen.Playing) SetScreen(GameScreen.Paused);
            else if (screen == GameScreen.Paused) SetScreen(GameScreen.Playing);
        }

        private void ToggleMute() { audioDirector.SetMuted(!audioDirector.Muted); }

        private void ShowMessage(string text, Color color)
        {
            message = text;
            messageColor = color;
            messageLife = 0.85f;
        }

        private void Burst(Vector3 position, Color color, int count, bool souls)
        {
            for (int i = 0; i < count; i++)
            {
                // Bounded by recent combat events; motes expire in under one second.
                var obj = ArenaBuilder.Shape("Ash mote", PrimitiveType.Cube, transform, position,
                    Vector3.one * UnityEngine.Random.Range(0.07f, 0.16f), color, true);
                float life = UnityEngine.Random.Range(0.35f, 0.8f);
                motes.Add(new Mote {
                    Transform = obj.transform, Life = life, Duration = life, Soul = souls,
                    Scale = obj.transform.localScale,
                    Velocity = new Vector3(UnityEngine.Random.Range(-3f, 3f), UnityEngine.Random.Range(1f, 4f), UnityEngine.Random.Range(-1f, 1f))
                });
            }
        }

        private void UpdateMotes(float dt)
        {
            if (screen == GameScreen.Paused) return;
            for (int i = motes.Count - 1; i >= 0; i--)
            {
                var mote = motes[i];
                mote.Life -= dt;
                if (mote.Life <= 0f)
                {
                    Destroy(mote.Transform.gameObject);
                    motes.RemoveAt(i);
                    continue;
                }
                mote.Velocity += (mote.Soul ? Vector3.up * 0.8f : Vector3.down * 7f) * dt;
                mote.Transform.position += mote.Velocity * dt;
                mote.Transform.Rotate(new Vector3(110f, 150f, 60f) * dt);
                mote.Transform.localScale = mote.Scale * (mote.Life / mote.Duration);
            }
        }

        private void OnGUI()
        {
            if (!initialized) return;
            var action = hud.Draw(screen, combat, bestScore, message, messageColor,
                messageLife, damageFlash, audioDirector.Muted, shakeEnabled);
            switch (action)
            {
                case GameHud.Action.Begin: StartRun(); break;
                case GameHud.Action.Pause: TogglePause(); break;
                case GameHud.Action.Mute: ToggleMute(); break;
                case GameHud.Action.Title: SetScreen(GameScreen.Title); break;
                case GameHud.Action.Shake:
                    shakeEnabled = !shakeEnabled;
                    PlayerPrefs.SetInt("Ashbound.Shake", shakeEnabled ? 1 : 0);
                    break;
            }
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused && initialized && screen == GameScreen.Playing)
                SetScreen(GameScreen.Paused);
        }

        private void OnApplicationFocus(bool focused)
        {
            if (!focused && initialized && screen == GameScreen.Playing)
                SetScreen(GameScreen.Paused);
        }

        private void OnDestroy()
        {
            if (audioDirector != null) audioDirector.Dispose();
            ArenaBuilder.DisposeMaterials();
        }
    }
}
