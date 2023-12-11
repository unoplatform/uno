using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Uno.Extensions;
using Uno.UI;
using Uno.UI.DataBinding;
using Windows.Foundation;
using Windows.UI.Core;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;

namespace Microsoft.UI.Xaml.Controls
{
	/// <summary>
	/// Declares a Content presenter
	/// </summary>
	/// <remarks>
	/// The content presenter is used for compatibility with WPF concepts,
	/// but the ContentSource property is not available, because there are ControlTemplates for now.
	/// </remarks>
	public partial class ContentPresenter : FrameworkElement
	{
		private BorderLayerRenderer _borderRenderer = new BorderLayerRenderer();
		private Rect? _lastArrangeRect;
		private Rect? _lastGlobalRect;
		private double? _lastOpacity;
		private bool? _lastVisiblity;
		private Rect? _lastClipRect;
		private Rect? _clipRect;
		private bool _nativeHostRegistered;

		// TODO: do we have a cleaner way to do this?
		public static Dictionary<object, IDisposable> NativeRenderDisposables { get; } = new();

		public ContentPresenter()
		{
			InitializeContentPresenter();

			Loaded += (s, e) => RegisterNativeHostSupport();
			Unloaded += (s, e) => UnregisterNativeHostSupport();
			LayoutUpdated += (s, e) => UpdateBorder();
			EffectiveViewportChanged += OnEffectiveViewportChanged;
		}

		private void SetUpdateTemplate()
		{
			UpdateContentTemplateRoot();
		}

		partial void RegisterContentTemplateRoot()
		{
			AddChild(ContentTemplateRoot);
		}

		partial void UnregisterContentTemplateRoot()
		{
			RemoveChild(ContentTemplateRoot);
		}

		partial void TryRegisterNativeElement(object newValue)
		{
			if (CoreWindow.Main.IsNativeElement(newValue))
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

				RegisterNativeHostSupport();
			}
			else if (IsNativeHost)
			{
				IsNativeHost = false;
				UnregisterNativeHostSupport();
			}
		}

		void RegisterNativeHostSupport()
		{
			if (IsNativeHost && XamlRoot is not null)
			{
				XamlRoot.InvalidateRender += UpdateNativeElementPosition;
				_nativeHostRegistered = true;
			}
		}

		void UnregisterNativeHostSupport()
		{
			if (_nativeHostRegistered)
			{
				XamlRoot.InvalidateRender -= UpdateNativeElementPosition;
				_nativeHostRegistered = false;

				NativeRenderDisposables.Remove(Content, out var disposable);
				disposable?.Dispose();
			}
		}

		private void UpdateCornerRadius(CornerRadius radius) => UpdateBorder();

		private void UpdateBorder()
		{
			if (IsLoaded)
			{
				_borderRenderer.UpdateLayer(
					this,
					Background,
					BackgroundSizing,
					BorderThickness,
					BorderBrush,
					CornerRadius,
					null
				);
			}
		}

		private void ClearBorder()
		{
			_borderRenderer.Clear();
		}

		partial void OnPaddingChangedPartial(Thickness oldValue, Thickness newValue)
		{
			UpdateBorder();
		}

		partial void ArrangeNativeElement(Rect arrangeRect)
		{
			if (IsNativeHost)
			{
				_lastArrangeRect = arrangeRect;

				UpdateNativeElementPosition();
			}
		}

		partial void TryAttachNativeElement()
		{
			if (IsNativeHost)
			{
				CoreWindow.Main.AttachNativeElement(XamlRoot, Content);
			}
		}

		partial void TryDetachNativeElement()
		{
			if (IsNativeHost)
			{
				CoreWindow.Main.DetachNativeElement(XamlRoot, Content);
			}
		}

		private Size MeasureNativeElement(Size childMeasuredSize, Size availableSize)
		{
			if (IsNativeHost)
			{
				return CoreWindow.Main.MeasureNativeElement(XamlRoot, Content, childMeasuredSize, availableSize);
			}
			else
			{
				return childMeasuredSize;
			}
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

					CoreWindow.Main.ArrangeNativeElement(XamlRoot, Content, globalRect, _clipRect);
					CoreWindow.Main.ChangeNativeElementOpacity(XamlRoot, Content, CalculatedOpacity);
					// TODO: revise if HitTestVisibility is good enough or maybe we need to add a new CalculatedVisibility property
					CoreWindow.Main.ChangeNativeElementVisiblity(XamlRoot, Content, HitTestVisibility != HitTestability.Collapsed);
				}
			}
		}

		private void OnEffectiveViewportChanged(FrameworkElement sender, EffectiveViewportChangedEventArgs args)
		{
			if (IsNativeHost)
			{
				var ev = args.EffectiveViewport;

				if (ev.IsEmpty)
				{
					_clipRect = new Rect(0, 0, 0, 0);
				} else if (ev.IsInfinite)
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
		}
	}
}
