using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Uno.UI.Samples.Controls;

namespace UITests.Shared.Windows_UI_Xaml_Controls.TextBoxTests;

[Sample("TextBox", Name = "TextBox_Keyboard_Navigation", IsManualTest = true, IgnoreInSnapshotTests = true,
	Description =
		"When XYFocusKeyboardNavigation is enabled for the surrounding grid and arrow keys are pressed, a TextBox should move to neighbouring TextBoxes as expected i.e. a left arrow input when the caret is at the beginning should navigate to the TextBox of the current TextBox\'s left, and similarly for a right arrow input when the caret is at the end of the TextBox.")]
public sealed partial class TextBox_Keyboard_Navigation : UserControl
{
	public TextBox_Keyboard_Navigation()
	{
		InitializeComponent();
	}

	public void Grid_KeyDown(object sender, KeyRoutedEventArgs keyRoutedEventArgs)
	{
		if (keyRoutedEventArgs.Key == VirtualKey.Left ||
			keyRoutedEventArgs.Key == VirtualKey.Up ||
			keyRoutedEventArgs.Key == VirtualKey.Right ||
			keyRoutedEventArgs.Key == VirtualKey.Down)
		{
			myContent.Text += $"{keyRoutedEventArgs.Key} bubbled up to outer grid\n";
		}
	}

	public void Button_OnClick(object sender, RoutedEventArgs e) => myContent.Text = "Event info:\n";
}
