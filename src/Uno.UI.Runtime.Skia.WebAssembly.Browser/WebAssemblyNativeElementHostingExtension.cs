#nullable enable

using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;
using Windows.UI.Core;

namespace Uno.UI.Runtime.Skia;

unsafe internal partial class WebAssemblyNativeElementHostingExtension : ContentPresenter.INativeElementHostingExtension
{
	public bool IsNativeElement(object content) => false;
	public void AttachNativeElement(object content) { }
	public void DetachNativeElement(object content) { }
	public void ArrangeNativeElement(object content, Rect arrangeRect, Rect clipRect) { }
	public Size MeasureNativeElement(object content, Size childMeasuredSize, Size availableSize) => availableSize;
	public object CreateSampleComponent(string text) => null!;

	public void ChangeNativeElementVisibility(object content, bool visible) { }

	public void ChangeNativeElementOpacity(object content, double opacity) { }
}
