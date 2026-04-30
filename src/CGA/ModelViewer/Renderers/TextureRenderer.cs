using Core.Entities;
using ModelViewer.Shading;
using Core.Utils;
using ModelViewer.Textures;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.TextFormatting;
using Vector = System.Numerics.Vector;
using System.Diagnostics;

namespace ModelViewer.Renderers;

public static class TextureRenderer
{

    private static ObjModel _objModel;
    private static float[]? _zBuffer;

    public static void RenderModel(
        ObjModel objectModel,
        WriteableBitmap bitmap,
        Vector3 eyePos,
        List<Material> materials,
        Dictionary<string, TextureMap> textureMaps)
    {
        _zBuffer = new float[bitmap.PixelHeight * bitmap.PixelWidth];
        _objModel = objectModel;
        ClearBitmap(bitmap, new Vector3(0, 0, 0));
        Draw(objectModel, bitmap, eyePos, materials, textureMaps);
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

    private static unsafe void Draw(
        ObjModel objectModel,
        WriteableBitmap bitmap,
        Vector3 eyePos,
        List<Material> materials,
        Dictionary<string, TextureMap> textureMaps)
    {
        var width = bitmap.PixelWidth;
        var height = bitmap.PixelHeight;

        bitmap.Lock();
        var buffer = (int*)bitmap.BackBuffer;

        var ambientCoeff = 0.12f;
        var ambientColor = Colors.Black;
        var specularCoeff = 0.5f;
        var specularColor = Colors.Black;
        var diffuseCoeff = 1.0f;
        var shininess = 32;

        var light1 = (eyePos + new Vector3(0, 0, 20), Color.FromScRgb(1.0f, 1.0f, 0.9f, 0.8f), 0.7f);
        Debug.WriteLine(eyePos);
        var lights = new[] { light1 };

        Parallel.ForEach(objectModel.Faces, face =>
        {
            var count = face.Indexes.Count;
            if (count < 2)
                return;

            var textures = GetTexturesForFace(materials, face.MaterialName, textureMaps);

            for (var i = 1; i < count - 1; i++)
            {
                var idx1 = face.Indexes[0].VertexIndex;
                var idx2 = face.Indexes[i].VertexIndex;
                var idx3 = face.Indexes[i + 1].VertexIndex;

                Vector3[] screenVertices =
                [
                    objectModel.ProjectionVertices[idx1].AsVector3() ,
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

                Vector3[] normals =
                [
                    face.Indexes[0].NormalIndex != null ? objectModel.GlobalNormales[face.Indexes[0].NormalIndex!.Value] : normal,
                    face.Indexes[i].NormalIndex != null ? objectModel.GlobalNormales[face.Indexes[i].NormalIndex!.Value] : normal,
                    face.Indexes[i + 1].NormalIndex != null ? objectModel.GlobalNormales[face.Indexes[i + 1].NormalIndex!.Value] : normal
                ];

                Vector3[] textureCoords =
                [
                    objectModel.TextureVertices[face.Indexes[0].TextureIndex.Value],
                    objectModel.TextureVertices[face.Indexes[i].TextureIndex.Value],
                    objectModel.TextureVertices[face.Indexes[i + 1].TextureIndex.Value]
                ];

                RasterTriangleWithTexture(
                    screenVertices: screenVertices,
                    worldVertices: worldVertices,
                    normals: normals,
                    textureCoordinates: textureCoords,
                    height: height,
                    width: width,
                    buffer: buffer,
                    eyePos: eyePos,
                    textures: textures,
                    lights: lights,
                    ambientCoeff: ambientCoeff,
                    ambientColor: ambientColor,
                    diffuseCoeff: diffuseCoeff,
                    specularCoeff: specularCoeff,
                    specularColor: specularColor,
                    shininess: shininess);
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


    private static (TextureMap? diffuseTexture, TextureMap? normalTexture, TextureMap? specularTexture)
        GetTexturesForFace(List<Material> materials, string materialName,
            Dictionary<string, TextureMap> textureMaps)
    {
        var material = materials.FirstOrDefault(a => a.Name == materialName);
        TextureMap? diffuseTexture = null;
        TextureMap? normalTexture = null;
        TextureMap? specularTexture = null;

        if (material != null && textureMaps.Count > 0)
        {
            textureMaps.TryGetValue(material.DiffuseMap, out diffuseTexture);
            textureMaps.TryGetValue(material.NormalMap, out normalTexture);
            textureMaps.TryGetValue(material.SpecularMap, out specularTexture);
        }

        return (diffuseTexture, normalTexture, specularTexture);
    }

    private static unsafe void RasterTriangleWithTexture(
    Vector3[] screenVertices,
    Vector3[] worldVertices,
    Vector3[] normals,
    Vector3[] textureCoordinates,
    int height,
    int width,
    int* buffer,
    Vector3 eyePos,
    (TextureMap? diffuseTexture, TextureMap? normalTexture, TextureMap? specularTexture) textures,
    (Vector3 SourceOfLight, Color Color, float Intensity)[] lights,
    float ambientCoeff,
    Color ambientColor,
    float diffuseCoeff,
    float specularCoeff,
    Color specularColor,
    float shininess)
    {
        var v0 = screenVertices[0];
        var v1 = screenVertices[1];
        var v2 = screenVertices[2];

        var world0 = worldVertices[0];
        var world1 = worldVertices[1];
        var world2 = worldVertices[2];

        var n0 = normals[0];
        var n1 = normals[1];
        var n2 = normals[2];

        var uv0 = textureCoordinates[0];
        var uv1 = textureCoordinates[1];
        var uv2 = textureCoordinates[2];

        // Perspective correction
        float invZ0 = 1.0f / v0.Z;
        float invZ1 = 1.0f / v1.Z;
        float invZ2 = 1.0f / v2.Z;

        int* bufferPtr = buffer;

        int xMin = (int)MathF.Max(0, MathF.Floor(MathF.Min(v0.X, MathF.Min(v1.X, v2.X))));
        int yMin = (int)MathF.Max(0, MathF.Floor(MathF.Min(v0.Y, MathF.Min(v1.Y, v2.Y))));
        int xMax = (int)MathF.Min(width - 1, MathF.Ceiling(MathF.Max(v0.X, MathF.Max(v1.X, v2.X))));
        int yMax = (int)MathF.Min(height - 1, MathF.Ceiling(MathF.Max(v0.Y, MathF.Max(v1.Y, v2.Y))));

        float denom = (v2.X - v0.X) * (v1.Y - v0.Y) - (v2.Y - v0.Y) * (v1.X - v0.X);
        if (Math.Abs(denom) < float.Epsilon)
            return;

        for (int y = yMin; y <= yMax; y++)
        {
            for (int x = xMin; x <= xMax; x++)
            {
                var pixel = new Vector2(x + 0.5f, y + 0.5f);

                float alpha = (pixel.X - v1.X) * (v2.Y - v1.Y) - (pixel.Y - v1.Y) * (v2.X - v1.X);
                float beta = (pixel.X - v2.X) * (v0.Y - v2.Y) - (pixel.Y - v2.Y) * (v0.X - v2.X);
                float gamma = (pixel.X - v0.X) * (v1.Y - v0.Y) - (pixel.Y - v0.Y) * (v1.X - v0.X);

                if (alpha < 0 || beta < 0 || gamma < 0)
                    continue;

                alpha /= denom;
                beta /= denom;
                gamma /= denom;

                // КЛЮЧЕВОЙ момент — линейная глубина
                float invZ =
                    alpha * invZ0 +
                    beta * invZ1 +
                    gamma * invZ2;

                if (invZ <= 0)
                    continue;

                int index = y * width + x;

                // сравнение по invZ (а не 1/z)
                if (_zBuffer == null || invZ <= _zBuffer[index])
                    continue;

                // UV (perspective correct)
                float u =
                    (alpha * uv0.X * invZ0 +
                     beta * uv1.X * invZ1 +
                     gamma * uv2.X * invZ2) / invZ;

                float v =
                    (alpha * uv0.Y * invZ0 +
                     beta * uv1.Y * invZ1 +
                     gamma * uv2.Y * invZ2) / invZ;

                u = Math.Clamp(u, 0.0f, 1.0f);
                v = Math.Clamp(1.0f - v, 0.0f, 1.0f);

                // Position (correct)
                var position =
                (
                    alpha * world0 * invZ0 +
                    beta * world1 * invZ1 +
                    gamma * world2 * invZ2
                ) / invZ;

                // Normal (correct)
                var interpolatedNormal =
                (
                    alpha * n0 * invZ0 +
                    beta * n1 * invZ1 +
                    gamma * n2 * invZ2
                ) / invZ;

                interpolatedNormal = Vector3.Normalize(interpolatedNormal);

                // Textures
                var diffuseSample = textures.diffuseTexture.GetColor(u, v);

                var finalNormal = GetNormal(
                    textures.normalTexture,
                    u, v,
                    n0, n1, n2,
                    alpha, beta, gamma, _objModel.GlobalMatrix
                    );

                (Color specularSample, float specularStrength) =
                    GetSpecular(textures.specularTexture, u, v, specularCoeff, specularColor);

                // Lighting
                var color = ShadePhong(
                    finalNormal,
                    position,
                    lights,
                    ambientCoeff,
                    ambientColor,
                    diffuseCoeff,
                    diffuseSample,
                    specularStrength,
                    specularSample,
                    shininess,
                    eyePos);

                _zBuffer[index] = invZ;
                bufferPtr[index] = color;
            }
        }
    }

    private static Vector3 GetNormal(
        TextureMap? normalMap,
        float u, float v,
        Vector3 n0, Vector3 n1, Vector3 n2,
        float alpha, float beta, float gamma,
        Matrix4x4 modelWorldMatrix)
    {
        if (normalMap == null)
        {
            var normal = n0 * alpha + n1 * beta + n2 * gamma;
            return normal;
        }

        var normalColor = normalMap.GetColor(u, v);
        var sampledNormal = new Vector3(
            normalColor.ScR * 2 - 1,
            normalColor.ScG * 2 - 1,
            normalColor.ScB * 2 - 1
        );

        sampledNormal = Vector3.TransformNormal(sampledNormal, modelWorldMatrix);
        return Vector3.Normalize(sampledNormal);
    }

    private static (Color specularColor, float specularCoeff) GetSpecular(
        TextureMap? specularMap,
        float u, float v,
        float baseSpecularCoeff,
        Color baseSpecularColor)
    {
        var specularStrength = 1.0f;
        var specularSample = baseSpecularColor;

        if (specularMap != null)
        {
            specularSample = specularMap.GetColor(u, v);
            specularStrength = (specularSample.ScR + specularSample.ScG + specularSample.ScB) / 3.0f;
        }

        var specularCoeff = baseSpecularCoeff * specularStrength;
        return (specularSample, specularCoeff);
    }

    private static int ShadePhong(
        Vector3 normal,
        Vector3 center,
        (Vector3 SourceOfLight, Color Color, float Intensity)[] lights,
        float ambientCoeff,
        Color ambientColor,
        float diffuseCoeff,
        Color diffuseSample,
        float specularCoeff,
        Color specularColor,
        float shininess,
        Vector3 eyePos)
    {
        normal = Vector3.Normalize(normal);
        var viewDir = Vector3.Normalize(eyePos - center);

        var rColor = ambientColor.ScR * ambientCoeff;
        var gColor = ambientColor.ScG * ambientCoeff;
        var bColor = ambientColor.ScB * ambientCoeff;

        foreach (var light in lights)
        {
            var lightDirection = Vector3.Normalize(light.SourceOfLight - center);
            var dot = Vector3.Dot(normal, lightDirection);

            if (dot <= 0)
                continue;

            var intensity = dot * light.Intensity;

            rColor += intensity * light.Color.ScR * diffuseCoeff * diffuseSample.ScR;
            gColor += intensity * light.Color.ScG * diffuseCoeff * diffuseSample.ScG;
            bColor += intensity * light.Color.ScB * diffuseCoeff * diffuseSample.ScB;

            var specular = CalcSpecular(normal, lightDirection, viewDir, shininess);
            rColor += specularCoeff * specular * light.Color.ScR * specularColor.ScR;
            gColor += specularCoeff * specular * light.Color.ScG * specularColor.ScG;
            bColor += specularCoeff * specular * light.Color.ScB * specularColor.ScB;
        }

        // Gamma correction
        rColor = MathF.Pow(rColor, 1 / 2.2f);
        gColor = MathF.Pow(gColor, 1 / 2.2f);
        bColor = MathF.Pow(bColor, 1 / 2.2f);

        return
            (int)MathF.Min(255.0f, MathF.Round(bColor * 255)) |
            (int)MathF.Min(255.0f, MathF.Round(gColor * 255)) << 8 |
            (int)MathF.Min(255.0f, MathF.Round(rColor * 255)) << 16 |
            ambientColor.A << 24;
    }

    private static float CalcSpecular(Vector3 normal, Vector3 lightDirection, Vector3 viewDir, float shininess)
    {
        var reflectedLight = Vector3.Reflect(-lightDirection, normal);
        var specFactor = MathF.Max(Vector3.Dot(reflectedLight, viewDir), 0.0f);
        return MathF.Pow(specFactor, shininess);
    }
}