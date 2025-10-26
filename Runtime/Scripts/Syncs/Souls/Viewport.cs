using PinionCore.NetSync.Syncs.Protocols;
using UnityEngine;
namespace PinionCore.NetSync.Syncs.Souls
{
    
    public class Viewport : MonoBehaviour
    {
    
        public UnityEngine.Collider Collider;
        public User User;

        // get collider trigger event when a collider enters the trigger
        public void OnTriggerEnter(Collider other)
        {
            // check if the collider is a ghost
            if (other.gameObject.TryGetComponent<Soul>(out var soul))
            {
                UnityEngine.Debug.Log("Soul Enter");
                soul.UserEnter(User);
            }
        }

        public void OnTriggerExit(Collider other)
        {
            if (other.gameObject.TryGetComponent<Soul>(out var soul))
            {
                soul.UserLeave(User);
                UnityEngine.Debug.Log("Soul Exit");
            }
        }



    }

}
