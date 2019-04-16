using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Uno.UI.Samples.Controls;

namespace UITests.Shared.Windows_UI_Core.SystemNavigationManagerTests
{
	[SampleControlInfo("SystemNavigationManager", "HardwareBackButton")]
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
			_output.Text += "Enable\r\n";
			Windows.UI.Core.SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Visible;
		}

		private void Disable(object sender, TappedRoutedEventArgs e)
		{
			_output.Text += "Collapse\r\n";
			Windows.UI.Core.SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Collapsed;
		}

		private void OnBackRequested(object sender, BackRequestedEventArgs args)
		{
			_output.Text += "Back requested\r\n";
			args.Handled = _handle.IsChecked.GetValueOrDefault();
		}
	}
}
