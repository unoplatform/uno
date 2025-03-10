using Uno.UI;
using Windows.Foundation;

namespace Windows.UI.Xaml;

partial record struct NonUniformCornerRadius
{
	internal void GetRadii(float[] radiiStore)
	{
		radiiStore[0] = ViewHelper.LogicalToPhysicalPixels(TopLeft.X);
		radiiStore[1] = ViewHelper.LogicalToPhysicalPixels(TopLeft.Y);
		radiiStore[2] = ViewHelper.LogicalToPhysicalPixels(TopRight.X);
		radiiStore[3] = ViewHelper.LogicalToPhysicalPixels(TopRight.Y);
		radiiStore[4] = ViewHelper.LogicalToPhysicalPixels(BottomRight.X);
		radiiStore[5] = ViewHelper.LogicalToPhysicalPixels(BottomRight.Y);
		radiiStore[6] = ViewHelper.LogicalToPhysicalPixels(BottomLeft.X);
		radiiStore[7] = ViewHelper.LogicalToPhysicalPixels(BottomLeft.Y);
	}
}
