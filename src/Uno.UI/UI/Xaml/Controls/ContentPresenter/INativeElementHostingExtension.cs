using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls;

partial class ContentPresenter
{
	internal interface INativeElementHostingExtension
	{
#if UNO_SUPPORTS_NATIVEHOST
		bool IsNativeElement(object content);

		void AttachNativeElement(XamlRoot owner, object content);

		void DetachNativeElement(XamlRoot owner, object content);

		void ArrangeNativeElement(XamlRoot owner, object content, Rect arrangeRect, Rect clipRect);

		Size MeasureNativeElement(XamlRoot owner, object content, Size childMeasuredSize, Size availableSize);

		object CreateSampleComponent(XamlRoot owner, string text);

		bool IsNativeElementAttached(XamlRoot owner, object nativeElement);

		void ChangeNativeElementVisibility(XamlRoot owner, object content, bool visible);

		void ChangeNativeElementOpacity(XamlRoot owner, object content, double opacity);
#endif
	}
}
