#nullable enable

using Microsoft.UI.Input;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Windows.Graphics;

namespace WindowingSamples;

/// <summary>
/// Window demonstrating InputNonClientPointerSource.SetRegionRects for Caption region.
/// This shows how to create a custom draggable region in the middle of the window.
/// </summary>
public sealed partial class DraggableRegionWindow : Window
{
	private AppWindow? _appWindow;
	private InputNonClientPointerSource? _nonClientPointerSource;

	public DraggableRegionWindow()
	{
		InitializeComponent();

		Title = "Draggable Region Demo";

		// Get AppWindow
		_appWindow = AppWindow;

		if (_appWindow is not null)
		{
			// Get the InputNonClientPointerSource for non-client area input
			try
			{
				_nonClientPointerSource = InputNonClientPointerSource.GetForWindowId(_appWindow.Id);

				if (_nonClientPointerSource is not null)
				{
					// Update the draggable region when the element is loaded or resized
					DraggableRegion.Loaded += (s, e) => UpdateDraggableRegion();
					DraggableRegion.SizeChanged += (s, e) => UpdateDraggableRegion();
					SizeChanged += (s, e) => UpdateDraggableRegion();
				}
			}
			catch
			{
				// InputNonClientPointerSource may not be available on all platforms
				// This is expected and we can safely ignore
			}
		}
	}

	private void UpdateDraggableRegion()
	{
		if (_nonClientPointerSource is null || DraggableRegion is null || _appWindow is null)
			return;

		try
		{
			// Get the draggable region's position relative to the window
			var transform = DraggableRegion.TransformToVisual(null);
			var position = transform.TransformPoint(new Windows.Foundation.Point(0, 0));

			// Calculate scale factor between logical and physical pixels
			var scale = (float)_appWindow.ClientSize.Width / (float)Content!.ActualSize.X;

			// Create a rect for the draggable region
			var rect = new RectInt32()
			{
				X = (int)(position.X * scale),
				Y = (int)(position.Y * scale),
				Width = (int)(DraggableRegion.ActualWidth * scale),
				Height = (int)(DraggableRegion.ActualHeight * scale)
			};

			// Set the region as a Caption region (draggable)
			_nonClientPointerSource.SetRegionRects(NonClientRegionKind.Caption, new[] { rect });
		}
		catch
		{
			// SetRegionRects may fail on some platforms or configurations
			// This is expected and we can safely ignore
		}
	}

	public void CloseClick(object sender, RoutedEventArgs args) => Close();
}
