using System;
using UnityEngine;

namespace Minesweeper
{
    public sealed partial class MinesweeperApp : MonoBehaviour
    {
        private readonly int[] widths = { 9, 16, 30 }, heights = { 9, 16, 16 }, counts = { 10, 40, 99 };
        private readonly string[] levels = { "初級", "中級", "上級" };
        private readonly Color background = Hex("101820"), panel = Hex("19252F"), muted = Hex("91A5B4"), ink = Hex("E9F1F5"), accent = Hex("79E2C1");
        private readonly Color[] numbers = { Color.clear, Hex("83BDFF"), Hex("79E2C1"), Hex("FF9891"), Hex("C6ACFF"), Hex("FFD28A"), Hex("7CE0EB"), Hex("F2B5DC"), Hex("C4CFD7") };
        private MineBoard board;
        private int difficulty, selected;
        private bool help, focused = true, keyboardSelection;
        private float elapsed;
        private Font font;
        private Texture2D dot;
        private GUIStyle label, button;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            if (FindObjectOfType<MinesweeperApp>() == null)
                new GameObject("Minesweeper").AddComponent<MinesweeperApp>();
        }

        private void Awake()
        {
            Application.targetFrameRate = 60;
            var cameraObject = new GameObject("2D Camera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true; camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = background; camera.cullingMask = 0;
            font = Application.platform == RuntimePlatform.Android
                ? Resources.Load<Font>("NotoSansJP-Regular")
                : Font.CreateDynamicFontFromOSFont(new[] { "Yu Gothic", "Meiryo", "Arial" }, 32);
            dot = new Texture2D(32, 32, TextureFormat.RGBA32, false);
            for (int y = 0; y < 32; y++) for (int x = 0; x < 32; x++)
            {
                float a = Mathf.Clamp01(15.5f - Vector2.Distance(new Vector2(x, y), new Vector2(15.5f, 15.5f)));
                dot.SetPixel(x, y, new Color(1, 1, 1, a));
            }
            dot.Apply();
            difficulty = Mathf.Clamp(PlayerPrefs.GetInt("difficulty", 0), 0, 2);
            NewGame();
        }

        private static Color Hex(string value) { ColorUtility.TryParseHtmlString("#" + value, out Color color); return color; }
        private void Update()
        {
            if (skipResumeFrame) skipResumeFrame = false;
            else if (focused && !paused && !help && board.State == GameState.Playing) elapsed += Time.unscaledDeltaTime;
            if (Application.platform == RuntimePlatform.Android) HandleTouch();
        }
        private bool paused, skipResumeFrame;
        private void OnApplicationPause(bool value)
        {
            paused = value; touchId = -1;
            if (!value && Application.platform == RuntimePlatform.Android) skipResumeFrame = true;
        }
        private void OnApplicationFocus(bool value)
        {
            focused = value;
            if (value && Application.platform == RuntimePlatform.Android) skipResumeFrame = true;
        }
        private void OnDestroy() { if (dot != null) Destroy(dot); if (font != null && Application.platform != RuntimePlatform.Android) Destroy(font); }

        private void NewGame()
        {
            board = new MineBoard(widths[difficulty], heights[difficulty], counts[difficulty], Guid.NewGuid().GetHashCode());
            elapsed = 0; selected = 0; keyboardSelection = false;
            flagMode = false; boardScroll = Vector2.zero; touchId = -1;
        }

        private void SelectLevel(int index)
        {
            if (difficulty == index) return;
            difficulty = index;
            PlayerPrefs.SetInt("difficulty", difficulty); PlayerPrefs.Save();
            NewGame();
        }

        private void Act(int index, int action)
        {
            GameState previous = board.State;
            if (action == 1) board.ToggleFlag(index);
            else if (action == 2) board.Chord(index);
            else board.Reveal(index);
            if (board.State == GameState.Won && previous != GameState.Won)
            {
                string key = "best." + difficulty;
                if (!PlayerPrefs.HasKey(key) || elapsed < PlayerPrefs.GetFloat(key))
                { PlayerPrefs.SetFloat(key, elapsed); PlayerPrefs.Save(); }
            }
        }

        private void InitStyles()
        {
            if (label != null) return;
            label = new GUIStyle(GUI.skin.label) { font = font, padding = new RectOffset(0, 0, 0, 0), clipping = TextClipping.Clip };
            button = new GUIStyle(GUI.skin.button) { font = font, fontSize = 16, alignment = TextAnchor.MiddleCenter, border = new RectOffset(), padding = new RectOffset() };
            button.normal.background = Texture2D.whiteTexture;
            button.hover.background = Texture2D.whiteTexture;
            button.active.background = Texture2D.whiteTexture;
            button.focused.background = Texture2D.whiteTexture;
            button.normal.textColor = button.hover.textColor = button.active.textColor = button.focused.textColor = ink;
        }

        private void OnGUI()
        {
            if (board == null) return;
            InitStyles();
            if (Application.platform == RuntimePlatform.Android) { DrawMobile(); return; }
            float scale = Mathf.Min(Screen.width / 1160f, Screen.height / 860f);
            var oldMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(new Vector3((Screen.width - 1160 * scale) / 2, (Screen.height - 860 * scale) / 2), Quaternion.identity, Vector3.one * scale);
            GUI.enabled = !help;
            Text(new Rect(40, 27, 800, 22), "FIELD NOTES   /   01", 13, accent);
            Text(new Rect(38, 53, 700, 55), "MINESWEEPER", 40, ink, FontStyle.Bold);
            Text(new Rect(40, 110, 850, 25), "一手ずつ、地雷のない場所を見つけよう。", 16, muted);
            if (Button(new Rect(970, 55, 150, 44), "遊び方  ?", panel)) help = true;

            for (int i = 0; i < 3; i++)
            {
                Rect r = new Rect(40 + i * 122, 162, 112, 48);
                if (Button(r, levels[i] + "  " + counts[i], difficulty == i ? Hex("305C54") : panel)) SelectLevel(i);
                if (difficulty == i) Fill(new Rect(r.x, r.yMax - 3, r.width, 3), accent);
            }
            Metric(430, "残りの地雷", (board.MineCount - board.Flags).ToString("000"), accent);
            Metric(592, "経過時間", FormatTime(elapsed), ink);
            string best = PlayerPrefs.HasKey("best." + difficulty) ? FormatTime(PlayerPrefs.GetFloat("best." + difficulty)) : "--:--";
            Metric(770, "ベスト", best, muted);
            if (Button(new Rect(968, 162, 152, 48), "新しい盤面  R", Hex("305C54"))) NewGame();

            Fill(new Rect(40, 240, 1080, 552), panel);
            DrawBoard();
            string status = board.State == GameState.Won ? "CLEAR!  すべての安全なマスを開きました。" :
                board.State == GameState.Lost ? "地雷を踏んでしまいました。新しい盤面で、もう一度。" :
                board.State == GameState.Ready ? "好きなマスからスタート。最初のマスと周囲8マスは安全です。" :
                !focused ? "一時停止中 — ウィンドウに戻ると再開します。" : "数字は周囲8マスにある地雷の数。旗を手掛かりに進もう。";
            Text(new Rect(40, 807, 880, 26), status, 16, board.State == GameState.Lost ? Hex("FF9891") : accent);
            Text(new Rect(930, 807, 190, 26), board.Revealed + " / " + (board.Width * board.Height - board.MineCount) + " OPEN", 13, muted, FontStyle.Normal, TextAnchor.MiddleRight);
            GUI.enabled = true;
            if (help) DrawHelp();
            HandleKeyboard();
            GUI.matrix = oldMatrix;
        }

        private void DrawBoard(float cellOverride = 0, bool mobile = false)
        {
            float cell = mobile ? cellOverride : Mathf.Min(52, Mathf.Min(1032f / board.Width, 512f / board.Height));
            float left = mobile ? -boardScroll.x : 580 - board.Width * cell / 2;
            float top = mobile ? -boardScroll.y : 516 - board.Height * cell / 2;
            Event e = Event.current;
            for (int i = 0; i < board.Width * board.Height; i++)
            {
                Rect r = new Rect(left + i % board.Width * cell + 1, top + i / board.Width * cell + 1, cell - 2, cell - 2);
                bool hover = !mobile && !help && r.Contains(e.mousePosition);
                bool revealed = board.IsOpen(i);
                Color color = revealed ? Hex("202E39") : Hex("344956");
                if (hover && !board.Finished) color = revealed ? Hex("2C3E4B") : Hex("476473");
                if (i == board.ExplodedIndex) color = Hex("A74347");
                Fill(r, color);
                if (!revealed) Fill(new Rect(r.x, r.y, r.width, 2), Hex("48616F"));
                if (keyboardSelection && selected == i && !help)
                {
                    Fill(new Rect(r.x, r.y, r.width, 2), accent); Fill(new Rect(r.x, r.yMax - 2, r.width, 2), accent);
                    Fill(new Rect(r.x, r.y, 2, r.height), accent); Fill(new Rect(r.xMax - 2, r.y, 2, r.height), accent);
                }
                if (board.State == GameState.Lost && board.IsMine(i)) MineIcon(r, board.IsFlagged(i) ? accent : ink);
                else if (board.IsFlagged(i)) FlagIcon(r, accent);
                else if (revealed && board.Adjacent(i) > 0)
                    Text(r, board.Adjacent(i).ToString(), Mathf.RoundToInt(cell * .48f), numbers[board.Adjacent(i)], FontStyle.Bold, TextAnchor.MiddleCenter);
                if (board.State == GameState.Lost && board.IsFlagged(i) && !board.IsMine(i))
                    Text(r, "×", Mathf.RoundToInt(cell * .85f), Hex("FF9891"), FontStyle.Bold, TextAnchor.MiddleCenter);

                if (hover && e.type == EventType.MouseDown && e.button <= 2)
                {
                    selected = i; keyboardSelection = false;
                    Act(i, e.button == 1 ? 1 : e.button == 2 || (revealed && e.clickCount >= 2) ? 2 : 0);
                    e.Use();
                }
            }
        }

        private void HandleKeyboard()
        {
            Event e = Event.current;
            if (e.type != EventType.KeyDown) return;
            if (e.keyCode == KeyCode.Escape) { help = !help; e.Use(); return; }
            if (help) return;
            int x = selected % board.Width, y = selected / board.Width;
            switch (e.keyCode)
            {
                case KeyCode.R: NewGame(); break;
                case KeyCode.LeftArrow: selected = y * board.Width + Mathf.Max(0, x - 1); keyboardSelection = true; break;
                case KeyCode.RightArrow: selected = y * board.Width + Mathf.Min(board.Width - 1, x + 1); keyboardSelection = true; break;
                case KeyCode.UpArrow: selected = Mathf.Max(0, y - 1) * board.Width + x; keyboardSelection = true; break;
                case KeyCode.DownArrow: selected = Mathf.Min(board.Height - 1, y + 1) * board.Width + x; keyboardSelection = true; break;
                case KeyCode.Return:
                case KeyCode.Space: keyboardSelection = true; Act(selected, board.IsOpen(selected) ? 2 : 0); break;
                case KeyCode.F: keyboardSelection = true; Act(selected, 1); break;
                default: return;
            }
            e.Use();
        }

        private void DrawHelp()
        {
            Fill(new Rect(0, 0, 1160, 860), new Color(0.025f, .045f, .06f, .94f));
            Fill(new Rect(235, 190, 690, 486), panel);
            Fill(new Rect(235, 190, 690, 4), accent);
            Text(new Rect(273, 225, 600, 44), "遊び方", 30, ink, FontStyle.Bold);
            Text(new Rect(273, 284, 600, 28), "地雷以外のすべてのマスを開くとクリアです。", 18, accent);
            string[] lines = {
                "左クリック  …  マスを開く",
                "右クリック  …  旗を立てる / 外す",
                "数字をダブルクリック / 中クリック  …  周囲を一括開放",
                "※ 周囲の旗が数字と同数のとき有効。旗の間違いに注意！",
                "矢印キーで移動、Enterで開く / 一括開放、Fで旗。",
                "Rで新しい盤面。Escでこの画面を開閉。"
            };
            for (int i = 0; i < lines.Length; i++)
                Text(new Rect(273, 333 + i * 34, 620, 29), lines[i], i == 3 ? 14 : 16, i == 3 ? muted : ink);
            Text(new Rect(273, 550, 600, 28), "遊び方の表示中・別ウィンドウの操作中はタイマーを停止。", 14, muted);
            if (Button(new Rect(273, 598, 614, 44), "盤面に戻る", Hex("305C54"))) help = false;
        }

        private void Metric(float x, string title, string value, Color color)
        {
            Text(new Rect(x, 155, 156, 24), title, 12, muted);
            Text(new Rect(x, 179, 156, 38), value, 28, color, FontStyle.Bold);
        }

        private static string FormatTime(float seconds)
        {
            int value = Mathf.FloorToInt(seconds);
            return (value / 60).ToString("00") + ":" + (value % 60).ToString("00");
        }

        private void Text(Rect r, string value, int size, Color color, FontStyle weight = FontStyle.Normal, TextAnchor align = TextAnchor.UpperLeft)
        {
            label.fontSize = size; label.normal.textColor = color; label.fontStyle = weight; label.alignment = align;
            GUI.Label(r, value, label);
        }

        private bool Button(Rect r, string value, Color color)
        {
            Color old = GUI.backgroundColor;
            GUI.backgroundColor = r.Contains(Event.current.mousePosition) && GUI.enabled ? color * 1.22f : color;
            bool pressed = GUI.Button(r, value, button);
            GUI.backgroundColor = old;
            return pressed;
        }

        private static void Fill(Rect r, Color color)
        {
            Color old = GUI.color; GUI.color = color; GUI.DrawTexture(r, Texture2D.whiteTexture); GUI.color = old;
        }

        private void FlagIcon(Rect r, Color color)
        {
            float s = r.width;
            Fill(new Rect(r.x + s * .37f, r.y + s * .23f, s * .07f, s * .55f), ink);
            Fill(new Rect(r.x + s * .44f, r.y + s * .23f, s * .29f, s * .25f), color);
            Fill(new Rect(r.x + s * .25f, r.y + s * .76f, s * .43f, s * .065f), ink);
        }

        private void MineIcon(Rect r, Color color)
        {
            float s = r.width;
            Fill(new Rect(r.x + s * .18f, r.y + s * .46f, s * .64f, s * .08f), color);
            Fill(new Rect(r.x + s * .46f, r.y + s * .18f, s * .08f, s * .64f), color);
            Color old = GUI.color; GUI.color = color;
            GUI.DrawTexture(new Rect(r.x + s * .27f, r.y + s * .27f, s * .46f, s * .46f), dot);
            GUI.color = old;
            Fill(new Rect(r.x + s * .38f, r.y + s * .35f, s * .1f, s * .1f), panel);
        }
    }
}
