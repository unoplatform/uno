using Uno.UI.Samples.Controls;
using Windows.UI.Xaml.Controls;

namespace Uno.UI.Samples.Content.UITests.TextBlockControl
{
	[SampleControlInfo("TextBlockControl", "SimpleText_MaxLines_Two_With_Wrap", description: "This sample shows a very long line that should wrap on a maximum of two lines.")]
	public sealed partial class SimpleText_MaxLines_Two_With_Wrap : UserControl
	{
		public SimpleText_MaxLines_Two_With_Wrap()
		{
			this.InitializeComponent();
		}
	}
}
