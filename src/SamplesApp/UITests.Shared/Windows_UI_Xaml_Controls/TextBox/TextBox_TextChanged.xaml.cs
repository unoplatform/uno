using Uno.UI.Samples.Controls;
using Microsoft.UI.Xaml.Controls;

namespace UITests.Shared.Windows_UI_Xaml_Controls.TextBoxTests
{
	[Sample("TextBox", "TextBox_TextChanged", description: "Demonstrates use of the TextChanged event to modify Text")]
	public sealed partial class TextBox_TextChanged : UserControl
	{
		public TextBox_TextChanged()
		{
			this.InitializeComponent();
		}

		private void AppendTextBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			var tb = sender as TextBox;
			if (tb.Text.Length == 4)
			{
				tb.Text = tb.Text + "Street";
				tb.SelectionStart = tb.Text.Length;
			}
		}

		private void CapitalizeTextBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			var tb = sender as TextBox;

			tb.Text = tb.Text.ToUpperInvariant();
			tb.SelectionStart = tb.Text.Length;
		}
	}
}
