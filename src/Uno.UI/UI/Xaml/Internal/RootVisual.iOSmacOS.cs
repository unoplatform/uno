#nullable enable

using CoreGraphics;
using Windows.Foundation;

namespace Uno.UI.Xaml.Core;

partial class RootVisual
{
	private CGRect _lastDimensions;

	public override void LayoutSubviews()
	{
		base.LayoutSubviews();

		UpdateLayout();

		if (_lastDimensions != Frame)
		{
			// Important for device orientation changes.
			_lastDimensions = Frame;
			InvalidateMeasure();
		}
	}
}
