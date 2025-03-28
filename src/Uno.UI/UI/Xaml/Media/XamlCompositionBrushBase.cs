using Windows.UI.Composition;
using Color = Windows.UI.Color;

namespace Windows.UI.Xaml.Media;

public partial class XamlCompositionBrushBase : Brush
{
	protected XamlCompositionBrushBase() : base()
	{
	}

	public Color FallbackColor
	{
		get => (Color)GetValue(FallbackColorProperty);
		set => SetValue(FallbackColorProperty, value);
	}

	public static DependencyProperty FallbackColorProperty { get; } =
		DependencyProperty.Register(
			nameof(FallbackColor), typeof(Color),
			typeof(XamlCompositionBrushBase),
			new FrameworkPropertyMetadata(default(Color)));

	/// <summary>
	/// Returns the fallback color mixed with opacity value.
	/// </summary>
	internal Color FallbackColorWithOpacity => FallbackColor.WithOpacity(Opacity);

	public CompositionBrush CompositionBrush
	{
		get => (CompositionBrush)GetValue(CompositionBrushProperty);
		set => SetValue(CompositionBrushProperty, value);
	}

	/// <summary>
	/// Internal DependencyProperty used to track the CompositionBrush property.
	/// </summary>
	internal static DependencyProperty CompositionBrushProperty { get; } =
		DependencyProperty.Register(
			nameof(CompositionBrush), typeof(CompositionBrush),
			typeof(XamlCompositionBrushBase),
			new FrameworkPropertyMetadata(default(CompositionBrush)));

	protected virtual void OnConnected()
	{
	}

	protected virtual void OnDisconnected()
	{
	}

	internal void OnConnectedInternal() => OnConnected();
	internal void OnDisconnectedInternal() => OnDisconnected();
}
