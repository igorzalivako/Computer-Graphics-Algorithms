using Core.MatrixTransformations;

namespace Core.Entities
{
    public class Scene
    {
        public ObjModel? ObjModel { get; set; }
        
        public Camera Camera { get; set; } = new();
        
        public int CanvasWidth { get; set; }
        
        public int CanvasHeight { get; set; }

        public void TransformObject()
        {
            if (ObjModel is null)
            {
                throw new NullReferenceException("Object model is null");
            }

            var world = Transformations.CreateTransformationMatrix(ObjModel.Scale, ObjModel.Rotation, ObjModel.Position);

            var view = Transformations.CreateViewMatrix(Camera.EyePosition, Camera.TargetPosition, Camera.UpVector);

            var projection = Transformations.CreateProjectionMatrix(Camera.Fov, Camera.AspectRatio, Camera.ZNear, Camera.ZFar);

            var viewport = Transformations.CreateViewportMatrix(CanvasWidth, CanvasHeight, 0.0f, 0.0f);

            var transformMatrix = world * view * projection * viewport;
            ObjModel.Transform(transformMatrix, Camera.ZNear, Camera.ZFar);
        }
    }
}
