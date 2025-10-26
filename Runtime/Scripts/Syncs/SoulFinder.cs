using PinionCore.NetSync.Syncs.Protocols;
using PinionCore.Remote;
using UnityEngine;


namespace PinionCore.NetSync.Syncs.Ghosts
{
}
namespace PinionCore.NetSync.Syncs.Souls
{/*
    public static class SoulFinder
    {
        public static ISoul Bind<T>(this GameObject gameObject , T soul) where T : class, IObject
        {
            if(!gameObject.TryGetComponent<Soul>(out var soulComponent))
            {
                soulComponent = gameObject.GetComponentInParent<Soul>(true);
            }

            if (soulComponent == null)
            {
                throw new System.Exception($"Soul component not found in {gameObject.name}");
            }

            return soulComponent.Bind(soul);
        }

        public static void Unbind(this GameObject gameObject, ISoul soul)
        {
            if (!gameObject.TryGetComponent<Soul>(out var soulComponent))
            {
                soulComponent = gameObject.GetComponentInParent<Soul>(true);
            }

            if (soulComponent == null)
            {
                throw new System.Exception($"Soul component not found in {gameObject.name}");
            }

            soulComponent.Unbind(soul);
        }

    }
    */
}
