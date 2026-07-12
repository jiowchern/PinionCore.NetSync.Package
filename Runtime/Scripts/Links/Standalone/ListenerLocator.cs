using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.SceneManagement;

namespace PinionCore.NetSync.Standalone
{
    /// <summary>
    /// 描述 Standalone 連線目標的元件;與 <see cref="Connector"/> 掛在同一個物件上,
    /// 讓使用端(如 ClientConsole)從連線物件解析目標,而不需各自持有場景/物件名稱設定。
    /// </summary>
    public class ListenerLocator : MonoBehaviour
    {
        [Tooltip("連線目標;有指派時優先使用(僅限同場景參照)。")]
        public Listener Listener;

        [Tooltip("Listener 未指派時,從此場景查找 Standalone.Listener。")]
        public string SceneName = "";

        [Tooltip("查找時比對掛載 Listener 的物件名稱;留空取場景中第一個。同場景有多個 Listener 時必須指定。")]
        public string ObjectName = "";

        /// <summary>
        /// 取得目標 Listener;找不到時回傳 null。
        /// </summary>
        public Listener Find()
        {
            if (Listener != null)
            {
                return Listener;
            }

            Scene scene = SceneManager.GetSceneByName(SceneName);
            GameObject[] roots = scene.isLoaded ? scene.GetRootGameObjects() : System.Array.Empty<GameObject>();
            IEnumerable<Listener> listeners = roots.SelectMany(root => root.GetComponentsInChildren<Listener>(true));
            return string.IsNullOrEmpty(ObjectName)
                ? listeners.FirstOrDefault()
                : listeners.FirstOrDefault(listener => listener.gameObject.name == ObjectName);
        }
    }
}
