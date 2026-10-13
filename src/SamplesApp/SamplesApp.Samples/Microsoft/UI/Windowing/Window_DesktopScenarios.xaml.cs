#nullable enable

using System;
using Microsoft.UI.Input;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Uno.UI.Samples.Controls;
using Windows.Graphics;

namespace UITests.Microsoft_UI_Windowing;

[Sample("Windowing", IsManualTest = true, IgnoreInSnapshotTests = true, Description = "Desktop window lifecycle, presenters, client decorations and interactive title bar regions.")]
public sealed partial class Window_DesktopScenarios : Page
{
	private Window? _window;
	private Grid? _titleBar;
	private Button? _titleButton;
	private XamlRoot? _titleRoot;
	private bool _regions;

	public Window_DesktopScenarios()
	{
		this.InitializeComponent();
		Unloaded += (_, _) =>
		{
			_window?.Close();
		};
	}

	private void OpenWindow(object sender, RoutedEventArgs e) => CreateWindow();

	private void CreateWindow()
	{
		if (_window is not null)
		{
			_window.Activate();
			return;
		}
		var window = new Window { Title = "Uno Platform desktop scenarios" };
		_window = window;
		_titleButton = new Button { Content = "Interactive title button", HorizontalAlignment = HorizontalAlignment.Right };
		_titleButton.Click += (_, _) => Status.Text = "Title button clicked";
		_titleBar = new Grid { Height = 40 };
		_titleBar.Children.Add(new TextBlock { Text = "Custom title bar", VerticalAlignment = VerticalAlignment.Center });
		_titleBar.Children.Add(_titleButton);
		var root = new StackPanel
		{
			Background = (Brush)Application.Current.Resources["ApplicationPageBackgroundThemeBrush"],
			Children = { _titleBar, new TextBox { Text = "Type here, then drag the title bar or resize the window.", Margin = new Thickness(16) } }
		};
		window.Content = root;
		var titleBar = _titleBar;
		titleBar.SizeChanged += (_, _) => UpdateRegions();
		titleBar.Loaded += (_, _) =>
		{
			if (_titleRoot is not null)
			{
				_titleRoot.Changed -= TitleRootChanged;
			}
			_titleRoot = titleBar.XamlRoot;
			if (_titleRoot is not null)
			{
				_titleRoot.Changed += TitleRootChanged;
			}
			UpdateRegions();
		};
		window.Closed += (_, _) =>
		{
			if (_titleRoot is not null)
			{
				_titleRoot.Changed -= TitleRootChanged;
			}
			_titleRoot = null;
			_window = null;
			_titleBar = null;
			_titleButton = null;
			_regions = false;
			Status.Text = "Secondary window closed";
		};
		window.Activate();
	}

	private void ResizeWindow(object sender, RoutedEventArgs e) => _window?.AppWindow.Resize(new SizeInt32(900, 600));
	private void MaximizeWindow(object sender, RoutedEventArgs e) => (_window?.AppWindow.Presenter as OverlappedPresenter)?.Maximize();
	private void RestoreWindow(object sender, RoutedEventArgs e) => (_window?.AppWindow.Presenter as OverlappedPresenter)?.Restore(true);
	private void MinimizeWindow(object sender, RoutedEventArgs e) => (_window?.AppWindow.Presenter as OverlappedPresenter)?.Minimize();
	private void CloseWindow(object sender, RoutedEventArgs e) => _window?.Close();

	private void ToggleFullScreen(object sender, RoutedEventArgs e)
	{
		_window?.AppWindow.SetPresenter(_window.AppWindow.Presenter.Kind == AppWindowPresenterKind.FullScreen
			? AppWindowPresenterKind.Overlapped : AppWindowPresenterKind.FullScreen);
	}

	private void SetTitleBar(object sender, RoutedEventArgs e)
	{
		if (_window is { } window)
		{
			ClearRegions();
			window.ExtendsContentIntoTitleBar = true;
			window.SetTitleBar(_titleBar);
		}
	}

	private void SetRegions(object sender, RoutedEventArgs e)
	{
		if (_window is { } window)
		{
			window.SetTitleBar(null);
			window.ExtendsContentIntoTitleBar = true;
			_regions = true;
			UpdateRegions();
		}
	}

	private void ResetTitleBar(object sender, RoutedEventArgs e)
	{
		if (_window is { } window)
		{
			ClearRegions();
			window.SetTitleBar(null);
			window.ExtendsContentIntoTitleBar = false;
		}
	}

	private void ClearRegions()
	{
		_regions = false;
		if (_window is { } window)
		{
			var source = InputNonClientPointerSource.GetForWindowId(window.AppWindow.Id);
			source.ClearRegionRects(NonClientRegionKind.Caption);
			source.ClearRegionRects(NonClientRegionKind.Passthrough);
		}
	}

	private void TitleRootChanged(XamlRoot sender, XamlRootChangedEventArgs args) => UpdateRegions();

	private void UpdateRegions()
	{
		if (!_regions || _window is null || _titleBar is null || _titleButton is null || _titleRoot is null)
		{
			return;
		}
		RectInt32 Bounds(FrameworkElement element)
		{
			var point = element.TransformToVisual(null).TransformPoint(default);
			var scale = _titleRoot.RasterizationScale;
			var left = (int)Math.Floor(point.X * scale);
			var top = (int)Math.Floor(point.Y * scale);
			return new RectInt32(left, top, (int)Math.Ceiling((point.X + element.ActualWidth) * scale) - left,
				(int)Math.Ceiling((point.Y + element.ActualHeight) * scale) - top);
		}
		var source = InputNonClientPointerSource.GetForWindowId(_window.AppWindow.Id);
		source.SetRegionRects(NonClientRegionKind.Caption, [Bounds(_titleBar)]);
		source.SetRegionRects(NonClientRegionKind.Passthrough, [Bounds(_titleButton)]);
	}
}
