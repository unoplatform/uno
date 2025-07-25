using Windows.Foundation;
using Uno.UI;

namespace Microsoft.UI.Xaml.Shapes
{
	public partial class Ellipse : Shape
	{
		/// <inheritdoc />
		protected override Size ArrangeOverride(Size finalSize)
			=> base.BasicArrangeOverride(finalSize, path => { path.AddOval(_logicalRenderingArea.ToRectF(), APath.Direction.Cw); });
	}
}
