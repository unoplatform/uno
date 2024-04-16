using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Uno.Extensions;
using Uno.Foundation.Extensibility;
using Uno.UI;
using Uno.UI.DataBinding;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using Windows.UI.Core;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Uno.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Controls;

partial class ContentPresenter
{
	private static readonly Lazy<INativeElementHostingExtension> _nativeElementHostingExtension = new Lazy<INativeElementHostingExtension>(() =>
	{
		ApiExtensibility.CreateInstance<INativeElementHostingExtension>(typeof(ContentPresenter), out var extension);
		return extension;
	});

	private Rect? _lastArrangeRect;
	private Rect? _lastGlobalRect;
	private double? _lastOpacity;
	private bool? _lastVisiblity;
	private Rect? _lastClipRect;
	private Rect? _clipRect;

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
				//If loaded, attach immediately. If not, wait for OnLoaded to attach, since
				// XamlRoot might not be set at this moment.
				AttachNativeElement();
			}
		}
		else if (IsNativeHost)
		{
			IsNativeHost = false;
		}
	}

	partial void ArrangeNativeElement(Rect arrangeRect)
	{
		global::System.Diagnostics.Debug.Assert(IsNativeHost);
		_lastArrangeRect = arrangeRect;
		UpdateNativeElementPosition();
	}

	partial void AttachNativeElement()
	{
		global::System.Diagnostics.Debug.Assert(IsNativeHost && XamlRoot is not null);
		_nativeElementHostingExtension.Value!.AttachNativeElement(XamlRoot, Content);
		XamlRoot.InvalidateRender += UpdateNativeElementPosition;
		EffectiveViewportChanged += OnEffectiveViewportChanged;
	}

	partial void DetachNativeElement()
	{
		global::System.Diagnostics.Debug.Assert(IsNativeHost);
		XamlRoot.InvalidateRender -= UpdateNativeElementPosition;
		EffectiveViewportChanged -= OnEffectiveViewportChanged;
		_nativeElementHostingExtension.Value!.DetachNativeElement(XamlRoot, Content);
	}

	private Size MeasureNativeElement(Size childMeasuredSize, Size availableSize)
	{
		global::System.Diagnostics.Debug.Assert(IsNativeHost);
		return _nativeElementHostingExtension.Value!.MeasureNativeElement(XamlRoot, Content, childMeasuredSize, availableSize);
	}

	private void UpdateNativeElementPosition()
	{
		if (_lastArrangeRect is { } lastArrangeRect)
		{
			var globalPosition = TransformToVisual(null).TransformPoint(lastArrangeRect.Location);
			var globalRect = new Rect(globalPosition, lastArrangeRect.Size);

			if (_lastGlobalRect != globalRect ||
				_lastOpacity != CalculatedOpacity ||
				_lastVisiblity != (HitTestVisibility != HitTestability.Collapsed) ||
				_lastClipRect != _clipRect)
			{
				_lastGlobalRect = globalRect;
				_lastOpacity = CalculatedOpacity;
				_lastVisiblity = HitTestVisibility != HitTestability.Collapsed;
				_lastClipRect = _clipRect;

				_nativeElementHostingExtension.Value!.ArrangeNativeElement(XamlRoot, Content, globalRect, _clipRect);
				_nativeElementHostingExtension.Value!.ChangeNativeElementOpacity(XamlRoot, Content, CalculatedOpacity);
				// TODO: revise if HitTestVisibility is good enough or maybe we need to add a new CalculatedVisibility property
				_nativeElementHostingExtension.Value!.ChangeNativeElementVisibility(XamlRoot, Content, HitTestVisibility != HitTestability.Collapsed);
			}
		}
	}

	private void OnEffectiveViewportChanged(FrameworkElement sender, EffectiveViewportChangedEventArgs args)
	{
		global::System.Diagnostics.Debug.Assert(IsNativeHost);
		var ev = args.EffectiveViewport;

		if (ev.IsEmpty)
		{
			_clipRect = new Rect(0, 0, 0, 0);
		}
		else if (ev.IsInfinite)
		{
			_clipRect = null;
		}
		else
		{
			var top = Math.Min(Math.Max(0, ev.Y), ActualHeight);
			var height = Math.Max(0, Math.Min(ev.Height + ev.Y, ActualHeight - top));
			var left = Math.Min(Math.Max(0, ev.X), ActualWidth);
			var width = Math.Max(0, Math.Min(ev.Width + ev.X, ActualWidth - left));
			_clipRect = new Rect(left, top, width, height);
		}

		UpdateNativeElementPosition();
	}

	internal static object CreateSampleComponent(string text)
		=> _nativeElementHostingExtension.Value?.CreateSampleComponent(text);
}
