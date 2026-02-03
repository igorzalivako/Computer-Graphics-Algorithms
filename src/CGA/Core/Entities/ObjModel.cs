namespace Core.ObjParser.Entities
{
    public class ObjModel
    {
        public List<Vertex> Vertices { get; private set; } = [];
        
        public List<TextureVertex> TextureVertices { get; private set; } = [];
        
        public List<VertexNormal> VertexNormals { get; private set; } = [];
        
        public List<Face> Faces { get; private set; } = [];
    }
}