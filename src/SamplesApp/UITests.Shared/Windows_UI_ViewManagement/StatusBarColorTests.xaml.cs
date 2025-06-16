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
		private StatusBar _statusBar;

		public StatusBarColorTests()
		{
			_statusBar = StatusBar.GetForCurrentView();

			this.InitializeComponent();
		}

		private void SetBackgroundColor_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				var colorString = StringToColor(ColorTextBox.Text);
				_statusBar.BackgroundColor = colorString;
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
				_statusBar.ForegroundColor = colorString;
			}
			catch (Exception ex)
			{
				ErrorMessage.Text = ex.Message;
			}
		}

		private void SetForegroundWhite_Click(object sender, RoutedEventArgs e)
		{
			_statusBar.ForegroundColor = Colors.White;
		}

		private void SetForegroundBlack_Click(object sender, RoutedEventArgs e)
		{
			_statusBar.ForegroundColor = Colors.Black;
		}

		private void ResetBackgroundColor_Click(object sender, RoutedEventArgs e)
		{
			_statusBar.BackgroundColor = null;
		}

		private void ResetForegroundColor_Click(object sender, RoutedEventArgs e)
		{
			_statusBar.ForegroundColor = null;
		}

		private static Color StringToColor(string hexColorString)
		{
			return string.IsNullOrWhiteSpace(hexColorString)
				? Colors.Transparent
				: (Color)XamlBindingHelper.ConvertValue(typeof(Color), hexColorString);
		}
	}
}
