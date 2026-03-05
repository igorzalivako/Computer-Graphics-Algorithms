using Core.Entities;
using System.Numerics;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ModelViewer.Renderers
{
    public static class WireframeRenderer
    {
        public static void RenderModel(ObjModel objectModel, WriteableBitmap bitmap, float zNear, float zFar, Color color)
        {
            ArgumentNullException.ThrowIfNull(bitmap);

            ClearBitmap(bitmap, Color.FromArgb(255, 255, 255, 255));
            Draw(objectModel, bitmap, zNear, zFar, color);
        }

        private static unsafe void ClearBitmap(WriteableBitmap bitmap, Color color)
        {
            int intColor = (color.A << 24) | (color.R << 16) | (color.G << 8) | color.B;

            bitmap.Lock();

            int* pBackBuffer = (int*)bitmap.BackBuffer;
            for (int i = 0; i < bitmap.PixelWidth * bitmap.PixelHeight; i++)
            {
                pBackBuffer[i] = intColor;
            }

            try
            {
                bitmap.AddDirtyRect(new Int32Rect(0, 0, bitmap.PixelWidth, bitmap.PixelHeight));
            }
            finally
            {
                bitmap.Unlock();
            }
        }

        private static unsafe void Draw(ObjModel objectModel, WriteableBitmap bitmap, float zNear, float zFar, Color color)
        {
            int pixelWidth = bitmap.PixelWidth;
            int pixelHeight = bitmap.PixelHeight;
            int colorARGB = (color.A << 24) | (color.R << 16) | (color.G << 8) | color.B;

            bitmap.Lock();

            int* buffer = (int*)bitmap.BackBuffer;

            Parallel.ForEach(objectModel.Faces, face => 
            {
                int count = face.Indexes.Count;
                if (count < 2)
                {
                    return;
                }

                for (int i = 0; i < count; i++)
                {
                    int index1 = face.Indexes[i].VertexIndex;
                    int index2 = face.Indexes[(i + 1) % count].VertexIndex;

                    if (!(index1 >= 0 && index1 < objectModel.ProjectionVertices.Length &&
                          index2 >= 0 && index2 < objectModel.ProjectionVertices.Length))
                    {
                        continue;
                    }

                    int x0 = (int)Math.Round(objectModel.ProjectionVertices[index1].X);
                    int y0 = (int)Math.Round(objectModel.ProjectionVertices[index1].Y);
                    float z0 = objectModel.ProjectionVertices[index1].Z;

                    int x1 = (int)Math.Round(objectModel.ProjectionVertices[index2].X);
                    int y1 = (int)Math.Round(objectModel.ProjectionVertices[index2].Y);
                    float z1 = objectModel.ProjectionVertices[index2].Z;


                    if ((x0 >= pixelWidth && x1 >= pixelWidth) ||
                        (x0 < 0 && x1 < 0) ||
                        (y0 >= pixelHeight && y1 >= pixelHeight) ||
                        (y0 < 0 && y1 < 0))
                    {
                        continue;
                    }

                    if (z0 < zNear || z1 < zNear ||
                        z0 > zFar || z1 > zFar)
                    {
                        continue;
                    }

                    DrawBresenhamLine(buffer, new(x0, y0), new(x1, y1), pixelWidth, pixelHeight, colorARGB);
                }
            });

            try
            {
                bitmap.AddDirtyRect(new Int32Rect(0, 0, pixelWidth, pixelHeight));
            }
            finally
            {
                bitmap.Unlock();
            }
        }

        private static unsafe void DrawBresenhamLine(
            int* buffer,
            Vector2 a, Vector2 b,
            int width, int height,
            int colorARGB)
        {
            int x0 = (int)Math.Round(a.X, MidpointRounding.AwayFromZero);
            int y0 = (int)Math.Round(a.Y, MidpointRounding.AwayFromZero);
            int x1 = (int)Math.Round(b.X, MidpointRounding.AwayFromZero);
            int y1 = (int)Math.Round(b.Y, MidpointRounding.AwayFromZero);

            int dx = Math.Abs(x1 - x0);
            int sx = x0 < x1 ? 1 : -1;
            int dy = Math.Abs(y1 - y0);
            int sy = y0 < y1 ? 1 : -1;

            int err = dx - dy;

            while (true)
            {
                if ((uint)x0 < (uint)width && (uint)y0 < (uint)height)
                    buffer[y0 * width + x0] = colorARGB;

                if (x0 == x1 && y0 == y1)
                    break;

                int e2 = err << 1; // 2*err

                if (e2 > -dy)
                {
                    err -= dy;
                    x0 += sx;
                }

                if (e2 < dx)
                {
                    err += dx;
                    y0 += sy;
                }
            }
        }
    }
}
