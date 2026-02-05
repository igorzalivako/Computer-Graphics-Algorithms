using System.Numerics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Core.Entities;
using Core.ObjParser;
using Microsoft.Win32;
using ModelViewer.Renderers;

namespace ModelViewer.MVVM.ViewModels;

public class CanvasViewModel : ObservableObject
{
    private string _filePath = string.Empty;

    private WriteableBitmap? _writeableBitmap;

    private Scene _scene;

    private Point _mousePosition;

    public Scene Scene
    {
        get => _scene;
        set
        {
            _scene = value;
            OnPropertyChanged();
        }
    }

    public WriteableBitmap? WriteableBitmap
    {
        get => _writeableBitmap;
        set
        {
            _writeableBitmap = value;
            OnPropertyChanged();
        }
    }

    public RelayCommand LoadFileCommand { get; }

    public RelayCommand MouseWheelCommand { get; }

    public RelayCommand MouseMoveCommand { get; }

    public RelayCommand KeyPressCommand { get; }

    public CanvasViewModel()
    {
        LoadFileCommand = new RelayCommand(LoadFile);
        MouseWheelCommand = new RelayCommand(OnMouseWheel);
        MouseMoveCommand = new RelayCommand(OnMouseMove);
        KeyPressCommand = new RelayCommand(OnKeyPress);

        _scene = new Scene();
    }

    public void OnViewLoaded()
    {
        ResizeBitmap();
    }

    public void OnResize()
    {
        ResizeBitmap();
    }

    public void ResizeBitmap()
    {
        if (WriteableBitmap == null 
            || WriteableBitmap.PixelWidth != Scene.CanvasWidth
            || WriteableBitmap.PixelHeight != Scene.CanvasHeight)
        {
            WriteableBitmap = new WriteableBitmap(
            pixelWidth: Scene.CanvasWidth,
            pixelHeight: Scene.CanvasHeight,
            dpiX: 96,
            dpiY: 96,
            pixelFormat: PixelFormats.Bgra32,
            palette: null);
        }
    }

    private void OnMouseWheel(object? parameter)
    {
        if (Scene.ObjModel is null)
        {
            return;
        }

        if (parameter is not MouseWheelEventArgs args)
        {
            return;
        }

        Scene.Camera.Radius -= args.Delta / 1000.0f;

        if (Scene.Camera.Radius < Scene.Camera.ZNear)
        {
            Scene.Camera.Radius = Scene.Camera.ZNear;
        }

        if (Scene.Camera.Radius > Scene.Camera.ZFar)
        {
            Scene.Camera.Radius = Scene.Camera.ZFar;
        }

        UpdateCanvas();
    }

    private void OnMouseMove(object? parameter)
    {
        if (Scene.ObjModel is null)
        {
            return;
        }

        if (parameter is not MouseEventArgs args)
        {
            return;
        }

        var currentMousePosition = args.GetPosition(null);
        var delta = currentMousePosition - _mousePosition;

        if (args.LeftButton == MouseButtonState.Pressed)
        {
            Scene.ObjModel.Rotation = new Vector3(
                Scene.ObjModel.Rotation.X + (float)delta.Y * MathF.PI / 360.0f,
                Scene.ObjModel.Rotation.Y,
                Scene.ObjModel.Rotation.Z);
        }
        else if (args.RightButton == MouseButtonState.Pressed)
        {
            Scene.ObjModel.Rotation = new Vector3(
                Scene.ObjModel.Rotation.X,
                Scene.ObjModel.Rotation.Y + (float)delta.X * MathF.PI / 360.0f,
                Scene.ObjModel.Rotation.Z);
        }

        _mousePosition = currentMousePosition;

        UpdateCanvas();
    }

    private void OnKeyPress(object? parameter)
    {
        if (Scene.ObjModel is null)
        {
            return;
        }

        if (parameter is not Key key)
        {
            return;
        }

        const float delta = 0.05f;
        switch (key)
        {
            case Key.W: Scene.ObjModel.Position += new Vector3(0, delta, 0); break;
            case Key.A: Scene.ObjModel.Position += new Vector3(-delta, 0, 0); break;
            case Key.S: Scene.ObjModel.Position += new Vector3(0, -delta, 0); break;
            case Key.D: Scene.ObjModel.Position += new Vector3(delta, 0, 0); break;
        }

        UpdateCanvas();
    }

    private void LoadFile(object? parameter)
    {
        try
        {
            var openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
            {
                _filePath = openFileDialog.FileName;
            }
            else
            {
                return;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при выборе файла: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        try
        {
            ObjParser objParser = new ObjParser();
            Scene.ObjModel = objParser.Load(_filePath);
            UpdateCanvas();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при загрузке файла: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void UpdateCanvas()
    {
        Scene.Camera.ChangeEyePosition();
        Scene.TransformObject();
        if (Scene.ObjModel != null && WriteableBitmap != null)
        {
            WireframeRenderer.RenderModel(
                Scene.ObjModel,
                WriteableBitmap,
                Scene.Camera.ZNear,
                Scene.Camera.ZFar,
                Color.FromArgb(255, 150, 147, 147));
        }
    }
}
