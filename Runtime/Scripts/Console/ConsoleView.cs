using System.Collections.Generic;
using System.Linq;

using UnityEngine;

namespace PinionCore.NetSync.Consoles
{
    /// <summary>
    /// 畫面上的主控台視窗(IMGUI)。
    /// 實作 PinionCore.Utility.Console 的輸入與輸出介面,
    /// 指令輸入後透過 <see cref="Command"/> 分派,執行結果與訊息顯示在視窗中。
    /// </summary>
    public class ConsoleView : MonoBehaviour,
        PinionCore.Utility.Console.IInput,
        PinionCore.Utility.Console.IViewer
    {
        [Tooltip("視窗標題。")]
        public string Title = "Console";

        [Tooltip("訊息保留的最大行數。")]
        public int MaxLineCount = 100;

        [Tooltip("是否將 PinionCore.Utility.Log 的訊息顯示在主控台。")]
        public bool ShowPinionCoreLog = true;

        [Tooltip("是否顯示主控台視窗。")]
        public bool Visible = true;

        [Tooltip("IMGUI 視窗識別碼;同場景有多個視窗時需錯開。")]
        public int WindowId = 0;

        [Tooltip("命令歷史保留筆數。")]
        public int HistoryCapacity = 20;

        readonly Queue<string> _Messages;
        readonly object _MessagesLock;
        // Layout pass 的訊息快照;Repaint 沿用同一份,兩個 pass 的控件數才會一致
        string[] _RenderLines;
        string _LastMessage;
        string _Input;
        Vector2 _ScrollView;
        bool _LogHooked;

        // 名稱允許重複,對應 Command 允許同名多次註冊;unregister 只移除一筆。
        readonly List<string> _CommandNames;
        List<string> _CompletionMatches;
        int _CompletionIndex;
        string _CompletionApplied;

        PinionCore.Utility.Console _Console;
        PinionCore.Utility.Doskey _Doskey;
        event PinionCore.Utility.Console.OnOutput _OutputEvent;

        public ConsoleView()
        {
            _Messages = new Queue<string>();
            _MessagesLock = new object();
            _RenderLines = System.Array.Empty<string>();
            _LastMessage = "";
            _Input = "";
            _ScrollView = Vector2.zero;
            _CommandNames = new List<string>();
        }

        public PinionCore.Utility.Command Command => _QueryConsole().Command;

        PinionCore.Utility.Console _QueryConsole()
        {
            if (_Console == null)
            {
                _Console = new PinionCore.Utility.Console(this, this);
                _Console.Command.RegisterEvent += _OnCommandRegister;
                _Console.Command.UnregisterEvent += _OnCommandUnregister;

                // help 在 Console 建構子內、訂閱之前就註冊了,事件收不到,手動補進。
                _CommandNames.Add("help");
            }

            return _Console;
        }

        PinionCore.Utility.Doskey _QueryDoskey()
        {
            if (_Doskey == null)
            {
                _Doskey = new PinionCore.Utility.Doskey(HistoryCapacity);
            }

            return _Doskey;
        }

        void _OnCommandRegister(string command, PinionCore.Utility.Command.CommandParameter ret, PinionCore.Utility.Command.CommandParameter[] args)
        {
            _CommandNames.Add(command);
        }

        void _OnCommandUnregister(string command)
        {
            _CommandNames.Remove(command);
        }

        void Awake()
        {
            _QueryConsole();
        }

        void OnEnable()
        {
            if (ShowPinionCoreLog && !_LogHooked)
            {
                PinionCore.Utility.Log.Instance.RecordEvent += _WriteLine;
                _LogHooked = true;
            }
        }

        void OnDisable()
        {
            if (_LogHooked)
            {
                PinionCore.Utility.Log.Instance.RecordEvent -= _WriteLine;
                _LogHooked = false;
            }
        }

        void OnGUI()
        {
            if (!Visible)
            {
                return;
            }

            GUILayout.Window(WindowId, new Rect(0, 0, Screen.width / 2, Screen.height), _WindowHandler, Title);
        }

        void _WindowHandler(int id)
        {
            var inputControlName = "ConsoleInput" + WindowId;

            var submitByKey = false;
            Event current = Event.current;
            if (current != null && current.type == EventType.KeyDown && current.keyCode == KeyCode.Return)
            {
                submitByKey = true;
            }

            // Tab 一次會送兩個 KeyDown(keyCode=Tab 與 character='\t'),都要吃掉以免 IMGUI 換焦點,
            // 完成邏輯只在 keyCode=Tab 那個事件觸發一次。
            if (current != null && current.type == EventType.KeyDown &&
                (current.keyCode == KeyCode.Tab || current.character == '\t') &&
                GUI.GetNameOfFocusedControl() == inputControlName)
            {
                if (current.keyCode == KeyCode.Tab)
                {
                    _CycleCompletion(current.shift);
                }

                current.Use();
            }

            // ↑/↓ 叫回命令歷史(doskey);到頭/尾時 TryGetPrev/TryGetNext 回 null,不動作。
            if (current != null && current.type == EventType.KeyDown &&
                (current.keyCode == KeyCode.UpArrow || current.keyCode == KeyCode.DownArrow) &&
                GUI.GetNameOfFocusedControl() == inputControlName)
            {
                var recalled = current.keyCode == KeyCode.UpArrow
                    ? _QueryDoskey().TryGetPrev()
                    : _QueryDoskey().TryGetNext();
                if (recalled != null)
                {
                    _ApplyInput(recalled);
                }

                current.Use();
            }

            GUILayout.BeginVertical();

            _ScrollView = GUILayout.BeginScrollView(_ScrollView, GUILayout.Width(Screen.width / 2), GUILayout.Height(Screen.height * 0.9f));

            // _Messages 可能被其他執行緒(log/網路事件)寫入;IMGUI 的 Layout 與 Repaint
            // 兩個 pass 之間若行數改變會拋 "Getting control ..." 例外,
            // 因此只在 Layout 時快照,其餘 pass 沿用同一份。
            if (current != null && current.type == EventType.Layout)
            {
                lock (_MessagesLock)
                {
                    var count = _Messages.Count + (_LastMessage.Length > 0 ? 1 : 0);
                    var lines = new string[count];
                    _Messages.CopyTo(lines, 0);
                    if (_LastMessage.Length > 0)
                    {
                        lines[count - 1] = _LastMessage;
                    }

                    _RenderLines = lines;
                }
            }

            foreach (var message in _RenderLines)
            {
                GUILayout.Label(message);
            }

            GUILayout.EndScrollView();

            GUILayout.EndVertical();

            GUILayout.BeginHorizontal();
            GUI.SetNextControlName(inputControlName);
            _Input = GUILayout.TextField(_Input);
            var submitByButton = GUILayout.Button("Send", GUILayout.Width(60));
            GUILayout.EndHorizontal();

            // 使用者手動編輯過就重新計算候選。
            if (_CompletionMatches != null && _Input != _CompletionApplied)
            {
                _ResetCompletion();
            }

            if ((submitByButton || submitByKey) && _Input != string.Empty)
            {
                var line = _Input;
                _Input = "";
                _ResetCompletion();
                Submit(line);
            }
        }

        void _CycleCompletion(bool reverse)
        {
            if (_CompletionMatches == null)
            {
                // 只補第一個 token(指令名),已輸入參數時不動作。
                if (_Input.IndexOf(' ') >= 0)
                {
                    return;
                }

                var matches = _CommandNames
                    .Distinct()
                    .Where(name => name.StartsWith(_Input, System.StringComparison.OrdinalIgnoreCase))
                    .OrderBy(name => name, System.StringComparer.OrdinalIgnoreCase)
                    .ToList();
                if (matches.Count == 0)
                {
                    return;
                }

                _CompletionMatches = matches;
                _CompletionIndex = reverse ? matches.Count - 1 : 0;
            }
            else
            {
                var count = _CompletionMatches.Count;
                _CompletionIndex = (_CompletionIndex + (reverse ? count - 1 : 1)) % count;
            }

            _ApplyInput(_CompletionMatches[_CompletionIndex]);
            _CompletionApplied = _Input;
        }

        void _ApplyInput(string text)
        {
            _Input = text;

            // 聚焦中的 TextField 以內部 TextEditor 狀態顯示,直接改 _Input 畫面不會更新,需同步。
            if (GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl) is TextEditor editor)
            {
                editor.text = _Input;
                editor.cursorIndex = _Input.Length;
                editor.selectIndex = _Input.Length;
            }
        }

        void _ResetCompletion()
        {
            _CompletionMatches = null;
            _CompletionIndex = 0;
            _CompletionApplied = null;
        }

        /// <summary>
        /// 以程式的方式送出一行指令,效果等同在輸入框輸入後按下 Send。
        /// 指令執行時的例外會顯示在主控台,不會向外拋出。
        /// </summary>
        public void Submit(string line)
        {
            _WriteLine("> " + line);
            var args = line.Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
            if (args.Length == 0)
            {
                return;
            }

            _QueryDoskey().Record(line);

            try
            {
                _OutputEvent?.Invoke(args);
            }
            catch (System.Exception e)
            {
                System.Exception inner = e;
                while (inner.InnerException != null)
                {
                    inner = inner.InnerException;
                }

                _WriteLine($"command error: {inner.GetType().Name}: {inner.Message}");
            }
        }

        public void WriteLine(string text)
        {
            var lines = text.Split(new[] { '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                _WriteLine(line);
            }
        }

        void _WriteLine(string text)
        {
            lock (_MessagesLock)
            {
                _Messages.Enqueue(_LastMessage + text);
                while (_Messages.Count > MaxLineCount)
                {
                    _Messages.Dequeue();
                }

                _LastMessage = "";
            }

            _ScrollView.y = Mathf.Infinity;
        }

        event PinionCore.Utility.Console.OnOutput PinionCore.Utility.Console.IInput.OutputEvent
        {
            add { _OutputEvent += value; }
            remove { _OutputEvent -= value; }
        }

        void PinionCore.Utility.Console.IViewer.Write(string message)
        {
            lock (_MessagesLock)
            {
                _LastMessage += message;
            }
        }

        void PinionCore.Utility.Console.IViewer.WriteLine(string message)
        {
            _WriteLine(message);
        }
    }
}
