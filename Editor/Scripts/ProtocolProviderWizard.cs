using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace PinionCore.NetSync.Editor
{
    /// <summary>
    /// 一鍵產生 Protocol「三件套」精靈：
    ///   {Name}ProtocolCreator.cs  — 觸發 Source Generator 的進入點 (partial class + [Creator])
    ///   {Name}ProtocolProvider.cs — 指派給 Server / Client 的 ScriptableObject 子類
    ///   {Name}Protocol.asset       — 上述 Provider 的實例資產
    /// 三者建立於使用者指定的資料夾;該資料夾須位於 reference 了 PinionCore.NetSync 的
    /// asmdef 底下,Source Generator 才會在該組件內掃描介面並生成完整 protocol。
    /// </summary>
    public class ProtocolProviderWizard : EditorWindow
    {
        // 寫完 script、等 domain reload 後才建立 .asset(需等 Provider 型別編譯完成),
        // 因此把待辦資訊存進 SessionState 跨重載接力。
        const string PendingKey = "PinionCore.NetSync.ProtocolProviderWizard.PendingAsset";

        [SerializeField] string _Name = "Game";
        [SerializeField] string _Namespace = "";
        [SerializeField] string _MenuName = "";
        [SerializeField] DefaultAsset _Folder;

        [MenuItem("Assets/Create/PinionCore/NetSync/Protocol Provider (三件套)", priority = 0)]
        [MenuItem("Tools/PinionCore/NetSync/Create Protocol Provider...")]
        static void Open()
        {
            var window = GetWindow<ProtocolProviderWizard>(true, "NetSync Protocol Provider", true);
            window.minSize = new Vector2(480f, 360f);
            window._InitFolderFromSelection();
            window.Show();
        }

        void _InitFolderFromSelection()
        {
            var path = "Assets";
            var obj = Selection.activeObject;
            if (obj != null)
            {
                var p = AssetDatabase.GetAssetPath(obj);
                if (!string.IsNullOrEmpty(p))
                {
                    path = AssetDatabase.IsValidFolder(p)
                        ? p
                        : Path.GetDirectoryName(p).Replace('\\', '/');
                }
            }
            _Folder = AssetDatabase.LoadAssetAtPath<DefaultAsset>(path);
        }

        void OnGUI()
        {
            EditorGUILayout.HelpBox(
                "依介面定義一鍵產生 Protocol 三件套:Creator + Provider + .asset。\n" +
                "產生後把 .asset 同時指派到 Server 與 Client 的 Provider 欄位即可。",
                MessageType.Info);

            EditorGUILayout.Space();

            _Name = EditorGUILayout.TextField(new GUIContent("名稱 (Name)",
                "產生的型別前綴,例如 Game → GameProtocolCreator / GameProtocolProvider / GameProtocol.asset"), _Name);
            _Namespace = EditorGUILayout.TextField(new GUIContent("命名空間 (選填)",
                "留空則產生於全域命名空間"), _Namespace);
            _MenuName = EditorGUILayout.TextField(new GUIContent("CreateAssetMenu (選填)",
                "Provider 的右鍵建立選單路徑;留空則自動帶入"), _MenuName);
            _Folder = (DefaultAsset)EditorGUILayout.ObjectField(new GUIContent("目標資料夾",
                "三件套的輸出位置,須在 reference 了 PinionCore.NetSync 的 asmdef 底下"),
                _Folder, typeof(DefaultAsset), false);

            EditorGUILayout.Space();

            var errors = new List<string>();
            var warnings = new List<string>();
            _Validate(errors, warnings);

            var folder = _FolderPath();
            EditorGUILayout.LabelField("將建立", EditorStyles.boldLabel);
            using (new EditorGUI.IndentLevelScope())
            {
                EditorGUILayout.LabelField($"{folder}/{_Name}ProtocolCreator.cs");
                EditorGUILayout.LabelField($"{folder}/{_Name}ProtocolProvider.cs");
                EditorGUILayout.LabelField($"{folder}/{_Name}Protocol.asset");
            }

            EditorGUILayout.Space();

            foreach (var w in warnings)
                EditorGUILayout.HelpBox(w, MessageType.Warning);

            // asmdef reference 防呆:若目標 asmdef 未 reference PinionCore.NetSync,提供一鍵補上。
            var asmdefPath = _FindAsmdefForFolder(folder);
            if (asmdefPath != null && !_AsmdefReferencesNetSync(asmdefPath))
            {
                EditorGUILayout.HelpBox(
                    $"組件 '{Path.GetFileNameWithoutExtension(asmdefPath)}' 未 reference PinionCore.NetSync,\n" +
                    "Source Generator 不會在此組件運作,產生的 Create() 會回傳 null。",
                    MessageType.Warning);
                if (GUILayout.Button("為該 asmdef 加入 PinionCore.NetSync 參考"))
                {
                    _AddNetSyncReference(asmdefPath);
                }
            }
            else if (asmdefPath == null)
            {
                EditorGUILayout.HelpBox(
                    "目標資料夾未被任何 asmdef 涵蓋(屬於預設 Assembly-CSharp)。\n" +
                    "建議改放在一個 reference 了 PinionCore.NetSync 的 asmdef 底下,以確保 Source Generator 運作。",
                    MessageType.Info);
            }

            foreach (var e in errors)
                EditorGUILayout.HelpBox(e, MessageType.Error);

            EditorGUILayout.Space();

            using (new EditorGUI.DisabledScope(errors.Count > 0))
            {
                if (GUILayout.Button("建立三件套", GUILayout.Height(28f)))
                {
                    _Create();
                }
            }
        }

        void _Validate(List<string> errors, List<string> warnings)
        {
            if (string.IsNullOrEmpty(_Name) || !Regex.IsMatch(_Name, "^[A-Za-z_][A-Za-z0-9_]*$"))
                errors.Add("名稱必須是合法的 C# 識別字(字母或底線開頭,僅含字母/數字/底線)。");

            if (!string.IsNullOrEmpty(_Namespace) &&
                !Regex.IsMatch(_Namespace, "^[A-Za-z_][A-Za-z0-9_]*(\\.[A-Za-z_][A-Za-z0-9_]*)*$"))
                errors.Add("命名空間格式不合法。");

            var folder = _FolderPath();
            if (!AssetDatabase.IsValidFolder(folder))
                errors.Add("請選擇一個 Assets 底下的有效資料夾。");

            if (errors.Count == 0)
            {
                foreach (var path in new[]
                {
                    $"{folder}/{_Name}ProtocolCreator.cs",
                    $"{folder}/{_Name}ProtocolProvider.cs",
                    $"{folder}/{_Name}Protocol.asset",
                })
                {
                    if (File.Exists(path))
                        errors.Add($"檔案已存在:{path}(請改用其他名稱或先移除舊檔)。");
                }
            }
        }

        string _FolderPath()
        {
            if (_Folder == null)
                return "Assets";
            var p = AssetDatabase.GetAssetPath(_Folder);
            return AssetDatabase.IsValidFolder(p) ? p : "Assets";
        }

        string _EffectiveMenuName()
        {
            return string.IsNullOrEmpty(_MenuName)
                ? $"PinionCore/NetSync/{_Name} Protocol Provider"
                : _MenuName;
        }

        void _Create()
        {
            var folder = _FolderPath();
            var creatorPath = $"{folder}/{_Name}ProtocolCreator.cs";
            var providerPath = $"{folder}/{_Name}ProtocolProvider.cs";
            var assetPath = $"{folder}/{_Name}Protocol.asset";

            var utf8 = new UTF8Encoding(false);
            File.WriteAllText(creatorPath, _CreatorSource(), utf8);
            File.WriteAllText(providerPath, _ProviderSource(), utf8);

            var providerFullName = string.IsNullOrEmpty(_Namespace)
                ? $"{_Name}ProtocolProvider"
                : $"{_Namespace}.{_Name}ProtocolProvider";
            // 待 domain reload 後由 _TryCreatePendingAsset 接手建立資產。
            SessionState.SetString(PendingKey, providerFullName + "|" + assetPath);

            AssetDatabase.Refresh();
            Close();
        }

        string _CreatorSource()
        {
            var ns = !string.IsNullOrEmpty(_Namespace);
            var ind = ns ? "    " : "";
            var sb = new StringBuilder();
            if (ns)
            {
                sb.AppendLine($"namespace {_Namespace}");
                sb.AppendLine("{");
            }
            sb.AppendLine($"{ind}// 由 PinionCore.Remote.Tools.Protocol.Sources Source Generator 在「本組件內」");
            sb.AppendLine($"{ind}// 掃描所有繼承 IObject 的介面,生成完整 protocol。");
            sb.AppendLine($"{ind}// 重要:協議介面必須與本檔位於同一個 assembly,Source Generator 只掃描當前組件。");
            sb.AppendLine($"{ind}public static partial class {_Name}ProtocolCreator");
            sb.AppendLine($"{ind}{{");
            sb.AppendLine($"{ind}    public static PinionCore.Remote.IProtocol Create()");
            sb.AppendLine($"{ind}    {{");
            sb.AppendLine($"{ind}        PinionCore.Remote.IProtocol protocol = null;");
            sb.AppendLine($"{ind}        _Create(ref protocol);");
            sb.AppendLine($"{ind}        return protocol;");
            sb.AppendLine($"{ind}    }}");
            sb.AppendLine();
            sb.AppendLine($"{ind}    [PinionCore.Remote.Protocol.Creator]");
            sb.AppendLine($"{ind}    static partial void _Create(ref PinionCore.Remote.IProtocol protocol);");
            sb.AppendLine($"{ind}}}");
            if (ns)
                sb.AppendLine("}");
            return sb.ToString();
        }

        string _ProviderSource()
        {
            var ns = !string.IsNullOrEmpty(_Namespace);
            var ind = ns ? "    " : "";
            var sb = new StringBuilder();
            sb.AppendLine("using UnityEngine;");
            sb.AppendLine();
            if (ns)
            {
                sb.AppendLine($"namespace {_Namespace}");
                sb.AppendLine("{");
            }
            sb.AppendLine($"{ind}[CreateAssetMenu(menuName = \"{_EffectiveMenuName()}\", fileName = \"{_Name}Protocol\")]");
            sb.AppendLine($"{ind}public class {_Name}ProtocolProvider : PinionCore.NetSync.ProtocolProvider");
            sb.AppendLine($"{ind}{{");
            sb.AppendLine($"{ind}    readonly PinionCore.Remote.IProtocol _Protocol;");
            sb.AppendLine();
            sb.AppendLine($"{ind}    public {_Name}ProtocolProvider()");
            sb.AppendLine($"{ind}    {{");
            sb.AppendLine($"{ind}        _Protocol = {_Name}ProtocolCreator.Create();");
            sb.AppendLine($"{ind}    }}");
            sb.AppendLine();
            sb.AppendLine($"{ind}    public override PinionCore.Remote.IProtocol Get()");
            sb.AppendLine($"{ind}    {{");
            sb.AppendLine($"{ind}        return _Protocol;");
            sb.AppendLine($"{ind}    }}");
            sb.AppendLine($"{ind}}}");
            if (ns)
                sb.AppendLine("}");
            return sb.ToString();
        }

        // ---- asmdef 偵測與參考補全 ----

        static string _FindAsmdefForFolder(string folder)
        {
            var dir = folder;
            while (!string.IsNullOrEmpty(dir) && dir.StartsWith("Assets"))
            {
                var asmdef = Directory.GetFiles(dir, "*.asmdef", SearchOption.TopDirectoryOnly)
                    .FirstOrDefault();
                if (asmdef != null)
                    return asmdef.Replace('\\', '/');
                if (dir == "Assets")
                    break;
                dir = Path.GetDirectoryName(dir)?.Replace('\\', '/');
            }
            return null;
        }

        static string _NetSyncAsmdefGuid()
        {
            foreach (var g in AssetDatabase.FindAssets("PinionCore.NetSync t:AssemblyDefinitionAsset"))
            {
                var p = AssetDatabase.GUIDToAssetPath(g);
                if (Path.GetFileNameWithoutExtension(p) == "PinionCore.NetSync")
                    return g;
            }
            return null;
        }

        static bool _AsmdefReferencesNetSync(string asmdefPath)
        {
            var text = File.ReadAllText(asmdefPath);
            if (text.Contains("\"PinionCore.NetSync\""))
                return true;
            var guid = _NetSyncAsmdefGuid();
            return guid != null && text.Contains(guid);
        }

        static void _AddNetSyncReference(string asmdefPath)
        {
            var text = File.ReadAllText(asmdefPath);
            var guid = _NetSyncAsmdefGuid();
            var newRef = guid != null ? $"GUID:{guid}" : "PinionCore.NetSync";

            var match = Regex.Match(text, "\"references\"\\s*:\\s*\\[(.*?)\\]", RegexOptions.Singleline);
            if (!match.Success)
            {
                Debug.LogError($"[NetSync] 無法解析 {asmdefPath} 的 references 欄位,請手動加入 PinionCore.NetSync 參考。");
                return;
            }

            var existing = Regex.Matches(match.Groups[1].Value, "\"([^\"]*)\"")
                .Cast<Match>()
                .Select(m => m.Groups[1].Value)
                .ToList();
            if (existing.Contains(newRef))
                return;
            existing.Add(newRef);

            var rebuilt = "\"references\": [\n" +
                string.Join(",\n", existing.Select(r => "        \"" + r + "\"")) +
                "\n    ]";
            text = text.Substring(0, match.Index) + rebuilt + text.Substring(match.Index + match.Length);
            File.WriteAllText(asmdefPath, text, new UTF8Encoding(false));
            AssetDatabase.ImportAsset(asmdefPath);
            Debug.Log($"[NetSync] 已為 {Path.GetFileNameWithoutExtension(asmdefPath)} 加入 PinionCore.NetSync 參考。");
        }

        // ---- domain reload 後接力建立 .asset ----

        [InitializeOnLoadMethod]
        static void _OnLoad()
        {
            EditorApplication.delayCall += _TryCreatePendingAsset;
        }

        static void _TryCreatePendingAsset()
        {
            var pending = SessionState.GetString(PendingKey, "");
            if (string.IsNullOrEmpty(pending))
                return;

            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += _TryCreatePendingAsset;
                return;
            }

            var parts = pending.Split('|');
            if (parts.Length != 2)
            {
                SessionState.EraseString(PendingKey);
                return;
            }
            var providerFullName = parts[0];
            var assetPath = parts[1];

            var type = TypeCache.GetTypesDerivedFrom<ProtocolProvider>()
                .FirstOrDefault(t => t.FullName == providerFullName);
            if (type == null)
            {
                SessionState.EraseString(PendingKey);
                Debug.LogWarning(
                    $"[NetSync] 找不到型別 {providerFullName}(可能編譯失敗),Provider 資產未建立。" +
                    "請修正編譯錯誤後,於該 Provider 的右鍵 Create 選單手動建立資產。");
                return;
            }

            SessionState.EraseString(PendingKey);

            if (AssetDatabase.LoadAssetAtPath<ProtocolProvider>(assetPath) != null)
                return;

            var instance = CreateInstance(type);
            AssetDatabase.CreateAsset(instance, assetPath);
            AssetDatabase.SaveAssets();
            Selection.activeObject = instance;
            EditorGUIUtility.PingObject(instance);
            Debug.Log($"[NetSync] 已建立 Protocol 三件套,Provider 資產:{assetPath}");
        }
    }
}
