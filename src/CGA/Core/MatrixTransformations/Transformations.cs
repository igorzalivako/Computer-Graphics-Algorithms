using System.Numerics;

namespace Core.MatrixTransformations
{
    public static class Transformations
    {
        public static Matrix4x4 CreateTranslationMatrix(Vector3 translation)
        {
            // Матрица перемещения:
            // [ 1  0  0  Tx ]
            // [ 0  1  0  Ty ]
            // [ 0  0  1  Tz ]
            // [ 0  0  0  1  ]

            var translationMatrix = new Matrix4x4(
                1, 0, 0, translation.X,
                0, 1, 0, translation.Y,
                0, 0, 1, translation.Z,
                0, 0, 0, 1
            );

            return Matrix4x4.Transpose(translationMatrix);
        }

        public static Matrix4x4 CreateScaleMatrix(Vector3 scale)
        {
            // Матрица масштаба:
            // [ Sx  0   0   0 ]
            // [ 0   Sy  0   0 ]
            // [ 0   0   Sz  0 ]
            // [ 0   0   0   1 ]

            var scaleMatrix = new Matrix4x4(
                scale.X, 0, 0, 0,
                0, scale.Y, 0, 0,
                0, 0, scale.Z, 0,
                0, 0, 0, 1
            );

            return Matrix4x4.Transpose(scaleMatrix);
        }

        public static Matrix4x4 CreateRotationXMatrix(float angleRadians)
        {
            float cos = MathF.Cos(angleRadians);
            float sin = MathF.Sin(angleRadians);

            // Матрица поворота вокруг оси X:
            // [ 1   0    0    0 ]
            // [ 0   cos -sin  0 ]
            // [ 0   sin  cos  0 ]
            // [ 0   0    0    1 ]

            var rotationXMatrix = new Matrix4x4(
                1, 0, 0, 0,
                0, cos, -sin, 0,
                0, sin, cos, 0,
                0, 0, 0, 1
            );

            return Matrix4x4.Transpose(rotationXMatrix);
        }

        public static Matrix4x4 CreateRotationYMatrix(float angleRadians)
        {
            float cos = MathF.Cos(angleRadians);
            float sin = MathF.Sin(angleRadians);

            // Матрица поворота вокруг оси Y:
            // [ cos  0   sin  0 ]
            // [ 0    1   0    0 ]
            // [ -sin 0   cos  0 ]
            // [ 0    0   0    1 ]

            var rotationYMatrix = new Matrix4x4(
                cos, 0, sin, 0,
                0, 1, 0, 0,
                -sin, 0, cos, 0,
                0, 0, 0, 1
            );

            return Matrix4x4.Transpose(rotationYMatrix);
        }

        public static Matrix4x4 CreateRotationZMatrix(float angleRadians)
        {
            float cos = MathF.Cos(angleRadians);
            float sin = MathF.Sin(angleRadians);

            // Матрица поворота вокруг оси Z:
            // [ cos -sin  0   0 ]
            // [ sin  cos  0   0 ]
            // [ 0    0    1   0 ]
            // [ 0    0    0   1 ]

            var rotationZMatrix = new Matrix4x4(
                cos, -sin, 0, 0,
                sin, cos, 0, 0,
                0, 0, 1, 0,
                0, 0, 0, 1
            );

            return Matrix4x4.Transpose(rotationZMatrix);
        }

        public static Matrix4x4 CreateTransformationMatrix(
            Vector3 scale,
            Vector3 rotation,
            Vector3 translation)
        {
            Matrix4x4 scaleMatrix = CreateScaleMatrix(scale);
            Matrix4x4 rotationXMatrix = CreateRotationXMatrix(rotation.X);
            Matrix4x4 rotationYMatrix = CreateRotationYMatrix(rotation.Y);
            Matrix4x4 rotationZMatrix = CreateRotationZMatrix(rotation.Z);
            Matrix4x4 translationMatrix = CreateTranslationMatrix(translation);

            //
            Matrix4x4 rotationMatrix = rotationXMatrix * rotationYMatrix * rotationZMatrix;

            return translationMatrix * rotationMatrix * scaleMatrix;
        }

        public static Matrix4x4 CreateViewMatrix(Vector3 eye, Vector3 target, Vector3 up)
        {
            Vector3 zAxis = Vector3.Normalize(eye - target);
            Vector3 xAxis = Vector3.Normalize(Vector3.Cross(up, zAxis));
            Vector3 yAxis = up;

            float tx = -Vector3.Dot(xAxis, eye);
            float ty = -Vector3.Dot(yAxis, eye);
            float tz = -Vector3.Dot(zAxis, eye);

            var viewMatrix = new Matrix4x4(
                xAxis.X, xAxis.Y, xAxis.Z, tx,
                yAxis.X, yAxis.Y, yAxis.Z, ty,
                zAxis.X, zAxis.Y, zAxis.Z, tz,
                0.0f, 0.0f, 0.0f, 1.0f);

            viewMatrix = Matrix4x4.Transpose(viewMatrix);

            return viewMatrix;
        }

        /// <summary>
        /// Возвращает матрицу преобразования из координат пространства наблюдателя в пространство проекции
        /// </summary>
        /// <param name="fov">Поле зрение камеры по оси Y</param>
        /// <param name="aspect">Соотношение сторон обзора камеры</param>
        /// <param name="znear">Расстояние до ближней плоскости обзора камеры</param>
        /// <param name="zfar">Расстояние до дальней плоскости обзора камеры</param>
        /// <returns></returns>
        public static Matrix4x4 CreateProjectionMatrix(float fov, float aspect, float znear, float zfar)
        {
            float tanHalfFov = MathF.Tan(fov / 2);

            float m00 = 1 / (aspect * tanHalfFov);
            float m11 = 1 / tanHalfFov;
            float m22 = zfar / (znear - zfar);
            float m32 = znear * zfar / (znear - zfar);

            var perspectiveMatrix = new Matrix4x4(
                m00, 0, 0, 0,
                0, m11, 0, 0,
                0, 0, m22, m32,
                0, 0, -1, 0
            );

            perspectiveMatrix = Matrix4x4.Transpose(perspectiveMatrix);

            return perspectiveMatrix;
        }

        /// <summary>
        /// Возвращает матрицу преобразования координат из пространства проекции в пространство окна просмотра
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="xmin"></param>
        /// <param name="ymin"></param>
        /// <returns></returns>
        public static Matrix4x4 CreateViewportMatrix(float width, float height, float xmin, float ymin)
        {
            float m00 = width / 2;
            float m03 = xmin + width / 2;
            float m11 = -height / 2;
            float m13 = ymin + height / 2;

            var viewportMatrix = new Matrix4x4(
                m00, 0, 0, m03,
                0, m11, 0, m13,
                0, 0, 1, 0,
                0, 0, 0, 1
            );

            viewportMatrix = Matrix4x4.Transpose(viewportMatrix);

            return viewportMatrix;
        }
    }
}