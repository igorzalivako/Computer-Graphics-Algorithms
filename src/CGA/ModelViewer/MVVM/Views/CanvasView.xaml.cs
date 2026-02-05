using System.Windows;
using System.Windows.Controls;
using ModelViewer.MVVM.ViewModels;

namespace ModelViewer.MVVM.Views;

public partial class CanvasView : UserControl
{
    public CanvasView()
    {
        InitializeComponent();
        SizeChanged += OnSizeChanged;
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
            canvasViewModel.OnResizee();
        }
    }

    private void UpdateCanvasSize(CanvasViewModel canvasViewModel)
    {
        canvasViewModel.Scene.CanvasHeight = (int)CanvasGrid.ActualHeight;
        canvasViewModel.Scene.CanvasWidth = (int)CanvasGrid.ActualWidth;
    }
}