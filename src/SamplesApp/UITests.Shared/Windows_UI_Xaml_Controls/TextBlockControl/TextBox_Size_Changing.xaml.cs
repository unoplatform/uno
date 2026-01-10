using Uno.UI.Samples.Controls;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.Samples.Presentation.SamplePages;

namespace Uno.UI.Samples.Content.UITests.TextBlockControl
{
	[Sample("TextBlock", "TextBox_Size_Changing", typeof(TextBlockViewModel), ignoreInSnapshotTests: true /*TextBlock Width is dynamically varying*/)]
	public sealed partial class TextBox_Size_Changing : UserControl
	{
		public TextBox_Size_Changing()
		{
			this.InitializeComponent();
		}
	}
}
