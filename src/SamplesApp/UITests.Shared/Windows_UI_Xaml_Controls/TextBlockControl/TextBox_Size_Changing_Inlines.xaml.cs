using Uno.UI.Samples.Controls;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.Samples.Presentation.SamplePages;

namespace Uno.UI.Samples.Content.UITests.TextBlockControl
{
	[Sample("TextBlock", "TextBox_Size_Changing_Inlines", typeof(TextBlockViewModel), ignoreInSnapshotTests: true /*TextBlock Text is dynamically varying*/)]
	public sealed partial class TextBox_Size_Changing_Inlines : UserControl
	{
		public TextBox_Size_Changing_Inlines()
		{
			this.InitializeComponent();
		}
	}
}
