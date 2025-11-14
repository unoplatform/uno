#nullable enable

using Microsoft.UI.Input;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Graphics;

namespace WindowingSamples;

/// <summary>
/// Window demonstrating SetBorderAndTitleBar(true, false) API with custom caption buttons.
/// Also demonstrates InputNonClientPointerSource.SetRegionRects for Maximize button to support Snap Layouts.
/// </summary>
public sealed partial class CustomTitleBarWindow : Window
{
	private AppWindow? _appWindow;
	private InputNonClientPointerSource? _nonClientPointerSource;

	public CustomTitleBarWindow()
	{
		InitializeComponent();

		Title = "Custom Title Bar with Caption Buttons";

		// Get AppWindow
		_appWindow = AppWindow;

		if (_appWindow is not null)
		{
			// SetBorderAndTitleBar with (true, false) to extend client area and hide system caption buttons
			var presenter = _appWindow.Presenter as OverlappedPresenter;
			presenter?.SetBorderAndTitleBar(hasBorder: true, hasTitleBar: false);

			// Set the drag region for the custom title bar
			SetTitleBar(DraggableBar);

			// Configure InputNonClientPointerSource for Maximize button to support Snap Layouts
			ConfigureMaximizeButtonSnapLayouts();

			// Update maximize/restore icon based on current state
			UpdateMaximizeRestoreIcon();

			// Subscribe to window state changes
			_appWindow.Changed += OnAppWindowChanged;
		}

		// Update icon when window state changes
		SizeChanged += (s, e) => UpdateMaximizeRestoreIcon();
	}

	private void ConfigureMaximizeButtonSnapLayouts()
	{
		if (_appWindow is null || MaximizeRestoreButton is null)
			return;

		try
		{
			// Get the InputNonClientPointerSource for non-client area input
			_nonClientPointerSource = InputNonClientPointerSource.GetForWindowId(_appWindow.Id);

			if (_nonClientPointerSource is not null)
			{
				// Update the region when the button is loaded or size changes
				_appWindow.Changed += (s, e) => UpdateMaximizeButtonRegion();
				MaximizeRestoreButton.Loaded += (s, e) => UpdateMaximizeButtonRegion();
				MaximizeRestoreButton.SizeChanged += (s, e) => UpdateMaximizeButtonRegion();
			}
		}
		catch
		{
			// InputNonClientPointerSource may not be available on all platforms
			// This is expected and we can safely ignore
		}
	}

	private void UpdateMaximizeButtonRegion()
	{
		if (_nonClientPointerSource is null || MaximizeRestoreButton is null || _appWindow is null)
			return;

		try
		{
			// Get the button's position relative to the window
			var transform = MaximizeRestoreButton.TransformToVisual(null);
			var buttonPosition = transform.TransformPoint(new Windows.Foundation.Point(0, 0));
			var scale = (float)_appWindow.Size.Width / (float)Content!.ActualSize.X;

			// Create a rect for the maximize button region
			var rect = new RectInt32()
			{
				X = (int)(buttonPosition.X * scale),
				Y = (int)(buttonPosition.Y * scale),
				Width = (int)(MaximizeRestoreButton.ActualWidth * scale),
				Height = (int)(MaximizeRestoreButton.ActualHeight * scale)
			};

			// Set the region for the Maximize button to enable Snap Layouts
			_nonClientPointerSource.SetRegionRects(NonClientRegionKind.Maximize, new[] { rect });
		}
		catch
		{
			// SetRegionRects may fail on some platforms or configurations
			// This is expected and we can safely ignore
		}
	}

	private void OnAppWindowChanged(AppWindow sender, AppWindowChangedEventArgs args)
	{
		if (args.DidPresenterChange)
		{
			UpdateMaximizeRestoreIcon();
		}
	}

	private void UpdateMaximizeRestoreIcon()
	{
		if (_appWindow is null || MaximizeRestoreIcon is null)
			return;

		// Update icon based on window state
		var isMaximized = _appWindow.Presenter.Kind == AppWindowPresenterKind.FullScreen ||
						 (_appWindow.Presenter is OverlappedPresenter overlapped && overlapped.State == OverlappedPresenterState.Maximized);

		MaximizeRestoreIcon.Glyph = isMaximized ? "\uE923" : "\uE922"; // Restore or Maximize icon

		var tooltip = isMaximized ? "Restore Down" : "Maximize";
		ToolTipService.SetToolTip(MaximizeRestoreButton, tooltip);
	}

	private void MinimizeClick(object sender, RoutedEventArgs args)
	{
		if (_appWindow?.Presenter is OverlappedPresenter presenter)
		{
			presenter.Minimize();
		}
	}

	private void MaximizeRestoreClick(object sender, RoutedEventArgs args)
	{
		if (_appWindow?.Presenter is OverlappedPresenter presenter)
		{
			if (presenter.State == OverlappedPresenterState.Maximized)
			{
				presenter.Restore();
			}
			else
			{
				presenter.Maximize();
			}
		}
	}

	public void CloseClick(object sender, RoutedEventArgs args) => Close();
}
