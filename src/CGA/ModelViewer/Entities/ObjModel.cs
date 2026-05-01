using Core.Entities;
using ModelViewer.Textures;
using System.Numerics;
using System.Windows.Media.TextFormatting;

namespace ModelViewer.Entities
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

        public Vector3[] GlobalNormales { get; private set; } = [];
        public string PathToMtlFile { get; set; }

        public float[] WValues { get; set; } = [];

        public Matrix4x4 GlobalMatrix { get; set; }
        
        public FaceTrg[] FaceTrgs { get; set; }

        public Matrix4x4 RotationMatrix { get; set; } = Matrix4x4.Identity;

        public Texture? DiffuseMap { get; set; } = null;
        public Texture? NormalMap { get; set; } = null;
        public Texture? SpecularMap { get; set; } = null;


        public FaceTrg[] Triangulate()
        {
            int totalTriangles = Faces.Sum(face => face.Indexes.Count - 2);

            FaceTrg[] faceTrgs = new FaceTrg[totalTriangles];

            int faceTrgIndex = 0;
            foreach (var face in Faces)
            {
                var faceVtxs = face.Indexes;
                if (faceVtxs.Count < 3) continue;

                var fv0 = faceVtxs[0];
                for (int j = 1; j < faceVtxs.Count - 1; j++)
                {
                    faceTrgs[faceTrgIndex++] = new FaceTrg(fv0, faceVtxs[j], faceVtxs[j + 1]);
                }
            }

            return faceTrgs;
        }

        public void Transform(Matrix4x4 transformMatrix, float zNear, float zFar)
        {
            if (ProjectionVertices.Length != Vertices.Count)
                ProjectionVertices = new Vector4[Vertices.Count];

            if (WValues.Length != Vertices.Count)
                WValues = new float[Vertices.Count];

            for (var i = 0; i < Vertices.Count; i++)
            {
                var vertexVector = Vector4.Transform(Vertices[i], transformMatrix);

                float originalW = vertexVector.W;
                WValues[i] = originalW;

                // Перспективное деление, только если вершина в пределах видимости
                if (originalW > zNear && originalW < zFar)
                {
                    vertexVector.X /= originalW;
                    vertexVector.Y /= originalW;
                    vertexVector.Z /= originalW;
                }
                // ВАЖНО: сохраняем оригинальную W для корректной интерполяции
                vertexVector.W = originalW;

                ProjectionVertices[i] = vertexVector;
            }
        }

        public void CalculateGlobalVertices(Matrix4x4 worldMatrix)
        {
            GlobalMatrix = worldMatrix;
            for (int i = 0; i < Vertices.Count; i++)
            {
                if (GlobalVertices.Length != Vertices.Count)
                {
                    GlobalVertices = new Vector4[Vertices.Count];
                }
                GlobalVertices[i] = Vector4.Transform(Vertices[i], worldMatrix);
            }
        }

        public void CalculateVertexNormals(Matrix4x4 worldMatrix)
        {
            if (GlobalNormales != null || GlobalNormales!.Length < Vertices.Count)
            {
                GlobalNormales = new Vector3[VertexNormals.Count];
            }

            for (var i = 0; i < VertexNormals.Count; i++)
            {
                GlobalNormales[i] = Vector3.Transform(VertexNormals[i], worldMatrix);
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

    public struct FaceTrg(FaceIndex v0, FaceIndex v1, FaceIndex v2)
    {
        public FaceIndex V0 { get; set; } = v0;
        public FaceIndex V1 { get; set; } = v1;
        public FaceIndex V2 { get; set; } = v2;

        public void Deconstruct(out FaceIndex v0, out FaceIndex v1, out FaceIndex v2)
        {
            v0 = V0;
            v1 = V1;
            v2 = V2;
        }
    }
}