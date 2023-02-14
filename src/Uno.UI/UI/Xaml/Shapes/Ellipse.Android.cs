using Windows.Foundation;
using Uno.UI;

namespace Microsoft.UI.Xaml.Shapes
{
	public partial class Ellipse : Shape
	{
		static Ellipse()
		{
			StretchProperty.OverrideMetadata(typeof(Ellipse), new FrameworkPropertyMetadata(defaultValue: Media.Stretch.Fill));
		}

		/// <inheritdoc />
		protected override Size MeasureOverride(Size availableSize) => base.MeasureRelativeShape(availableSize);

		/// <inheritdoc />
		protected override Size ArrangeOverride(Size finalSize)
			=> base.BasicArrangeOverride(finalSize, path => { path.AddOval(_logicalRenderingArea.ToRectF(), Android.Graphics.Path.Direction.Cw); });
	}
}
