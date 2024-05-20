using System;
using Uno.Foundation.Extensibility;
using Windows.Foundation;
using Uno.Disposables;
using Uno.UI;

namespace Microsoft.UI.Xaml.Controls;

partial class ContentPresenter
{
	private Lazy<INativeElementHostingExtension> _nativeElementHostingExtension;

	partial void InitializePlatform()
	{
		_nativeElementHostingExtension = new Lazy<INativeElementHostingExtension>(() =>
		{
			ApiExtensibility.CreateInstance<INativeElementHostingExtension>(this, out var extension);
			return extension;
		});
	}

	private IDisposable _nativeElementDisposable;

	partial void TryRegisterNativeElement(object oldValue, object newValue)
	{
		if (IsNativeHost && IsLoaded)
		{
			DetachNativeElement(oldValue);
		}

		if (_nativeElementHostingExtension.Value?.IsNativeElement(newValue) ?? false)
		{
			IsNativeHost = true;

			if (ContentTemplate is not null)
			{
				throw new InvalidOperationException("ContentTemplate cannot be set when the Content is a native element");
			}
			if (ContentTemplateSelector is not null)
			{
				throw new InvalidOperationException("ContentTemplateSelector cannot be set when the Content is a native element");
			}

			if (IsLoaded)
			{
				//If loaded, attach immediately. If not, don't attach since OnLoaded will attach later.
				AttachNativeElement();
			}
		}
		else if (IsNativeHost)
		{
			IsNativeHost = false;
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

		Rect clippingBounds;
		if (ev.IsEmpty)
		{
			clippingBounds = new Rect(0, 0, 0, 0);
		}
		else if (ev.IsInfinite)
		{
			clippingBounds = null;
		}
		else
		{
			var top = Math.Min(Math.Max(0, ev.Y), ActualHeight);
			var height = Math.Max(0, Math.Min(ev.Height + ev.Y, ActualHeight - top));
			var left = Math.Min(Math.Max(0, ev.X), ActualWidth);
			var width = Math.Max(0, Math.Min(ev.Width + ev.X, ActualWidth - left));
			clippingBounds = new Rect(left, top, width, height);
		}

		_nativeElementHostingExtension.Value!.ArrangeNativeElement(
			XamlRoot,
			Content,
			arrangeRect,
			clippingBounds);
	}

	partial void AttachNativeElement()
	{
		global::System.Diagnostics.Debug.Assert(IsNativeHost && XamlRoot is not null);
		_nativeElementHostingExtension.Value!.AttachNativeElement(XamlRoot, Content);
		EffectiveViewportChanged += OnEffectiveViewportChanged;
		LayoutUpdated += OnLayoutUpdated;
		var opacityToken = RegisterPropertyChangedCallback(CalculatedOpacityProperty, OnCalculatedOpacityChanged);
		var visiblityToken = RegisterPropertyChangedCallback(HitTestVisibilityProperty, OnHitTestVisiblityChanged);
		_nativeElementDisposable = Disposable.Create(() =>
		{
			UnregisterPropertyChangedCallback(CalculatedOpacityProperty, opacityToken);
			UnregisterPropertyChangedCallback(HitTestVisibilityProperty, visiblityToken);
		});
	}

	partial void DetachNativeElement(object content)
	{
		global::System.Diagnostics.Debug.Assert(IsNativeHost);
		EffectiveViewportChanged -= OnEffectiveViewportChanged;
		LayoutUpdated -= OnLayoutUpdated;
		_nativeElementHostingExtension.Value!.DetachNativeElement(XamlRoot, content);
		_nativeElementDisposable?.Dispose();
	}

	private Size MeasureNativeElement(Size childMeasuredSize, Size availableSize)
	{
		global::System.Diagnostics.Debug.Assert(IsNativeHost);
		return _nativeElementHostingExtension.Value!.MeasureNativeElement(XamlRoot, Content, childMeasuredSize, availableSize);
	}

	private void OnCalculatedOpacityChanged(DependencyObject sender, DependencyProperty dp)
	{
		_nativeElementHostingExtension.Value!.ChangeNativeElementOpacity(XamlRoot, Content, CalculatedOpacity);
	}

	private void OnHitTestVisiblityChanged(DependencyObject sender, DependencyProperty dp)
	{
		_nativeElementHostingExtension.Value!.ChangeNativeElementVisibility(XamlRoot, Content, HitTestVisibility != HitTestability.Collapsed);
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
		return _nativeElementHostingExtension.Value?.CreateSampleComponent(XamlRoot, text);
	}
}
