using Uno.UI.Samples.Controls;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.Samples.Presentation.SamplePages;

namespace Uno.UI.Samples.Content.UITests.TextBlockControl
{
	[Sample("TextBlock", ViewModelType = typeof(TextBlockViewModel), IgnoreInSnapshotTests = true /*Databound to current date and time*/)]
	public sealed partial class Attributed_text_Simple_databound : UserControl
	{
		public Attributed_text_Simple_databound()
		{
			this.InitializeComponent();
		}
	}
}
