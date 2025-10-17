using System;
using System.Collections.Generic;
using System.Linq;
using Uno.Disposables;
using Uno.Foundation.Extensibility;
using Uno.Foundation.Logging;
using Uno.UI;
using Windows.Foundation;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml.Media;

namespace Microsoft.UI.Xaml.Controls;

partial class ContentPresenter
{
	private Lazy<INativeElementHostingExtension> _nativeElementHostingExtension;
	private static readonly HashSet<ContentPresenter> _nativeHosts = new();

	private (Rect layoutRect, int zOrder)? _lastNativeArrangeArgs;
#if DEBUG
	private bool _nativeElementAttached;
#endif

	internal static bool HasNativeElements() => _nativeHosts.Count > 0;

	partial void InitializePlatform()
	{
		_nativeElementHostingExtension = new Lazy<INativeElementHostingExtension>(() =>
		{
			try
			{
				ApiExtensibility.CreateInstance<INativeElementHostingExtension>(this, out var extension);
				return extension;
			}
			catch (Exception e) // this catches weird cases like an enqueued Loaded event on a ContentPresenter that dispatches after the window of that ContentPresenter is closed
			{
				if (this.Log().IsEnabled(LogLevel.Error))
				{
					this.Log().LogError($"Couldn't create an {nameof(INativeElementHostingExtension)}.", e);
				}
				return null;
			}
		});
	}

	partial void TryRegisterNativeElement(object oldValue, object newValue)
	{
		if (IsNativeHost && IsInLiveTree)
		{
			DetachNativeElement(oldValue);
		}

		if (_nativeElementHostingExtension.Value?.IsNativeElement(newValue) ?? false)
		{
			IsNativeHost = true;
			Visual.SetAsNativeHostVisual(true);

			if (ContentTemplate is not null)
			{
				throw new InvalidOperationException("ContentTemplate cannot be set when the Content is a native element");
			}
			if (ContentTemplateSelector is not null)
			{
				throw new InvalidOperationException("ContentTemplateSelector cannot be set when the Content is a native element");
			}

			if (IsInLiveTree)
			{
				//If in visual tree, attach immediately. If not, don't attach since Enter will attach later.
				AttachNativeElement();
			}
		}
		else
		{
			IsNativeHost = false;
			Visual.SetAsNativeHostVisual(null);
		}
	}

	private void ArrangeNativeElement(int zOrder)
	{
		if (!IsNativeHost)
		{
			// the ArrangeNativeElement call is queued on the dispatcher, so by the time we get here, the ContentPresenter
			// might no longer be a NativeHost
			return;
		}
		var arrangeRect = this.GetAbsoluteBoundsRect();

		var nativeArrangeArgs = (arrangeRect, zOrder);
		if (_lastNativeArrangeArgs != nativeArrangeArgs)
		{
			_lastNativeArrangeArgs = nativeArrangeArgs;
			_nativeElementHostingExtension.Value!.ArrangeNativeElement(
				Content,
				arrangeRect);
		}
	}

	partial void AttachNativeElement()
	{
#if DEBUG
		global::System.Diagnostics.Debug.Assert(IsNativeHost && XamlRoot is not null && !_nativeElementAttached);
		_nativeElementAttached = true;
#endif
		_nativeElementHostingExtension.Value!.AttachNativeElement(Content);
		_nativeHosts.Add(this);
	}

	partial void DetachNativeElement(object content)
	{
#if DEBUG
		global::System.Diagnostics.Debug.Assert(IsNativeHost && _nativeElementAttached);
		_nativeElementAttached = false;
#endif
		_lastNativeArrangeArgs = null;
		_nativeHosts.Remove(this);
		_nativeElementHostingExtension.Value!.DetachNativeElement(content);
	}

	private Size MeasureNativeElement(Size childMeasuredSize, Size availableSize)
	{
		global::System.Diagnostics.Debug.Assert(IsNativeHost);
		var ret = _nativeElementHostingExtension.Value!.MeasureNativeElement(Content, childMeasuredSize, availableSize);
		if (ret.Width is double.PositiveInfinity or double.NaN)
		{
			ret.Width = 0;
		}
		if (ret.Height is double.PositiveInfinity or double.NaN)
		{
			ret.Height = 0;
		}
		return ret;
	}

	internal static void UpdateNativeHostContentPresentersOpacities()
	{
		foreach (var contentPresenter in _nativeHosts)
		{
			double finalOpacity = 1;
			UIElement parent = contentPresenter;
			while (parent is not null)
			{
				finalOpacity *= parent.Opacity;
				parent = parent.GetParent() as UIElement;
			}

			contentPresenter._nativeElementHostingExtension!.Value.ChangeNativeElementOpacity(contentPresenter.Content, finalOpacity);
		}
	}

	/// <remarks>
	/// <see cref="visualToLayoutRect"/> is read-only and won't be modified.
	/// </remarks>
	internal static void OnFrameRendered(List<Visual> visualToLayoutRect)
	{
		foreach (var host in _nativeHosts.Where(cp => !visualToLayoutRect.Contains(cp.Visual)).ToList())
		{
			host.DetachNativeElement(host.Content);
		}

		for (var index = 0; index < visualToLayoutRect.Count; index++)
		{
			var entry = visualToLayoutRect[index];
			var host = (ContentPresenter)((ContainerVisual)entry).Owner!.Target;
			if (host._lastNativeArrangeArgs is { } lastNativeArrangeArgs && lastNativeArrangeArgs.zOrder != index)
			{
				host.DetachNativeElement(host.Content);
				host.AttachNativeElement();
				host.ArrangeNativeElement(index);
			}
			else if (!host._nativeElementAttached)
			{
				host.AttachNativeElement();
				host.ArrangeNativeElement(index);
			}
			else
			{
				host.ArrangeNativeElement(index);
			}
		}
	}

	internal object CreateSampleComponent(string text)
	{
		return _nativeElementHostingExtension.Value?.CreateSampleComponent(text);
	}
}
