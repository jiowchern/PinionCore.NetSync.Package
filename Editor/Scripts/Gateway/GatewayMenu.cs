using PinionCore.NetSync.Gateways;
using UnityEditor;
using UnityEngine;

namespace PinionCore.NetSync.Editor
{
    /// <summary>
    /// Hierarchy 右鍵選單:一鍵生成接好線的 Gateway 物件。
    /// </summary>
    public static class GatewayMenu
    {
        const string _MenuRoot = "GameObject/PinionCore/NetSync/";

        [MenuItem(_MenuRoot + "Gateway Router", false, 10)]
        public static void CreateRouter(MenuCommand command)
        {
            var go = _Create("GatewayRouter", command);
            go.AddComponent<GatewayRouter>();
            _CreateEndpoint(go, "RegistryEndpoint", GatewayRouterEndpointType.Registry);
            _CreateEndpoint(go, "SessionEndpoint", GatewayRouterEndpointType.Session);
        }

        static GameObject _CreateEndpoint(GameObject router, string name, GatewayRouterEndpointType type)
        {
            var go = new GameObject(name);
            go.transform.SetParent(router.transform);
            var endpoint = go.AddComponent<GatewayRouterEndpoint>();
            endpoint.Endpoint = type;
            endpoint.Router = router.GetComponent<GatewayRouter>();
            // 與 Server 相同的監聽架構:掛任一 IListenableHost 對應的 Listener 即可。
            // 預設給 TcpListener + 自動 Bind;WebGL 需求可自行替換或並掛 WebListener。
            var listener = go.AddComponent<PinionCore.NetSync.Tcp.TcpListener>();
            go.AddComponent<PinionCore.NetSync.Kits.TcpStartToBind>().Listener = listener;
            return go;
        }

        [MenuItem(_MenuRoot + "Gateway Service (Server + Registry)", false, 11)]
        public static void CreateService(MenuCommand command)
        {
            var go = _Create("GatewayService", command);
            var provider = _FindSingleProtocolProvider();
            var server = go.AddComponent<Server>();
            server.Provider = provider;
            var registry = go.AddComponent<GatewayRegistry>();
            registry.Provider = provider;
            go.AddComponent<PinionCore.NetSync.Tcp.TcpConnector>();
        }

        [MenuItem(_MenuRoot + "Gateway Client (TCP)", false, 12)]
        public static void CreateTcpClient(MenuCommand command)
        {
            var go = _CreateClient(command);
            go.AddComponent<PinionCore.NetSync.Tcp.TcpConnector>();
        }

        [MenuItem(_MenuRoot + "Gateway Client (WebSocket)", false, 13)]
        public static void CreateWebClient(MenuCommand command)
        {
            var go = _CreateClient(command);
            go.AddComponent<PinionCore.NetSync.Web.WebConnector>();
        }

        static GameObject _CreateClient(MenuCommand command)
        {
            var go = _Create("GatewayClient", command);
            var client = go.AddComponent<GatewayClient>();
            client.Provider = _FindSingleProtocolProvider();
            return go;
        }

        [MenuItem(_MenuRoot + "Gateway Client (Standalone)", false, 14)]
        public static void CreateStandaloneClient(MenuCommand command)
        {
            var go = _CreateClient(command);
            // Connector 的 Listener 欄位需指向 Router Session 端點上的 Standalone.Listener。
            go.AddComponent<PinionCore.NetSync.Standalone.Connector>();
        }

        static ProtocolProvider _FindSingleProtocolProvider()
        {
            var guids = AssetDatabase.FindAssets("t:" + nameof(ProtocolProvider));
            if (guids.Length != 1)
            {
                return null;
            }
            var path = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<ProtocolProvider>(path);
        }

        static GameObject _Create(string name, MenuCommand command)
        {
            var go = new GameObject(name);
            GameObjectUtility.SetParentAndAlign(go, command.context as GameObject);
            Undo.RegisterCreatedObjectUndo(go, $"Create {name}");
            Selection.activeObject = go;
            return go;
        }
    }
}
