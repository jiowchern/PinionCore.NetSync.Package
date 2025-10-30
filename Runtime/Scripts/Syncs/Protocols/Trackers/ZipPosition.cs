namespace PinionCore.NetSync.Syncs.Protocols
{


    namespace Trackers
    {
        public static class ZipPositionExtension
        {
            public static ZipPosition ToZip(this UnityEngine.Vector3 position, ZipPosition min, ZipPosition max, uint scale)
            {                
                return new ZipPosition
                {
                    X = max.X != 0 ? (int)(((position.x * scale - min.X) /  max.X) * scale) : 0,
                    Y = max.Y != 0 ? (int)(((position.y * scale - min.Y) / max.Y) * scale) : 0,
                    Z = max.Z != 0 ? (int)(((position.z * scale - min.Z) / max.Z) * scale) : 0
                };
            }
        }
        public struct ZipPosition
        {
            public int X;
            public int Y;
            public int Z;            

            public UnityEngine.Vector3 Unzip(ZipPosition min, ZipPosition max, uint scale)
            {
                return new UnityEngine.Vector3
                {
                    x = (X/ (float)scale * max.X + min.X) /  scale,
                    y = (Y/ (float)scale * max.Y + min.Y) / scale,
                    z = (Z / (float)scale* max.Z + min.Z)  / scale,
                };
            }

        }
    }
}


