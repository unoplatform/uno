using Windows.Foundation;
using Uno.UI;

namespace Microsoft.UI.Xaml.Shapes
{
	public partial class Rectangle : Shape
	{
		static Rectangle()
		{
			StretchProperty.OverrideMetadata(typeof(Rectangle), new FrameworkPropertyMetadata(defaultValue: Media.Stretch.Fill));
		}

		public Rectangle()
		{
		}

		/// <inheritdoc />
		protected override Size MeasureOverride(Size availableSize)
			=> base.MeasureRelativeShape(availableSize);

		/// <inheritdoc />
		protected override Size ArrangeOverride(Size finalSize)
			=> base.BasicArrangeOverride(finalSize, path => { path.AddRoundRect(_logicalRenderingArea.ToRectF(), (float)RadiusX, (float)RadiusY, Android.Graphics.Path.Direction.Cw); });
	}
}
