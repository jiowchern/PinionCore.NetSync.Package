using PinionCore.NetSync.Syncs.Protocols;
using UnityEngine;
namespace PinionCore.NetSync.Syncs.Souls
{
    public class Viewport : MonoBehaviour
    {
        public UnityEngine.Collider Collider;
        
        
        public Viewport()
        {
            //_Souls = new System.Collections.Generic.HashSet<Remote.ISoul>();
        }
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            
        }

        // Update is called once per frame
        void Update()
        {

        }

        // get collider trigger event when a collider enters the trigger
        void OnTriggerEnter(Collider other)
        {
            // check if the collider is a ghost
            if (other.gameObject.TryGetComponent<Soul>(out var soul))
            {
                gameObject.Bind<IObject>(soul.gameObject.GetComponent<IObject>());
                gameObject.Bind<ITransform>(soul.gameObject.GetComponent<ITransform>());
            }
        }

    }

}
