using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ModelViewer.MVVM.ViewModels;

namespace ModelViewer.MVVM.Views;

public partial class CanvasView : UserControl
{
    public CanvasView()
    {
        InitializeComponent();
        SizeChanged += OnSizeChanged;
    }
    
    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is CanvasViewModel canvasViewModel)
        {
            UpdateCanvasSize(canvasViewModel);
            canvasViewModel.OnViewLoaded();
        }
    }

    private void OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (DataContext is CanvasViewModel canvasViewModel)
        {
            UpdateCanvasSize(canvasViewModel);
            canvasViewModel.OnResize();
        }
    }

    private void UpdateCanvasSize(CanvasViewModel canvasViewModel)
    {

        canvasViewModel.Scene.CanvasHeight = (int)CanvasGrid.ActualHeight;
        canvasViewModel.Scene.CanvasWidth = (int)CanvasGrid.ActualWidth;
        canvasViewModel.Scene.Camera.AspectRatio = (float)(CanvasGrid.ActualWidth / CanvasGrid.ActualHeight);

        DpiScale dpi = VisualTreeHelper.GetDpi(this);
        Console.WriteLine(dpi.DpiScaleX);
        canvasViewModel.Scale = (dpi.DpiScaleX, dpi.DpiScaleY);
    }
}