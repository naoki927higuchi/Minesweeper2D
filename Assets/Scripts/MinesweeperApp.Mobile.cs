using UnityEngine;

namespace Minesweeper
{
    public sealed partial class MinesweeperApp
    {
        private bool flagMode, touchDragged;
        private int touchId = -1;
        private Vector2 boardScroll, touchStart, lastTouch;
        private Rect mobileViewport, mobileSafe;
        private float mobileScale = 1, mobileCell = 44;

        private Vector2 MobilePoint(Vector2 screen)
        {
            return new Vector2((screen.x - mobileSafe.x) / mobileScale,
                (Screen.height - screen.y - (Screen.height - mobileSafe.yMax)) / mobileScale);
        }

        private void HandleTouch()
        {
            if (help || paused || !focused || Input.touchCount != 1) { touchId = -1; return; }
            Touch t = Input.GetTouch(0);
            Vector2 p = MobilePoint(t.position);
            if (t.phase == TouchPhase.Began)
            {
                touchId = mobileViewport.Contains(p) ? t.fingerId : -1;
                touchStart = lastTouch = p; touchDragged = false;
            }
            if (touchId != t.fingerId) return;
            if ((p - touchStart).sqrMagnitude > 64) touchDragged = true;
            if (t.phase == TouchPhase.Moved && touchDragged)
            {
                boardScroll += lastTouch - p;
                ClampScroll();
            }
            if (t.phase == TouchPhase.Ended)
            {
                if (!touchDragged && mobileViewport.Contains(p))
                {
                    Vector2 local = p - mobileViewport.position + boardScroll;
                    int x = Mathf.FloorToInt(local.x / mobileCell), y = Mathf.FloorToInt(local.y / mobileCell);
                    if (x >= 0 && y >= 0 && x < board.Width && y < board.Height)
                    {
                        int i = y * board.Width + x;
                        Act(i, flagMode ? 1 : board.IsOpen(i) ? 2 : 0);
                    }
                }
                touchId = -1;
            }
            if (t.phase == TouchPhase.Canceled) touchId = -1;
            lastTouch = p;
        }

        private void ClampScroll()
        {
            boardScroll.x = Mathf.Clamp(boardScroll.x, 0, Mathf.Max(0, board.Width * mobileCell - mobileViewport.width));
            boardScroll.y = Mathf.Clamp(boardScroll.y, 0, Mathf.Max(0, board.Height * mobileCell - mobileViewport.height));
        }

        private void DrawMobile()
        {
            mobileSafe = Screen.safeArea;
            mobileScale = mobileSafe.width / 440f;
            float h = mobileSafe.height / mobileScale;
            var oldMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(new Vector3(mobileSafe.x, Screen.height - mobileSafe.yMax), Quaternion.identity, Vector3.one * mobileScale);
            GUI.enabled = !help;
            Text(new Rect(16, 12, 335, 34), "MINESWEEPER", 28, ink, FontStyle.Bold);
            if (Button(new Rect(366, 10, 58, 44), "?", panel)) { help = true; touchId = -1; }
            for (int i = 0; i < 3; i++)
                if (Button(new Rect(16 + i * 138, 64, 132, 48), levels[i] + "  " + counts[i], difficulty == i ? Hex("305C54") : panel)) SelectLevel(i);
            Text(new Rect(16, 126, 200, 26), "残り " + (board.MineCount - board.Flags) + "    " + FormatTime(elapsed), 20, accent);
            string best = PlayerPrefs.HasKey("best." + difficulty) ? FormatTime(PlayerPrefs.GetFloat("best." + difficulty)) : "--:--";
            Text(new Rect(258, 129, 166, 26), "ベスト " + best, 16, muted, FontStyle.Normal, TextAnchor.UpperRight);
            mobileCell = 44;
            mobileViewport = new Rect(16, 166, 408, Mathf.Max(88, h - 356));
            ClampScroll();
            Fill(mobileViewport, panel);
            GUI.BeginGroup(mobileViewport);
            DrawBoard(mobileCell, true);
            GUI.EndGroup();
            float bottom = mobileViewport.yMax;
            Text(new Rect(16, bottom + 6, 408, 25), "盤面をスワイプで移動  ·  数字タップで一括開放", 14, muted);
            if (Button(new Rect(16, bottom + 36, 198, 52), flagMode ? "開く" : "✓ 開く", flagMode ? panel : Hex("305C54"))) flagMode = false;
            if (Button(new Rect(226, bottom + 36, 198, 52), flagMode ? "✓ 旗" : "旗", flagMode ? Hex("305C54") : panel)) flagMode = true;
            string status = board.State == GameState.Won ? "CLEAR! 残った地雷に旗を立てました。" :
                board.State == GameState.Lost ? "地雷！ 新しい盤面で再挑戦。" :
                board.State == GameState.Ready ? "最初のマスと周囲8マスは安全です。" :
                flagMode ? "旗モード：タップで旗を立てる / 外す" : "開くモード：タップでマスを開く";
            Text(new Rect(16, bottom + 96, 408, 26), status, 16, accent);
            if (Button(new Rect(16, bottom + 130, 408, 48), "新しい盤面", panel)) NewGame();
            GUI.enabled = true;
            if (help)
            {
                Fill(new Rect(0, 0, 440, h), background);
                Text(new Rect(20, 30, 400, 44), "遊び方", 28, accent, FontStyle.Bold);
                string[] lines = { "地雷以外のすべてのマスを開くとクリア。", "残った地雷には自動で旗が立ちます。", "「開く」を選んでマスをタップ。", "「旗」を選んでタップすると旗を変更。", "開くモードで数字をタップすると一括開放。", "周囲の旗が数字と同数のとき有効です。", "旗の位置が間違っていると地雷を踏みます。", "大きな盤面はスワイプして移動できます。", "最初のマスと周囲8マスは安全です。", "遊び方・バックグラウンド中は時間停止。" };
                for (int i = 0; i < lines.Length; i++) Text(new Rect(20, 92 + i * 34, 400, 30), lines[i], 17, ink);
                if (Button(new Rect(20, h - 76, 400, 52), "盤面に戻る", Hex("305C54"))) help = false;
            }
            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
            { help = !help; touchId = -1; Event.current.Use(); }
            GUI.matrix = oldMatrix;
        }
    }
}
