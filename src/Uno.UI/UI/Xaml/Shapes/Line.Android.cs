using Windows.Foundation;
using Uno.UI;

namespace Windows.UI.Xaml.Shapes
{
    public partial class Line : ArbitraryShapeBase
	{
		protected override Android.Graphics.Path GetPath(Size availableSize)
		{
			var output = new Android.Graphics.Path();

			output.MoveTo(ViewHelper.LogicalToPhysicalPixels(X1), ViewHelper.LogicalToPhysicalPixels(Y1));
			output.LineTo(ViewHelper.LogicalToPhysicalPixels(X2), ViewHelper.LogicalToPhysicalPixels(Y2));

			return output;
		}
	}
}
