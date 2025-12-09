#nullable enable

using System;
using System.Diagnostics;
using System.Runtime.InteropServices.JavaScript;
using Uno.UI.NativeElementHosting;
using ContentPresenter = Microsoft.UI.Xaml.Controls.ContentPresenter;

namespace Uno.UI.Runtime.Skia;

internal partial class BrowserNativeElementHostingExtension : ContentPresenter.INativeElementHostingExtension
{
	private readonly ContentPresenter _presenter;
	private static string? _lastSvgClipPath;

	public BrowserNativeElementHostingExtension(ContentPresenter contentPresenter)
	{
		_presenter = contentPresenter;
	}

	public bool IsNativeElement(object content)
		=> content is BrowserHtmlElement skiaWasmHtmlElement && NativeMethods.IsNativeElement(skiaWasmHtmlElement.ElementId);

	public void AttachNativeElement(object content)
	{
		Debug.Assert(content is BrowserHtmlElement);
		NativeMethods.AttachNativeElement(((BrowserHtmlElement)content).ElementId);
	}

	public void DetachNativeElement(object content)
	{
		Debug.Assert(content is BrowserHtmlElement);
		NativeMethods.DetachNativeElement(((BrowserHtmlElement)content).ElementId);
	}

	public void ArrangeNativeElement(object content, Windows.Foundation.Rect arrangeRect)
	{
		Debug.Assert(content is BrowserHtmlElement);
		NativeMethods.ArrangeNativeElement(((BrowserHtmlElement)content).ElementId, arrangeRect.X, arrangeRect.Y, arrangeRect.Width, arrangeRect.Height);
	}

	public void ChangeNativeElementOpacity(object content, double opacity)
	{
		Debug.Assert(content is BrowserHtmlElement);
		NativeMethods.ChangeNativeElementOpacity(((BrowserHtmlElement)content).ElementId, opacity);
	}

	public bool SupportsZIndex() => true;

	public void SetZIndex(object content, int zIndex)
	{
		Debug.Assert(content is BrowserHtmlElement);
		NativeMethods.SetZIndex(((BrowserHtmlElement)content).ElementId, zIndex);
	}

	public static void SetSvgClipPathForNativeElementHost(string path)
	{
		if (_lastSvgClipPath != path)
		{
			_lastSvgClipPath = path;
			NativeMethods.SetSvgClipPathForNativeElementHost(path);
		}
	}

	public Windows.Foundation.Size MeasureNativeElement(object content, Windows.Foundation.Size childMeasuredSize, Windows.Foundation.Size availableSize) => availableSize;

	public object CreateSampleComponent(string text)
	{
		var element = BrowserHtmlElement.CreateHtmlElement("div");
		NativeMethods.CreateSampleComponent(element.ElementId, text);
		return element;
	}

	private static partial class NativeMethods
	{
		[JSImport($"globalThis.Uno.UI.NativeElementHosting.{nameof(BrowserHtmlElement)}.isNativeElement")]
		internal static partial bool IsNativeElement(string content);

		[JSImport($"globalThis.Uno.UI.NativeElementHosting.{nameof(BrowserHtmlElement)}.attachNativeElement")]
		internal static partial bool AttachNativeElement(string content);

		[JSImport($"globalThis.Uno.UI.NativeElementHosting.{nameof(BrowserHtmlElement)}.detachNativeElement")]
		internal static partial bool DetachNativeElement(string content);

		[JSImport($"globalThis.Uno.UI.NativeElementHosting.{nameof(BrowserHtmlElement)}.arrangeNativeElement")]
		internal static partial bool ArrangeNativeElement(string content, double x, double y, double width, double height);

		[JSImport($"globalThis.Uno.UI.NativeElementHosting.{nameof(BrowserHtmlElement)}.createSampleComponent")]
		internal static partial void CreateSampleComponent(string parentId, string text);

		[JSImport($"globalThis.Uno.UI.NativeElementHosting.{nameof(BrowserHtmlElement)}.changeNativeElementOpacity")]
		internal static partial string ChangeNativeElementOpacity(string content, double opacity);

		[JSImport($"globalThis.Uno.UI.NativeElementHosting.{nameof(BrowserHtmlElement)}.setSvgClipPathForNativeElementHost")]
		internal static partial string SetSvgClipPathForNativeElementHost(string path);

		[JSImport($"globalThis.Uno.UI.NativeElementHosting.{nameof(BrowserHtmlElement)}.setZIndex")]
		internal static partial void SetZIndex(string content, int zIndex);
	}
}
