#nullable enable

using Windows.Foundation;
using Windows.UI.Core;

namespace Uno.UI.Runtime.Skia;

internal partial class CoreWindowExtension : INativeElementHostingExtension
{
	public bool IsNativeElement(object content) => false;
	public void AttachNativeElement(object owner, object content) { }
	public void DetachNativeElement(object owner, object content) { }
	public void ArrangeNativeElement(object owner, object content, Rect arrangeRect) { }
	public Size MeasureNativeElement(object owner, object content, Size size) => size;
	public bool IsNativeElementAttached(object owner, object nativeElement) => false;
}
