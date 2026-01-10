using Uno.UI.Samples.Controls;
using Microsoft.UI.Xaml.Controls;

namespace Uno.UI.Samples.Content.UITests.TextBlockControl
{
	[Sample("TextBlock", "TextBlock_Nested_Measure_With_Outer_Alignments", description: "Demonstrates that TextBlocks inside a StackPanel should return their own width, not the suggested max width. Both lines should be aligned to the center of the screen.")]
	public sealed partial class TextBlock_Nested_Measure_With_Outer_Alignments : UserControl
	{
		public TextBlock_Nested_Measure_With_Outer_Alignments()
		{
			this.InitializeComponent();
		}
	}
}
