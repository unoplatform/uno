#nullable enable

using Microsoft.UI.Xaml.Media;

namespace Microsoft.UI.Xaml.Controls;

public partial class IconSource : DependencyObject
{
	protected IconSource()
	{
	}

	public Brush? Foreground
	{
		get => (Brush?)GetValue(ForegroundProperty);
		set => SetValue(ForegroundProperty, value);
	}

	public static DependencyProperty ForegroundProperty { get; } =
		DependencyProperty.Register(nameof(Foreground), typeof(Brush), typeof(IconSource), new FrameworkPropertyMetadata(null));

	public virtual IconElement? CreateIconElement() => default;
}