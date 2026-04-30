using Core.Entities;
using System.Numerics;
using System.Windows;
using System.Windows.Media.Imaging;
using Core.Utils;
using ModelViewer.Utilities;

namespace ModelViewer.Shading
{
    public class PhongShading : IShading
    {
        // параметры освещения
        private static Vector3 _lightPos = new(100, 100, 100); // позиция источника света
        private static Vector3 _lightColor = new(1f, 1f, 1f); // цвет света
        private static readonly float _ka = 0.2f;
        private static readonly float _ks = 0.5f;
        private static readonly float _kd = 1f;
        private static readonly int _shininess = 32;
        private static SpinLock[,]? spinLocks;

        public unsafe void DrawShading(ObjModel objectModel, WriteableBitmap bitmap, Vector3 color, Vector3 eyePos, float[,] zBuffer)
        {
            int width = bitmap.PixelWidth;
            int height = bitmap.PixelHeight;
            PrepareSpinLocks(zBuffer.GetLength(0), zBuffer.GetLength(1));

            bitmap.Lock();

            int* buffer = (int*)bitmap.BackBuffer;

            Parallel.ForEach(objectModel.Faces, face =>
            {
                int count = face.Indexes.Count;
                if (count < 3)
                    return;

                int idx = face.Indexes[0].VertexIndex;
                Vector3 worldVertex = objectModel.GlobalVertices[idx].AsVector3();
                Vector3 viewDirection = eyePos - worldVertex;

                // отрисовка треугольника с интерполяцией нормалей
                for (int i = 1; i < count - 1; i++)
                {
                    int idx1 = face.Indexes[0].VertexIndex;
                    int idx2 = face.Indexes[i].VertexIndex;
                    int idx3 = face.Indexes[i + 1].VertexIndex;

                    Vector3[] screenVertices =
                    [
                        objectModel.ProjectionVertices[idx1].AsVector3(),
                        objectModel.ProjectionVertices[idx2].AsVector3(),
                        objectModel.ProjectionVertices[idx3].AsVector3()
                    ];

                    Vector3[] worldVertices =
                    [
                        objectModel.GlobalVertices[idx1].AsVector3(),
                        objectModel.GlobalVertices[idx2].AsVector3(),
                        objectModel.GlobalVertices[idx3].AsVector3()
                    ];

                    Vector3 edge1 = worldVertices[0] - worldVertices[1];
                    Vector3 edge2 = worldVertices[0] - worldVertices[2];
                    Vector3 normal = Vector3.Normalize(Vector3.Cross(edge1, edge2));

                    // Back-face culling
                    if (Vector3.Dot(normal, viewDirection) < 0)
                        continue;

                    Vector3[] normals =
                    [
                        face.Indexes[0].NormalIndex != null ? objectModel.GlobalNormales[face.Indexes[0].NormalIndex!.Value] : normal,
                        face.Indexes[i].NormalIndex != null ? objectModel.GlobalNormales[face.Indexes[i].NormalIndex!.Value] : normal,
                        face.Indexes[i + 1].NormalIndex != null ? objectModel.GlobalNormales[face.Indexes[i + 1].NormalIndex!.Value] : normal
                    ];

                    RasterWithPhongShading(
                        screenVertices: screenVertices,
                        worldVertices: worldVertices,
                        normals: normals,
                        height: height,
                        width: width,
                        buffer: buffer,
                        eyePos: eyePos,
                        objectColor: color,
                        zBuffer: zBuffer);
                }
            });

            try
            {
                bitmap.AddDirtyRect(new Int32Rect(0, 0, width, height));
            }
            finally
            {
                bitmap.Unlock();
            }
        }

        private static void PrepareSpinLocks(int height, int width)
        {
            if (spinLocks == null || spinLocks.GetLength(0) < height || spinLocks.GetLength(1) < width)
            {
                spinLocks = new SpinLock[height, width];
            }
        }

        private static unsafe void RasterWithPhongShading(
            Vector3[] screenVertices,
            Vector3[] worldVertices,
            Vector3[] normals,
            int height, int width,
            int* buffer, Vector3 eyePos, Vector3 objectColor, float[,] zBuffer)
        {
            SortVerticesByY(screenVertices, worldVertices, normals);

            // обратная высота каждой стороны
            float invDeltaY13 = 1.0f / (screenVertices[2].Y - screenVertices[0].Y);
            float invDeltaY12 = 1.0f / (screenVertices[1].Y - screenVertices[0].Y);
            float invDeltaY23 = 1.0f / (screenVertices[2].Y - screenVertices[1].Y);

            // вектор-приращение по каждой стороне при изменении у на 1
            Vector2 edge13 = ((screenVertices[2] - screenVertices[0]) * invDeltaY13).AsVector2();
            Vector2 edge12 = ((screenVertices[1] - screenVertices[0]) * invDeltaY12).AsVector2();
            Vector2 edge23 = ((screenVertices[2] - screenVertices[1]) * invDeltaY23).AsVector2();

            // то же самое в мировых
            Vector3 world13 = (worldVertices[2] - worldVertices[0]) * invDeltaY13;
            Vector3 world12 = (worldVertices[1] - worldVertices[0]) * invDeltaY12;
            Vector3 world23 = (worldVertices[2] - worldVertices[1]) * invDeltaY23;

            // приращение для интерполяции нормалей
            Vector3 normal13 = (normals[2] - normals[0]) * invDeltaY13;
            Vector3 normal12 = (normals[1] - normals[0]) * invDeltaY12;
            Vector3 normal23 = (normals[2] - normals[1]) * invDeltaY23;

            float z13 = (screenVertices[2].Z - screenVertices[0].Z) * invDeltaY13;
            float z12 = (screenVertices[1].Z - screenVertices[0].Z) * invDeltaY12;
            float z23 = (screenVertices[2].Z - screenVertices[1].Z) * invDeltaY23;

            int startY = Math.Max(0, (int)Math.Ceiling(screenVertices[0].Y));
            int endY = Math.Min(height, (int)Math.Ceiling(screenVertices[2].Y));

            for (int y = startY; y < endY; y++)
            {
                float dy = y - screenVertices[0].Y;

                Vector2 aPoint, bPoint;
                Vector3 aWorld, bWorld;
                Vector3 aNormal, bNormal;
                float aZ, bZ;

                if (y < screenVertices[1].Y)
                {
                    aPoint = screenVertices[0].AsVector2() + edge13 * dy;
                    bPoint = screenVertices[0].AsVector2() + edge12 * dy;
                    aWorld = worldVertices[0] + world13 * dy;
                    bWorld = worldVertices[0] + world12 * dy;
                    aNormal = normals[0] + normal13 * dy;
                    bNormal = normals[0] + normal12 * dy;
                    aZ = screenVertices[0].Z + z13 * dy;
                    bZ = screenVertices[0].Z + z12 * dy;
                }
                else
                {
                    dy = y - screenVertices[1].Y;
                    aPoint = screenVertices[0].AsVector2() + edge13 * (y - screenVertices[0].Y);
                    bPoint = screenVertices[1].AsVector2() + edge23 * dy;
                    aWorld = worldVertices[0] + world13 * (y - screenVertices[0].Y);
                    bWorld = worldVertices[1] + world23 * dy;
                    aNormal = normals[0] + normal13 * (y - screenVertices[0].Y);
                    bNormal = normals[1] + normal23 * dy;
                    aZ = screenVertices[0].Z + z13 * (y - screenVertices[0].Y);
                    bZ = screenVertices[1].Z + z23 * dy;
                }

                if (aPoint.X > bPoint.X)
                {
                    (aPoint, bPoint) = (bPoint, aPoint);
                    (aWorld, bWorld) = (bWorld, aWorld);
                    (aNormal, bNormal) = (bNormal, aNormal);
                    (aZ, bZ) = (bZ, aZ);
                }

                int startX = Math.Max(0, (int)Math.Ceiling(aPoint.X));
                int endX = Math.Min(width, (int)Math.Ceiling(bPoint.X));

                if (startX < endX)
                {
                    float dx = endX - startX;
                    float tStep = 1.0f / dx;
                    float t = 0;

                    for (int x = startX; x < endX; x++, t += tStep)
                    {
                        Vector3 pixelWorld = aWorld + (bWorld - aWorld) * t;
                        Vector3 pixelNormal = Vector3.Normalize(aNormal + (bNormal - aNormal) * t);
                        float pixelZ = aZ + (bZ - aZ) * t;

                        Vector3 lightDir = Vector3.Normalize(_lightPos - pixelWorld);
                        Vector3 viewDir = Vector3.Normalize(eyePos - pixelWorld);
                        Vector3 reflectDir = Vector3.Reflect(-lightDir, pixelNormal);
                        Vector3 ambient = _ka * _lightColor;

                        float diff = Math.Max(Vector3.Dot(pixelNormal, lightDir), 0.0f);
                        Vector3 diffuse = _kd * diff * _lightColor;

                        float spec = (float)Math.Pow(Math.Max(Vector3.Dot(viewDir, reflectDir), 0.0f), _shininess);
                        Vector3 specular = _ks * spec * _lightColor;

                        Vector3 result = (ambient + diffuse + specular) * objectColor;
                        result = Vector3.Clamp(result, Vector3.Zero, Vector3.One);

                        int color = ColorUtility.ColorToInt(result);
                        int index = y * width + x;

                        bool lockTaken = false;
                        spinLocks![y, x].Enter(ref lockTaken);
                        if (pixelZ < zBuffer[y, x])
                        {

                            buffer[index] = color;
                            zBuffer[y, x] = pixelZ;
                        }
                        spinLocks![y, x].Exit();
                    }
                }
            }
        }

        private static void SortVerticesByY(Vector3[] screenVertices, Vector3[] worldVertices, Vector3[] normals)
        {
            if (screenVertices[0].Y > screenVertices[2].Y)
            {
                (screenVertices[0], screenVertices[2]) = (screenVertices[2], screenVertices[0]);
                (worldVertices[0], worldVertices[2]) = (worldVertices[2], worldVertices[0]);
                (normals[0], normals[2]) = (normals[2], normals[0]);
            }

            if (screenVertices[0].Y > screenVertices[1].Y)
            {
                (screenVertices[0], screenVertices[1]) = (screenVertices[1], screenVertices[0]);
                (worldVertices[0], worldVertices[1]) = (worldVertices[1], worldVertices[0]);
                (normals[0], normals[1]) = (normals[1], normals[0]);
            }

            if (screenVertices[1].Y > screenVertices[2].Y)
            {
                (screenVertices[1], screenVertices[2]) = (screenVertices[2], screenVertices[1]);
                (worldVertices[1], worldVertices[2]) = (worldVertices[2], worldVertices[1]);
                (normals[1], normals[2]) = (normals[2], normals[1]);
            }
        }
    }
}
