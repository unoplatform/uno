using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace Microsoft.UI.Xaml.Controls
{
	public class ImageIconSource : IconSource
	{
		public ImageSource ImageSource
		{
			get => (ImageSource)GetValue(ImageSourceProperty);
			set => SetValue(ImageSourceProperty, value);
		}

		public static DependencyProperty ImageSourceProperty { get; } =
			DependencyProperty.Register(nameof(ImageSource), typeof(ImageSource), typeof(ImageIconSource), new PropertyMetadata(null, OnPropertyChanged));

		internal protected override IconElement CreateIconElementCore()
		{
			var imageIcon = new ImageIcon();
			if (ImageSource is { } imageSource)
			{
				imageIcon.Source = imageSource;
			}
			if (Foreground is { } newForeground)
			{
				imageIcon.Foreground = newForeground;
			}
			return imageIcon;
		}

		internal protected override DependencyProperty GetIconElementPropertyCore(DependencyProperty sourceProperty)
		{
			if (sourceProperty == ImageSourceProperty)
			{
				return ImageIcon.SourceProperty;
			}

			return base.GetIconElementPropertyCore(sourceProperty);
		}
	}
}
