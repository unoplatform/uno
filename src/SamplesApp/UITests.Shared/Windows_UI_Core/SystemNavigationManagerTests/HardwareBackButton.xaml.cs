using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Uno.UI.Samples.Controls;

namespace UITests.Windows_UI_Core.SystemNavigationManagerTests
{
	[SampleControlInfo("Windows.UI.Core", "HardwareBackButton")]
	public sealed partial class HardwareBackButton : Page
	{
		public HardwareBackButton()
		{
			this.InitializeComponent();

			Loaded += (snd, e) => Windows.UI.Core.SystemNavigationManager.GetForCurrentView().BackRequested += OnBackRequested;
			Unloaded += (snd, e) => Windows.UI.Core.SystemNavigationManager.GetForCurrentView().BackRequested -= OnBackRequested;
		}

		private void Enable(object sender, TappedRoutedEventArgs e)
		{
			OutputTextBlock.Text += "Enable\r\n";
			Windows.UI.Core.SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Visible;
		}

		private void Disable(object sender, TappedRoutedEventArgs e)
		{
			OutputTextBlock.Text += "Collapse\r\n";
			Windows.UI.Core.SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Collapsed;
		}

		private void OnBackRequested(object sender, BackRequestedEventArgs args)
		{
			OutputTextBlock.Text += "Back requested\r\n";
			var handled = HandleCheckBox.IsChecked.GetValueOrDefault();
			HandleCheckBox.IsChecked = false;
			args.Handled = handled;
		}
	}
}
