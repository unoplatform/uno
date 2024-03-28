using Windows.Foundation;
using Uno.UI;

namespace Windows.UI.Xaml.Shapes
{
	public partial class Ellipse : Shape
	{
		/// <inheritdoc />
		protected override Size ArrangeOverride(Size finalSize)
			=> base.BasicArrangeOverride(finalSize, path => { path.AddOval(_logicalRenderingArea.ToRectF(), Android.Graphics.Path.Direction.Cw); });
	}
}
