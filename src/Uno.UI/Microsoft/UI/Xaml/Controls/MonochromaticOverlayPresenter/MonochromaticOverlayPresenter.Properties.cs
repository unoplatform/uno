using Windows.UI;
using Windows.UI.Xaml;

namespace Microsoft/* UWP don't rename */.UI.Xaml.Controls.Primitives
{
	public partial class MonochromaticOverlayPresenter
	{
		public Color ReplacementColor
		{
			get => (Color)GetValue(ReplacementColorProperty);
			set => SetValue(ReplacementColorProperty, value);
		}

		public static DependencyProperty ReplacementColorProperty { get; } =
			DependencyProperty.Register(
				nameof(ReplacementColor),
				typeof(Color),
				typeof(MonochromaticOverlayPresenter),
				new FrameworkPropertyMetadata(default(Color), OnPropertyChanged));

		public UIElement SourceElement
		{
			get => (UIElement)GetValue(SourceElementProperty);
			set => SetValue(SourceElementProperty, value);
		}

		public static DependencyProperty SourceElementProperty { get; } =
			DependencyProperty.Register(
				nameof(SourceElement),
				typeof(UIElement),
				typeof(MonochromaticOverlayPresenter),
				new FrameworkPropertyMetadata(null, OnPropertyChanged));

		private static void OnPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
		{
			var presenter = sender as MonochromaticOverlayPresenter;
			presenter?.OnPropertyChanged(args);
		}
	}
}
