using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Uno.UI.Samples.Controls;
using Windows.UI;
using System.Reflection;
using Windows.UI.ViewManagement;
using Microsoft.UI.Xaml.Markup;
using System.Threading.Tasks;

namespace UITests.Shared.Windows_UI_ViewManagement
{
	[Sample("Windows.UI.ViewManagement", "StatusBar_Color", IsManualTest: true, Description: "Status bar can be styled at runtime for mobile targets")]
	public sealed partial class StatusBarColorTests : UserControl
	{
		public StatusBarColorTests()
		{
			this.InitializeComponent();
		}

		private void SetBackground(Color? color)
		{
#if __ANDROID__ || __IOS__
			StatusBar.GetForCurrentView().BackgroundColor = color;
#else
			Console.WriteLine($"SetBackground: {color}; Not supported on the platform");
#endif
		}

		private void SetForeground(Color? color)
		{
#if __ANDROID__ || __IOS__
			StatusBar.GetForCurrentView().ForegroundColor = color;

#else
			Console.WriteLine($"SetForeground: {color}; Not supported on the platform");
#endif
		}

		private void SetBackgroundColor_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				var colorString = StringToColor(ColorTextBox.Text);
				SetBackground(colorString);
			}
			catch (Exception ex)
			{
				ErrorMessage.Text = ex.Message;
			}
		}

		private void SetForegroundColor_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				var colorString = StringToColor(ColorTextBox.Text);
				SetForeground(colorString);
			}
			catch (Exception ex)
			{
				ErrorMessage.Text = ex.Message;
			}
		}

		private void SetForegroundWhite_Click(object sender, RoutedEventArgs e)
		{
			SetForeground(Colors.White);
		}

		private void SetForegroundBlack_Click(object sender, RoutedEventArgs e)
		{
			SetForeground(Colors.Black);
		}

		private void ResetBackgroundColor_Click(object sender, RoutedEventArgs e)
		{
			SetBackground(null);
		}

		private void ResetForegroundColor_Click(object sender, RoutedEventArgs e)
		{
			SetForeground(null);
		}

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
		private async void ShowStatusBar_Click(object sender, RoutedEventArgs e)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
		{
#if __ANDROID__ || __IOS__
			await StatusBar.GetForCurrentView().ShowAsync();
#else
			Console.WriteLine("ShowStatusBar: Not supported on this platform");
#endif
		}

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
		private async void HideStatusBar_Click(object sender, RoutedEventArgs e)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
		{
#if __ANDROID__ || __IOS__
			await StatusBar.GetForCurrentView().HideAsync();
#else
			Console.WriteLine("HideStatusBar: Not supported on this platform");
#endif
		}

		private static Color StringToColor(string hexColorString)
		{
			return string.IsNullOrWhiteSpace(hexColorString)
				? Colors.Transparent
				: (Color)XamlBindingHelper.ConvertValue(typeof(Color), hexColorString);
		}
	}
}
