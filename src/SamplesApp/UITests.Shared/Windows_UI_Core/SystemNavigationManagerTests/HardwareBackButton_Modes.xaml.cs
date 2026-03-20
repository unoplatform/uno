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
	[Sample("Windows.UI.Core", Name = "HardwareBackButton_Modes")]
	public sealed partial class HardwareBackButton_Modes : Page
	{
		private bool _isSubscribed;
		private bool _isAndroid16OrHigher;

		public HardwareBackButton_Modes()
		{
			this.InitializeComponent();

			Loaded += OnLoaded;
			Unloaded += OnUnloaded;
		}

		private void OnLoaded(object sender, RoutedEventArgs e)
		{
			DetectPlatformAndVersion();
			DoSubscribe();
		}

		private void OnUnloaded(object sender, RoutedEventArgs e)
		{
			DoUnsubscribe();
		}

		private void DetectPlatformAndVersion()
		{
			if (OperatingSystem.IsAndroid())
			{
				_isAndroid16OrHigher = OperatingSystem.IsAndroidVersionAtLeast(36);

				PlatformInfoText.Text = $"Android {(_isAndroid16OrHigher ? "16 and newer" : "15 and earlier")}";

				if (_isAndroid16OrHigher)
				{
					BehaviorDescriptionText.Text = "Using subscription-based back handling. The Handled property is IGNORED.";
					// Highlight Android 16+ section, dim pre-Android 16 section
					Android16Section.Opacity = 1.0;
					PreAndroid16Section.Opacity = 0.5;
				}
				else
				{
					BehaviorDescriptionText.Text = "Using Handled property approach. Subscription state does not affect system behavior.";
					// Highlight pre-Android 16 section, dim Android 16+ section
					Android16Section.Opacity = 0.5;
					PreAndroid16Section.Opacity = 1.0;
				}
			}
			else
			{
				_isAndroid16OrHigher = false;
				PlatformInfoText.Text = "Non-Android Platform";
				BehaviorDescriptionText.Text = "Using Handled property approach (standard WinUI behavior).";
				Android16Section.Opacity = 0.5;
				PreAndroid16Section.Opacity = 1.0;
			}
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
				Log("Subscribed to BackRequested");

				if (_isAndroid16OrHigher)
				{
					Log("  -> Back button will be consumed by app (Android 16+ behavior)");
				}
			}
		}

		private void DoUnsubscribe()
		{
			if (_isSubscribed)
			{
				SystemNavigationManager.GetForCurrentView().BackRequested -= OnBackRequested;
				_isSubscribed = false;
				UpdateSubscriptionStatus();
				Log("Unsubscribed from BackRequested");

				if (_isAndroid16OrHigher)
				{
					Log("  -> Back button will be handled by system (Android 16+ behavior)");
				}
			}
		}

		private void UpdateSubscriptionStatus()
		{
			if (_isAndroid16OrHigher)
			{
				SubscriptionStatusText.Text = _isSubscribed
					? "Status: Subscribed (back consumed by app)"
					: "Status: Unsubscribed (back handled by system - will exit!)";
			}
			else
			{
				SubscriptionStatusText.Text = _isSubscribed
					? "Status: Subscribed"
					: "Status: Unsubscribed";
			}
			SubscribeButton.IsEnabled = !_isSubscribed;
			UnsubscribeButton.IsEnabled = _isSubscribed;
		}

		private void Enable(object sender, TappedRoutedEventArgs e)
		{
			Log("AppViewBackButtonVisibility = Visible");
			SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Visible;
		}

		private void Disable(object sender, TappedRoutedEventArgs e)
		{
			Log("AppViewBackButtonVisibility = Collapsed");
			SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Collapsed;
		}

		private void OnBackRequested(object sender, BackRequestedEventArgs args)
		{
			Log("BackRequested event fired");

			if (_isAndroid16OrHigher)
			{
				// On Android 16+, the Handled property is ignored.
				// The subscription itself determines whether the app handles back navigation.
				// Back is always consumed when subscribed.
				Log("  -> Android 16+: Back consumed (Handled property ignored)");
				args.Handled = true; // Set for consistency, but ignored on Android 16+
			}
			else
			{
				// On Android < 16 and other platforms, the Handled property controls behavior.
				var handled = HandleCheckBox.IsChecked.GetValueOrDefault();
				HandleCheckBox.IsChecked = false;
				args.Handled = handled;

				Log($"  -> Handled = {handled}");
				if (handled)
				{
					Log("  -> Back consumed by app");
				}
				else
				{
					Log("  -> Back passed to system (may exit app)");
				}
			}
		}

		private void ClearOutput(object sender, RoutedEventArgs e)
		{
			OutputTextBlock.Text = string.Empty;
		}

		private void Log(string message)
		{
			OutputTextBlock.Text += $"{DateTime.Now:HH:mm:ss} {message}\r\n";
		}
	}
}
