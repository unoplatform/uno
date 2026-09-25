using Uno.UI.Samples.Controls;
using Microsoft.UI.Xaml.Controls;

namespace Uno.UI.Samples.Content.UITests.TextBlockControl
{
	[Sample("TextBlock", Name = "TextBlock_ICU_Initialization", Description = "Validates ICU initialization and text rendering (BiDi, line breaking, scripts). See #22772.")]
	public sealed partial class TextBlock_ICU_Initialization : UserControl
	{
		public TextBlock_ICU_Initialization()
		{
			this.InitializeComponent();
		}
	}
}
