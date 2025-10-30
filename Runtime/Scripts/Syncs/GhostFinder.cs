using PinionCore.NetSync.Syncs.Protocols;
using PinionCore.Remote;
using UnityEngine;


namespace PinionCore.NetSync.Syncs.Ghosts
{
    public static class GhostFinder
    {
        public static INotifier<T> Query<T>(this GameObject gameObject) where T : class, IObject
        {
            if (!gameObject.TryGetComponent<Ghost>(out var ghostComponent))
            {
                ghostComponent = gameObject.GetComponentInParent<Ghost>(true);
            }

            if (ghostComponent == null)
            {
                return null;
            }

            return ghostComponent.Query<T>();
        }

    }
}
