using System;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls;

partial class ContentPresenter
{
	/// <remarks>
	/// Implementations report platform focus moving into an attached native element (or a descendant of it) with
	/// <see cref="OnNativeElementGotFocus"/>, and, where the platform can tell, focus leaving it for something
	/// outside XAML with <see cref="OnNativeElementLostFocus"/>. Both are called on the UI thread.
	/// </remarks>
	internal interface INativeElementHostingExtension
	{
#if UNO_SUPPORTS_NATIVEHOST
		bool IsNativeElement(object content);

		void AttachNativeElement(object content);

		void DetachNativeElement(object content);

		void ArrangeNativeElement(object content, Rect arrangeRect);

		Size MeasureNativeElement(object content, Size childMeasuredSize, Size availableSize);

		object CreateSampleComponent(string text);

		void ChangeNativeElementOpacity(object content, double opacity);

		bool SupportsZIndex() => false;

		void SetZIndex(object content, int zIndex)
		{
			if (SupportsZIndex())
			{
				throw new InvalidOperationException($"{nameof(SetZIndex)} should be implemented if {nameof(SupportsZIndex)}.");
			}
		}
#endif
	}
}
