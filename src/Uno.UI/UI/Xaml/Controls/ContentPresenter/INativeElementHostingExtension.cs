using Windows.Foundation;

namespace Windows.UI.Xaml.Controls;

partial class ContentPresenter
{
	internal interface INativeElementHostingExtension
	{
#if UNO_SUPPORTS_NATIVEHOST
		bool IsNativeElement(object content);

		void AttachNativeElement(object content);

		void DetachNativeElement(object content);

		void ArrangeNativeElement(object content, Rect arrangeRect, Rect clipRect);

		Size MeasureNativeElement(object content, Size childMeasuredSize, Size availableSize);

		object CreateSampleComponent(string text);

		void ChangeNativeElementVisibility(object content, bool visible);

		void ChangeNativeElementOpacity(object content, double opacity);
#endif
	}
}
