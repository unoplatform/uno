#nullable enable

using CoreGraphics;
using Windows.Foundation;

namespace Uno.UI.Xaml.Core;

partial class RootVisual
{
	private CGRect _lastDimensions;

#if __IOS__
	public override void LayoutSubviews()
	{
		base.LayoutSubviews();
#else
	public override void Layout()
	{
		base.Layout();
#endif
		UpdateLayout();

		if (_lastDimensions != Frame)
		{
			// Important for device orientation changes.
			_lastDimensions = Frame;
			InvalidateMeasure();
		}
	}
}
