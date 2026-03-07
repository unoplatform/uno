using Uno.UI.Samples.Controls;
using Microsoft.UI.Xaml.Controls;

namespace UITests.Shared.Windows_UI_Xaml_Controls.TextBoxTests
{
	/// <summary>
	/// Manual test sample for verifying backspace key functionality on Android soft keyboards.
	///
	/// Issue: https://github.com/unoplatform/uno/issues/22230
	///
	/// On Android browsers, soft keyboards return keyCode 229 (IME processing) for all keys,
	/// making it impossible to detect Backspace via keydown events. The fix removes
	/// preventDefault() from the keydown handler, allowing the browser to handle keyboard
	/// input natively and propagate text changes through the oninput event.
	/// </summary>
	[SampleControlInfo("TextBox", "TextBox_Backspace_SoftKeyboard", description: "Tests backspace on Android soft keyboards", ignoreInSnapshotTests: true)]
	public sealed partial class TextBox_Backspace_SoftKeyboard : UserControl
	{
		public TextBox_Backspace_SoftKeyboard()
		{
			InitializeComponent();
			UpdateCharCounts();
		}

		private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			SingleLineStatus.Text = $"Length: {SingleLineTextBox.Text.Length} chars";
			UpdateCharCounts();
		}

		private void SpacedTextBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			SpacedTextStatus.Text = $"Length: {SpacedTextBox.Text.Length} chars";
			UpdateCharCounts();
		}

		private void PasswordBox_PasswordChanged(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
		{
			PasswordStatus.Text = $"Length: {TestPasswordBox.Password.Length} chars";
			UpdateCharCounts();
		}

		private void MultilineTextBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			var lineCount = MultilineTextBox.Text.Split('\r').Length;
			MultilineStatus.Text = $"Length: {MultilineTextBox.Text.Length} chars, {lineCount} lines";
			UpdateCharCounts();
		}

		private void UpdateCharCounts()
		{
			CharCountDisplay.Text =
				$"SingleLine: {SingleLineTextBox.Text.Length}\n" +
				$"Spaced:     {SpacedTextBox.Text.Length}\n" +
				$"Password:   {TestPasswordBox.Password.Length}\n" +
				$"Multiline:  {MultilineTextBox.Text.Length}";
		}
	}
}
