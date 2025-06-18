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

namespace UITests.Shared.Windows_UI_ViewManagement
{
	[SampleControlInfo("Windows.UI.ViewManagement", "StatusBar_Color", isManualTest: true, description: "Status bar can be styled at runtime for mobile targets")]
	public sealed partial class StatusBarColorTests : UserControl
	{
#if __ANDROID__ || __IOS__
		private StatusBar _statusBar;
#endif

		public StatusBarColorTests()
		{
#if __ANDROID__ || __IOS__
			_statusBar = StatusBar.GetForCurrentView();
#endif

			this.InitializeComponent();
		}

		private void SetBackgroundColor_Click(object sender, RoutedEventArgs e)
		{
#if __ANDROID__ || __IOS__
			try
			{
				var colorString = StringToColor(ColorTextBox.Text);
				_statusBar.BackgroundColor = colorString;
			}
			catch (Exception ex)
			{
				ErrorMessage.Text = ex.Message;
			}
#endif
		}

		private void SetForegroundColor_Click(object sender, RoutedEventArgs e)
		{
#if __ANDROID__ || __IOS__
			try
			{
				var colorString = StringToColor(ColorTextBox.Text);
				_statusBar.ForegroundColor = colorString;
			}
			catch (Exception ex)
			{
				ErrorMessage.Text = ex.Message;
			}
#endif
		}

		private void SetForegroundWhite_Click(object sender, RoutedEventArgs e)
		{
#if __ANDROID__ || __IOS__
			_statusBar.ForegroundColor = Colors.White;
#endif
		}

		private void SetForegroundBlack_Click(object sender, RoutedEventArgs e)
		{
#if __ANDROID__ || __IOS__
			_statusBar.ForegroundColor = Colors.Black;
#endif		
		}

		private void ResetBackgroundColor_Click(object sender, RoutedEventArgs e)
		{
#if __ANDROID__ || __IOS__
			_statusBar.BackgroundColor = null;
#endif
		}

		private void ResetForegroundColor_Click(object sender, RoutedEventArgs e)
		{
#if __ANDROID__ || __IOS__
			_statusBar.ForegroundColor = null;
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
