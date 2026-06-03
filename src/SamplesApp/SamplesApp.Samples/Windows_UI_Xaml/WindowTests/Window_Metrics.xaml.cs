using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;
using Uno.UI.Toolkit;

namespace UITests.Windows_UI_Xaml.WindowTests
{
	[Sample("Windowing")]
	public sealed partial class Window_Metrics : UserControl
	{
		private Window _window;

		public Window_Metrics()
		{
			this.InitializeComponent();

			_window = SamplesApp.App.MainWindow;
			ExtendsIntoTitleBarCheckBox.IsChecked = _window.ExtendsContentIntoTitleBar;
			ExtendsIntoTitleBarCheckBox.Checked += (s, e) => _window.ExtendsContentIntoTitleBar = true;
			ExtendsIntoTitleBarCheckBox.Unchecked += (s, e) => _window.ExtendsContentIntoTitleBar = false;

			this.Loaded += Window_Metrics_Loaded;
		}

		private void Window_Metrics_Loaded(object sender, RoutedEventArgs e) => GetMetricsClick();

		private void GetMetricsClick()
		{
#if HAS_UNO_WINUI // AppWindow APIs are only available in WinUI flavor of Uno Platform
			AppWindowSize.Text = GetSafe(() => $"{_window.AppWindow.Size.Width:0.##} x {_window.AppWindow.Size.Height:0.##}");
			AppWindowPosition.Text = GetSafe(() => $"{_window.AppWindow.Position.X:0.##}, {_window.AppWindow.Position.Y:0.##}");
			AppWindowClientSize.Text = GetSafe(() => $"{_window.AppWindow.ClientSize.Width:0.##} x {_window.AppWindow.ClientSize.Height:0.##}");
			TitleBarHeight.Text = GetSafe(() => $"{_window.AppWindow.TitleBar.Height:0.##}");
			var visibleBounds = XamlRoot.VisualTree.VisibleBounds;
			VisualTreeVisibleBounds.Text = GetSafe(() => $"X: {visibleBounds.X:0.##}, Y: {visibleBounds.Y:0.##}, Width: {visibleBounds.Width:0.##}, Height: {visibleBounds.Height:0.##}");
#endif
			var windowBounds = _window.Bounds;
			WindowBounds.Text = GetSafe(() => $"X: {windowBounds.X:0.##}, Y: {windowBounds.Y:0.##}, Width: {windowBounds.Width:0.##}, Height: {windowBounds.Height:0.##}");
			XamlRootSize.Text = GetSafe(() => $"{XamlRoot.Size.Width:0.##} x {XamlRoot.Size.Height:0.##}");
			var padding = VisibleBoundsPadding.WindowPadding;
			VisibleBoundsPaddingValue.Text = GetSafe(() => $"{padding.Left:0.##}, {padding.Top:0.##}, {padding.Right:0.##}, {padding.Bottom:0.##}");
		}

		private string GetSafe(Func<string> getter)
		{
			try
			{
				return getter();
			}
			catch (Exception)
			{
				return "N/A";
			}
		}
	}
}
