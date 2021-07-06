using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class IconSource : DependencyObject
	{
		protected IconSource()
		{
		}

		public Brush Foreground
		{
			get => (Brush)GetValue(ForegroundProperty);
			set => SetValue(ForegroundProperty, value);
		}

		public static DependencyProperty ForegroundProperty { get; } =
			DependencyProperty.Register(nameof(Foreground), typeof(Brush), typeof(IconSource), new PropertyMetadata(null));

#nullable enable

		public virtual IconElement? CreateIconElement() => default;
	}
}
