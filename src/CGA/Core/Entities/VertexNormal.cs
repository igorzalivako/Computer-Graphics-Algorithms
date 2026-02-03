namespace Core.ObjParser.Entities
{
    public class VertexNormal(float i, float j, float k)
    {
        public float I { get; set; } = i;
        public float J { get; set; } = j;
        public float K { get; set; } = k;

        public override string ToString()
        {
            return $"vn {I} {J} {K}";
        }
    }
}
