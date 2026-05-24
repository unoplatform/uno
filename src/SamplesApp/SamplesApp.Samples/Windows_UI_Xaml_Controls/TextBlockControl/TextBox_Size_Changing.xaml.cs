using Uno.UI.Samples.Controls;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.Samples.Presentation.SamplePages;

namespace Uno.UI.Samples.Content.UITests.TextBlockControl
{
	[Sample("TextBlock", Name = "TextBox_Size_Changing", ViewModelType = typeof(TextBlockViewModel), IgnoreInSnapshotTests = true /*TextBlock Width is dynamically varying*/)]
	public sealed partial class TextBox_Size_Changing : UserControl
	{
		public TextBox_Size_Changing()
		{
			this.InitializeComponent();
		}
	}
}
