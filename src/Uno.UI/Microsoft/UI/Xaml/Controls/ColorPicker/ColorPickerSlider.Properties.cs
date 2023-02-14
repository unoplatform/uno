using Microsoft.UI.Xaml;

namespace Microsoft.UI.Xaml.Controls.Primitives
{
	public partial class ColorPickerSlider
	{
		public ColorPickerHsvChannel ColorChannel
		{
			get => (ColorPickerHsvChannel)GetValue(ColorChannelProperty);
			set => SetValue(ColorChannelProperty, value);
		}

		public static DependencyProperty ColorChannelProperty { get; } =
			DependencyProperty.Register(
				nameof(ColorChannel),
				typeof(ColorPickerHsvChannel),
				typeof(ColorPickerSlider),
				new FrameworkPropertyMetadata(ColorPickerHsvChannel.Value));
	}
}
