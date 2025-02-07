using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Uno.UI.Samples.Controls;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

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
			AppWindowSize.Text = $"{_window.AppWindow.Size.Width} x {_window.AppWindow.Size.Height}";
			AppWindowPosition.Text = $"{_window.AppWindow.Position.X}, {_window.AppWindow.Position.Y}";
			AppWindowClientSize.Text = $"{_window.AppWindow.ClientSize.Width} x {_window.AppWindow.ClientSize.Height}";
			WindowBounds.Text = $"{_window.Bounds.X}, {_window.Bounds.Y}, {_window.Bounds.Width}, {_window.Bounds.Height}";
			XamlRootSize.Text = $"{XamlRoot.Size.Width} x {XamlRoot.Size.Height}";
			TitleBarHeight.Text = $"{_window.AppWindow.TitleBar.Height}";
		}
	}
}
