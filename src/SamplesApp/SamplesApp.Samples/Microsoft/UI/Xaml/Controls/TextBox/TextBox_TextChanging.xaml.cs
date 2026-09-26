using Uno.UI.Samples.Controls;
using Microsoft.UI.Xaml.Controls;

namespace UITests.Shared.Windows_UI_Xaml_Controls.TextBoxTests
{
	[Sample("TextBox", Name = "TextBox_TextChanging")]
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
