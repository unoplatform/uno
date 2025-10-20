using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls;

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

		void ChangeNativeElementOpacity(object content, double opacity);
#endif
	}
}
