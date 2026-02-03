namespace Core.ObjParser.Entities
{
    public class Vertex(float x, float y, float z, float w = 1.0f)
    {
        public float X { get; set; } = x;
        public float Y { get; set; } = y;
        public float Z { get; set; } = z;
        public float W { get; set; } = w;

        public override string ToString()
        {
            return $"v {X} {Y} {Z} {W}";
        }
    }
}
