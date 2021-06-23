using Windows.UI.Xaml;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class AnimatedIcon
	{
		public IconSource FallbackIconSource
		{
			get => (IconSource)GetValue(FallbackIconSourceProperty);
			set => SetValue(FallbackIconSourceProperty, value);
		}

		public static DependencyProperty FallbackIconSourceProperty { get; } =
			DependencyProperty.Register(nameof(FallbackIconSource), typeof(IconSource), typeof(AnimatedIcon), new PropertyMetadata(null, OnFallbackIconSourcePropertyChanged));

		public IAnimatedVisualSource2 Source
		{
			get => (IAnimatedVisualSource2)GetValue(SourceProperty);
			set => SetValue(SourceProperty, value);
		}

		public static DependencyProperty SourceProperty { get; } =
			DependencyProperty.Register(nameof(Source), typeof(IAnimatedVisualSource2), typeof(AnimatedIcon), new PropertyMetadata(null, OnSourcePropertyChanged));

		private static void OnFallbackIconSourcePropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
		{
			var owner = sender as AnimatedIcon;
			owner?.OnFallbackIconSourcePropertyChanged(args);
		}

		private static void OnSourcePropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
		{
			var owner = sender as AnimatedIcon;
			owner?.OnFallbackIconSourcePropertyChanged(args);
		}
	}
}
