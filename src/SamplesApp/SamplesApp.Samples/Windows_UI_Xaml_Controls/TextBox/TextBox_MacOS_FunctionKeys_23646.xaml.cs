using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Uno.UI.Samples.Controls;

namespace UITests.Windows_UI_Xaml_Controls.TextBox;

[Sample(
	"TextBox",
	Name = "TextBox_MacOS_FunctionKeys_23646",
	Description = "Manual reproduction for GitHub issue #23646. Function keys must not insert control characters into a TextBox.",
	IsManualTest = true,
	IgnoreInSnapshotTests = true)]
public sealed partial class TextBox_MacOS_FunctionKeys_23646 : Page
{
	public TextBox_MacOS_FunctionKeys_23646()
	{
		this.InitializeComponent();
	}

	private void OnInputKeyDown(object sender, KeyRoutedEventArgs e) =>
		LastKey.Text = $"Last key: {e.Key}";

	private void OnInputTextChanged(object sender, TextChangedEventArgs e)
	{
		CodePoints.Text = Input.Text.Length == 0
			? "Code points: none"
			: $"Code points: {string.Join(", ", Input.Text.Select(character => $"U+{(int)character:X4}"))}";
	}

	private void OnClearClicked(object sender, RoutedEventArgs e)
	{
		Input.Text = string.Empty;
		LastKey.Text = "Last key: none";
		Input.Focus(FocusState.Programmatic);
	}
}
