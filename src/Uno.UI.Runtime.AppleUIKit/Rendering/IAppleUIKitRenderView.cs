using Foundation;

namespace Uno.UI.Runtime.AppleUIKit;

/// <summary>
/// Common contract letting <see cref="RootViewController"/> host either render view behind one seam. Implementors are
/// also <c>UIView</c>s, added as the controller's render subview.
/// </summary>
internal interface IAppleUIKitRenderView
{
	void SetOwner(RootViewController owner);

	void QueueRender();

	/// <summary>Stops driving frames for good. A view whose window is gone would otherwise keep rendering into a
	/// context being torn down.</summary>
	void StopRender();

	/// <summary>The elements XCTest reads through the informal <c>automationElements</c> protocol, set by the
	/// accessibility adapter.</summary>
	NSObject[]? AutomationElements { get; set; }
}
