using Windows.UI.Xaml.Media;

namespace Windows.UI.Xaml.Controls.Primitives;

public sealed partial class TickBar : FrameworkElement
{
	/// <summary>
	/// Gets or sets the Brush that draws on the background area of the TickBar.
	/// </summary>
	public Brush Fill
	{
		get => (Brush)GetValue(FillProperty);
		set => SetValue(FillProperty, value);
	}

	/// <summary>
	/// Identifies the Fill dependency property.
	/// </summary>
	public static DependencyProperty FillProperty { get; } =
		DependencyProperty.Register(nameof(Fill), typeof(Brush), typeof(TickBar), new FrameworkPropertyMetadata(null));
}
