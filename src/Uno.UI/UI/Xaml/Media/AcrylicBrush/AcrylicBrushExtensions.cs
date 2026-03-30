namespace Microsoft.UI.Xaml.Media;

public static class AcrylicBrushExtensions
{
	/// <remarks>
	/// When set to <c>true</c> on an <see cref="AcrylicBrush"/>, the brush uses the
	/// CompositionEffectBrush-based rendering pipeline (Skia only). When <c>false</c>,
	/// the optimized Skia-native implementation is used.
	/// This property has no effect on non-Skia targets.
	/// </remarks>
	public static DependencyProperty UseCompositionEffectBrushProperty { get; } =
		DependencyProperty.RegisterAttached(
			"UseCompositionEffectBrush",
			typeof(bool),
			typeof(AcrylicBrushExtensions),
			new FrameworkPropertyMetadata(true));

	/// <summary>
	/// Gets a value indicating whether the specified <see cref="AcrylicBrush"/> uses the
	/// CompositionEffectBrush-based implementation (Skia only).
	/// </summary>
	public static bool GetUseCompositionEffectBrush(AcrylicBrush brush) =>
		(bool)brush.GetValue(UseCompositionEffectBrushProperty);

	/// <summary>
	/// Sets a value indicating whether the specified <see cref="AcrylicBrush"/> should use the
	/// CompositionEffectBrush-based implementation (Skia only).
	/// </summary>
	public static void SetUseCompositionEffectBrush(AcrylicBrush brush, bool value) =>
		brush.SetValue(UseCompositionEffectBrushProperty, value);
}
