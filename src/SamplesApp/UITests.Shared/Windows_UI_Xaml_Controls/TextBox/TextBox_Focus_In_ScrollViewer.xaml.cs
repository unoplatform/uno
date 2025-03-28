using Uno.UI.Samples.Controls;
using Windows.UI.Xaml.Controls;

namespace Uno.UI.Samples.Content.UITests.TextBoxControl
{
	[Sample(
		"TextBox",
		Description = "When using touch to scroll, the TextBoxes should not get focus when pressed/released.",
		IsManualTest = true)]
	public sealed partial class TextBox_Focus_In_ScrollViewer : Page
	{
		public TextBox_Focus_In_ScrollViewer()
		{
			this.InitializeComponent();
		}
	}
}
