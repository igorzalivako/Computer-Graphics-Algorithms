using Core.Entities;
using Core.Utils;
using ModelViewer.Entities;
using ModelViewer.Textures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ModelViewer.Renderers;

internal unsafe class TextureRendererV2
{
    private int _width = 0;
    private int _height = 0;
    private int* _buffer = null;
    private float[] _zBuffer = [];
    private ObjModel _model;
    private Vector3 _eyePosition;
    private SceneSettings _sceneSettings;

    private struct DrawerVertex
    {
        public Vector4 Transform;
        public Vector3 World;
        public Vector3 Normal;
        public Vector3 Texel;

        public static DrawerVertex Lerp(DrawerVertex v0, DrawerVertex v1, float t)
        {
            return new DrawerVertex()
            {
                Transform = Vector4.Lerp(v0.Transform, v1.Transform, t),
                World = Vector3.Lerp(v0.World, v1.World, t),
                Normal = Vector3.Lerp(v0.Normal, v1.Normal, t),
                Texel = Vector3.Lerp(v0.Texel, v1.Texel, t),
            };
        }
    }

    public void RenderModel(
        ObjModel objectModel,
        WriteableBitmap bitmap,
        Vector3 eyePos,
        SceneSettings sceneSettings)
    {
        _zBuffer = new float[bitmap.PixelHeight * bitmap.PixelWidth];
        _model = objectModel;
        _eyePosition = eyePos;
        _sceneSettings = sceneSettings;
        ClearBitmap(bitmap, new Vector3(0, 0, 0));
        DrawModel(bitmap, objectModel);
    }

    public void DrawModel(WriteableBitmap wb, ObjModel model)
    {
        _width = wb.PixelWidth;
        _height = wb.PixelHeight;
        _model = model;

        if (_zBuffer.Length < _width * _height)
        {
            _zBuffer = new float[_width * _height];
        }
        Array.Fill(_zBuffer, float.MaxValue);

        wb.Lock();

        _buffer = (int*)wb.BackBuffer;

        foreach (var (v0, v1, v2) in model.FaceTrgs)
        {
            var worldV0 = model.GlobalVertices[v0.VertexIndex].AsVector3();
            var worldV1 = model.GlobalVertices[v1.VertexIndex].AsVector3();
            var worldV2 = model.GlobalVertices[v2.VertexIndex].AsVector3();

            Vector3 faceNormal = Vector3.Cross(worldV1 - worldV0, worldV2 - worldV0);
            Vector3 viewDir = _eyePosition - worldV0;

            if (Vector3.Dot(faceNormal, viewDir) <= 0) continue;

            DrawerVertex dv0 = CreateDrawerVertex(
                model.ProjectionVertices[v0.VertexIndex],
                worldV0,
                model.GlobalNormales[v0.NormalIndex ?? 0],
                model.TextureVertices[v0.TextureIndex!.Value]
            );

            DrawerVertex dv1 = CreateDrawerVertex(
                model.ProjectionVertices[v1.VertexIndex],
                worldV1,
                model.GlobalNormales[v1.NormalIndex ?? 0],
                model.TextureVertices[v1.TextureIndex!.Value]
            );

            DrawerVertex dv2 = CreateDrawerVertex(
                model.ProjectionVertices[v2.VertexIndex],
                worldV2,
                model.GlobalNormales[v2.NormalIndex ?? 0],
                model.TextureVertices[v2.TextureIndex!.Value]
            );

            RasterizeTriangle(dv0, dv1, dv2);
        }

        wb.Unlock();
    }

    private DrawerVertex CreateDrawerVertex(Vector4 transform, Vector3 world, Vector3 normal, Vector3 texel)
    {
        float invW = 1 / transform.W;
        transform.W = invW;
        return new DrawerVertex()
        {
            Transform = transform,
            World = world * invW,
            Normal = normal * invW,
            Texel = texel * invW
        };
    }

    private void RasterizeTriangle(DrawerVertex dv0, DrawerVertex dv1, DrawerVertex dv2)
    {
        if (dv1.Transform.Y < dv0.Transform.Y) (dv0, dv1) = (dv1, dv0);
        if (dv2.Transform.Y < dv0.Transform.Y) (dv0, dv2) = (dv2, dv0);
        if (dv2.Transform.Y < dv1.Transform.Y) (dv1, dv2) = (dv2, dv1);

        if (Math.Abs(dv2.Transform.Y - dv0.Transform.Y) < float.Epsilon) return;

        if (Math.Abs(dv1.Transform.Y - dv0.Transform.Y) < float.Epsilon)
        {
            FillTriangleWithFlatTop(dv0, dv1, dv2);
            return;
        }

        if (Math.Abs(dv2.Transform.Y - dv1.Transform.Y) < float.Epsilon)
        {
            FillTriangleWithFlatBottom(dv0, dv1, dv2);
            return;
        }

        float t = (dv1.Transform.Y - dv0.Transform.Y) / (dv2.Transform.Y - dv0.Transform.Y);
        DrawerVertex dv3 = DrawerVertex.Lerp(dv0, dv2, t);

        FillTriangleWithFlatBottom(dv0, dv1, dv3);
        FillTriangleWithFlatTop(dv1, dv3, dv2);
    }

    private void FillTriangleWithFlatBottom(DrawerVertex dv0, DrawerVertex dv1, DrawerVertex dv2)
    {
        int iStartY = (int)Math.Max(0, Math.Ceiling(dv0.Transform.Y));
        int iFinishY = (int)Math.Min(_height - 1, Math.Floor(dv1.Transform.Y));
        float totalHeight = (dv1.Transform.Y - dv0.Transform.Y);
        float startY = dv0.Transform.Y;

        for (int y = iStartY; y <= iFinishY; y++)
        {
            float t = (y - startY) / totalHeight;
            DrawerVertex left = DrawerVertex.Lerp(dv0, dv1, t);
            DrawerVertex right = DrawerVertex.Lerp(dv0, dv2, t);

            if (left.Transform.X > right.Transform.X)
            {
                (left, right) = (right, left);
            }

            DrawHorizontalLine(left, right, y);
        }
    }

    private void FillTriangleWithFlatTop(DrawerVertex dv0, DrawerVertex dv1, DrawerVertex dv2)
    {
        int iStartY = (int)Math.Max(0, Math.Ceiling(dv0.Transform.Y));
        int iFinishY = (int)Math.Min(_height - 1, Math.Floor(dv2.Transform.Y));
        float totalHeight = (dv2.Transform.Y - dv0.Transform.Y);
        float startY = dv0.Transform.Y;

        for (int y = iStartY; y <= iFinishY; y++)
        {
            float t = (y - startY) / totalHeight;
            DrawerVertex left = DrawerVertex.Lerp(dv0, dv2, t);
            DrawerVertex right = DrawerVertex.Lerp(dv1, dv2, t);

            if (left.Transform.X > right.Transform.X)
            {
                (left, right) = (right, left);
            }

            DrawHorizontalLine(left, right, y);
        }
    }

    private void DrawHorizontalLine(DrawerVertex dvLeft, DrawerVertex dvRight, int y)
    {
        int iLeftX = (int)Math.Ceiling(dvLeft.Transform.X);
        int iRightX = (int)Math.Floor(dvRight.Transform.X);

        if (iLeftX >= _width || iRightX < 0)
            return;

        int clampedXLeft = (int)Math.Max(iLeftX, 0.0f);
        int clampedXRight = (int)Math.Min(iRightX, _width - 1.0f);

        float totalWidth = dvRight.Transform.X - dvLeft.Transform.X;
        float leftX = dvLeft.Transform.X;
        float totalZWidth = dvRight.Transform.Z - dvLeft.Transform.Z;
        float leftZ = dvLeft.Transform.Z;

        int bufferIndex = y * _width + clampedXLeft;
        int* row = _buffer + y * _width;

        for (int x = clampedXLeft; x <= clampedXRight; x++)
        {
            float t = totalWidth > float.Epsilon ? (x - leftX) / totalWidth : 0.0f;

            float z = leftZ + t * totalZWidth;

            if (z < _zBuffer[bufferIndex])
            {
                _zBuffer[bufferIndex] = z;

                float currentInvW = dvLeft.Transform.W + t * (dvRight.Transform.W - dvLeft.Transform.W);

                Vector3 currentWorld = Vector3.Lerp(dvLeft.World, dvRight.World, t) / currentInvW;
                Vector3 currentNormal = Vector3.Lerp(dvLeft.Normal, dvRight.Normal, t) / currentInvW;
                Vector3 currentTexel = Vector3.Lerp(dvLeft.Texel, dvRight.Texel, t) / currentInvW;

                row[x] = GetPhongColor(currentWorld, currentNormal, currentTexel);
            }

            bufferIndex++;
        }
    }

    private int GetPhongColor(Vector3 world, Vector3 normal, Vector3 texel)
    {
        var baseColor = new Vector4(
            _sceneSettings.ModelColor.R,
            _sceneSettings.ModelColor.G,
            _sceneSettings.ModelColor.B,
            _sceneSettings.ModelColor.A
        );

        if (_model.DiffuseMap != null)
        {
            baseColor = _model.DiffuseMap.Sample(texel.X, texel.Y);
        }

        baseColor /= 255.0f;

        var normalizeNormal = Vector3.Normalize(normal);
        if (_model.NormalMap != null)
        {
            var texelNormalColor = _model.NormalMap.Sample(texel.X, texel.Y);
            var localNormal = new Vector3(texelNormalColor.X, texelNormalColor.Y, texelNormalColor.Z) / 127.5f - Vector3.One;
            normalizeNormal = Vector3.Normalize(Vector3.Transform(localNormal, _model.RotationMatrix));
        }

        float specReflectionIntensity = _sceneSettings.ReflectionIntensity;
        if (_model.SpecularMap != null)
        {
            Vector4 specColor = _model.SpecularMap.Sample(texel.X, texel.Y);
            specReflectionIntensity *= specColor.X / 255.0f;
        }

        Vector3 normalizeView = Vector3.Normalize(_eyePosition - world);
        Vector3 normalizeLight = Vector3.Normalize(_sceneSettings.LightPosition - world);

        float ambientLight = _sceneSettings.AmbientIntensity;
        float diff = Math.Max(0, Vector3.Dot(normalizeNormal, normalizeLight));
        float diffuseLight = _sceneSettings.DiffuseIntensity * diff;

        float reflectionLight = 0.0f;

        if (diff > float.Epsilon)
        {
            Vector3 reflection = 2.0f * Math.Max(0f, Vector3.Dot(normalizeLight, normalizeNormal)) * normalizeNormal - normalizeLight;
            float specAngle = Math.Max(0, Vector3.Dot(Vector3.Normalize(reflection), normalizeView));
            reflectionLight = specReflectionIntensity * (float)Math.Pow(specAngle, _sceneSettings.ReflectionAlpha);
        }

        float intensity = Math.Clamp(ambientLight + diffuseLight, 0f, 1f);

        int r = (int)(Math.Clamp(baseColor.X * intensity + reflectionLight, 0f, 1f) * 255f);
        int g = (int)(Math.Clamp(baseColor.Y * intensity + reflectionLight, 0f, 1f) * 255f);
        int b = (int)(Math.Clamp(baseColor.Z * intensity + reflectionLight, 0f, 1f) * 255f);

        return (255 << 24) | (r << 16) | (g << 8) | b;
    }


    private static unsafe void ClearBitmap(WriteableBitmap bitmap, Vector3 color)
    {
        var intColor = 255 << 24 | (int)(255 * color.X) << 16 | (int)(255 * color.Y) << 8 | (int)(255 * color.Z);

        bitmap.Lock();

        var pBackBuffer = (int*)bitmap.BackBuffer;
        for (var i = 0; i < bitmap.PixelWidth * bitmap.PixelHeight; i++)
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
}

public struct SceneSettings()
{
    public Vector3 LightPosition = new(-4, 10, 4);
    public float AmbientIntensity = 0.2f;
    public float DiffuseIntensity = 1.0f;
    public float ReflectionIntensity = 100f;
    public float ReflectionAlpha = 128f;
    public Color ModelColor = Colors.Pink;
}