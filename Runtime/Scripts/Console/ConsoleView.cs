using System.Collections.Generic;

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

        readonly Queue<string> _Messages;
        readonly object _MessagesLock;
        string _LastMessage;
        string _Input;
        Vector2 _ScrollView;
        bool _LogHooked;

        PinionCore.Utility.Console _Console;
        event PinionCore.Utility.Console.OnOutput _OutputEvent;

        public ConsoleView()
        {
            _Messages = new Queue<string>();
            _MessagesLock = new object();
            _LastMessage = "";
            _Input = "";
            _ScrollView = Vector2.zero;
        }

        public PinionCore.Utility.Command Command => _QueryConsole().Command;

        PinionCore.Utility.Console _QueryConsole()
        {
            if (_Console == null)
            {
                _Console = new PinionCore.Utility.Console(this, this);
            }

            return _Console;
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
            var submitByKey = false;
            Event current = Event.current;
            if (current != null && current.type == EventType.KeyDown && current.keyCode == KeyCode.Return)
            {
                submitByKey = true;
            }

            GUILayout.BeginVertical();

            _ScrollView = GUILayout.BeginScrollView(_ScrollView, GUILayout.Width(Screen.width / 2), GUILayout.Height(Screen.height * 0.9f));

            lock (_MessagesLock)
            {
                foreach (var message in _Messages)
                {
                    GUILayout.Label(message);
                }

                if (_LastMessage.Length > 0)
                {
                    GUILayout.Label(_LastMessage);
                }
            }

            GUILayout.EndScrollView();

            GUILayout.EndVertical();

            GUILayout.BeginHorizontal();
            _Input = GUILayout.TextField(_Input);
            var submitByButton = GUILayout.Button("Send", GUILayout.Width(60));
            GUILayout.EndHorizontal();

            if ((submitByButton || submitByKey) && _Input != string.Empty)
            {
                var line = _Input;
                _Input = "";
                Submit(line);
            }
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
