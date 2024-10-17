using Uno.UI.Samples.Controls;
using Windows.UI.Xaml.Controls;
using Uno.UI.Samples.Presentation.SamplePages;

namespace Uno.UI.Samples.Content.UITests.TextBoxControl
{
	[Sample(
		"TextBox",
		IsManualTest = true,
		IgnoreInSnapshotTests = true,
		Description =
			"Press left mouse button somewhere in the text and drag to the right." +
			"The input's text should scroll along with the selection to reveal more content. " +
			"Conversely dragging mouse to the left should scroll back to the start of the text.")]
	public sealed partial class Input_TextScroll : UserControl
	{
		public Input_TextScroll()
		{
			this.InitializeComponent();
		}
	}
}
