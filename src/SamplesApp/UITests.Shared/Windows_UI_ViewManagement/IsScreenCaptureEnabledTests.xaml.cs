using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Uno.UI.Samples.Controls;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Foundation.Metadata;

namespace UITests.Shared.Windows_UI_ViewManagement
{
	[SampleControlInfo("Windows.UI.ViewManagement", "IsScreenCaptureEnabled", description: "Allows disabling screen capture (currently available on Android only)")]
	public sealed partial class IsScreenCaptureEnabledTests : UserControl
	{
		public IsScreenCaptureEnabledTests()
		{
			this.InitializeComponent();
			if (ApiInformation.IsPropertyPresent("Windows.UI.ViewManagement.ApplicationView", "IsScreenCaptureEnabled"))
			{
				CaptureCheckBox.IsChecked = ApplicationView.GetForCurrentView().IsScreenCaptureEnabled;
			}
			else
			{
				InfoMessage.Text = "API not supported on this platform.";
				CaptureCheckBox.IsEnabled = false;
			}
		}

		private void CaptureCheckBoxChanged(object sender, RoutedEventArgs e)
		{
			var checkBox = (CheckBox)sender;
			var enabled = checkBox.IsChecked ?? false;
			ApplicationView.GetForCurrentView().IsScreenCaptureEnabled = enabled;
			InfoMessage.Visibility = enabled ? Visibility.Collapsed : Visibility.Visible;
		}
	}
}
