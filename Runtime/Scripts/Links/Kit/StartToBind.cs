using PinionCore.NetSync;
using UnityEngine;


namespace PinionCore.NetSync.Kits
{

    public class StartToBind<TListener> : MonoBehaviour where TListener : IBindable
    {

        public TListener Listener;
        void Start()
        {
            Listener.Bind();

        }

        void OnDestroy()
        {
            Listener.Close();
        }
    }
}
