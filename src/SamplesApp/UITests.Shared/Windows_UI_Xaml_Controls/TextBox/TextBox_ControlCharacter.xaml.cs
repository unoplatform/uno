using Uno.UI.Samples.Controls;
using Microsoft.UI.Xaml.Controls;

namespace Uno.UI.Samples.Content.UITests.TextBoxControl;

[Sample(
	"TextBox",
	Name = "TextBox_ControlCharacter",
	IsManualTest = true,
	IgnoreInSnapshotTests = true,
	Description = "Validates that Ctrl+letter shortcuts do not insert control characters (squares/rectangles) into TextBox. " +
		"Focus the TextBox and press Ctrl+F, Ctrl+G, etc. - no characters should appear.")]
public sealed partial class TextBox_ControlCharacter : UserControl
{
	public TextBox_ControlCharacter()
	{
		InitializeComponent();
	}
}
