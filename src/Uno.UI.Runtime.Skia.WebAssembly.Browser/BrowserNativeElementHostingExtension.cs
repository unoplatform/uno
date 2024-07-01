#nullable enable

using System.Diagnostics;
using System.Runtime.InteropServices.JavaScript;
using ContentPresenter = Microsoft.UI.Xaml.Controls.ContentPresenter;

namespace Uno.UI.Runtime.Skia;

internal partial class BrowserNativeElementHostingExtension : ContentPresenter.INativeElementHostingExtension
{
	private readonly ContentPresenter _presenter;

	public BrowserNativeElementHostingExtension(ContentPresenter contentPresenter)
	{
		_presenter = contentPresenter;
	}

	public bool IsNativeElement(object content)
		=> content is string s && NativeMethods.IsNativeElement(s);

	public void AttachNativeElement(object content)
	{
		Debug.Assert(content is string);
		NativeMethods.AttachNativeElement((string)content);
	}

	public void DetachNativeElement(object content)
	{
		Debug.Assert(content is string);
		NativeMethods.DetachNativeElement((string)content);
	}

	public void ArrangeNativeElement(object content, Windows.Foundation.Rect arrangeRect, Windows.Foundation.Rect clipRect)
	{
		Debug.Assert(content is string);
		NativeMethods.ArrangeNativeElement((string)content, arrangeRect.X, arrangeRect.Y, arrangeRect.Width, arrangeRect.Height);
	}

	public object CreateSampleComponent(string text)
	{
		return NativeMethods.CreateSampleComponent(text);
	}

	public void ChangeNativeElementVisibility(object content, bool visible)
	{
	}

	public void ChangeNativeElementOpacity(object content, double opacity)
	{
		Debug.Assert(content is string);
		NativeMethods.ChangeNativeElementOpacity((string)content, opacity);
	}

	public static void SetSvgClipPathForNativeElementHost(string path)
	{
		NativeMethods.SetSvgClipPathForNativeElementHost(path);
	}

	public Windows.Foundation.Size MeasureNativeElement(object content, Windows.Foundation.Size childMeasuredSize, Windows.Foundation.Size availableSize) => availableSize;

	internal static partial class NativeMethods
	{
		[JSImport($"globalThis.Uno.UI.Runtime.Skia.{nameof(BrowserNativeElementHostingExtension)}.isNativeElement")]
		internal static partial bool IsNativeElement(string content);

		[JSImport($"globalThis.Uno.UI.Runtime.Skia.{nameof(BrowserNativeElementHostingExtension)}.attachNativeElement")]
		internal static partial bool AttachNativeElement(string content);

		[JSImport($"globalThis.Uno.UI.Runtime.Skia.{nameof(BrowserNativeElementHostingExtension)}.detachNativeElement")]
		internal static partial bool DetachNativeElement(string content);

		[JSImport($"globalThis.Uno.UI.Runtime.Skia.{nameof(BrowserNativeElementHostingExtension)}.arrangeNativeElement")]
		internal static partial bool ArrangeNativeElement(string content, double x, double y, double width, double height);

		[JSImport($"globalThis.Uno.UI.Runtime.Skia.{nameof(BrowserNativeElementHostingExtension)}.createSampleComponent")]
		internal static partial string CreateSampleComponent(string text);

		[JSImport($"globalThis.Uno.UI.Runtime.Skia.{nameof(BrowserNativeElementHostingExtension)}.changeNativeElementOpacity")]
		internal static partial string ChangeNativeElementOpacity(string content, double opacity);

		[JSImport($"globalThis.Uno.UI.Runtime.Skia.{nameof(BrowserNativeElementHostingExtension)}.setSvgClipPathForNativeElementHost")]
		internal static partial string SetSvgClipPathForNativeElementHost(string path);
	}
}
