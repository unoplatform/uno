// #define LOG_LAYOUT

using Windows.UI.Xaml.Media;
using Microsoft.Extensions.Logging;
using Uno.UI;
#if !__WASM__
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Uno.Extensions;
using Uno;
using Uno.Logging;
using Uno.Collections;
using static System.Double;
using static System.Math;
using static Uno.UI.LayoutHelper;
using Uno.Diagnostics.Eventing;
using Windows.Foundation;
#if XAMARIN_ANDROID
using Android.Views;
using View = Android.Views.View;
using Font = Android.Graphics.Typeface;
#elif XAMARIN_IOS_UNIFIED
using View = UIKit.UIView;
using Color = UIKit.UIColor;
using Font = UIKit.UIFont;
using CoreGraphics;
#elif __MACOS__
using View = AppKit.NSView;
using Color = AppKit.NSColor;
using Font = AppKit.NSFont;
using CoreGraphics;
#elif XAMARIN_IOS
using CoreGraphics;
using View = MonoTouch.UIKit.UIView;
using Color = MonoTouch.UIKit.UIColor;
using Font = MonoTouch.UIKit.UIFont;
#elif NET461 || __WASM__
using View = Windows.UI.Xaml.UIElement;
#endif

namespace Windows.UI.Xaml.Controls
{
	public abstract partial class Layouter : ILayouter
	{
		private static readonly IEventProvider _trace = Tracing.Get(FrameworkElement.TraceProvider.Id);
		private readonly ILogger _logDebug;

		private readonly Size MaxSize = new Size(double.PositiveInfinity, double.PositiveInfinity);

		private Size _unclippedDesiredSize;

		private const double SIZE_EPSILON = 0.05d;

		public IFrameworkElement Panel { get; }

		protected Layouter(IFrameworkElement element)
		{
			Panel = element;

			var log = this.Log();
			if (log.IsEnabled(LogLevel.Debug))
			{
				_logDebug = log;
			}
		}

		/// <summary>
		/// Determine the size of the panel.
		/// </summary>
		/// <param name="availableSize">The available size, in logical pixels.</param>
		/// <returns>The size of the panel, in logical pixel.</returns>
		public Size Measure(Size availableSize)
		{
			IDisposable traceActivity = null;
			if (_trace.IsEnabled)
			{
				traceActivity = _trace.WriteEventActivity(
					FrameworkElement.TraceProvider.FrameworkElement_MeasureStart,
					FrameworkElement.TraceProvider.FrameworkElement_MeasureStop,
					new object[] { LoggingOwnerTypeName, Panel.GetDependencyObjectId() }
				);
			}

			if (Panel.Visibility == Visibility.Collapsed)
			{
				// A collapsed element should not be measured at all
				return default;
			}

			using (traceActivity)
			{
				var (minSize, maxSize) = Panel.GetMinMax();

				var marginSize = Panel.GetMarginSize();

				// NaN values are accepted as input here, particularly when coming from
				// SizeThatFits in Image or Scrollviewer. Clamp the value here as it is reused
				// below for the clipping value.
				availableSize = availableSize
					.NumberOrDefault(MaxSize);

				var frameworkAvailableSize = availableSize
					.Subtract(marginSize)
					.AtLeast(default) // 0.0,0.0
					.AtMost(maxSize);

				var desiredSize = MeasureOverride(frameworkAvailableSize);

				_logDebug?.LogTrace($"{this}.MeasureOverride(availableSize={frameworkAvailableSize}): desiredSize={desiredSize}");

				if (
					double.IsNaN(desiredSize.Width)
					|| double.IsNaN(desiredSize.Height)
					|| double.IsInfinity(desiredSize.Width)
					|| double.IsInfinity(desiredSize.Height)
				)
				{
					throw new InvalidOperationException($"{this}: Invalid measured size {desiredSize}. NaN or Infinity are invalid desired size.");
				}

				desiredSize = desiredSize.AtLeast(minSize);

				_unclippedDesiredSize = desiredSize;

				desiredSize = desiredSize.AtMost(maxSize);

				// DesiredSize must include margins
				// However, we return the size to the parent without the margins
				SetDesiredChildSize(Panel as View, desiredSize.Add(marginSize));

				var clippedDesiredSize = desiredSize

					.AtMost(availableSize)
					.AtLeast(new Size(0, 0));

				return clippedDesiredSize;
			}
		}

		private static bool IsLessThanAndNotCloseTo(double a, double b)
		{
			return a < b - SIZE_EPSILON;
		}

		/// <summary>
		/// Places the children of the panel using a specific size, in logical pixels.
		/// </summary>
		public void Arrange(Rect finalRect)
		{
			var uiElement = Panel as UIElement;

			if (uiElement != null)
			{
				uiElement.LayoutSlot = finalRect;
			}

			IDisposable traceActivity = null;
			if (_trace.IsEnabled)
			{
				traceActivity = _trace.WriteEventActivity(
					FrameworkElement.TraceProvider.FrameworkElement_ArrangeStart,
					FrameworkElement.TraceProvider.FrameworkElement_ArrangeStop,
					new object[] { LoggingOwnerTypeName, Panel.GetDependencyObjectId() }
				);
			}
			using (traceActivity)
			{
				if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
				{
					this.Log().DebugFormat("[{0}/{1}] Arrange({2}/{3}/{4}/{5})", LoggingOwnerTypeName, Name, GetType(), Panel.Name, finalRect, Panel.Margin);
				}

				var arrangeSize = finalRect.Size;

				bool allowClipToSlot;
				bool needsClipToSlot;

				if (Panel is ICustomClippingElement customClippingElement)
				{
					// Some controls may control itself how clipping is applied
					allowClipToSlot = customClippingElement.AllowClippingToLayoutSlot;
					needsClipToSlot = customClippingElement.ForceClippingToLayoutSlot;
				}
				else
				{
					allowClipToSlot = true;
					needsClipToSlot = false;
				}

				_logDebug?.Debug($"{this}: InnerArrangeCore({finalRect}) - allowClip={allowClipToSlot}, arrangeSize={arrangeSize}, _unclippedDesiredSize={_unclippedDesiredSize}, forcedClipping={needsClipToSlot}");

				if (allowClipToSlot && !needsClipToSlot)
				{
					if (IsLessThanAndNotCloseTo(arrangeSize.Width, _unclippedDesiredSize.Width))
					{
						_logDebug?.Debug($"{this}: (arrangeSize.Width) {arrangeSize.Width} < {_unclippedDesiredSize.Width}: NEEDS CLIPPING.");
						needsClipToSlot = true;
					}

					else if (IsLessThanAndNotCloseTo(arrangeSize.Height, _unclippedDesiredSize.Height))
					{
						_logDebug?.Debug($"{this}: (arrangeSize.Height) {arrangeSize.Height} < {_unclippedDesiredSize.Height}: NEEDS CLIPPING.");
						needsClipToSlot = true;
					}
				}

				var (minSize, maxSize) = this.Panel.GetMinMax();

				arrangeSize = arrangeSize
					.AtLeast(default); // 0.0,0.0

				// We have to choose max between _unclippedDesiredSize and maxSize here, because
				// otherwise setting of max property could cause arrange at less then _unclippedDesiredSize.
				// Clipping by Max is needed to limit stretch here
				var effectiveMaxSize = Max(_unclippedDesiredSize, maxSize);

				if (allowClipToSlot)
				{
					if (IsLessThanAndNotCloseTo(effectiveMaxSize.Width, arrangeSize.Width))
					{
						_logDebug?.Debug($"{this}: (effectiveMaxSize.Width) {effectiveMaxSize.Width} < {arrangeSize.Width}: NEEDS CLIPPING.");
						needsClipToSlot = true;
						arrangeSize.Width = effectiveMaxSize.Width;
					}

					if (IsLessThanAndNotCloseTo(effectiveMaxSize.Height, arrangeSize.Height))
					{
						_logDebug?.Debug($"{this}: (effectiveMaxSize.Height) {effectiveMaxSize.Height} < {arrangeSize.Height}: NEEDS CLIPPING.");
						needsClipToSlot = true;
						arrangeSize.Height = effectiveMaxSize.Height;
					}
				}

				var renderSize = ArrangeOverride(arrangeSize);

				if (uiElement != null)
				{
					uiElement.RenderSize = renderSize.AtMost(maxSize);
					uiElement.NeedsClipToSlot = needsClipToSlot;
					uiElement.ApplyClip();

					if (Panel is FrameworkElement fe)
					{
						fe.OnLayoutUpdated();
					}
				}
			}
		}


		/// <summary>
		/// Determine the size of the panel.
		/// </summary>
		/// <param name="availableSize">The available size, in logical pixels.</param>
		/// <returns>The size of the panel, in logical pixel.</returns>
		protected abstract Size MeasureOverride(Size availableSize);

		/// <summary>
		/// Places the children of the panel using a specific size, in logical pixels.
		/// </summary>
		/// <param name="finalSize">The final panel size</param>
		protected abstract Size ArrangeOverride(Size finalSize);

		/// <summary>
		/// Sets the desired child size back on the view. (Used in iOS which does not store measured size)
		/// </summary>
		partial void SetDesiredChildSize(View view, Size desiredSize);

		protected Size MeasureChild(View view, Size slotSize)
		{
			var frameworkElement = view as IFrameworkElement;
			var ret = default(Size);

			// NaN values are accepted as input for MeasureOverride, but are treated as Infinity.
			slotSize = slotSize.NumberOrDefault(MaxSize);

			if (frameworkElement?.Visibility == Visibility.Collapsed)
			{
				// By default iOS views measure to normal size, even if they're hidden.
				// We want the collapsed behavior, so we return a 0,0 size instead.

				// Note: Visibility is checked in both Measure and MeasureChild, since some IFrameworkElement children may not have their own Layouter
				SetDesiredChildSize(view, ret);

				if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
				{
					var viewName = frameworkElement.SelectOrDefault(f => f.Name, "NativeView");

					this.Log().DebugFormat(
						"[{0}/{1}] MeasureChild(HIDDEN/{2}/{3}/{4}/{5}) = {6}",
						LoggingOwnerTypeName,
						Name,
						view.GetType(),
						viewName,
						slotSize,
						frameworkElement.Margin,
						ret
					);
				}

				return ret;
			}

			if (frameworkElement != null
				&& !(frameworkElement is FrameworkElement)
				&& !(frameworkElement is Image)
				)
			{
				// For IFrameworkElement implementers that are not FrameworkElements, the constraint logic must
				// be performed by the parent. Otherwise, the native element will take the size it needs without
				// regards to explicit XAML size characteristics. The Android ProgressBar is a good example of
				// that behavior.

				var margin = frameworkElement.Margin;

				if (margin != Thickness.Empty)
				{
					// Apply the margin for framework elements, as if it were padding to the child.
					slotSize = new Size(
						Max(0, slotSize.Width - margin.Left - margin.Right),
						Max(0, slotSize.Height - margin.Top - margin.Bottom)
					);
				}

				// Alias the Dependency Properties values to avoid double calls.
				var childWidth = frameworkElement.Width;
				var childMaxWidth = frameworkElement.MaxWidth;
				var childHeight = frameworkElement.Height;
				var childMaxHeight = frameworkElement.MaxHeight;

				var optionalMaxWidth = !IsInfinity(childMaxWidth) && !IsNaN(childMaxWidth) ? childMaxWidth : (double?)null;
				var optionalWidth = !IsNaN(childWidth) ? childWidth : (double?)null;
				var optionalMaxHeight = !IsInfinity(childMaxHeight) && !IsNaN(childMaxHeight) ? childMaxHeight : (double?)null;
				var optionalHeight = !IsNaN(childHeight) ? childHeight : (double?)null;

				// After the margin has been removed, ensure the remaining space slot does not go
				// over the explicit or maximum size of the child.
				if (optionalMaxWidth != null || optionalWidth != null)
				{
					var constrainedWidth = Min(
						optionalMaxWidth ?? double.PositiveInfinity,
						optionalWidth ?? double.PositiveInfinity
					);

					slotSize.Width = Min(slotSize.Width, constrainedWidth);
				}

				if (optionalMaxHeight != null || optionalHeight != null)
				{
					var constrainedHeight = Min(
						optionalMaxHeight ?? double.PositiveInfinity,
						optionalHeight ?? double.PositiveInfinity
					);

					slotSize.Height = Min(slotSize.Height, constrainedHeight);
				}
			}

			ret = MeasureChildOverride(view, slotSize);

			if (
				IsPositiveInfinity(ret.Height) || IsPositiveInfinity(ret.Width)
			)
			{
				if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Error))
				{
					var viewName = frameworkElement.SelectOrDefault(f => f.Name, "NativeView");
					var margin = frameworkElement.SelectOrDefault(f => f.Margin, Thickness.Empty);

					this.Log().ErrorFormat(
						"[{0}/{1}] MeasureChild({2}/{3}/{4}/{5}) = Child returned INFINITY {6}",
						LoggingOwnerTypeName,
						Name,
						view.GetType(),
						viewName,
						slotSize,
						margin,
						ret
					);
				}

				ret = new Size(0, 0);
			}
			else
			{
				if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
				{
					var viewName = frameworkElement.SelectOrDefault(f => f.Name, "NativeView");
					var margin = frameworkElement.SelectOrDefault(f => f.Margin, Thickness.Empty);

					this.Log().DebugFormat(
						"[{0}/{1}] MeasureChild({2}/{3}/{4}/{5}) = {6}",
						LoggingOwnerTypeName,
						Name,
						view.GetType(),
						viewName,
						slotSize,
						margin,
						ret
					);
				}
			}

			if (frameworkElement != null && frameworkElement.Visibility != Visibility.Collapsed)
			{
				var margin = frameworkElement.Margin;

				if (margin != Thickness.Empty)
				{
					// Report the size to the parent without the margin, only if the
					// size has changed or that the control required a measure
					//
					// This condition is required because of the measure caching that
					// some systems apply (Like android UI).
					ret = new Size(
						ret.Width + margin.Left + margin.Right,
						ret.Height + margin.Top + margin.Bottom
					);
				}
			}

			SetDesiredChildSize(view, ret);

			return ret;
		}

		/// <summary>
		/// Arranges the location a child in the current panel
		/// </summary>
		/// <param name="view">The child instance</param>
		/// <param name="frame">The rectangle to use, in Logical position</param>
		public void ArrangeChild(View view, Rect frame)
		{
			ArrangeChild(view, frame, true);
		}

		internal void ArrangeChild(View view, Rect frame, bool raiseLayoutUpdated)
		{
			if ((view as IFrameworkElement)?.Visibility == Visibility.Collapsed)
			{
				return;
			}
			frame = ApplyMarginAndAlignments(view, frame);

			ArrangeChildOverride(view, frame);

			if (raiseLayoutUpdated && view is FrameworkElement fe)
			{
				fe?.OnLayoutUpdated();
			}
		}

		private void LogArrange(View view, Rect frame)
		{
			if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
			{
				var viewName = (view as IFrameworkElement).SelectOrDefault(f => f.Name, "NativeView");
				var margin = (view as IFrameworkElement).SelectOrDefault(f => f.Margin, Thickness.Empty);

				this.Log().DebugFormat("[{0}/{1}] ArrangeChild({2}/{3}/{4}/{5})", LoggingOwnerTypeName, Name, view.GetType(), viewName, frame, margin);
			}

		}

		protected Thickness MarginChild(View view)
		{
			if (view is IFrameworkElement frameworkElement)
			{
				return frameworkElement.Margin;
			}
			else
			{
				return Thickness.Empty;
			}
		}

		protected abstract string Name { get; }

		private Rect ApplyMarginAndAlignments(View view, Rect frame)
		{
			// In this implementation, since we do not have the ability to intercept proprely the measure and arrange
			// because of the type of hierarchy (inheriting from native views), we must apply the margins and alignements
			// from within the panel to its children. This makes the authoring of custom panels that do not inherit from
			// Panel that do not use this helper a bit more complex, but for all other panels that use this
			// layouter, the logic is implied.

			if (view is IFrameworkElement frameworkElement)
			{
				// Apply the margin for framework elements, as if it were padding to the child.
				double x = frame.X;
				double y = frame.Y;
				double width = frame.Width;
				double height = frame.Height;

				// capture the child's state to avoid getting DependencyProperties values multiple times.
				var childVerticalAlignment = frameworkElement.VerticalAlignment;
				var childHorizontalAlignment = frameworkElement.HorizontalAlignment;

				AdjustAlignment(view, ref childHorizontalAlignment, ref childVerticalAlignment);

				var childMaxHeight = frameworkElement.MaxHeight;
				var childMaxWidth = frameworkElement.MaxWidth;
				var childMinHeight = frameworkElement.MinHeight;
				var childMinWidth = frameworkElement.MinWidth;
				var childWidth = frameworkElement.Width;
				var childHeight = frameworkElement.Height;
				var childMargin = frameworkElement.Margin;
				var hasChildHeight = !IsNaN(childHeight);
				var hasChildWidth = !IsNaN(childWidth);
				var hasChildMaxWidth = !IsInfinity(childMaxWidth) && !IsNaN(childMaxWidth);
				var hasChildMaxHeight = !IsInfinity(childMaxHeight) && !IsNaN(childMaxHeight);
				var hasChildMinWidth = childMinWidth > 0.0f;
				var hasChildMinHeight = childMinHeight > 0.0f;

				if (
					childVerticalAlignment != VerticalAlignment.Stretch
					|| childHorizontalAlignment != HorizontalAlignment.Stretch
					|| hasChildWidth
					|| hasChildHeight
					|| hasChildMaxWidth
					|| hasChildMaxHeight
					|| hasChildMinWidth
					|| hasChildMinHeight
					)
				{
					var desiredSize = DesiredChildSize(view);

					// Apply vertical alignment
					if (
						childVerticalAlignment != VerticalAlignment.Stretch
						|| hasChildHeight
						|| hasChildMaxHeight
						|| hasChildMinHeight
					)
					{
						var actualHeight = GetActualSize(
							height,
							childVerticalAlignment == VerticalAlignment.Stretch,
							childMaxHeight,
							childMinHeight,
							childHeight,
							childMargin.Top + childMargin.Bottom,
							hasChildHeight,
							hasChildMaxHeight,
							hasChildMinHeight,
							desiredSize.Height,
							frame.Height);

						switch (childVerticalAlignment)
						{
							case VerticalAlignment.Top:
								y = frame.Y;
								break;

							case VerticalAlignment.Bottom:
								y = frame.Y + frame.Height - actualHeight;
								break;

							case VerticalAlignment.Stretch:
							case VerticalAlignment.Center:
								y = frame.Y + (frame.Height - actualHeight) / 2;
								break;
						}

						height = actualHeight;
					}

					// Apply horizontal alignment
					if (
						childHorizontalAlignment != HorizontalAlignment.Stretch
						|| hasChildWidth
						|| hasChildMaxWidth
						|| hasChildMinWidth
					)
					{
						var actualWidth = GetActualSize(
							width,
							childHorizontalAlignment == HorizontalAlignment.Stretch,
							childMaxWidth,
							childMinWidth,
							childWidth,
							childMargin.Left + childMargin.Right,
							hasChildWidth,
							hasChildMaxWidth,
							hasChildMinWidth,
							desiredSize.Width,
							frame.Width);

						switch (childHorizontalAlignment)
						{
							case HorizontalAlignment.Left:
								x = frame.X;
								break;

							case HorizontalAlignment.Right:
								x = frame.X + frame.Width - actualWidth;
								break;

							case HorizontalAlignment.Stretch:
							case HorizontalAlignment.Center:
								x = frame.X + (frame.Width - actualWidth) / 2;
								break;
						}

						width = actualWidth;
					}
				}

				frame = new Rect(
					x + childMargin.Left,
					y + childMargin.Top,
					width - childMargin.Left - childMargin.Right,
					height - childMargin.Top - childMargin.Bottom
					);

				frame.Size = frameworkElement.AdjustArrange(frame.Size);

			}

			return new Rect(
				x: IsNaN(frame.X) ? 0 : frame.X,
				y: IsNaN(frame.Y) ? 0 : frame.Y,
				width: Max(0, IsNaN(frame.Width) ? 0 : frame.Width),
				height: Max(0, IsNaN(frame.Height) ? 0 : frame.Height)
			);
		}

		protected virtual void AdjustAlignment(View view, ref HorizontalAlignment childHorizontalAlignment,
			ref VerticalAlignment childVerticalAlignment)
		{
			if (view is Image img && (img.Stretch == Stretch.None || img.Stretch == Stretch.Uniform))
			{
				// Image is a special control and is using the Vertical/Horizontal Alignment
				// to calculate the final position of the image. On UWP, there's no difference
				// between "Stretch" and "Center": they all behave like "Center", so we need
				// to do the same here.
				if (childVerticalAlignment == VerticalAlignment.Stretch)
				{
					childVerticalAlignment = VerticalAlignment.Center;
				}

				if (childHorizontalAlignment == HorizontalAlignment.Stretch)
				{
					childHorizontalAlignment = HorizontalAlignment.Center;
				}
			}
		}

		private double GetActualSize(
			double size,
			bool isStretch,
			double childMaxHeight,
			double childMinHeight,
			double childHeight,
			double childMargins,
			bool hasChildHeight,
			bool hasChildMaxHeight,
			bool hasChildMinHeight,
			double desiredSize,
			double frameSize)
		{
			//Default value
			//childVerticalAlignment != VerticalAlignment.Stretch
			var actualHeight = Min(size, desiredSize);

			if (hasChildHeight)
			{
				actualHeight = Min(childHeight + childMargins, size);
			}
			else if (hasChildMaxHeight && hasChildMinHeight)
			{
				actualHeight = Min(childMaxHeight + childMargins,
					isStretch
						? size
						: desiredSize
				);

				actualHeight = Max(childMinHeight + childMargins, actualHeight);
			}
			else if (hasChildMaxHeight)
			{
				actualHeight = Min(childMaxHeight + childMargins,
					isStretch
						? size
						: desiredSize
				);
			}
			else if (hasChildMinHeight)
			{
				actualHeight = Max(childMinHeight + childMargins,
					isStretch
						? size
						: desiredSize
				);
			}

			return Min(actualHeight, frameSize);
		}

		/// <summary>
		/// Measures the specified child.
		/// </summary>
		/// <param name="view">The view to measure</param>
		/// <param name="slotSize">The maximum size the child can use.</param>
		/// <returns>The size the view requires.</returns>
		/// <remarks>
		/// Provides the ability for external implementations to measure children.
		/// Mainly used for compatibility with existing WPF/WinRT implementations.
		/// </remarks>
		void ILayouter.ArrangeChild(View view, Rect frame)
		{
			ArrangeChild(view, frame);
		}

		/// <summary>
		/// Arranges the specified view.
		/// </summary>
		/// <param name="view">The view to arrange</param>
		/// <param name="frame">The frame available for the child.</param>
		/// <remarks>
		/// Provides the ability for external implementations to measure children.
		/// Mainly used for compatibility with existing WPF/WinRT implementations.
		/// </remarks>
		Size ILayouter.MeasureChild(View view, Size slotSize)
		{
			return MeasureChild(view, slotSize);
		}

		private string LoggingOwnerTypeName => ((object)Panel ?? this).GetType().Name;

		public override string ToString() => $"[{LoggingOwnerTypeName}.Layouter]" + (string.IsNullOrEmpty(Panel?.Name) ? default : $"(name='{Panel.Name}')");
	}
}
#endif
