using Uno.UI.Samples.Controls;
using Uno.UI.Samples.Presentation.SamplePages;
using Microsoft.UI.Xaml.Controls;

namespace Uno.UI.Samples.Content.UITests.TextBlockControl
{
	[Sample("TextBlock",
		"TextBlock_Multiline_In_StarStackPanel",
		typeof(TextBlockViewModel),
		description: "A multiline textblock that contains data-bound runs that should wrap properly.",
		ignoreInSnapshotTests: true /*TextBlock Text is dynamically varying*/)]
	public sealed partial class TextBlock_Multiline_In_StarStackPanel : UserControl
	{
		public TextBlock_Multiline_In_StarStackPanel()
		{
			this.InitializeComponent();
		}
	}
}
