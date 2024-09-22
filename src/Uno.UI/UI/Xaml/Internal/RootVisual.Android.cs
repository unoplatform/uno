#nullable enable

namespace Uno.UI.Xaml.Core;

partial class RootVisual
{
	protected override void OnLayoutCore(bool changed, int left, int top, int right, int bottom, bool localIsLayoutRequested)
	{
		base.OnLayoutCore(changed, left, top, right, bottom, localIsLayoutRequested);

		// Important for device orientation changes.
		InvalidateMeasure();
	}
}
