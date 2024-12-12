using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Controls;

namespace Windows.UI.Xaml.Controls;

public partial class PathIconSource : IconSource
{
	public PathIconSource()
	{
	}

	public Geometry Data
	{
		get => (Geometry)GetValue(DataProperty);
		set => SetValue(DataProperty, value);
	}

	public static DependencyProperty DataProperty { get; } =
		DependencyProperty.Register(nameof(Data), typeof(Geometry), typeof(PathIconSource), new FrameworkPropertyMetadata(null));

	public override IconElement CreateIconElement()
	{
		var pathIcon = new PathIcon();

		if (Data != null)
		{
			pathIcon.Data = Data;
		}

		if (Foreground != null)
		{
			pathIcon.Foreground = Foreground;
		}

		return pathIcon;
	}
}
