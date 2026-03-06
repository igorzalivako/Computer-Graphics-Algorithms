using Core.Entities;
using ModelViewer.Utilities;
using System.Numerics;
using System.Windows;
using System.Windows.Media.Imaging;

namespace ModelViewer.Shading
{
    public class FlatShading : IShading
    {
        private static SpinLock[,]? spinLocks;

        public unsafe void DrawShading(
            ObjModel objectModel,
            WriteableBitmap bitmap,
            Vector3 color,
            Vector3 eyePos,
            float[,] zBuffer)
        {
            int width = bitmap.PixelWidth;
            int height = bitmap.PixelHeight;

            PrepareSpinLocks(zBuffer.GetLength(0), zBuffer.GetLength(1));

            bitmap.Lock();

            int* buffer = (int*)bitmap.BackBuffer;

            // направление света
            Vector3 lightDirection = Vector3.Normalize(new Vector3(0, 0.5f, 1));

            Parallel.ForEach(objectModel.Faces, face =>
            {
                int count = face.Indexes.Count;
                if (count < 3)
                    return;

                int idx0 = face.Indexes[0].VertexIndex;

                Vector3 worldVertex = objectModel.GlobalVertices[idx0].AsVector3();

                // направление на камеру
                Vector3 viewDirection = Vector3.Normalize(eyePos - worldVertex);

                // нормаль грани
                Vector3 normal = Vector3.Normalize(face.SurfaceNormal);

                // Back-face culling
                if (Vector3.Dot(normal, viewDirection) <= 0)
                    return;

                // ----- освещение -----

                float ambient = 0.2f;

                float diffuse = MathF.Max(Vector3.Dot(normal, lightDirection), 0);

                float strength = ambient + diffuse * (1 - ambient);

                int shadedColorBgra = ColorUtility.ColorToInt(
                    color: color,
                    strength: strength,
                    alpha: 255);

                // ----- растеризация -----

                for (int i = 1; i < count - 1; i++)
                {
                    int idx1 = face.Indexes[0].VertexIndex;
                    int idx2 = face.Indexes[i].VertexIndex;
                    int idx3 = face.Indexes[i + 1].VertexIndex;

                    Vector3 screenVertex1 = objectModel.ProjectionVertices[idx1].AsVector3();
                    Vector3 screenVertex2 = objectModel.ProjectionVertices[idx2].AsVector3();
                    Vector3 screenVertex3 = objectModel.ProjectionVertices[idx3].AsVector3();

                    RasterWithScanningLine(
                        vertex1: screenVertex1,
                        vertex2: screenVertex2,
                        vertex3: screenVertex3,
                        height: height,
                        width: width,
                        buffer: buffer,
                        color: shadedColorBgra,
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
            if (spinLocks == null || spinLocks.GetLength(0) < height || spinLocks.GetLength(1) != width)
            {
                spinLocks = new SpinLock[height, width];
            }
        }

        private static unsafe void RasterWithScanningLine(
            Vector3 vertex1, Vector3 vertex2, Vector3 vertex3,
            int height, int width,
            int* buffer, int color, float[,] zBuffer)
        {
            // отсортировать вершины по Y
            if (vertex1.Y > vertex3.Y)
            {
                (vertex1, vertex3) = (vertex3, vertex1);
            }

            if (vertex1.Y > vertex2.Y)
            {
                (vertex1, vertex2) = (vertex2, vertex1);
            }

            if (vertex2.Y > vertex3.Y)
            {
                (vertex2, vertex3) = (vertex3, vertex2);
            }

            // векторное изменение координат вдоль ребер треугольника
            Vector3 coefVer1 = (vertex3 - vertex1) / (vertex3.Y - vertex1.Y);
            Vector3 coefVer2 = (vertex2 - vertex1) / (vertex2.Y - vertex1.Y);
            Vector3 coefVer3 = (vertex3 - vertex2) / (vertex3.Y - vertex2.Y);

            // границы перебора
            int top = Math.Max(0, (int)Math.Ceiling(vertex1.Y));
            int bottom = Math.Min(height, (int)Math.Ceiling(vertex3.Y));

            // алгоритм отрисовки
            for (int y = top; y < bottom; y++)
            {
                Vector3 aPoint = vertex1 + (y - vertex1.Y) * coefVer1;
                // Если до вектор2 множим на коэфф2, если ниже спустились - то на коэфф3 (направление от 2 к 3)
                Vector3 bPoint = y < vertex2.Y
                    ? vertex1 + (y - vertex1.Y) * coefVer2
                    : vertex2 + (y - vertex2.Y) * coefVer3;

                if (aPoint.X > bPoint.X)
                {
                    (aPoint, bPoint) = (bPoint, aPoint);
                }

                int left = Math.Max(0, (int)Math.Ceiling(aPoint.X));
                int right = Math.Min(width, (int)Math.Ceiling(bPoint.X));

                for (int x = left; x < right; x++)
                {
                    // относительная позиция текущего х от точки a
                    float t = (x - aPoint.X) / (bPoint.X - aPoint.X);
                    // линейная интерполяция глубины (у нас есть Z крайних точек, а z внутренних точке нет, поэтому мы считаем, что она плавно (линейно) изменяется от одной точки к другой)
                    float z = aPoint.Z + t * (bPoint.Z - aPoint.Z);
                   
                    int index = y * width + x;
                    // рисуем новый пиксель, если он ближе к камере

                    bool lockTaken = false;
                    spinLocks![y, x].Enter(ref lockTaken);
                    if (z < zBuffer[y, x])
                    {
                        buffer[index] = color;
                        zBuffer[y, x] = z;
                    }
                    spinLocks![y, x].Exit();
                }
            }
        }
    }
}