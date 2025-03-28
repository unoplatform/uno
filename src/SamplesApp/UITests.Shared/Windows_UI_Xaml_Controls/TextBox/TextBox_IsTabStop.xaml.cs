using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Uno.UI.Samples.Controls;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace UITests.Windows_UI_Xaml_Controls.TextBox
{
	[Sample(Description = "When IsTabStop=False, given element should not be focusable by pointer nor keyboard")]
	public sealed partial class TextBox_IsTabStop : Page
	{
		public TextBox_IsTabStop()
		{
			this.InitializeComponent();
			FocusManager.GettingFocus += FocusManager_GettingFocus;
			this.Unloaded += TextBox_IsTabStop_Unloaded;
		}

		private void FocusManager_GettingFocus(object sender, GettingFocusEventArgs args)
		{
			CurrentlyFocusedTextBlock.Text = (args.NewFocusedElement as FrameworkElement)?.Name ?? args.NewFocusedElement?.GetType()?.Name ?? "NONE";
		}

		private void TextBox_IsTabStop_Unloaded(object sender, RoutedEventArgs e)
		{
			FocusManager.GettingFocus -= FocusManager_GettingFocus;
		}
	}
}
