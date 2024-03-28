using Windows.Foundation;

namespace Windows.UI.Xaml.Shapes
{
	public partial class Ellipse : Shape
	{
		static Ellipse()
		{
			StretchProperty.OverrideMetadata(typeof(Ellipse), new FrameworkPropertyMetadata(defaultValue: Media.Stretch.Fill));
		}

#if __IOS__ || __MACOS__ || __SKIA__ || __ANDROID__ || __WASM__
		protected override Size MeasureOverride(Size availableSize) => MeasureRelativeShape(availableSize);
#endif

#if __NETSTD_REFERENCE__
		protected override Size MeasureOverride(Size availableSize) => base.MeasureOverride(availableSize);
		protected override Size ArrangeOverride(Size finalSize) => base.ArrangeOverride(finalSize);
#endif
	}
}
