using System.Numerics;

namespace Core.Entities
{
    public class ObjModel
    {
        public List<Vector4> Vertices { get; private set; } = [];
        
        public List<Vector3> TextureVertices { get; private set; } = [];
        
        public List<Vector3> VertexNormals { get; private set; } = [];
        
        public List<Face> Faces { get; private set; } = [];

        public Vector4[] WorldVertices { get; private set; } = [];

        public Vector4[] ProjectionVertices { get; private set; } = [];

        public Vector3 Position { get; set; }

        public Vector3 Rotation { get; set; }

        public Vector3 Scale { get; set; } = new(2.0f, 2.0f, 2.0f);

        public void Transform(Matrix4x4 transformMatrix, float zNear, float zFar)
        {
            if (ProjectionVertices.Length != Vertices.Count)
            {
                ProjectionVertices = new Vector4[Vertices.Count];
            }

            for (var i = 0; i < Vertices.Count; i++)
            {
                // *?
                var vertexVector = Vector4.Transform(Vertices[i], transformMatrix);

                if (vertexVector.W > zNear && vertexVector.W < zFar)
                    vertexVector /= vertexVector.W;

                ProjectionVertices[i] = vertexVector;
            }
        }

        public void CalcGlobalVertices(Matrix4x4 worldMatrix)
        {
            if (WorldVertices.Length != Vertices.Count)
            {
                WorldVertices = new Vector4[Vertices.Count];
            }

            for (var i = 0; i < Vertices.Count; i++)
            {
                // *?
                WorldVertices[i] = Vector4.Transform(Vertices[i], worldMatrix);
            }
        }
    }
}