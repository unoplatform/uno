using Uno.UI.Samples.Controls;
using Microsoft.UI.Xaml.Controls;
using UITests.Shared.Windows_UI_Xaml_Controls.Slider;

namespace Uno.UI.Samples.Content.UITests.Slider
{
	[Sample("Slider", "Slider_Frequency", typeof(SliderViewModel), description: "Slider which enforces the frequency of its steps")]
	public sealed partial class Slider_Frequency : UserControl
	{
		public Slider_Frequency()
		{
			this.InitializeComponent();
		}
	}
}
