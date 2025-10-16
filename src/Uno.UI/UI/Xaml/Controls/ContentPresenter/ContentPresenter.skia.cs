using System;
using System.Collections.Generic;
using Uno.Disposables;
using Uno.Foundation.Extensibility;
using Uno.Foundation.Logging;
using Uno.UI;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls;

partial class ContentPresenter
{
	private Lazy<INativeElementHostingExtension> _nativeElementHostingExtension;
	private static readonly HashSet<ContentPresenter> _nativeHosts = new();

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

	private IDisposable _nativeElementDisposable;

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

	private void ArrangeNativeElement()
	{
		if (!IsNativeHost)
		{
			// the ArrangeNativeElement call is queued on the dispatcher, so by the time we get here, the ContentPresenter
			// might no longer be a NativeHost
			return;
		}
		var arrangeRect = this.GetAbsoluteBoundsRect();
		var ev = GetParentViewport().Effective;

		Rect clipRect;
		if (ev.IsEmpty)
		{
			clipRect = new Rect(0, 0, 0, 0);
		}
		else if (ev.IsInfinite)
		{
			clipRect = null;
		}
		else
		{
			var top = Math.Min(Math.Max(0, ev.Y), ActualHeight);
			var height = Math.Max(0, Math.Min(ev.Height + ev.Y, ActualHeight - top));
			var left = Math.Min(Math.Max(0, ev.X), ActualWidth);
			var width = Math.Max(0, Math.Min(ev.Width + ev.X, ActualWidth - left));
			clipRect = new Rect(left, top, width, height);
		}

		var clipInGlobalCoordinates = new Rect(
			arrangeRect.X + clipRect.X,
			arrangeRect.Y + clipRect.Y,
			clipRect.Width,
			clipRect.Height);

		_nativeElementHostingExtension.Value!.ArrangeNativeElement(
			Content,
			arrangeRect,
			clipRect);
	}

	partial void AttachNativeElement()
	{
#if DEBUG
		global::System.Diagnostics.Debug.Assert(IsNativeHost && XamlRoot is not null && !_nativeElementAttached);
		_nativeElementAttached = true;
#endif
		_nativeElementHostingExtension.Value!.AttachNativeElement(Content);
		_nativeHosts.Add(this);
		EffectiveViewportChanged += OnEffectiveViewportChanged;
		LayoutUpdated += OnLayoutUpdated;
		var visiblityToken = RegisterPropertyChangedCallback(HitTestVisibilityProperty, OnHitTestVisiblityChanged);
		_nativeElementDisposable = Disposable.Create(() =>
		{
			UnregisterPropertyChangedCallback(HitTestVisibilityProperty, visiblityToken);
		});
	}

	partial void DetachNativeElement(object content)
	{
#if DEBUG
		global::System.Diagnostics.Debug.Assert(IsNativeHost && _nativeElementAttached);
		_nativeElementAttached = false;
#endif
		_nativeHosts.Remove(this);
		EffectiveViewportChanged -= OnEffectiveViewportChanged;
		LayoutUpdated -= OnLayoutUpdated;
		_nativeElementHostingExtension.Value!.DetachNativeElement(content);
		_nativeElementDisposable?.Dispose();
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

	private void OnHitTestVisiblityChanged(DependencyObject sender, DependencyProperty dp)
	{
		_nativeElementHostingExtension.Value!.ChangeNativeElementVisibility(Content, HitTestVisibility != HitTestability.Collapsed);
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

	private void OnLayoutUpdated(object sender, object e)
	{
		// Not quite sure why we need to queue the arrange call, but the native element either explodes or doesn't
		// respect alignments correctly otherwise. This is particularly relevant for the initial load.
		DispatcherQueue.TryEnqueue(ArrangeNativeElement);
	}

	private void OnEffectiveViewportChanged(FrameworkElement sender, EffectiveViewportChangedEventArgs args)
	{
		global::System.Diagnostics.Debug.Assert(IsNativeHost);
		// The arrange call here is queued because EVPChanged is fired before the layout of the ContentPresenter is updated,
		// so calling ArrangeNativeElement synchronously would get outdated coordinates.
		DispatcherQueue.TryEnqueue(ArrangeNativeElement);
	}

	internal object CreateSampleComponent(string text)
	{
		return _nativeElementHostingExtension.Value?.CreateSampleComponent(text);
	}
}
