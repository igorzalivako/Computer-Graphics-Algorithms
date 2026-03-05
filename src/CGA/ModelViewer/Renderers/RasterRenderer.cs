using Core.Entities;
using ModelViewer.Shading;
using System.Numerics;
using System.Windows;
using System.Windows.Media.Imaging;

namespace ModelViewer.Renderers
{
    public class RasterRenderer
    {
        private static float[,]? _zBuffer;
        private static FlatShading _flatShading = new();

        public static void RenderModel(ObjModel objectModel, WriteableBitmap bitmap, Vector3 color, Vector3 eyePos, IShading shading)
        {
            ArgumentNullException.ThrowIfNull(bitmap);

            ClearBitmap(bitmap, new(0, 0, 0));
            ClearZBuffer(bitmap.PixelWidth, bitmap.PixelHeight);

            shading.DrawShading(objectModel, bitmap, color, eyePos, _zBuffer!);
        }

        private static unsafe void ClearBitmap(WriteableBitmap bitmap, Vector3 color)
        {
            int intColor = 255 << 24 | (int)(255 * color.X) << 16 | (int)(255 * color.Y) << 8 | (int)(255 * color.Z);

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

        private static void ClearZBuffer(int width, int height)
        {
            _zBuffer ??= new float[height, width];

            for (int i = 0; i < height; i++)
            {
                for (int j = 0; j < width; j++)
                {
                    _zBuffer[i, j] = 1f;
                }
            }
        }
    }
}
