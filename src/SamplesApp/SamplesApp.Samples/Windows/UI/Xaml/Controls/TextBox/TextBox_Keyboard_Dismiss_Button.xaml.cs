using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;
#if HAS_UNO
using Uno.UI.Xaml.Controls;
#endif

namespace UITests.Shared.Windows_UI_Xaml_Controls.TextBoxTests;

[Sample("TextBox", Name = "TextBox_Keyboard_Dismiss_Button", IsManualTest = true, IgnoreInSnapshotTests = true,
	Description =
		"iOS only. TextBoxExtensions.ShowKeyboardDismissButton adds a bar with a \"Done\" button above the soft keyboard " +
		"that dismisses it. Tapping into the multiline box or into the inputs of the opted-in panel should show the bar; " +
		"tapping Done should hide the keyboard and unfocus the input. The box that opts out locally, and the one outside the " +
		"panel, should show no bar. Other platforms ignore the property, so no bar appears anywhere.")]
public sealed partial class TextBox_Keyboard_Dismiss_Button : UserControl
{
	public TextBox_Keyboard_Dismiss_Button()
	{
		InitializeComponent();

		GotFocus += OnGotFocus;
	}

	private void OnGotFocus(object sender, RoutedEventArgs e)
	{
		if (e.OriginalSource is Control control)
		{
#if HAS_UNO
			StatusText.Text = $"Focus: {control.GetType().Name}, " +
				$"ShowKeyboardDismissButton={TextBoxExtensions.GetShowKeyboardDismissButton(control)}";
#else
			StatusText.Text = $"Focus: {control.GetType().Name}";
#endif
		}
	}
}
