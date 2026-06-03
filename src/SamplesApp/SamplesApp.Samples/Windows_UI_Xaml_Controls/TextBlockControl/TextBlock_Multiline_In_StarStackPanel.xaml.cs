using Uno.UI.Samples.Controls;
using Uno.UI.Samples.Presentation.SamplePages;
using Microsoft.UI.Xaml.Controls;

namespace Uno.UI.Samples.Content.UITests.TextBlockControl
{
	[Sample("TextBlock", Name = "TextBlock_Multiline_In_StarStackPanel", ViewModelType = typeof(TextBlockViewModel),
		Description = "A multiline textblock that contains data-bound runs that should wrap properly.",
		IgnoreInSnapshotTests = true /*TextBlock Text is dynamically varying*/)]
	public sealed partial class TextBlock_Multiline_In_StarStackPanel : UserControl
	{
		public TextBlock_Multiline_In_StarStackPanel()
		{
			this.InitializeComponent();
		}
	}
}
