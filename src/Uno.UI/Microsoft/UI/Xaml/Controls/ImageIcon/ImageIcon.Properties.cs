#nullable disable

using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class ImageIcon
	{
		public ImageSource Source
		{
			get => (ImageSource)GetValue(SourceProperty);
			set => SetValue(SourceProperty, value);
		}

		public static DependencyProperty SourceProperty { get; } =
			DependencyProperty.Register(nameof(Source), typeof(ImageSource), typeof(ImageIcon), new FrameworkPropertyMetadata(null, OnSourcePropertyChanged));


		private static void OnSourcePropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
		{
			var owner = sender as ImageIcon;
			owner?.OnSourcePropertyChanged(args);
		}
	}
}
