using Uno.UI.Samples.Controls;
using Microsoft.UI.Xaml.Controls;

namespace Uno.UI.Samples.Content.UITests.Slider
{
	[Sample("Slider", Name = "Slider_Inside_ListViewHeader")]
	public sealed partial class Slider_Inside_ListViewHeader : UserControl
	{
#pragma warning disable CS0414
		private double OverallPerformance = 0.0; // Used in XAML

		public Slider_Inside_ListViewHeader()
		{
			this.OverallPerformance = 5.0;

			this.InitializeComponent();
		}
	}
}
