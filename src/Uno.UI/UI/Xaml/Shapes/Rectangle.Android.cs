using Windows.Foundation;
using Uno.UI;

namespace Windows.UI.Xaml.Shapes
{
	public partial class Rectangle : Shape
	{
		public Rectangle()
		{
		}

		/// <inheritdoc />
		protected override Size ArrangeOverride(Size finalSize)
			=> base.BasicArrangeOverride(finalSize, path => { path.AddRoundRect(_logicalRenderingArea.ToRectF(), (float)RadiusX, (float)RadiusY, Android.Graphics.Path.Direction.Cw); });
	}
}
