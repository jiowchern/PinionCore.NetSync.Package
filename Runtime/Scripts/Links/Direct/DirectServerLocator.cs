using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.SceneManagement;

namespace PinionCore.NetSync.Direct
{
    /// <summary>
    /// 描述 Direct 連線目標的元件;與 <see cref="DirectConnector"/> 掛在同一個物件上,
    /// 讓使用端從連線物件解析目標 Server,而不需各自持有場景/物件名稱設定。
    /// </summary>
    public class DirectServerLocator : MonoBehaviour
    {
        [Tooltip("連線目標;有指派時優先使用(僅限同場景參照)。")]
        public Server Server;

        [Tooltip("Server 未指派時,從此場景查找 Server。")]
        public string SceneName = "";

        [Tooltip("查找時比對掛載 Server 的物件名稱;留空取場景中第一個。同場景有多個 Server 時必須指定。")]
        public string ObjectName = "";

        /// <summary>
        /// 取得目標 Server;找不到時回傳 null。
        /// </summary>
        public Server Find()
        {
            if (Server != null)
            {
                return Server;
            }

            Scene scene = SceneManager.GetSceneByName(SceneName);
            GameObject[] roots = scene.isLoaded ? scene.GetRootGameObjects() : System.Array.Empty<GameObject>();
            IEnumerable<Server> servers = roots.SelectMany(root => root.GetComponentsInChildren<Server>(true));
            return string.IsNullOrEmpty(ObjectName)
                ? servers.FirstOrDefault()
                : servers.FirstOrDefault(server => server.gameObject.name == ObjectName);
        }
    }
}
