#nullable enable

using Windows.Foundation;
using Windows.UI.Core;

namespace Uno.UI.Runtime.Skia;

internal partial class CoreWindowExtension : ICoreWindowExtension
{
	public bool IsNativeElement(object content) => false;
	public void AttachNativeElement(object owner, object content) { }
	public void DetachNativeElement(object owner, object content) { }
	public void ArrangeNativeElement(object owner, object content, Rect arrangeRect, Rect? clipRect) { }
	public Size MeasureNativeElement(object owner, object content, Size childMeasuredSize, Size availableSize) => childMeasuredSize;
	public void ClipNativeElement(object owner, object content, Rect clip) { }
	public object? CreateSampleComponent(string text) => null;
	public bool IsNativeElementAttached(object owner, object nativeElement) => false;
	public void ChangeNativeElementVisiblity(object owner, object content, bool visible) { }
	public void ChangeNativeElementOpacity(object owner, object content, double opacity) { }
}
