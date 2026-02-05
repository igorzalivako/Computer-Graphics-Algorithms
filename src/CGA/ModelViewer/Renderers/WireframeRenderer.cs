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
            int x1 = (int)Math.Round(a.X, MidpointRounding.AwayFromZero);
            int y1 = (int)Math.Round(a.Y, MidpointRounding.AwayFromZero);
            int x2 = (int)Math.Round(b.X, MidpointRounding.AwayFromZero);
            int y2 = (int)Math.Round(b.Y, MidpointRounding.AwayFromZero);

            int dx = x2 - x1;
            int dy = y2 - y1;

            int w = Math.Abs(dx);
            int h = Math.Abs(dy);
            int l = Math.Max(w, h);

            int m00 = Math.Sign(dx);
            int m01 = 0;
            int m10 = 0;
            int m11 = Math.Sign(dy);
            if (w < h)
            {
                (m00, m01) = (m01, m00);
                (m10, m11) = (m11, m10);
            }

            int y = 0;
            int e = 0;
            int eDec = 2 * l;
            int eInc = 2 * Math.Min(w, h);

            for (int x = 0; x <= l; x++)
            {
                int xt = x1 + m00 * x + m01 * y;
                int yt = y1 + m10 * x + m11 * y;

                if (xt >= 0 && xt < width && yt >= 0 && yt < height)
                {
                    int index = yt * width + xt;
                    buffer[index] = colorARGB;
                }

                if ((e += eInc) > 1)
                {
                    e -= eDec;
                    y++;
                }
            }
        }
    }
}
