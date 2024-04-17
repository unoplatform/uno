using System;
using Uno.Foundation.Extensibility;
using Windows.Foundation;
using Uno.Disposables;

namespace Microsoft.UI.Xaml.Controls;

partial class ContentPresenter
{
	private static readonly Lazy<INativeElementHostingExtension> _nativeElementHostingExtension = new Lazy<INativeElementHostingExtension>(() =>
	{
		ApiExtensibility.CreateInstance<INativeElementHostingExtension>(typeof(ContentPresenter), out var extension);
		return extension;
	});

	private IDisposable _nativeElementDisposable;

	partial void TryRegisterNativeElement(object newValue)
	{
		if (IsNativeHost && IsLoaded)
		{
			DetachNativeElement(); // remove the old native element
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
		global::System.Diagnostics.Debug.Assert(IsNativeHost);
		var arrangeRect = TransformToVisual(null).TransformBounds(LayoutSlotWithMarginsAndAlignments);
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

	partial void DetachNativeElement()
	{
		global::System.Diagnostics.Debug.Assert(IsNativeHost);
		EffectiveViewportChanged -= OnEffectiveViewportChanged;
		LayoutUpdated -= OnLayoutUpdated;
		_nativeElementHostingExtension.Value!.DetachNativeElement(XamlRoot, Content);
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
		ArrangeNativeElement();
	}

	internal static object CreateSampleComponent(XamlRoot root, string text)
		=> _nativeElementHostingExtension.Value?.CreateSampleComponent(root, text);
}
