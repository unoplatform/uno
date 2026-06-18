using Uno.UI.Samples.Controls;
using Microsoft.UI.Xaml.Controls;

namespace UITests.Windows_UI_ViewManagement;

[Sample("Windows.UI.ViewManagement",
	Name = "InputPane_SoftwareKeyboard",
	Description = "TextBox/PasswordBox in a ScrollViewer for touch-keyboard validation (issue #17363).",
	IsManualTest = true)]
public sealed partial class SoftwareKeyboardSample : Page
{
	public SoftwareKeyboardSample() => this.InitializeComponent();
}
