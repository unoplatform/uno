namespace Microsoft.UI.Xaml.Media;

public static class AcrylicBrushExtensions
{
	public static DependencyProperty UseCompositionEffectBrushProperty { get; } =
		DependencyProperty.RegisterAttached(
			"UseCompositionEffectBrush",
			typeof(bool),
			typeof(AcrylicBrushExtensions),
			new FrameworkPropertyMetadata(true));

	/// <summary>
	/// Gets a value indicating whether the specified AcrylicBrush uses the
	/// CompositionEffectBrush-based implementation (Skia only).
	/// When true, uses the CompositionEffectBrush-based implementation which goes through the
	/// full composition effect graph (blur, luminosity blend, tint blend, noise).
	/// When false (default), uses an optimized Skia-based implementation that renders
	/// similar but not identical output.
	/// </summary>
	public static bool GetUseCompositionEffectBrush(AcrylicBrush brush) =>
		(bool)brush.GetValue(UseCompositionEffectBrushProperty);

	/// <summary>
	/// Sets a value indicating whether the specified AcrylicBrush should use the
	/// CompositionEffectBrush-based implementation (Skia only).
	/// </summary>
	public static void SetUseCompositionEffectBrush(AcrylicBrush brush, bool value) =>
		brush.SetValue(UseCompositionEffectBrushProperty, value);
}
