using PinionCore.NetSync.Syncs.Protocols;
using UnityEngine;

namespace PinionCore.NetSync.Syncs.Ghosts
{
    public class GhostProvider : MonoBehaviour
    {
        [Tooltip("Ghost 來源;未指派時會改抓同物件上的 IQueryerHost(Client 或 GatewayClient)。")]
        public Client Client;

        public Ghost GhostPrefab;

        PinionCore.Remote.INotifierQueryable _Queryer;

        readonly System.Collections.Generic.Dictionary<EntityId, Ghost> _Ghosts;
        public GhostProvider()
        {
            _Ghosts = new System.Collections.Generic.Dictionary<EntityId, Ghost>();
        }

        PinionCore.Remote.INotifierQueryable _GetQueryer()
        {
            if (_Queryer != null)
            {
                return _Queryer;
            }
            IQueryerHost host = Client != null ? Client : GetComponent<IQueryerHost>();
            if (host == null)
            {
                UnityEngine.Debug.LogError($"[{nameof(GhostProvider)}] 找不到 {nameof(IQueryerHost)},請指派 Client 或與 Client / GatewayClient 掛在同一個 GameObject。", this);
                return null;
            }
            _Queryer = host.Queryer;
            return _Queryer;
        }

        public void Start()
        {
            var queryer = _GetQueryer();
            if (queryer == null)
            {
                return;
            }
            queryer.QueryNotifier<IObject>().Supply += _OnGhostSupply;
            queryer.QueryNotifier<IObject>().Unsupply += _OnGhostUnsupply;

        }
        public void OnDestroy()
        {
            if (_Queryer == null)
            {
                return;
            }
            _Queryer.QueryNotifier<IObject>().Unsupply -= _OnGhostUnsupply;
            _Queryer.QueryNotifier<IObject>().Supply -= _OnGhostSupply;
        }

        private void _OnGhostUnsupply(IObject obj)
        {
            if (!_Ghosts.TryGetValue(obj.Id, out var ghost))
            {
                return;
            }
            ghost.Finial(obj, _Queryer);
            _Ghosts.Remove(obj.Id);
            GameObject.Destroy(ghost.gameObject);
        }

        private void _OnGhostSupply(IObject obj)
        {
            if (GhostPrefab == null)
            {
                UnityEngine.Debug.LogWarning($"[{nameof(GhostProvider)}] 未指派 GhostPrefab,略過此物件的 Ghost 生成。", this);
                return;
            }
            var go = GameObject.Instantiate(GhostPrefab, transform);
            var ghost = go.GetComponent<Ghost>();
            ghost.Initial(obj, _Queryer);
            _Ghosts.Add(obj.Id, ghost);
        }



    }
}
