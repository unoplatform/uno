using Microsoft.UI.Xaml;
using Uno.UI.Samples.Controls;
using Microsoft.UI.Xaml.Controls;

namespace UITests.Windows_UI_Xaml.Clipping
{
	[Sample]
	public sealed partial class CornerRadiusControls : Page
	{
		public CornerRadiusControls()
		{
			this.InitializeComponent();
		}

		private CornerRadius ToRadius(double value) => new CornerRadius(value);

		private double Negate(double value) => -value;
	}
}
