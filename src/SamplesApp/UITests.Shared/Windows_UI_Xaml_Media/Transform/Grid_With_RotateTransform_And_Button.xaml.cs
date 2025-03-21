using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;

namespace SamplesApp.Wasm.Windows_UI_Xaml_Media.Transform
{
	[SampleControlInfo("Transform", "Grid_With_RotateTransform_And_Button", description: "Rotated Grid with Button inside. Button should be clickable.")]
	public sealed partial class Grid_With_RotateTransform_And_Button : UserControl
	{
		public Grid_With_RotateTransform_And_Button()
		{
			this.InitializeComponent();
		}

		private int counter;
		private void IncrementCounter(object sender, RoutedEventArgs e)
		{
			counter++;
			ClickCountTextBlock.Text = $"Button clicked {counter} times.";
		}
	}
}
