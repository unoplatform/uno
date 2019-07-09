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

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace UITests.Shared.Windows_UI_Xaml_Controls.TextBoxTests
{
	[SampleControlInfo("TextBox", "TextBox_TextChanging")]
	public sealed partial class TextBox_TextChanging : UserControl
	{
		public TextBox_TextChanging()
		{
			this.InitializeComponent();
		}

		private void CapitalizePreviousTextBox_TextChanging(TextBox sender, TextBoxTextChangingEventArgs args)
		{
			if (sender.Text.Length > 2)
			{
				sender.Text = sender.Text.Substring(0, sender.Text.Length - 2).ToUpperInvariant() + sender.Text.Substring(sender.Text.Length - 2);
				sender.SelectionStart = (sender as TextBox).Text.Length;
			}
		}

		private void LimitLengthTextBox_TextChanging(TextBox sender, TextBoxTextChangingEventArgs args)
		{
			const int maxLength = 15;
			if (sender.Text.Length > maxLength)
			{
				sender.Text = sender.Text.Substring(sender.Text.Length - maxLength);
				sender.SelectionStart = (sender as TextBox).Text.Length;
			}
		}
	}
}
