
using PinionCore.NetSync.Syncs.Protocols;
using PinionCore.Remote;

using UnityEngine;



namespace PinionCore.NetSync.Syncs.Souls
{
    
    public class Soul : MonoBehaviour , IObject
    {
        class IntProvider : ILandlordProviable<int>
        {
            int _Num;
            public IntProvider()
            {
                _Num = 0;
            }

            int ILandlordProviable<int>.Spawn()
            {
                return ++_Num;
            }
        }
        
        static readonly PinionCore.Remote.Landlord<int> landlord = new PinionCore.Remote.Landlord<int>(new IntProvider());
        public readonly int Id;

        Property<int> IObject.Id => new Property<int>(gameObject.GetInstanceID());

        public Soul()
        {
            Id = landlord.Rent();
        
        }
        ~Soul()
        {
            landlord.Return(Id);
        }

        public void UserEnter(User user)
        {
            BroadcastMessage("UserEnter", user);
        }
        public void UserLeave(User user)
        {
            BroadcastMessage("UserLeave", user);
        }

    }

}
