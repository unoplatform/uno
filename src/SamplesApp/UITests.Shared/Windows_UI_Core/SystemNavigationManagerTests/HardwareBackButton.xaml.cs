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
		private bool _isSubscribed;

		public HardwareBackButton()
		{
			this.InitializeComponent();

			// Subscribe on Loaded, unsubscribe on Unloaded.
			// On Android 15+, subscription state determines whether the app handles back navigation.
			Loaded += (snd, e) => DoSubscribe();
			Unloaded += (snd, e) => DoUnsubscribe();
		}

		private void Subscribe(object sender, RoutedEventArgs e)
		{
			DoSubscribe();
		}

		private void Unsubscribe(object sender, RoutedEventArgs e)
		{
			DoUnsubscribe();
		}

		private void DoSubscribe()
		{
			if (!_isSubscribed)
			{
				SystemNavigationManager.GetForCurrentView().BackRequested += OnBackRequested;
				_isSubscribed = true;
				UpdateSubscriptionStatus();
				OutputTextBlock.Text += "Subscribed to BackRequested\r\n";
			}
		}

		private void DoUnsubscribe()
		{
			if (_isSubscribed)
			{
				SystemNavigationManager.GetForCurrentView().BackRequested -= OnBackRequested;
				_isSubscribed = false;
				UpdateSubscriptionStatus();
				OutputTextBlock.Text += "Unsubscribed from BackRequested\r\n";
			}
		}

		private void UpdateSubscriptionStatus()
		{
			SubscriptionStatusText.Text = _isSubscribed
				? "Status: Subscribed (back button consumed by app)"
				: "Status: Unsubscribed (back button handled by system)";
			SubscribeButton.IsEnabled = !_isSubscribed;
			UnsubscribeButton.IsEnabled = _isSubscribed;
		}

		private void Enable(object sender, TappedRoutedEventArgs e)
		{
			OutputTextBlock.Text += "AppViewBackButtonVisibility = Visible\r\n";
			SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Visible;
		}

		private void Disable(object sender, TappedRoutedEventArgs e)
		{
			OutputTextBlock.Text += "AppViewBackButtonVisibility = Collapsed\r\n";
			SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Collapsed;
		}

		private void OnBackRequested(object sender, BackRequestedEventArgs args)
		{
			OutputTextBlock.Text += "Back requested\r\n";

			// On Android 15+, the Handled property is ignored.
			// The subscription itself determines whether the app handles back navigation.
			// This checkbox only affects behavior on Android < 15 and other platforms.
			var handled = HandleCheckBox.IsChecked.GetValueOrDefault();
			HandleCheckBox.IsChecked = false;
			args.Handled = handled;
		}
	}
}
