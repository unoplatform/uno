namespace Windows.UI.Xaml.Media;

public partial class RevealBrush : XamlCompositionBrushBase
{
	public RevealBrush()
	{

	}

	[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "IS_UNIT_TESTS", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
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

	[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "IS_UNIT_TESTS", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
	public static global::Windows.UI.Xaml.DependencyProperty ColorProperty { get; } =
	Windows.UI.Xaml.DependencyProperty.Register(
		nameof(Color), typeof(global::Windows.UI.Color),
		typeof(global::Windows.UI.Xaml.Media.RevealBrush),
		new FrameworkPropertyMetadata(default(global::Windows.UI.Color)));
}
