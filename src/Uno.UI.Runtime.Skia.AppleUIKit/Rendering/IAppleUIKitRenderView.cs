namespace Uno.UI.Runtime.Skia.AppleUIKit;

/// <summary>
/// Common contract for the AppleUIKit render views so <see cref="RootViewController"/> can host either the default
/// Skia-on-Metal view (<see cref="UnoSKMetalView"/>) or the experimental WebGPU view (<see cref="UnoSKWebGpuMetalView"/>)
/// behind the same seam. Implementors are also <c>UIView</c>s, added as the controller's render subview.
/// </summary>
internal interface IAppleUIKitRenderView
{
	void SetOwner(RootViewController owner);

	void QueueRender();
}
