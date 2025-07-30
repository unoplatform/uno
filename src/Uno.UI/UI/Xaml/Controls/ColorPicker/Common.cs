using Windows.UI;

namespace Microsoft.UI.Xaml.Controls
{
	public enum ColorSpectrumShape
	{
		Box = 0,
		Ring = 1,
	};

	public enum ColorSpectrumComponents
	{
		HueValue = 0,
		ValueHue = 1,
		HueSaturation = 2,
		SaturationHue = 3,
		SaturationValue = 4,
		ValueSaturation = 5,
	};

	public enum ColorPickerHsvChannel
	{
		Hue = 0,
		Saturation = 1,
		Value = 2,
		Alpha = 3,
	};

	public interface IColorChangedEventArgs
	{
		Color OldColor { get; }
		Color NewColor { get; }
	}
}
