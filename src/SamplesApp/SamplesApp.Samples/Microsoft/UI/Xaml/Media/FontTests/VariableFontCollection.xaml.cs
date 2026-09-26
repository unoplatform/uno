using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Uno.UI.Samples.Controls;
using Windows.UI.Text;

namespace UITests.Windows_UI_Xaml_Media.FontTests
{
	[Sample("Fonts", Description = "Loads two families (Inter + Lora) from a single .ttc via the '#family-name' hint, and sweeps the wght axis of a collection face.")]
	public sealed partial class VariableFontCollection : Page
	{
		public VariableFontCollection()
		{
			this.InitializeComponent();
			ApplyWeight((ushort)WeightSlider.Value);
		}

		private void OnWeightSliderValueChanged(object sender, RangeBaseValueChangedEventArgs e)
		{
			// Can fire during InitializeComponent before the named elements exist.
			if (LiveText is null)
			{
				return;
			}

			ApplyWeight((ushort)e.NewValue);
		}

		private void ApplyWeight(ushort weight)
		{
			LiveText.FontWeight = new FontWeight(weight);
			WeightLabel.Text = $"FontWeight = {weight}";
		}
	}
}
