using Windows.Foundation;
using Uno.UI;

namespace Microsoft.UI.Xaml.Shapes
{
	public partial class Rectangle : Shape
	{
		public Rectangle()
		{
		}

		/// <inheritdoc />
		protected override Size ArrangeOverride(Size finalSize)
			=> base.BasicArrangeOverride(finalSize);

		private protected override void SetupPath(Android.Graphics.Path path, global::Windows.Foundation.Rect logicalRenderingArea)
		{
			path.AddRoundRect(logicalRenderingArea.ToRectF(), (float)RadiusX, (float)RadiusY, Android.Graphics.Path.Direction.Cw);
		}
	}
}
