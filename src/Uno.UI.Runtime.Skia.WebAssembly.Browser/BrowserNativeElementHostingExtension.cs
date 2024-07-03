#nullable enable

using System;
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
		=> content is SkiaWasmHtmlElement skiaWasmHtmlElement && NativeMethods.IsNativeElement(skiaWasmHtmlElement.ElementId);

	public void AttachNativeElement(object content)
	{
		Debug.Assert(content is SkiaWasmHtmlElement);
		NativeMethods.AttachNativeElement(((SkiaWasmHtmlElement)content).ElementId);
	}

	public void DetachNativeElement(object content)
	{
		Debug.Assert(content is SkiaWasmHtmlElement);
		NativeMethods.DetachNativeElement(((SkiaWasmHtmlElement)content).ElementId);
	}

	public void ArrangeNativeElement(object content, Windows.Foundation.Rect arrangeRect, Windows.Foundation.Rect clipRect)
	{
		Debug.Assert(content is SkiaWasmHtmlElement);
		NativeMethods.ArrangeNativeElement(((SkiaWasmHtmlElement)content).ElementId, arrangeRect.X, arrangeRect.Y, arrangeRect.Width, arrangeRect.Height);
	}

	public void ChangeNativeElementVisibility(object content, bool visible)
	{
		// no need to do anything here, airspace clipping logic will take care of it automatically
	}

	public void ChangeNativeElementOpacity(object content, double opacity)
	{
		Debug.Assert(content is SkiaWasmHtmlElement);
		NativeMethods.ChangeNativeElementOpacity(((SkiaWasmHtmlElement)content).ElementId, opacity);
	}

	public static void SetSvgClipPathForNativeElementHost(string path)
	{
		NativeMethods.SetSvgClipPathForNativeElementHost(path);
	}

	public Windows.Foundation.Size MeasureNativeElement(object content, Windows.Foundation.Size childMeasuredSize, Windows.Foundation.Size availableSize) => availableSize;

	public object CreateSampleComponent(string text)
	{
		var id = new string(Random.Shared.GetItems("abcdefghijklmnopqrstuvwxyz".ToCharArray(), 10));
		var element = new SkiaWasmHtmlElement(id, "div");
		NativeMethods.CreateSampleComponent(id, text);
		return element;
	}

	private static partial class NativeMethods
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
		internal static partial void CreateSampleComponent(string parentId, string text);

		[JSImport($"globalThis.Uno.UI.Runtime.Skia.{nameof(BrowserNativeElementHostingExtension)}.changeNativeElementOpacity")]
		internal static partial string ChangeNativeElementOpacity(string content, double opacity);

		[JSImport($"globalThis.Uno.UI.Runtime.Skia.{nameof(BrowserNativeElementHostingExtension)}.setSvgClipPathForNativeElementHost")]
		internal static partial string SetSvgClipPathForNativeElementHost(string path);
	}
}
