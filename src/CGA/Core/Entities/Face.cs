namespace Core.ObjParser.Entities
{
    public class Face
    {
        public List<FaceIndex> Indices { get; set; } = new List<FaceIndex>();

        public override string ToString()
        {
            return $"f {string.Join(" ", Indices)}";
        }
    }
}
