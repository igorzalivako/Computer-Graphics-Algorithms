namespace Core.ObjParser.Entities
{
    public class TextureVertex(float u, float v = 0.0f, float w = 0.0f)
    {
        public float U { get; set; } = u;
        public float V { get; set; } = v;
        public float W { get; set; } = w;

        public override string ToString()
        {
            return $"vt {U} {V} {W}";
        }
    }
}
