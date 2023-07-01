using Windows.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;

namespace UITests.Shared.Windows_UI_Xaml_Controls.TextBoxTests
{
	[Sample("TextBox", Name = "TextBox_Keyboard_Navigation",
		Description = "When XYFocusKeyboardNavigation is enabled for the surrounding grid and arrow keys are pressed, a TextBox should move to neighbouring TextBoxes as expected i.e. a left arrow input when the caret is at the beginning should navigate to the TextBox of the current TextBox\'s left, and similarly for a right arrow input when the caret is at the end of the TextBox.")]
	public sealed partial class TextBox_Keyboard_Navigation : UserControl
	{
		public TextBox_Keyboard_Navigation()
		{
			this.InitializeComponent();
		}
	}
}
