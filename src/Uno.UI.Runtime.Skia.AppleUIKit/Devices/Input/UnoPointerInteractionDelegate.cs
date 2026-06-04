#if __IOS__
using UIKit;

namespace Uno.UI.Runtime.Skia.AppleUIKit;

/// <summary>
/// Drives the iPadOS pointer shape from the managed cursor. The whole host view is a single pointer
/// region; the actual cursor is resolved from the topmost hovered element by the managed input
/// pipeline (UIElement.ProtectedCursor -> CoreWindow.PointerCursor) and mapped to a UIPointerStyle.
/// </summary>
internal sealed class UnoPointerInteractionDelegate : UIPointerInteractionDelegate
{
	public override UIPointerStyle GetStyleForRegion(UIPointerInteraction interaction, UIPointerRegion region)
		=> AppleUIKitCorePointerInputSource.Instance.GetPointerStyle();
}
#endif
