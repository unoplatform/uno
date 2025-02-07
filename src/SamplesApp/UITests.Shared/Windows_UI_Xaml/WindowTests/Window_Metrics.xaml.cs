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
			AppWindowSize.Text = GetSafe(() => $"{_window.AppWindow.Size.Width:F2} x {_window.AppWindow.Size.Height:F2}");
			AppWindowPosition.Text = GetSafe(() => $"{_window.AppWindow.Position.X:F2}, {_window.AppWindow.Position.Y:F2}");
			AppWindowClientSize.Text = GetSafe(() => $"{_window.AppWindow.ClientSize.Width:F2} x {_window.AppWindow.ClientSize.Height:F2}");
			WindowBounds.Text = GetSafe(() => $"{_window.Bounds.X:F2}, {_window.Bounds.Y:F2}, {_window.Bounds.Width:F2}, {_window.Bounds.Height:F2}");
			XamlRootSize.Text = GetSafe(() => $"{XamlRoot.Size.Width:F2} x {XamlRoot.Size.Height:F2}");
			TitleBarHeight.Text = GetSafe(() => $"{_window.AppWindow.TitleBar.Height:F2}");
			var padding = VisibleBoundsPadding.WindowPadding;
			VisibleBoundsPaddingValue.Text = GetSafe(() => $"{padding.Left:F2}, {padding.Top:F2}, {padding.Right:F2}, {padding.Bottom:F2}");
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
