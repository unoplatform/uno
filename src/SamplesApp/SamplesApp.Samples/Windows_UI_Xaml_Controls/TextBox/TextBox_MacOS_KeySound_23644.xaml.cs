using Microsoft.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;

namespace UITests.Windows_UI_Xaml_Controls.TextBox;

[Sample(
	"TextBox",
	Name = "TextBox_MacOS_KeySound_23644",
	Description = "Manual regression verification for GitHub issue #23644. Text editing commands must not play the macOS error sound.",
	IsManualTest = true,
	IgnoreInSnapshotTests = true)]
public sealed partial class TextBox_MacOS_KeySound_23644 : Page
{
	public TextBox_MacOS_KeySound_23644()
	{
		this.InitializeComponent();
	}
}
