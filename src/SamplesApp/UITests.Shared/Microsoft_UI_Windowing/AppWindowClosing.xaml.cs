using System;
using System.Threading.Tasks;
using Microsoft.UI.Windowing;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using SamplesApp;
using Uno.Disposables;
using Uno.UI.Common;
using Uno.UI.Samples.Controls;
using Uno.UI.Samples.UITests.Helpers;
using Windows.Graphics;

#if !WINDOWS_UWP && !WINAPPSDK
using Uno.UI.Xaml;
using Uno.UI.Xaml.Controls;
using WinUICoreServices = Uno.UI.Xaml.Core.CoreServices;
#endif

namespace UITests.Microsoft_UI_Windowing;

[Sample(
	"Windowing",
	IsManualTest = true,
	Description =
		"Click the button to open a secondary window. " +
		"Try to close it via title bar close button - a dialog should appear and window will remain open. " +
		"Close it via the 'Close' button - no dialog should appear.")]
public sealed partial class AppWindowClosing : Page
{
	public AppWindowClosing()
	{
		this.InitializeComponent();
		if (!SupportsMultipleWindows)
		{
			LogTextBlock.Text = "This platform does not support multiple windows.";
			OpenWindowButton.IsEnabled = false;
		}
	}

	private bool SupportsMultipleWindows =>
#if HAS_UNO
		NativeWindowFactory.SupportsMultipleWindows;
#else
		true;
#endif

	private void OnOpenWindow(object sender, RoutedEventArgs args)
	{
		var secondaryWindow = new Window();
		var content = new Border();
		content.Child = new Button { Content = "Close", Command = new DelegateCommand(() => secondaryWindow.Close()), HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
		secondaryWindow.Content = content;
		secondaryWindow.AppWindow.Closing += OnSecondaryWindowClosing;
		secondaryWindow.Activate();

		async void OnSecondaryWindowClosing(AppWindow sender, AppWindowClosingEventArgs args)
		{
			args.Cancel = true;
			var dialog = new ContentDialog
			{
				Title = "Closing the window canceled.",
				Content = "The secondary window should remain open.",
				CloseButtonText = "OK",
				XamlRoot = content.XamlRoot,
			};

			await dialog.ShowAsync();
		}
	}
}
