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

        private sealed class SlashWave
        {
            internal Transform Root;
            internal Side Side;
            internal float Age;
            internal float TravelDistance;
        }

        private readonly Dictionary<int, CharacterView> enemies = new Dictionary<int, CharacterView>();
        private readonly List<int> staleEnemies = new List<int>();
        private readonly List<Mote> motes = new List<Mote>();
        private readonly List<SlashWave> waves = new List<SlashWave>();
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
        private float ultimateFlash;
        private float messageLife;
        private string message = "";
        private Color messageColor;
        private Side lastStrike = Side.Right;
        private bool shakeEnabled;
        private int bestScore;
        private float screenAge;
        private bool initialized;
        private Side? pendingUltimate;
        private Mesh slashMesh;

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
            slashMesh = BuildSlashMesh();
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
                // Tick the response deadline; enemy positions advance only after
                // a kill or after the front enemy lands a timeout attack.
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
            UpdateWaves(dt);
            damageFlash = Mathf.Max(0f, damageFlash - dt * 1.6f);
            ultimateFlash = Mathf.Max(0f, ultimateFlash - dt * 3.5f);
            messageLife = Mathf.Max(0f, messageLife - dt);
            shake = Mathf.Max(0f, shake - dt * 2.4f);
            Vector3 offset = shakeEnabled ? new Vector3(Mathf.Sin(visualTime * 91f), Mathf.Cos(visualTime * 113f), 0f) * shake * 0.16f : Vector3.zero;
            arenaCamera.transform.position = cameraPosition + offset;
        }

        private void ReadAttacks()
        {
            Side? ultimate = pendingUltimate;
            pendingUltimate = null;
            if (Input.GetKeyDown(KeyCode.Q)) ultimate = Side.Left;
            else if (Input.GetKeyDown(KeyCode.E)) ultimate = Side.Right;
            if (ultimate.HasValue && combat.ActivateUltimate(ultimate.Value)) return;

            bool left = Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A);
            bool right = Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D);
            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch touch = Input.GetTouch(i);
                if (touch.phase != TouchPhase.Began || hud.BlocksAttack(touch.position)) continue;
                if (touch.position.x < Screen.width * 0.5f) left = true;
                else right = true;
            }
            if (Input.touchCount == 0 && Input.GetMouseButtonDown(0) && !hud.BlocksAttack(Input.mousePosition))
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
                        knight.Strike(e.Side, e.Style);
                        audioDirector.Swing();
                        break;
                    case CombatEventKind.Kill:
                        Burst(new Vector3((int)e.Side * e.Distance, 1.2f, 0f),
                            e.Perfect || e.IsUltimate ? ArenaBuilder.Gold : ArenaBuilder.Soul, 15, true);
                        if (e.IsUltimate) break; // Preserve the wave's sound, shake, and banner.
                        shake = e.Perfect ? 0.48f : 0.26f;
                        audioDirector.Impact(e.Perfect);
                        ShowMessage(e.Perfect ? "PERFECT" : "SOUL CLAIMED", e.Perfect ? ArenaBuilder.Gold : ArenaBuilder.Soul);
                        break;
                    case CombatEventKind.Ultimate:
                        lastStrike = e.Side;
                        knight.Ultimate(e.Side);
                        LaunchWave(e.Side);
                        Burst(new Vector3(0f, 1.4f, -0.2f), ArenaBuilder.Soul, 24, true);
                        ultimateFlash = 0.65f;
                        shake = 1.1f;
                        audioDirector.Ultimate();
                        ShowMessage(e.Side == Side.Left ? "OATHBREAKER  <" : "OATHBREAKER  >", ArenaBuilder.Gold);
                        break;
                    case CombatEventKind.Miss:
                        ShowMessage(combat.Enemies.Count == 0 ? "GET READY" : "WRONG SIDE", new Color(0.62f, 0.62f, 0.61f));
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
                view.Animate(visualTime, dt, (int)enemy.Side * enemy.Distance, -(int)enemy.Side, false);
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
            ClearWaves();
            combat.Reset(Environment.TickCount);
            messageLife = damageFlash = ultimateFlash = shake = 0f;
            pendingUltimate = null;
            lastStrike = Side.Right;
            knight.ResetPose();
            SetScreen(GameScreen.Playing);
        }

        private void SetScreen(GameScreen next)
        {
            screen = next;
            screenAge = 0f;
            pendingUltimate = null;
        }

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

        private static Mesh BuildSlashMesh()
        {
            const int segments = 32;
            var vertices = new Vector3[(segments + 1) * 2];
            var normals = new Vector3[vertices.Length];
            var triangles = new int[segments * 12];
            for (int i = 0; i <= segments; i++)
            {
                float t = i / (float)segments;
                float angle = Mathf.Lerp(-1.25f, 1.25f, t);
                float x = Mathf.Cos(angle) * 0.9f;
                float y = Mathf.Sin(angle) * 1.65f;
                float width = Mathf.Sin(t * Mathf.PI) * 0.2f + 0.015f;
                vertices[i * 2] = new Vector3(x - width, y, 0f);
                vertices[i * 2 + 1] = new Vector3(x + width, y, 0f);
                normals[i * 2] = normals[i * 2 + 1] = Vector3.back;
                if (i == segments) continue;
                int v = i * 2;
                int index = i * 12;
                // Both windings keep the crescent visible when mirrored left.
                int[] face = { v, v + 2, v + 1, v + 1, v + 2, v + 3,
                    v + 1, v + 2, v, v + 3, v + 2, v + 1 };
                Array.Copy(face, 0, triangles, index, face.Length);
            }
            var mesh = new Mesh { name = "Oathbreaker crescent", vertices = vertices, normals = normals, triangles = triangles };
            mesh.RecalculateBounds();
            return mesh;
        }

        private void LaunchWave(Side side)
        {
            var root = new GameObject("Oathbreaker wave").transform;
            root.SetParent(transform, false);
            for (int layer = 0; layer < 3; layer++)
            {
                var ribbon = new GameObject("Crescent trail " + layer, typeof(MeshFilter), typeof(MeshRenderer));
                ribbon.transform.SetParent(root, false);
                ribbon.transform.localPosition = new Vector3(-layer * 0.48f, 0f, -0.1f - layer * 0.02f);
                ribbon.transform.localScale = new Vector3(1f - layer * 0.15f, 1f - layer * 0.12f, 1f);
                ribbon.GetComponent<MeshFilter>().sharedMesh = slashMesh;
                var renderer = ribbon.GetComponent<MeshRenderer>();
                renderer.sharedMaterial = ArenaBuilder.Material(layer == 0 ? ArenaBuilder.Gold : ArenaBuilder.Soul * (1f - layer * 0.15f), true);
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            var wave = new SlashWave {
                Root = root, Side = side,
                TravelDistance = Mathf.Max(CombatSession.SpawnDistance + 3f, arenaCamera.orthographicSize * arenaCamera.aspect + 3f)
            };
            PositionWave(wave);
            waves.Add(wave);
        }

        private static void PositionWave(SlashWave wave)
        {
            float t = Mathf.Clamp01(wave.Age / 0.5f);
            wave.Root.localPosition = new Vector3((int)wave.Side * Mathf.Lerp(0.45f, wave.TravelDistance, t), 1.5f, -0.3f);
            wave.Root.localScale = new Vector3((int)wave.Side * (1f + t * 0.7f), 1f + t * 0.45f, 1f);
        }

        private void UpdateWaves(float dt)
        {
            if (screen == GameScreen.Paused) return;
            for (int i = waves.Count - 1; i >= 0; i--)
            {
                var wave = waves[i];
                wave.Age += dt;
                if (wave.Age >= 0.5f)
                {
                    Destroy(wave.Root.gameObject);
                    waves.RemoveAt(i);
                }
                else PositionWave(wave);
            }
        }

        private void ClearWaves()
        {
            foreach (var wave in waves) Destroy(wave.Root.gameObject);
            waves.Clear();
        }

        private void OnGUI()
        {
            if (!initialized) return;
            var action = hud.Draw(screen, combat, bestScore, message, messageColor,
                messageLife, damageFlash, ultimateFlash, audioDirector.Muted, shakeEnabled);
            switch (action)
            {
                case GameHud.Action.Begin: StartRun(); break;
                case GameHud.Action.Pause: TogglePause(); break;
                case GameHud.Action.Mute: ToggleMute(); break;
                case GameHud.Action.Title: SetScreen(GameScreen.Title); break;
                case GameHud.Action.UltimateLeft:
                    if (screen == GameScreen.Playing && combat.IsUltimateReady) pendingUltimate = Side.Left;
                    break;
                case GameHud.Action.UltimateRight:
                    if (screen == GameScreen.Playing && combat.IsUltimateReady) pendingUltimate = Side.Right;
                    break;
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
            ClearWaves();
            if (slashMesh != null) Destroy(slashMesh);
            ArenaBuilder.DisposeMaterials();
        }
    }
}
