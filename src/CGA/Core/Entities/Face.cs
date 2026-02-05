using System.Numerics;

namespace Core.Entities
{
    public class Face
    {
        public List<FaceIndex> Indexes { get; set; } = [];

        public Vector3 VertexNormal { get; set; }

        public override string ToString()
        {
            return $"f {string.Join(" ", Indexes)}";
        }
    }
}
