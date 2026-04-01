using System.Numerics;

namespace Core.Utils
{
    public static class Vector3Extensions
    {
        public static Vector2 AsVector2(this Vector3 vertex)
        {
            return new Vector2(vertex.X, vertex.Y);
        }
    }
}
