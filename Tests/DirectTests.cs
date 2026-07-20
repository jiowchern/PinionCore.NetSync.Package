using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace PinionCore.NetSync.Tests
{
    public class DirectTests
    {
        class EchoSoul : IEchoable
        {
            PinionCore.Remote.Value<int> IEchoable.Echo(int value)
            {
                return value;
            }
        }

        /// <summary>
        /// Direct 直通模式端到端:DirectConnector.Connect(Server) 後,
        /// 客戶端 Supply 取得的 ghost 即伺服器端 Soul 實例本身(零序列化的決定性證據),
        /// 方法呼叫回傳值正確。訂閱在 Connect 前完成,同時驗證 DirectEntryRelay 延後接線。
        /// </summary>
        [UnityTest]
        public IEnumerator DirectClientServerEndToEnd()
        {
            var provider = ScriptableObject.CreateInstance<TestProtocolProvider>();

            var serverGo = new GameObject("Server");
            var server = serverGo.AddComponent<Server>();
            // Direct 模式不經 SessionEngine,但 Server.Start 仍需 Provider(見 README 限制)
            server.Provider = provider;
            if (server.BinderEvent == null)
            {
                server.BinderEvent = new UnityEngine.Events.UnityEvent<Server.BinderCommand>();
            }
            var soul = new EchoSoul();
            server.BinderEvent.AddListener(command =>
            {
                if (command.Status == Server.BinderCommand.OperatorStatus.Add)
                {
                    command.Binder.Bind<IEchoable>(soul);
                }
            });

            var clientGo = new GameObject("DirectClient");
            var client = clientGo.AddComponent<Direct.DirectClient>();
            var connector = clientGo.AddComponent<Direct.DirectConnector>();

            IEchoable echo = null;
            client.Queryer.QueryNotifier<IEchoable>().Supply += e => echo = e;

            // 等待 Start
            yield return null;

            connector.Connect(server);

            for (var i = 0; i < 600 && echo == null; ++i)
            {
                yield return null;
            }
            Assert.NotNull(echo, "客戶端未能以 Direct 模式取得 IEchoable 服務");
            Assert.IsTrue(ReferenceEquals(echo, soul), "Direct 模式的 ghost 應為 Soul 實例本身(共用參考)");

            int? result = null;
            echo.Echo(42).OnValue += (v, error) => result = v;
            for (var i = 0; i < 600 && !result.HasValue; ++i)
            {
                yield return null;
            }
            Assert.IsTrue(result.HasValue, "Echo 呼叫未收到回傳值");
            Assert.AreEqual(42, result.Value);

            Object.Destroy(clientGo);
            Object.Destroy(serverGo);
            Object.Destroy(provider);
            yield return null;
        }

        /// <summary>
        /// Disconnect 後 Unsupply 觸發;且比照遊戲端(WorldsEntry/UsersEntry)在
        /// BinderCommand.Remove 時呼叫 Unbind 的斷線清理模式,Server 下一幀 drain Remove
        /// 事件補呼叫 Unbind 時不得丟例外(Shutdown 已同步撤銷的容忍語意)。
        /// </summary>
        [UnityTest]
        public IEnumerator DirectDisconnectUnsuppliesAndUnbindTolerated()
        {
            var provider = ScriptableObject.CreateInstance<TestProtocolProvider>();

            var serverGo = new GameObject("Server");
            var server = serverGo.AddComponent<Server>();
            server.Provider = provider;
            if (server.BinderEvent == null)
            {
                server.BinderEvent = new UnityEngine.Events.UnityEvent<Server.BinderCommand>();
            }
            var soul = new EchoSoul();
            PinionCore.Remote.ISoul boundSoul = null;
            server.BinderEvent.AddListener(command =>
            {
                if (command.Status == Server.BinderCommand.OperatorStatus.Add)
                {
                    boundSoul = command.Binder.Bind<IEchoable>(soul);
                }
                else
                {
                    command.Binder.Unbind(boundSoul);
                }
            });

            var clientGo = new GameObject("DirectClient");
            var client = clientGo.AddComponent<Direct.DirectClient>();
            var connector = clientGo.AddComponent<Direct.DirectConnector>();

            IEchoable supplied = null;
            IEchoable unsupplied = null;
            client.Queryer.QueryNotifier<IEchoable>().Supply += e => supplied = e;
            client.Queryer.QueryNotifier<IEchoable>().Unsupply += e => unsupplied = e;

            yield return null;

            connector.Connect(server);
            for (var i = 0; i < 600 && supplied == null; ++i)
            {
                yield return null;
            }
            Assert.NotNull(supplied, "客戶端未能取得 IEchoable 服務");

            // Shutdown 比照網路模式 Disable 的同步語意:立即撤銷
            connector.Disconnect();
            Assert.IsTrue(ReferenceEquals(supplied, unsupplied), "Disconnect 後應立即 Unsupply");
            Assert.IsFalse(connector.IsConnect());

            // 讓 Server drain Remove 事件 → 遊戲端模式補呼叫 Unbind,不得丟例外
            // (若丟例外,UnityEvent 會 LogException,測試即失敗)
            for (var i = 0; i < 10; ++i)
            {
                yield return null;
            }

            Object.Destroy(clientGo);
            Object.Destroy(serverGo);
            Object.Destroy(provider);
            yield return null;
        }

        /// <summary>
        /// 重連:Connect → Disconnect → Connect,ghost 重新 Supply。
        /// </summary>
        [UnityTest]
        public IEnumerator DirectReconnect()
        {
            var provider = ScriptableObject.CreateInstance<TestProtocolProvider>();

            var serverGo = new GameObject("Server");
            var server = serverGo.AddComponent<Server>();
            server.Provider = provider;
            if (server.BinderEvent == null)
            {
                server.BinderEvent = new UnityEngine.Events.UnityEvent<Server.BinderCommand>();
            }
            var soul = new EchoSoul();
            server.BinderEvent.AddListener(command =>
            {
                if (command.Status == Server.BinderCommand.OperatorStatus.Add)
                {
                    command.Binder.Bind<IEchoable>(soul);
                }
            });

            var clientGo = new GameObject("DirectClient");
            var client = clientGo.AddComponent<Direct.DirectClient>();
            var connector = clientGo.AddComponent<Direct.DirectConnector>();

            var supplyCount = 0;
            client.Queryer.QueryNotifier<IEchoable>().Supply += e => supplyCount++;

            yield return null;

            connector.Connect(server);
            for (var i = 0; i < 600 && supplyCount == 0; ++i)
            {
                yield return null;
            }
            Assert.AreEqual(1, supplyCount, "首次連線未 Supply");

            connector.Disconnect();
            yield return null;

            connector.Connect(server);
            for (var i = 0; i < 600 && supplyCount < 2; ++i)
            {
                yield return null;
            }
            Assert.AreEqual(2, supplyCount, "重連後未重新 Supply");

            Object.Destroy(clientGo);
            Object.Destroy(serverGo);
            Object.Destroy(provider);
            yield return null;
        }

        /// <summary>
        /// 晚訂閱補發:連線且 Supply 發生後才訂閱,仍能收到已供給的實例(Depot 補發語意)。
        /// </summary>
        [UnityTest]
        public IEnumerator DirectLateSubscribeReplay()
        {
            var provider = ScriptableObject.CreateInstance<TestProtocolProvider>();

            var serverGo = new GameObject("Server");
            var server = serverGo.AddComponent<Server>();
            server.Provider = provider;
            if (server.BinderEvent == null)
            {
                server.BinderEvent = new UnityEngine.Events.UnityEvent<Server.BinderCommand>();
            }
            var soul = new EchoSoul();
            server.BinderEvent.AddListener(command =>
            {
                if (command.Status == Server.BinderCommand.OperatorStatus.Add)
                {
                    command.Binder.Bind<IEchoable>(soul);
                }
            });

            var clientGo = new GameObject("DirectClient");
            var client = clientGo.AddComponent<Direct.DirectClient>();
            var connector = clientGo.AddComponent<Direct.DirectConnector>();

            IEchoable early = null;
            client.Queryer.QueryNotifier<IEchoable>().Supply += e => early = e;

            yield return null;

            connector.Connect(server);
            for (var i = 0; i < 600 && early == null; ++i)
            {
                yield return null;
            }
            Assert.NotNull(early);

            // 晚訂閱
            IEchoable late = null;
            client.Queryer.QueryNotifier<IEchoable>().Supply += e => late = e;
            Assert.IsTrue(ReferenceEquals(soul, late), "晚訂閱應立即收到已供給的實例");

            Object.Destroy(clientGo);
            Object.Destroy(serverGo);
            Object.Destroy(provider);
            yield return null;
        }
    }
}
