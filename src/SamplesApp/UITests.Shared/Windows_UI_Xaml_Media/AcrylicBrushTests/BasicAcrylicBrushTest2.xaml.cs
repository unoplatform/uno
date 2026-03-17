using Uno.UI.Samples.Controls;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace UITests.Windows_UI_Xaml_Media.AcrylicBrushTests
{
	[Sample("Brushes", Description = "Demonstrates a basic Acrylic brush")]
	public sealed partial class BasicAcrylicBrushTest2 : Page
	{
		public BasicAcrylicBrushTest2()
		{
			this.InitializeComponent();
		}
		private void TintOpacitySlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
		{
			InteractiveAcrylic.TintOpacity = e.NewValue;
		}

		private void TintLuminosityOpacitySlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
		{
			InteractiveAcrylic.TintLuminosityOpacity = e.NewValue;
		}
	}
}
