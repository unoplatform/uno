#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices.JavaScript;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.NativeElementHosting;
using Uno.UI.Extensions;
using ContentPresenter = Microsoft.UI.Xaml.Controls.ContentPresenter;

namespace Uno.UI.Runtime.Skia;

internal partial class BrowserNativeElementHostingExtension : ContentPresenter.INativeElementHostingExtension
{
	private readonly ContentPresenter _presenter;
	private static (string path, string fillType)? _lastSvgClipPath;
	private static readonly Dictionary<string, WeakReference<BrowserNativeElementHostingExtension>> _hosts = new();

	public BrowserNativeElementHostingExtension(ContentPresenter contentPresenter)
	{
		_presenter = contentPresenter;
	}

	public bool IsNativeElement(object content)
		=> content is BrowserHtmlElement skiaWasmHtmlElement && NativeMethods.IsNativeElement(skiaWasmHtmlElement.ElementId);

	public void AttachNativeElement(object content)
	{
		Debug.Assert(content is BrowserHtmlElement);
		var element = (BrowserHtmlElement)content;
		NativeMethods.AttachNativeElement(element.ElementId);
		_hosts[element.ElementId] = new(this);
	}

	public void DetachNativeElement(object content)
	{
		Debug.Assert(content is BrowserHtmlElement);
		var element = (BrowserHtmlElement)content;
		_hosts.Remove(element.ElementId);
		NativeMethods.DetachNativeElement(element.ElementId);
	}

	internal static bool ApplyNegotiatedScroll(string elementId, double horizontalDelta, double verticalDelta)
	{
		if (!_hosts.TryGetValue(elementId, out var weakExtension)
			|| !weakExtension.TryGetTarget(out var extension))
		{
			_hosts.Remove(elementId);
			return false;
		}

		return extension.ApplyNegotiatedScroll(horizontalDelta, verticalDelta);
	}

	private bool ApplyNegotiatedScroll(double horizontalDelta, double verticalDelta)
	{
		var remainingHorizontalDelta = horizontalDelta;
		var remainingVerticalDelta = verticalDelta;
		var didScroll = false;

		foreach (var ancestor in _presenter.GetVisualAncestry())
		{
			if (ancestor is not ScrollViewer scrollViewer)
			{
				continue;
			}

			var initialHorizontalOffset = scrollViewer.HorizontalOffset;
			var initialVerticalOffset = scrollViewer.VerticalOffset;
			scrollViewer.ChangeView(
				horizontalOffset: initialHorizontalOffset + remainingHorizontalDelta,
				verticalOffset: initialVerticalOffset + remainingVerticalDelta,
				zoomFactor: null,
				disableAnimation: true);

			remainingHorizontalDelta -= scrollViewer.HorizontalOffset - initialHorizontalOffset;
			remainingVerticalDelta -= scrollViewer.VerticalOffset - initialVerticalOffset;
			didScroll |= initialHorizontalOffset != scrollViewer.HorizontalOffset || initialVerticalOffset != scrollViewer.VerticalOffset;

			if (remainingHorizontalDelta == 0 && remainingVerticalDelta == 0)
			{
				break;
			}
		}

		return didScroll;
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

	public static void SetSvgClipPathForNativeElementHost(string path, string fillType)
	{
		if (_lastSvgClipPath != (path, fillType))
		{
			_lastSvgClipPath = (path, fillType);
			NativeMethods.SetSvgClipPathForNativeElementHost(path, fillType);
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
		internal static partial void AttachNativeElement(string content);

		[JSImport($"globalThis.Uno.UI.NativeElementHosting.{nameof(BrowserHtmlElement)}.detachNativeElement")]
		internal static partial void DetachNativeElement(string content);

		[JSImport($"globalThis.Uno.UI.NativeElementHosting.{nameof(BrowserHtmlElement)}.arrangeNativeElement")]
		internal static partial void ArrangeNativeElement(string content, double x, double y, double width, double height);

		[JSImport($"globalThis.Uno.UI.NativeElementHosting.{nameof(BrowserHtmlElement)}.createSampleComponent")]
		internal static partial void CreateSampleComponent(string parentId, string text);

		[JSImport($"globalThis.Uno.UI.NativeElementHosting.{nameof(BrowserHtmlElement)}.changeNativeElementOpacity")]
		internal static partial void ChangeNativeElementOpacity(string content, double opacity);

		[JSImport($"globalThis.Uno.UI.NativeElementHosting.{nameof(BrowserHtmlElement)}.setSvgClipPathForNativeElementHost")]
		internal static partial void SetSvgClipPathForNativeElementHost(string path, string fillType);

		[JSImport($"globalThis.Uno.UI.NativeElementHosting.{nameof(BrowserHtmlElement)}.setZIndex")]
		internal static partial void SetZIndex(string content, int zIndex);
	}
}
