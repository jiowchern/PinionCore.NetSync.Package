using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using PinionCore.NetSync.Extensions;
using PinionCore.Remote;
using UnityEditor;
using UnityEngine;

namespace PinionCore.NetSync.Editor
{
    /// <summary>
    /// ProtocolProvider(含所有子類)的 Inspector。
    /// 唯讀呈現由 Source Generator 產生的 protocol 內容:
    ///   - Version Hash(IProtocol.VersionCode)
    ///   - MemberMap 已公開支援的 介面 / 方法 / 屬性 / 事件
    ///   - 支援序列化的型別(IProtocol.SerializeTypes)
    /// editorForChildClasses:true → 套用到 {Name}ProtocolProvider 等所有子類資產。
    /// </summary>
    [CustomEditor(typeof(ProtocolProvider), true)]
    [CanEditMultipleObjects]
    public class ProtocolProviderEditor : UnityEditor.Editor
    {
        bool _FoldInterfaces = true;
        bool _FoldMethods;
        bool _FoldProperties;
        bool _FoldEvents;
        bool _FoldSerializeTypes;

        public override void OnInspectorGUI()
        {
            var provider = (ProtocolProvider)target;

            IProtocol protocol;
            try
            {
                protocol = provider.Get();
            }
            catch (Exception e)
            {
                EditorGUILayout.HelpBox(
                    "取得 protocol 時發生例外,可能是 Source Generator 尚未產生內容或編譯失敗:\n" + e.Message,
                    MessageType.Error);
                return;
            }

            if (protocol == null)
            {
                EditorGUILayout.HelpBox(
                    "Get() 回傳 null。請確認:\n" +
                    "1. 本 Provider 所在的 asmdef 已 reference PinionCore.NetSync,\n" +
                    "2. 同組件內有繼承 IObject 的協議介面,\n" +
                    "3. 專案已重新編譯以觸發 Source Generator。",
                    MessageType.Warning);
                return;
            }

            _DrawHash(protocol);
            EditorGUILayout.Space();
            _DrawMemberMap(protocol);
            EditorGUILayout.Space();
            _DrawSerializeTypes(protocol);
        }

        void _DrawHash(IProtocol protocol)
        {
            EditorGUILayout.LabelField("Version Hash", EditorStyles.boldLabel);
            var hash = protocol.VersionCode != null ? protocol.VersionCode.ToHexString() : "null";
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.SelectableLabel(hash,
                    EditorStyles.textField,
                    GUILayout.Height(EditorGUIUtility.singleLineHeight));
                if (GUILayout.Button("Copy", GUILayout.Width(48f)))
                    EditorGUIUtility.systemCopyBuffer = hash;
            }
        }

        void _DrawMemberMap(IProtocol protocol)
        {
            MemberMap map;
            try
            {
                map = protocol.GetMemberMap();
            }
            catch (Exception e)
            {
                EditorGUILayout.HelpBox("讀取 MemberMap 失敗:" + e.Message, MessageType.Error);
                return;
            }

            if (map == null)
            {
                EditorGUILayout.HelpBox("MemberMap 為 null。", MessageType.Warning);
                return;
            }

            EditorGUILayout.LabelField("Member Map", EditorStyles.boldLabel);

            _DrawSection(ref _FoldInterfaces, "Interfaces", map.Interfaces.Item2s,
                (id, type) => $"#{id}  {_FullName(type)}");

            _DrawSection(ref _FoldMethods, "Methods", map.Methods,
                (id, method) => $"#{id}  {_FormatMethod(method)}");

            _DrawSection(ref _FoldProperties, "Properties", map.Properties.Item2s,
                (id, property) => $"#{id}  {_FormatProperty(property)}");

            _DrawSection(ref _FoldEvents, "Events", map.Events,
                (id, evt) => $"#{id}  {_FormatEvent(evt)}");
        }

        void _DrawSerializeTypes(IProtocol protocol)
        {
            var types = protocol.SerializeTypes ?? Array.Empty<Type>();
            _FoldSerializeTypes = EditorGUILayout.Foldout(_FoldSerializeTypes,
                $"Serialize Types ({types.Length})", true);
            if (!_FoldSerializeTypes)
                return;

            using (new EditorGUI.IndentLevelScope())
            {
                if (types.Length == 0)
                {
                    EditorGUILayout.LabelField("(無)");
                    return;
                }
                foreach (var type in types.OrderBy(_FullName, StringComparer.Ordinal))
                    EditorGUILayout.LabelField(_FullName(type));
            }
        }

        // ---- 通用 section 繪製:依 id(key)排序後逐列顯示 ----

        static void _DrawSection<TValue>(
            ref bool fold,
            string title,
            IReadOnlyDictionary<int, TValue> items,
            Func<int, TValue, string> format)
        {
            var count = items != null ? items.Count : 0;
            fold = EditorGUILayout.Foldout(fold, $"{title} ({count})", true);
            if (!fold)
                return;

            using (new EditorGUI.IndentLevelScope())
            {
                if (count == 0)
                {
                    EditorGUILayout.LabelField("(無)");
                    return;
                }
                foreach (var pair in items.OrderBy(p => p.Key))
                    EditorGUILayout.LabelField(format(pair.Key, pair.Value));
            }
        }

        // ---- 型別與成員的可讀化 ----

        static string _FormatMethod(MethodInfo method)
        {
            if (method == null)
                return "null";
            var ps = method.GetParameters()
                .Select(p => $"{_Friendly(p.ParameterType)} {p.Name}");
            return $"{_Friendly(method.ReturnType)} {_Owner(method.DeclaringType)}{method.Name}({string.Join(", ", ps)})";
        }

        static string _FormatProperty(PropertyInfo property)
        {
            if (property == null)
                return "null";
            return $"{_Friendly(property.PropertyType)} {_Owner(property.DeclaringType)}{property.Name}";
        }

        static string _FormatEvent(EventInfo evt)
        {
            if (evt == null)
                return "null";
            return $"{_Friendly(evt.EventHandlerType)} {_Owner(evt.DeclaringType)}{evt.Name}";
        }

        static string _Owner(Type declaringType)
        {
            return declaringType != null ? declaringType.Name + "." : "";
        }

        static string _FullName(Type type)
        {
            if (type == null)
                return "null";
            return !string.IsNullOrEmpty(type.FullName) ? type.FullName : _Friendly(type);
        }

        // 泛型也可讀,例如 Value<Int32>、Notifier<IObject>
        static string _Friendly(Type type)
        {
            if (type == null)
                return "null";
            if (!type.IsGenericType)
                return type.Name;

            var name = type.Name;
            var tick = name.IndexOf('`');
            if (tick >= 0)
                name = name.Substring(0, tick);
            var args = type.GetGenericArguments().Select(_Friendly);
            return $"{name}<{string.Join(", ", args)}>";
        }
    }
}
