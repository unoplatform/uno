namespace Microsoft.UI.Xaml.Media;

public static class AcrylicBrushExtensions
{
	/// <remarks>
	/// Retained for compatibility. The acrylic brush now always renders through the
	/// backend-neutral CompositionEffectBrush pipeline, so this property no longer
	/// selects an alternative implementation.
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
