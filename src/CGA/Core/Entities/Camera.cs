using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Core.Entities
{
    public class Camera
    {
        public Vector3 EyePosition { get; private set; } = Vector3.Zero;

        public Vector3 TargetPosition { get; private set; } = Vector3.Zero;

        public Vector3 UpVector { get; private set; } = Vector3.UnitY;

        public float AspectRatio { get; set; } = 16f / 9f;

        public float ZNear { get; set; } = 0.01f;

        public float ZFar { get; set; } = 100f;

        public float Radius { get; set; } = 5f;

        public float Fov { get; set; } = MathF.PI / 4.0f;

        private float Teta { get; set; } = (float)Math.PI / (float)2.3;

        private float Phi { get; set; } = (float)Math.PI / 2;

        public void ChangeEyePosition()
        {
            EyePosition = TargetPosition + new Vector3(
                Radius * (float)Math.Cos(Phi) * (float)Math.Sin(Teta),
                Radius * (float)Math.Cos(Teta),
                Radius * (float)Math.Sin(Phi) * (float)Math.Sin(Teta));
        }
    }
}
