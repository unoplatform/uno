using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Uno.UI.Samples.Controls;

namespace UITests.Shared.Windows_UI_Xaml.Font;

[SampleControlInfo("Fonts", "FontAlignmentTests", isManualTest: true, description: "Tests the alignment of text in different UI Controls")]
public sealed partial class FontAlignmentTests : Page
{
	public FontAlignmentTests()
	{
		this.InitializeComponent();
	}

	private void ToggleButton_OnChecked(object sender, RoutedEventArgs e)
	{
		var check = (CheckBox)sender;
		MyRadioButtons.IsEnabled = check.IsChecked.Value;
	}

	private void ToggleButton_OnUnChecked(object sender, RoutedEventArgs e)
	{
		var check = (CheckBox)sender;
		MyRadioButtons.IsEnabled = check.IsChecked.Value;
	}

	private void ShowTip(object sender, RoutedEventArgs e)
	{
		ToggleThemeTeachingTip3.IsOpen = true;
	}
}

