using Windows.Foundation;

namespace Windows.UI.Core;

internal interface INativeElementHostingExtension
{
	bool IsNativeElement(object content);

	void AttachNativeElement(object owner, object content);

	void DetachNativeElement(object owner, object content);

	void ArrangeNativeElement(object owner, object content, Rect arrangeRect);

	Size MeasureNativeElement(object owner, object content, Size size);

	bool IsNativeElementAttached(object owner, object nativeElement);
}
