using Uno.UI.Samples.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Uno.UI.Samples.Content.UITests.TextBoxControl
{
	[Sample("TextBox", "TextBox_Multiple_Focus_Keyboard", IgnoreInSnapshotTests: true /*Cursor blinks in TextBox*/)]
	public sealed partial class TextBox_Multiple_Focus_Keyboard : UserControl
	{
		public TextBox_Multiple_Focus_Keyboard()
		{
			InitializeComponent();
		}
	}
}
