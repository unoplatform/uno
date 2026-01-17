using Uno.UI.Samples.Controls;
using Microsoft.UI.Xaml.Controls;

namespace UITests.Shared.Windows_UI_Xaml_Controls.TextBoxTests
{
	[Sample("TextBox", "TextBox_BeforeTextChanging")]
	public sealed partial class TextBox_BeforeTextChanging : UserControl
	{
		public TextBox_BeforeTextChanging()
		{
			this.InitializeComponent();
		}

		private void BeforeTextBox_BeforeTextChanging(TextBox sender, TextBoxBeforeTextChangingEventArgs args)
		{
			if (args.NewText.Length > 0 && !args.NewText.EndsWith("e"))
			{
				args.Cancel = true;
			}
		}
	}
}
