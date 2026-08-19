#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Uno.Foundation.Logging;
using System.Runtime.InteropServices.JavaScript;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.NativeElementHosting;
using Uno.UI.Extensions;
using ContentPresenter = Microsoft.UI.Xaml.Controls.ContentPresenter;

namespace Uno.UI.Runtime.Skia;

internal partial class BrowserNativeElementHostingExtension : ContentPresenter.INativeElementHostingExtension
{
	private static readonly Logger _log = typeof(BrowserNativeElementHostingExtension).Log();

	private readonly ContentPresenter _presenter;
	private static (string path, string fillType)? _lastSvgClipPath;

	// Keyed by BrowserHtmlElement.UnoElementId (the GCHandle), never by the ElementId string: that string is
	// settable by the caller (OwnHtmlElement) and observable from the hosted DOM, so it can collide or be
	// spoofed by hosted content. The handle cannot.
	private static readonly Dictionary<nint, WeakReference<BrowserNativeElementHostingExtension>> _hosts = new();

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

		// An element collected without a matching DetachNativeElement would leave its registration
		// behind forever, so drop the dead ones whenever the map grows.
		PruneCollectedHosts();
		_hosts[element.UnoElementId] = new(this);
	}

	public void DetachNativeElement(object content)
	{
		Debug.Assert(content is BrowserHtmlElement);
		var element = (BrowserHtmlElement)content;

		// Only drop the registration if it is still ours: when an element is recycled, the new host can
		// attach before the old one detaches, and unregistering then would silently kill its chaining.
		if (_hosts.TryGetValue(element.UnoElementId, out var registered)
			&& (!registered.TryGetTarget(out var host) || ReferenceEquals(host, this)))
		{
			_hosts.Remove(element.UnoElementId);
		}

		NativeMethods.DetachNativeElement(element.ElementId);
	}

	private static void PruneCollectedHosts()
	{
		List<nint>? collected = null;
		foreach (var (unoElementId, host) in _hosts)
		{
			if (!host.TryGetTarget(out _))
			{
				(collected ??= new()).Add(unoElementId);
			}
		}

		if (collected is not null)
		{
			foreach (var unoElementId in collected)
			{
				_hosts.Remove(unoElementId);
			}
		}
	}

	private static BrowserNativeElementHostingExtension? TryGetHost(nint unoElementId)
	{
		if (!_hosts.TryGetValue(unoElementId, out var weakExtension))
		{
			return null;
		}

		if (!weakExtension.TryGetTarget(out var extension))
		{
			_hosts.Remove(unoElementId);
			return null;
		}

		return extension;
	}

	internal static bool ApplyNegotiatedScroll(nint unoElementId, double horizontalDelta, double verticalDelta, bool isIntermediate)
	{
		if (TryGetHost(unoElementId) is not { } extension)
		{
			if (_log.IsEnabled(LogLevel.Warning))
			{
				_log.Warn($"Received a negotiated scroll for an unknown or collected native element ({unoElementId}).");
			}

			return false;
		}

		var result = ScrollViewer.ChainScrollFromDescendant(
			extension._presenter,
			horizontalDelta,
			verticalDelta,
			isIntermediate);

		if (!result.DidScroll && _log.IsEnabled(LogLevel.Debug))
		{
			_log.Debug($"No ScrollViewer consumed the negotiated scroll for {unoElementId} (h={horizontalDelta}, v={verticalDelta}).");
		}

		return result.DidScroll;
	}

	internal static void CompleteNegotiatedScroll(nint unoElementId)
	{
		if (TryGetHost(unoElementId) is { } extension)
		{
			ScrollViewer.CompleteChainedScrollFromDescendant(extension._presenter);
		}
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
