namespace Core.Entities
{
    public class FaceIndex
    {
        public int VertexIndex { get; set; }
        public int? TextureIndex { get; set; }
        public int? NormalIndex { get; set; }

        public FaceIndex(int vertexIndex, int? textureIndex = null, int? normalIndex = null)
        {
            VertexIndex = vertexIndex;
            TextureIndex = textureIndex;
            NormalIndex = normalIndex;
        }

        public override string ToString()
        {
            if (TextureIndex.HasValue && NormalIndex.HasValue)
            {
                return $"{VertexIndex}/{TextureIndex}/{NormalIndex}";
            }
            else if (TextureIndex.HasValue)
            {
                return $"{VertexIndex}/{TextureIndex}";
            }
            else if (NormalIndex.HasValue)
            {
                return $"{VertexIndex}//{NormalIndex}";
            }
            else
            {
                return $"{VertexIndex}";
            }
        }
    }
}
