using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Uno.UI.Samples.Controls;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.ViewManagement;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Windows.Foundation.Metadata;

namespace UITests.Shared.Windows_UI_ViewManagement
{
	[Sample("Windows.UI.ViewManagement", "IsScreenCaptureEnabled", Description: "Allows disabling screen capture (currently available on Android only)")]
	public sealed partial class IsScreenCaptureEnabledTests : UserControl
	{
		public IsScreenCaptureEnabledTests()
		{
			this.InitializeComponent();
			if (ApiInformation.IsPropertyPresent("Windows.UI.ViewManagement.ApplicationView, Uno", "IsScreenCaptureEnabled"))
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
