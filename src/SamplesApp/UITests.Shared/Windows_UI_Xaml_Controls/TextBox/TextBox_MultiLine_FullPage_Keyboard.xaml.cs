using Uno.UI.Samples.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Uno.UI.Samples.Content.UITests.TextBoxControl
{
	[Sample("TextBox", Name = "TextBox_MultiLine_FullPage_Keyboard", IgnoreInSnapshotTests = true,
		Description = "Multiline TextBox filling the entire page - keyboard should push content up so cursor stays visible")]
	public sealed partial class TextBox_MultiLine_FullPage_Keyboard : UserControl
	{
		public TextBox_MultiLine_FullPage_Keyboard()
		{
			InitializeComponent();

			// Pre-populate with enough repeated text to simulate the reported scenario
			// where the content exceeds the visible area when the keyboard is open.
			var lines = new System.Text.StringBuilder();
			for (int i = 0; i < 20; i++)
			{
				lines.AppendLine("Sample placeholder text line A for multiline TextBox.");
				lines.AppendLine();
				lines.AppendLine("Sample placeholder text line B for multiline TextBox.");
				lines.AppendLine();
				lines.AppendLine("Sample placeholder text line C for multiline TextBox.");
				lines.AppendLine();
			}

			FullPageTextBox.Text = lines.ToString();

			FullPageTextBox.Loaded += FullPageTextBox_Loaded;
		}

		private void FullPageTextBox_Loaded(object sender, RoutedEventArgs e)
		{
			// Place cursor at the end so the user is typing at the bottom,
			// which is the area most likely to be covered by the keyboard.
			FullPageTextBox.SelectionStart = FullPageTextBox.Text.Length;
			FullPageTextBox.Focus(FocusState.Programmatic);
		}
	}
}
