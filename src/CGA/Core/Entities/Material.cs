using System.Numerics;

namespace Core.Entities
{
    public class Material
    {
        public string Name { get; set; }

        public Vector3 AmbientColor { get; set; }
        public Vector3 DiffuseColor { get; set; }
        public Vector3 SpecularColor { get; set; }

        public float Transparency { get; set; }
        public float OpticalDensity { get; set; }
        public int IlluminationModel { get; set; }
        public float BumpScale = 1;
        public float Shininess { get; set; }

        public string DiffuseMap { get; set; } = string.Empty;
        public string NormalMap { get; set; } = string.Empty;
        public string BumpMap { get; set; } = string.Empty;
        public string MraoMap { get; set; } = string.Empty;
        public string AmbientOcclusionMap { get; set; } = string.Empty;
        public string ReflectionMap { get; set; } = string.Empty;
        public string RoughnessMap { get; set; } = string.Empty;
        public string EmissiveMap { get; set; } = string.Empty;
        public string SpecularMap { get; set; } = string.Empty;
    }
}
