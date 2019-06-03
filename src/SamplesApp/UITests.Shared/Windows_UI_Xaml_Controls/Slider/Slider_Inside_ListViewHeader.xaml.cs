using Uno.UI.Samples.Controls;
using Windows.UI.Xaml.Controls;

namespace Uno.UI.Samples.Content.UITests.Slider
{
	[SampleControlInfoAttribute("Slider", "Slider_Inside_ListViewHeader")]
	public sealed partial class Slider_Inside_ListViewHeader : UserControl
    {
		double OverallPerformance = 0.0;

        public Slider_Inside_ListViewHeader()
        {
			this.OverallPerformance = 5.0;

            this.InitializeComponent();
        }
    }
}
