using System.Numerics;

namespace Core.Entities
{
    public class ObjModel
    {
        public List<Vector4> Vertices { get; private set; } = [];
        
        public List<Vector3> TextureVertices { get; private set; } = [];

        public List<Vector3> VertexNormals { get; private set; } = [];
        
        public List<Face> Faces { get; private set; } = [];

        public Vector4[] ProjectionVertices { get; private set; } = [];

        public Vector3 Position { get; set; }

        public Vector3 Rotation { get; set; }

        public Vector3 Scale { get; set; } = new(1, 1, 1);

        public Vector4[] GlobalVertices { get; private set; } = [];


        public void Transform(Matrix4x4 transformMatrix, float zNear, float zFar)
        {
            if (ProjectionVertices.Length != Vertices.Count)
            {
                ProjectionVertices = new Vector4[Vertices.Count];
            }

            for (var i = 0; i < Vertices.Count; i++)
            {
                var vertexVector = Vector4.Transform(Vertices[i], transformMatrix);

                if (vertexVector.W > zNear && vertexVector.W < zFar)
                {
                    vertexVector /= vertexVector.W;
                }

                ProjectionVertices[i] = vertexVector;
            }
        }

        public void CalculateGlobalVertices(Matrix4x4 worldMatrix)
        {
            for (int i = 0; i < Vertices.Count; i++)
            {
                if (GlobalVertices.Length != Vertices.Count)
                {
                    GlobalVertices = new Vector4[Vertices.Count];
                }
                GlobalVertices[i] = Vector4.Transform(Vertices[i], worldMatrix);
            }
        }

        public void CalculateNormals(Matrix4x4 transformMatrix)
        {
            Vector4[] tempVertices = new Vector4[Vertices.Count];
            for (var i = 0; i < Vertices.Count; i++)
                tempVertices[i] = Vector4.Transform(Vertices[i], transformMatrix);

            foreach (var face in Faces)
            {
                Vector3 v1 = new Vector3(
                    tempVertices[face.Indexes[1].VertexIndex].X - tempVertices[face.Indexes[0].VertexIndex].X,
                    tempVertices[face.Indexes[1].VertexIndex].Y - tempVertices[face.Indexes[0].VertexIndex].Y,
                    tempVertices[face.Indexes[1].VertexIndex].Z - tempVertices[face.Indexes[0].VertexIndex].Z);

                Vector3 v2 = new Vector3(
                    tempVertices[face.Indexes[2].VertexIndex].X - tempVertices[face.Indexes[0].VertexIndex].X,
                    tempVertices[face.Indexes[2].VertexIndex].Y - tempVertices[face.Indexes[0].VertexIndex].Y,
                    tempVertices[face.Indexes[2].VertexIndex].Z - tempVertices[face.Indexes[0].VertexIndex].Z);

                Vector3 surfaceNormal = Vector3.Normalize(Vector3.Cross(v1, v2));

                face.SurfaceNormal = surfaceNormal;
            }
        }
    }
}