using Uno.UI.Samples.Controls;
using Windows.UI.Xaml.Controls;

namespace Uno.UI.Samples.Content.UITests.Slider
{
	[SampleControlInfoAttribute("Slider", "Slider_EncoreStyle")]
	public sealed partial class Slider_EncoreStyle : UserControl
    {
		double OverallPerformance = 0.0;

        public Slider_EncoreStyle()
        {
			this.OverallPerformance = 5.0;

            this.InitializeComponent();
        }
    }
}
