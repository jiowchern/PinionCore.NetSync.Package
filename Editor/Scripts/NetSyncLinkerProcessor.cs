#if UNITY_EDITOR
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.UnityLinker;
using UnityEngine;

namespace PinionCore.NetSync.Editor
{
    /// <summary>
    /// 針對「所有平台」：若該平台的 Managed Stripping Level 非 Disabled，
    /// 於建置時提供額外的 link.xml（不汙染 Assets）。
    /// 產出：Library/PinionCore_Link/link.generated.xml
    /// </summary>
    public sealed class NetSyncLinkerProcessor : IUnityLinkerProcessor
    {
        public int callbackOrder => -100;

        public string GenerateAdditionalLinkXmlFile(BuildReport report, UnityLinkerBuildPipelineData data)
        {
            if (report == null) return null;

            // 取得本次建置的平台與平台群組
            var target = report.summary.platform;
            var group = BuildPipeline.GetBuildTargetGroup(target);

            // 讀取該平台的 Managed Stripping Level
            var stripping = GetManagedStrippingLevelCompat(group);

            // 僅在「該平台的裁剪未停用」時輸出 link.xml
            if (stripping == ManagedStrippingLevel.Disabled)
                return null;

            // 你也可以在這裡加上額外條件，例如僅在 IL2CPP 執行：
            // var backend = PlayerSettings.GetScriptingBackend(group);
            // if (backend != ScriptingImplementation.IL2CPP) return null;

            var outPath = Path.GetFullPath("Library/PinionCore_Link/link.generated.xml");
            Directory.CreateDirectory(Path.GetDirectoryName(outPath)!);

            File.WriteAllText(outPath, BuildLinkXml(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

            Debug.Log($"[PinionCore] link.xml 已產生：{outPath}\n" +
                      $"Target: {target}, Group: {group}, Stripping: {stripping}");

            return outPath;
        }

        /// <summary>
        /// Unity 6000.2 推薦用 NamedBuildTarget；舊版維持相容。
        /// </summary>
        private static ManagedStrippingLevel GetManagedStrippingLevelCompat(BuildTargetGroup group)
        {
#if UNITY_6000_0_OR_NEWER
            var nbt = NamedBuildTarget.FromBuildTargetGroup(group);
            return PlayerSettings.GetManagedStrippingLevel(nbt);
#else
            // 2022/2023 舊 API（在 6000 會標示 obsolete）
            return PlayerSettings.GetManagedStrippingLevel(group);
#endif
        }

        /// <summary>
        /// 以你提供的清單組出 link.xml 內容（全平台共用）。
        /// </summary>
        private static string BuildLinkXml()
        {
            var sb = new StringBuilder();
            sb.AppendLine("<linker>");

            // ── PinionCore.NetSync Package：僅保留指定型別上的指定方法 ──
            sb.AppendLine(@"  <assembly fullname=""PinionCore.NetSync"">");
            sb.AppendLine(@"    <type fullname=""PinionCore.NetSync.Web.WebSocketStream"">");
            sb.AppendLine(@"      <method name=""WebSocketAllocate"" />");
            sb.AppendLine(@"      <method name=""WebSocketSetOnOpen"" />");
            sb.AppendLine(@"      <method name=""WebSocketSetOnMessage"" />");
            sb.AppendLine(@"      <method name=""WebSocketSetOnError"" />");
            sb.AppendLine(@"      <method name=""WebSocketSetOnClose"" />");
            sb.AppendLine(@"    </type>");
            sb.AppendLine(@"  </assembly>");

            // ── PinionCore.Remote Core Assemblies (required for RMI) ──
            sb.AppendLine(@"  <assembly fullname=""PinionCore.Remote"" preserve=""all"" />");
            sb.AppendLine(@"  <assembly fullname=""PinionCore.Remote.Ghost"" preserve=""all"" />");
            sb.AppendLine(@"  <assembly fullname=""PinionCore.Remote.Soul"" preserve=""all"" />");
            sb.AppendLine(@"  <assembly fullname=""PinionCore.Remote.Client"" preserve=""all"" />");
            sb.AppendLine(@"  <assembly fullname=""PinionCore.Remote.Server"" preserve=""all"" />");
            sb.AppendLine(@"  <assembly fullname=""PinionCore.Remote.Gateway"" preserve=""all"" />");
            sb.AppendLine(@"  <assembly fullname=""PinionCore.Remote.Gateway.Protocols"" preserve=""all"" />");
            sb.AppendLine(@"  <assembly fullname=""PinionCore.Remote.Protocol.Identify"" preserve=""all"" />");
            sb.AppendLine(@"  <assembly fullname=""PinionCore.Remote.Standalone"" preserve=""all"" />");

            // ── PinionCore Utility and Serialization ──
            sb.AppendLine(@"  <assembly fullname=""PinionCore.Utility"" preserve=""all"" />");
            sb.AppendLine(@"  <assembly fullname=""PinionCore.Serialization"" preserve=""all"" />");
            sb.AppendLine(@"  <assembly fullname=""PinionCore.Network"" preserve=""all"" />");

            sb.AppendLine("</linker>");
            return sb.ToString();
        }
    }
}
#endif
