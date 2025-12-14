using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace UITests.Microsoft_UI_Xaml_Controls.WebView2Tests
{
	/// <summary>
	/// Test page for WebView2 window closing behavior.
	/// Tests that keyboard shortcuts (Alt+F4 on Windows, Cmd+W/Cmd+Q on macOS) 
	/// properly close the window/application when the WebView has focus.
	/// </summary>
	[Uno.UI.Samples.Controls.Sample(
		"WebView",
		IsManualTest = true,
		Description = "Tests that window-closing keyboard shortcuts work correctly when WebView2 has focus. " +
					  "On Windows, Alt+F4 should close the window. On macOS, Cmd+W should close the window and Cmd+Q should quit the app. " +
					  "Enable the checkbox to test that the Closing event cancellation still works.",
		IgnoreInSnapshotTests = true)]
	public sealed partial class WebView2_WindowClose : Page
	{
		public WebView2_WindowCloseViewModel ViewModel { get; }

		public WebView2_WindowClose()
		{
			this.InitializeComponent();
			ViewModel = new WebView2_WindowCloseViewModel();

			this.Loaded += OnLoaded;
			this.Unloaded += OnUnloaded;
		}

		private void OnLoaded(object sender, RoutedEventArgs e)
		{
			// Subscribe to the window closing event
			var window = XamlRoot?.HostWindow;
			if (window != null)
			{
				window.AppWindow.Closing += OnWindowClosing;
				ViewModel.StatusMessage = "Ready to test. Focus the WebView and try closing shortcuts (Cmd+W or Cmd+Q on macOS).";
			}
			else
			{
				ViewModel.StatusMessage = "ERROR: Could not access window. XamlRoot.HostWindow is null.";
			}
		}

		private void OnWindowClosing(Microsoft.UI.Windowing.AppWindow sender, Microsoft.UI.Windowing.AppWindowClosingEventArgs args)
		{
			if (ViewModel.PreventClose)
			{
				args.Cancel = true;
				ViewModel.StatusMessage = $"Window close prevented at {DateTime.Now:HH:mm:ss}";
			}
		}

		private void OnUnloaded(object sender, RoutedEventArgs e)
		{
			// Clean up event handler
			var window = XamlRoot?.HostWindow;
			if (window != null)
			{
				window.AppWindow.Closing -= OnWindowClosing;
			}
		}
	}

	public class WebView2_WindowCloseViewModel : INotifyPropertyChanged
	{
		private bool _preventClose;
		private string _statusMessage = "Ready to test. Focus the WebView and try closing shortcuts.";

		public bool PreventClose
		{
			get => _preventClose;
			set
			{
				if (_preventClose != value)
				{
					_preventClose = value;
					OnPropertyChanged();
					StatusMessage = value
						? "Window closing is now prevented. Try closing shortcuts - window should stay open."
						: "Window closing is allowed. Try closing shortcuts - window should close.";
				}
			}
		}

		public string StatusMessage
		{
			get => _statusMessage;
			set
			{
				if (_statusMessage != value)
				{
					_statusMessage = value;
					OnPropertyChanged();
				}
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;

		private void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}
