using PinionCore.NetSync.Syncs.Protocols;
using UnityEngine;

namespace PinionCore.NetSync.Syncs.Ghosts
{
    public class GhostProvider : MonoBehaviour
    {
        public Client Client;

        public Ghost GhostPrefab;

        readonly System.Collections.Generic.Dictionary<int, Ghost> _Ghosts;
        public GhostProvider()
        {
            _Ghosts = new System.Collections.Generic.Dictionary<int, Ghost>();
        }

        public void Start()
        {
            Client.Queryer.QueryNotifier<IObject>().Supply += _OnGhostSupply;
            Client.Queryer.QueryNotifier<IObject>().Unsupply += _OnGhostUnsupply;

        }
        public void OnDestroy()
        {
            Client.Queryer.QueryNotifier<IObject>().Unsupply -= _OnGhostUnsupply;
            Client.Queryer.QueryNotifier<IObject>().Supply -= _OnGhostSupply;
        }

        private void _OnGhostUnsupply(IObject obj)
        {
            var ghost = _Ghosts[obj.Id];
            ghost.Finial(obj, Client.Queryer);
            _Ghosts.Remove(obj.Id);
            GameObject.Destroy(ghost.gameObject);
        }

        private void _OnGhostSupply(IObject obj)
        {
            var go = GameObject.Instantiate(GhostPrefab, transform);
            var ghost = go.GetComponent<Ghost>();
            ghost.Initial(obj, Client.Queryer);
            _Ghosts.Add(obj.Id, ghost);
        }


       
    }
}
