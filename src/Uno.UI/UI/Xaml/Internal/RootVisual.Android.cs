#nullable enable

using Windows.Foundation;

namespace Uno.UI.Xaml.Core;

partial class RootVisual
{
	private (int, int, int, int) _lastDimensions;

	protected override void OnLayoutCore(bool changed, int left, int top, int right, int bottom, bool localIsLayoutRequested)
	{
		base.OnLayoutCore(changed, left, top, right, bottom, localIsLayoutRequested);

		UpdateLayout();
		if (_lastDimensions != (left, top, right, bottom))
		{
			// Important for device orientation changes.
			_lastDimensions = (left, top, right, bottom);
			InvalidateMeasure();
		}
	}
}
