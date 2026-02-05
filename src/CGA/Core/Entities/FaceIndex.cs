namespace Core.Entities
{
    public class FaceIndex(int vertexIndex, int? textureIndex = null, int? normalIndex = null)
    {
        public int VertexIndex { get; set; } = vertexIndex;

        public int? TextureIndex { get; set; } = textureIndex;

        public int? NormalIndex { get; set; } = normalIndex;

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
