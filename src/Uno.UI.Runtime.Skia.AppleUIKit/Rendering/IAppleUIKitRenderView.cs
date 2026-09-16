namespace Uno.UI.Runtime.Skia.AppleUIKit;

/// <summary>
/// Common contract letting <see cref="RootViewController"/> host either render view behind one seam. Implementors are
/// also <c>UIView</c>s, added as the controller's render subview.
/// </summary>
internal interface IAppleUIKitRenderView
{
	void SetOwner(RootViewController owner);

	void QueueRender();
}
