using UnityEngine;
using Ashbound.Core;

namespace Ashbound
{
    internal sealed class GameHud
    {
        internal enum Action { None, Begin, Pause, Mute, Title, Shake, UltimateLeft, UltimateRight }
        private static readonly Color Paper = new Color(0.89f, 0.88f, 0.81f);
        private static readonly Color Muted = new Color(0.56f, 0.63f, 0.64f);
        private static readonly Color Ink = new Color(0.025f, 0.04f, 0.05f);
        private static readonly Color Line = new Color(0.31f, 0.35f, 0.33f);
        private readonly Rect pauseButton = new Rect(1180f, 28f, 56f, 42f);
        private readonly Rect muteButton = new Rect(1080f, 28f, 88f, 42f);
        private readonly Rect ultimateLeftButton = new Rect(438f, 602f, 194f, 47f);
        private readonly Rect ultimateRightButton = new Rect(648f, 602f, 194f, 47f);
        private GUIStyle text;
        private GUIStyle button;
        private Rect viewport;
        private float scale;

        private void Measure()
        {
            Rect safe = Screen.safeArea;
            if (safe.width <= 0f || safe.height <= 0f) safe = new Rect(0f, 0f, Screen.width, Screen.height);
            scale = Mathf.Min(safe.width / 1280f, safe.height / 720f);
            viewport = new Rect(safe.x + (safe.width - 1280f * scale) * 0.5f,
                Screen.height - safe.yMax + (safe.height - 720f * scale) * 0.5f,
                1280f * scale, 720f * scale);
        }

        internal bool BlocksAttack(Vector2 screenPoint)
        {
            Measure();
            var point = new Vector2((screenPoint.x - viewport.x) / scale,
                (Screen.height - screenPoint.y - viewport.y) / scale);
            return pauseButton.Contains(point) || muteButton.Contains(point)
                || ultimateLeftButton.Contains(point) || ultimateRightButton.Contains(point);
        }

        internal Action Draw(AshboundGame.GameScreen screen, CombatSession combat, int best,
            string message, Color messageColor, float messageLife, float damageFlash, float ultimateFlash, bool muted, bool shake)
        {
            Measure();
            if (text == null)
            {
                var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                text = new GUIStyle { font = font, wordWrap = true, richText = false };
                button = new GUIStyle { font = font, alignment = TextAnchor.MiddleCenter,
                    fontSize = 16, fontStyle = FontStyle.Bold };
                button.normal.textColor = Paper;
                button.hover.textColor = ArenaBuilder.Gold;
                button.active.textColor = Color.white;
            }
            Matrix4x4 oldMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(new Vector3(viewport.x, viewport.y, 0f), Quaternion.identity, Vector3.one * scale);
            Action action = Action.None;
            bool playing = screen == AshboundGame.GameScreen.Playing;

            // Soft letterbox panels keep text legible over the world.
            Fill(new Rect(0f, 0f, 1280f, playing ? 112f : 84f), WithAlpha(Ink, 0.88f));
            Fill(new Rect(40f, playing ? 110f : 82f, 1200f, 1f), WithAlpha(Line, 0.7f));
            Label(new Rect(44f, 25f, 260f, 28f), "A S H B O U N D", 21, Paper, TextAnchor.MiddleLeft, true);
            Label(new Rect(45f, 54f, 260f, 21f), "THE LAST OATHKEEPER", 10, Muted, TextAnchor.MiddleLeft);
            if (Button(muteButton, muted ? "SOUND OFF" : "SOUND ON", false)) action = Action.Mute;

            if (playing)
            {
                if (Button(pauseButton, "II", false)) action = Action.Pause;
                Label(new Rect(380f, 26f, 155f, 19f), "SOULS", 11, Muted);
                Label(new Rect(380f, 48f, 155f, 42f), combat.Score.ToString("N0"), 31, ArenaBuilder.Gold, TextAnchor.MiddleCenter, true);
                Label(new Rect(560f, 28f, 160f, 20f), "NIGHT " + combat.Wave.ToString("00"), 14, Paper, TextAnchor.MiddleCenter, true);
                Label(new Rect(560f, 55f, 160f, 26f), TimeText(combat.Elapsed), 19, Muted);
                Label(new Rect(775f, 28f, 210f, 18f), "YOUR OATH", 10, Muted);
                for (int i = 0; i < CombatSession.MaxHealth; i++)
                {
                    Rect life = new Rect(827f + i * 33f, 57f, 23f, 10f);
                    Fill(life, i < combat.Health ? ArenaBuilder.Gold : WithAlpha(Line, 0.6f));
                }

                if (combat.Combo > 1)
                {
                    Label(new Rect(470f, 126f, 340f, 42f), combat.Combo + "  CHAIN", 29, Paper, TextAnchor.MiddleCenter, true);
                    Label(new Rect(490f, 167f, 300f, 24f), "SOUL MULTIPLIER  x" + combat.Multiplier, 11, ArenaBuilder.Gold);
                }
                if (messageLife > 0f)
                    Label(new Rect(390f, 224f - (0.85f - messageLife) * 16f, 500f, 35f), message, 20,
                        WithAlpha(messageColor, Mathf.Min(1f, messageLife * 3f)), TextAnchor.MiddleCenter, true);

                DrawControl(new Rect(42f, 595f, 360f, 82f), "<", "STRIKE LEFT", "A / LEFT ARROW");
                DrawControl(new Rect(878f, 595f, 360f, 82f), ">", "STRIKE RIGHT", "D / RIGHT ARROW");
                Fill(new Rect(423f, 536f, 434f, 144f), WithAlpha(Ink, 0.88f));
                Label(new Rect(438f, 542f, 404f, 21f), combat.IsUltimateReady ? "OATHBREAKER  /  READY" :
                    "OATHBREAKER  " + combat.UltimateCharge.ToString("0.#") + " / 100", 12,
                    combat.IsUltimateReady ? ArenaBuilder.Gold : Paper, TextAnchor.MiddleCenter, true);
                Fill(new Rect(438f, 574f, 404f, 11f), Line);
                Fill(new Rect(438f, 574f, 404f * combat.UltimateCharge / CombatSession.UltimateMaxCharge, 11f),
                    combat.IsUltimateReady ? ArenaBuilder.Gold : ArenaBuilder.Soul);
                bool wasEnabled = GUI.enabled;
                GUI.enabled = wasEnabled && combat.IsUltimateReady;
                if (Button(ultimateLeftButton, "<  WAVE LEFT  [Q]", combat.IsUltimateReady)) action = Action.UltimateLeft;
                if (Button(ultimateRightButton, "WAVE RIGHT  [E]  >", combat.IsUltimateReady)) action = Action.UltimateRight;
                GUI.enabled = wasEnabled;
                Label(new Rect(438f, 654f, 404f, 19f), combat.IsUltimateReady ? "CHOOSE A SIDE. CLEAR EVERY ENEMY." :
                    "+2.5 PER KILL  /  40 KILLS TO FILL", 10, combat.IsUltimateReady ? ArenaBuilder.Gold : Muted);
                Label(new Rect(360f, 690f, 560f, 18f), combat.Elapsed < 10f ? "Strike after enemies cross the gold marks beside you" :
                    "A / D TO STRIKE   •   Q / E FOR ULTIMATE", 10, Muted);
            }
            else
            {
                Fill(new Rect(0f, 85f, 1280f, 635f), WithAlpha(Ink, screen == AshboundGame.GameScreen.Title ? 0.62f : 0.88f));
                if (screen == AshboundGame.GameScreen.Title)
                {
                    Label(new Rect(240f, 137f, 800f, 24f), "THE DEAD HAVE RISEN. YOUR OATH REMAINS.", 13, ArenaBuilder.Gold);
                    Label(new Rect(180f, 177f, 920f, 108f), "ASHBOUND", 88, Paper, TextAnchor.MiddleCenter, true);
                    Fill(new Rect(570f, 305f, 140f, 2f), ArenaBuilder.Gold);
                    Label(new Rect(320f, 324f, 640f, 56f), "One knight. Two directions.\nAn endless night at the gates of the forgotten.", 17, Muted);
                    if (Button(new Rect(470f, 412f, 340f, 62f), "TAKE THE OATH", true)) action = Action.Begin;
                    Label(new Rect(410f, 487f, 460f, 22f), "ENTER TO BEGIN   /   BEST " + best.ToString("N0") + " SOULS", 10, Muted);
                    TitleGuide();
                }
                else if (screen == AshboundGame.GameScreen.Paused)
                {
                    Label(new Rect(290f, 152f, 700f, 30f), "A MOMENT BETWEEN SHADOWS", 12, ArenaBuilder.Gold);
                    Label(new Rect(290f, 200f, 700f, 75f), "OATH SUSPENDED", 49, Paper, TextAnchor.MiddleCenter, true);
                    if (Button(new Rect(470f, 316f, 340f, 58f), "RETURN TO THE FIGHT", true)) action = Action.Pause;
                    if (Button(new Rect(470f, 392f, 340f, 45f), "SCREEN SHAKE  " + (shake ? "ON" : "OFF"), false)) action = Action.Shake;
                    if (Button(new Rect(470f, 455f, 340f, 45f), "ABANDON RUN", false)) action = Action.Title;
                    Label(new Rect(390f, 536f, 500f, 22f), "ULTIMATE " + combat.UltimateCharge.ToString("0.#") + " / 100   •   Q LEFT / E RIGHT", 11, ArenaBuilder.Gold);
                    Label(new Rect(390f, 572f, 500f, 28f), "P / ESC TO RESUME    •    M TO MUTE", 11, Muted);
                }
                else
                {
                    Label(new Rect(290f, 134f, 700f, 28f), "EVEN ASH REMEMBERS", 12, ArenaBuilder.Gold);
                    Label(new Rect(240f, 178f, 800f, 72f), "THE OATH IS BROKEN", 46, Paper, TextAnchor.MiddleCenter, true);
                    Label(new Rect(400f, 274f, 480f, 24f), "SOULS CLAIMED", 11, Muted);
                    Label(new Rect(350f, 300f, 580f, 68f), combat.Score.ToString("N0"), 58, ArenaBuilder.Gold, TextAnchor.MiddleCenter, true);
                    Label(new Rect(260f, 395f, 240f, 45f), "BEST CHAIN\n" + combat.BestCombo, 15, Paper);
                    Label(new Rect(520f, 395f, 240f, 45f), "SURVIVED\n" + TimeText(combat.Elapsed), 15, Paper);
                    Label(new Rect(780f, 395f, 240f, 45f), "VANQUISHED\n" + combat.Kills, 15, Paper);
                    if (Button(new Rect(470f, 492f, 340f, 60f), "RISE AGAIN", true)) action = Action.Begin;
                    if (Button(new Rect(535f, 571f, 210f, 40f), "RETURN TO TITLE", false)) action = Action.Title;
                    Label(new Rect(350f, 639f, 580f, 25f), "BEST " + best.ToString("N0") + " SOULS   /   ENTER TO RETRY", 11, Muted);
                }
            }
            if (damageFlash > 0f && playing)
                Fill(new Rect(0f, 0f, 1280f, 720f), new Color(0.65f, 0.06f, 0.025f, damageFlash * 0.2f));
            if (ultimateFlash > 0f && playing)
                Fill(new Rect(0f, 0f, 1280f, 720f), WithAlpha(ArenaBuilder.Gold, ultimateFlash * 0.15f));
            GUI.matrix = oldMatrix;
            GUI.color = Color.white;
            return action;
        }

        private void TitleGuide()
        {
            Fill(new Rect(170f, 559f, 940f, 1f), Line);
            Label(new Rect(170f, 581f, 280f, 24f), "01   WATCH THE THRESHOLD", 12, Paper, TextAnchor.MiddleCenter, true);
            Label(new Rect(170f, 614f, 280f, 42f), "Wait until the dead cross\nthe gold marks beside you.", 12, Muted);
            Label(new Rect(500f, 581f, 280f, 24f), "02   STRIKE THEIR SIDE", 12, Paper, TextAnchor.MiddleCenter, true);
            Label(new Rect(500f, 614f, 280f, 42f), "Tap left / right, or press A / D.\nHolding does not repeat attacks.", 12, Muted);
            Label(new Rect(830f, 581f, 280f, 24f), "03   KEEP YOUR OATH", 12, Paper, TextAnchor.MiddleCenter, true);
            Label(new Rect(830f, 614f, 280f, 42f), "Chain kills to multiply souls.\nThree wounds end the night.", 12, Muted);
            Label(new Rect(270f, 683f, 740f, 20f), "ULTIMATE: +2.5 PER KILL. AT 100, Q CLEARS LEFT / E CLEARS RIGHT.", 10, ArenaBuilder.Gold);
        }

        private void DrawControl(Rect rect, string arrow, string title, string key)
        {
            Fill(rect, WithAlpha(Ink, 0.85f));
            Fill(new Rect(rect.x, rect.y, rect.width, 1f), WithAlpha(ArenaBuilder.Gold, 0.45f));
            Label(new Rect(rect.x + 20f, rect.y + 11f, 45f, 54f), arrow, 33, ArenaBuilder.Gold);
            Label(new Rect(rect.x + 75f, rect.y + 18f, 250f, 22f), title, 15, Paper, TextAnchor.MiddleLeft, true);
            Label(new Rect(rect.x + 75f, rect.y + 46f, 250f, 18f), "TAP THIS HALF  /  " + key, 9, Muted, TextAnchor.MiddleLeft);
        }

        private bool Button(Rect rect, string caption, bool primary)
        {
            bool hover = rect.Contains(Event.current.mousePosition);
            Fill(rect, primary ? WithAlpha(ArenaBuilder.Gold, hover ? 0.23f : 0.12f) : WithAlpha(Ink, 0.8f));
            Color border = primary || hover ? ArenaBuilder.Gold : Line;
            Fill(new Rect(rect.x, rect.y, rect.width, 1f), border);
            Fill(new Rect(rect.x, rect.yMax - 1f, rect.width, 1f), border);
            Fill(new Rect(rect.x, rect.y, 1f, rect.height), border);
            Fill(new Rect(rect.xMax - 1f, rect.y, 1f, rect.height), border);
            button.fontSize = rect.height < 50f ? 11 : 16;
            return GUI.Button(rect, caption, button);
        }

        private void Label(Rect rect, string caption, int size, Color color,
            TextAnchor alignment = TextAnchor.MiddleCenter, bool bold = false)
        {
            text.fontSize = size;
            text.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            text.alignment = alignment;
            text.normal.textColor = color;
            GUI.Label(rect, caption, text);
        }

        private static void Fill(Rect rect, Color color)
        {
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        private static Color WithAlpha(Color color, float alpha) { color.a = alpha; return color; }
        private static string TimeText(float seconds) { return ((int)seconds / 60).ToString("00") + ":" + ((int)seconds % 60).ToString("00"); }
    }
}
