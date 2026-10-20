namespace Microsoft.UI.Xaml.Media;

public partial class RevealBrush : XamlCompositionBrushBase
{
	public RevealBrush()
	{

	}

	[global::Uno.NotImplemented("__SKIA__")]
	public global::Windows.UI.Color Color
	{
		get
		{
			return (global::Windows.UI.Color)this.GetValue(ColorProperty);
		}
		set
		{
			this.SetValue(ColorProperty, value);
		}
	}

	[global::Uno.NotImplemented("__SKIA__")]
	public static global::Microsoft.UI.Xaml.DependencyProperty ColorProperty { get; } =
	Microsoft.UI.Xaml.DependencyProperty.Register(
		nameof(Color), typeof(global::Windows.UI.Color),
		typeof(global::Microsoft.UI.Xaml.Media.RevealBrush),
		new FrameworkPropertyMetadata(default(global::Windows.UI.Color)));
}
