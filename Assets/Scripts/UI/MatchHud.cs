using UnityEngine;

namespace Moba
{
    /// <summary>
    /// HUD и экран итогов на IMGUI (не требует Canvas, шрифтов и TMP Essentials).
    /// Масштабируется по высоте экрана относительно 720p.
    /// </summary>
    public class MatchHud : MonoBehaviour
    {
        const float H = 720f;
        static readonly string[] Keys = { "Q", "W", "E" };

        PlayerHero player;
        GUIStyle center, big, left;

        void Start()
        {
            player = FindFirstObjectByType<PlayerHero>();
        }

        static void Box(Rect r, Color c)
        {
            Color old = GUI.color;
            GUI.color = c;
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = old;
        }

        void EnsureStyles()
        {
            if (center != null) return;
            center = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 16,
                fontStyle = FontStyle.Bold
            };
            center.normal.textColor = Color.white;

            big = new GUIStyle(center) { fontSize = 48 };
            left = new GUIStyle(center) { alignment = TextAnchor.MiddleLeft, fontSize = 14, fontStyle = FontStyle.Normal };
        }

        void OnGUI()
        {
            GameInput.FeedGuiEvent(Event.current);

            MatchController mc = MatchController.Instance;
            if (mc == null) return;
            EnsureStyles();

            float s = Screen.height / H;
            GUI.matrix = Matrix4x4.Scale(new Vector3(s, s, 1f));
            float W = Screen.width / s;

            DrawTopBar(mc, W);
            if (player != null) DrawPlayerPanel(W);
            GUI.Label(new Rect(10f, H - 30f, 520f, 24f), "RMB: move / attack    Q W E: abilities    P: pause", left);

            switch (mc.State)
            {
                case MatchState.Preparing:
                    GUI.Label(new Rect(0f, 250f, W, 80f),
                        "Match starts in " + Mathf.CeilToInt(Mathf.Max(0f, mc.CountdownRemaining)), big);
                    break;
                case MatchState.Finished:
                    DrawResults(mc, W);
                    break;
                default:
                    if (mc.Paused) GUI.Label(new Rect(0f, 250f, W, 80f), "PAUSED", big);
                    break;
            }
        }

        void DrawTopBar(MatchController mc, float W)
        {
            Box(new Rect(W / 2f - 160f, 8f, 320f, 36f), new Color(0f, 0f, 0f, 0.55f));
            int total = Mathf.FloorToInt(mc.Elapsed);
            string text = string.Format("{0:00}:{1:00}     Blue {2} : {3} Red",
                total / 60, total % 60, mc.Teams[0].heroKills, mc.Teams[1].heroKills);
            GUI.Label(new Rect(W / 2f - 160f, 8f, 320f, 36f), text, center);
        }

        void DrawPlayerPanel(float W)
        {
            Unit u = player.Unit;
            float x = W / 2f - 220f;

            // HP
            Box(new Rect(x, 600f, 440f, 16f), new Color(0f, 0f, 0f, 0.6f));
            Box(new Rect(x, 600f, 440f * u.Health.Percent, 16f), new Color(0.25f, 0.85f, 0.3f));
            GUI.Label(new Rect(x, 598f, 440f, 20f),
                Mathf.CeilToInt(u.Health.Current) + " / " + Mathf.CeilToInt(u.Health.Max), center);

            // Mana
            HeroAbilities ab = player.Abilities;
            Box(new Rect(x, 620f, 440f, 12f), new Color(0f, 0f, 0f, 0.6f));
            Box(new Rect(x, 620f, 440f * (ab.Mana / ab.maxMana), 12f), new Color(0.25f, 0.5f, 1f));

            // Abilities
            for (int i = 0; i < 3; i++)
            {
                AbilityData a = ab.abilities[i];
                Rect r = new Rect(W / 2f - 110f + i * 76f, 640f, 68f, 56f);
                Box(r, new Color(0.1f, 0.1f, 0.15f, 0.85f));
                if (a == null) continue;

                float cd = ab.CooldownRemaining(i);
                bool noMana = ab.Mana < a.manaCost;
                if (cd > 0f || noMana) Box(r, new Color(0f, 0f, 0f, 0.55f));

                GUI.Label(new Rect(r.x, r.y + 2f, r.width, 22f), Keys[i] + "  " + a.abilityName, center);
                string info = cd > 0f ? cd.ToString("0.0") + "s" : (noMana ? "no mana" : "ready");
                GUI.Label(new Rect(r.x, r.y + 28f, r.width, 22f), info, center);
            }

            if (u.IsDead)
            {
                GUI.Label(new Rect(0f, 300f, W, 60f),
                    "You died. Respawn in " + Mathf.CeilToInt(player.Respawn.Remaining), center);
            }
        }

        void DrawResults(MatchController mc, float W)
        {
            Rect panel = new Rect(W / 2f - 320f, 130f, 640f, 400f);
            Box(panel, new Color(0f, 0f, 0f, 0.82f));

            bool victory = mc.Winner == mc.playerTeam;
            Color old = GUI.contentColor;
            GUI.contentColor = victory ? new Color(0.4f, 1f, 0.5f) : new Color(1f, 0.45f, 0.45f);
            GUI.Label(new Rect(panel.x, panel.y + 10f, panel.width, 70f), victory ? "VICTORY" : "DEFEAT", big);
            GUI.contentColor = old;

            int total = Mathf.FloorToInt(mc.Elapsed);
            GUI.Label(new Rect(panel.x, panel.y + 80f, panel.width, 26f),
                string.Format("Winner: {0} team    Match time: {1:00}:{2:00}", mc.Winner, total / 60, total % 60), center);

            float y = panel.y + 125f;
            Row(panel.x, y, "Team", "Heroes", "Minions", "Structures", "Damage", true);
            for (int t = 0; t < 2; t++)
            {
                TeamStats ts = mc.Teams[t];
                Row(panel.x, y + 30f + t * 28f, ((TeamId)t).ToString(),
                    ts.heroKills.ToString(), ts.minionKills.ToString(),
                    ts.structureKills.ToString(), Mathf.RoundToInt(ts.damageDealt).ToString(), false);
            }

            if (player != null)
            {
                UnitStats st = player.Unit.Stats;
                GUI.Label(new Rect(panel.x, y + 100f, panel.width, 26f),
                    string.Format("Your hero:  kills {0}   deaths {1}   minions {2}   damage {3}",
                        st.kills, st.deaths, st.minionKills, Mathf.RoundToInt(st.damageDealt)), center);
            }

            if (GUI.Button(new Rect(W / 2f - 90f, panel.y + 330f, 180f, 44f), "Play again (R)"))
                mc.Restart();
        }

        void Row(float x, float y, string a, string b, string c, string d, string e, bool header)
        {
            float[] xs = { 30f, 170f, 270f, 380f, 510f };
            string[] cells = { a, b, c, d, e };
            for (int i = 0; i < 5; i++)
            {
                GUIStyle style = i == 0 ? left : center;
                GUI.Label(new Rect(x + xs[i] - (i == 0 ? 0f : 50f), y, i == 0 ? 120f : 100f, 26f), cells[i],
                    header ? center : style);
            }
        }
    }
}
