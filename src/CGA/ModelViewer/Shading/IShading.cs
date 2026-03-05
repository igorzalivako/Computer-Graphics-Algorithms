using Core.Entities;
using System.Numerics;
using System.Windows.Media.Imaging;

namespace ModelViewer.Shading
{
    public interface IShading
    {
        void DrawShading(ObjModel objectModel, WriteableBitmap bitmap, Vector3 color, Vector3 eyePos, float[,] zBuffer);
    }
}
