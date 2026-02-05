using System.Globalization;
using System.Numerics;
using Core.Entities;

namespace Core.ObjParser
{
    public class ObjParser
    {
        private readonly NumberFormatInfo _numberFormat = CultureInfo.InvariantCulture.NumberFormat;

        public ObjModel Load(string filePath)
        {
            var objModel = new ObjModel();
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"Файл не найден: {filePath}");
            }

            objModel.Vertices.Clear();
            objModel.TextureVertices.Clear();
            objModel.VertexNormals.Clear();
            objModel.Faces.Clear();

            string[] lines = File.ReadAllLines(filePath);
            ParseLines(lines, objModel);

            return objModel;
        }

        public void LoadFromString(string objContent, ObjModel objModel)
        {
            objModel.Vertices.Clear();
            objModel.TextureVertices.Clear();
            objModel.VertexNormals.Clear();
            objModel.Faces.Clear();

            string[] lines = objContent.Split('\n');
            ParseLines(lines, objModel);
        }

        private void ParseLines(string[] lines, ObjModel objModel)
        {
            foreach (string line in lines)
            {
                ParseLine(line.Trim(), objModel);
            }
        }

        private void ParseLine(string line, ObjModel objModel)
        {
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#'))
            {
                return;
            }

            string[] parts = line.Split([' '], StringSplitOptions.RemoveEmptyEntries);

            string command = parts[0];
            string[] data = [.. parts.Skip(1)];

            switch (command.ToLower())
            {
                case "v":
                    ParseVertex(data, objModel);
                    break;
                case "vt":
                    ParseTextureVertex(data, objModel);
                    break;
                case "vn":
                    ParseVertexNormal(data, objModel);
                    break;
                case "f":
                    ParseFace(data, objModel);
                    break;
            }
        }

        private void ParseVertex(string[] data, ObjModel objModel)
        {
            if (data.Length < 3)
            {
                return;
            }

            float x = ParseFloat(data[0]);
            float y = ParseFloat(data[1]);
            float z = ParseFloat(data[2]);
            float w = data.Length >= 4 ? ParseFloat(data[3]) : 1.0f;

            objModel.Vertices.Add(new Vector4(x, y, z, w));
        }

        private void ParseTextureVertex(string[] data, ObjModel objModel)
        {
            if (data.Length < 1)
            {
                return;
            }

            float u = ParseFloat(data[0]);
            float v = data.Length >= 2 ? ParseFloat(data[1]) : 0.0f;
            float w = data.Length >= 3 ? ParseFloat(data[2]) : 0.0f;

            objModel.TextureVertices.Add(new Vector3(u, v, w));
        }

        private void ParseVertexNormal(string[] data, ObjModel objModel)
        {
            if (data.Length < 3)
            {
                return;
            }

            float i = ParseFloat(data[0]);
            float j = ParseFloat(data[1]);
            float k = ParseFloat(data[2]);

            objModel.VertexNormals.Add(new Vector3(i, j, k));
        }

        private static void ParseFace(string[] data, ObjModel objModel)
        {
            if (data.Length < 3)
            {
                return;
            }

            var face = new Face();

            foreach (string faceData in data)
            {
                FaceIndex index = ParseFaceIndex(faceData, objModel);
                face.Indexes.Add(index);
            }

            objModel.Faces.Add(face);
        }

        private static FaceIndex ParseFaceIndex(string faceData, ObjModel objModel)
        {
            string[] parts = faceData.Split('/');

            // Обработка отрицательных индексов и преобразование к индексам с 0
            int vertexIndex = ParseIndex(parts[0], objModel.Vertices.Count);
            int? textureIndex = null;
            int? normalIndex = null;

            if (parts.Length > 1 && !string.IsNullOrEmpty(parts[1]))
            {
                textureIndex = ParseIndex(parts[1], objModel.TextureVertices.Count);
            }

            if (parts.Length > 2 && !string.IsNullOrEmpty(parts[2]))
            {
                normalIndex = ParseIndex(parts[2], objModel.VertexNormals.Count);
            }

            return new FaceIndex(vertexIndex, textureIndex, normalIndex);
        }

        private static int ParseIndex(string indexStr, int listCount)
        {
            int index = int.Parse(indexStr);

            if (index < 0)
            {
                index = listCount + index + 1;
            }

            // в obj индексы с 1
            return index - 1;
        }

        private float ParseFloat(string value)
        {
            return float.Parse(value, _numberFormat);
        }

        public void Save(string filePath, ObjModel objModel)
        {
            using StreamWriter writer = new StreamWriter(filePath);

            foreach (var vertex in objModel.Vertices)
            {
                writer.WriteLine(vertex.ToString());
            }

            foreach (var texVertex in objModel.TextureVertices)
            {
                writer.WriteLine(texVertex.ToString());
            }

            foreach (var normal in objModel.VertexNormals)
            {
                writer.WriteLine(normal.ToString());
            }

            foreach (var face in objModel.Faces)
            {
                writer.WriteLine(face.ToString());
            }
        }

    }
}
