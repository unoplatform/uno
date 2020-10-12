using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;

namespace Microsoft.UI.Xaml.Controls
{
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
			DependencyProperty.Register(nameof(Data), typeof(Geometry), typeof(PathIconSource), new PropertyMetadata(null));
	}
}
