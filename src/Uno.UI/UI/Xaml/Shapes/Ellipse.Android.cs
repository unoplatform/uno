using Windows.Foundation;
using Uno.UI;

namespace Microsoft.UI.Xaml.Shapes
{
	public partial class Ellipse : Shape
	{
		/// <inheritdoc />
		protected override Size ArrangeOverride(Size finalSize)
			=> base.BasicArrangeOverride(finalSize);

		private protected override void SetupPath(Android.Graphics.Path path, global::Windows.Foundation.Rect logicalRenderingArea)
		{
			path.AddOval(logicalRenderingArea.ToRectF(), Android.Graphics.Path.Direction.Cw);
		}
	}
}
