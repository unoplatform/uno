// #define LOG_LAYOUT

#if !UNO_REFERENCE_API
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Windows.Foundation;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Media;
using Uno;
using Uno.Extensions;
using Uno.Foundation.Logging;
using Uno.Collections;
using Uno.Diagnostics.Eventing;
using Uno.UI;
using static System.Double;
using static System.Math;
using static Uno.UI.LayoutHelper;

#if __ANDROID__
using Android.Views;
using View = Android.Views.View;
using Font = Android.Graphics.Typeface;
#elif __IOS__
using View = UIKit.UIView;
using Color = UIKit.UIColor;
using Font = UIKit.UIFont;
using CoreGraphics;
#elif __MACOS__
using View = AppKit.NSView;
using Color = AppKit.NSColor;
using Font = AppKit.NSFont;
using CoreGraphics;
#elif IS_UNIT_TESTS || __WASM__
using View = Windows.UI.Xaml.UIElement;
#endif

namespace Windows.UI.Xaml.Controls
{
	internal abstract partial class Layouter : ILayouter
	{
		private static readonly IEventProvider _trace = Tracing.Get(FrameworkElement.TraceProvider.Id);
		private readonly Logger _logDebug;

		private readonly Size MaxSize = new Size(double.PositiveInfinity, double.PositiveInfinity);

		internal Size _unclippedDesiredSize;

		private readonly UIElement _elementAsUIElement;

		public IFrameworkElement Panel { get; }

		protected Layouter(IFrameworkElement element)
		{
			Panel = element;
			_elementAsUIElement = element as UIElement;

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
			using var traceActivity = _trace.IsEnabled
				? _trace.WriteEventActivity(
					FrameworkElement.TraceProvider.FrameworkElement_MeasureStart,
					FrameworkElement.TraceProvider.FrameworkElement_MeasureStop,
					new object[] { LoggingOwnerTypeName, Panel.GetDependencyObjectId() }
				)
				: null;

			if (Panel.Visibility == Visibility.Collapsed)
			{
				// A collapsed element should not be measured at all
				return default;
			}

			try
			{
				if (_elementAsUIElement?.IsVisualTreeRoot ?? false)
				{
					UIElement.IsLayoutingVisualTreeRoot = true;
				}

				var (minSize, maxSize) = Panel.GetMinMax();
				var marginSize = Panel.GetMarginSize();

				// NaN values are accepted as input here, particularly when coming from
				// SizeThatFits in Image or Scrollviewer. Clamp the value here as it is reused
				// below for the clipping value.
				var frameworkAvailableSize = availableSize
					.NumberOrDefault(MaxSize);

				frameworkAvailableSize = frameworkAvailableSize
					.Subtract(marginSize)
					.AtLeastZero()
					.AtMost(maxSize);

				// TODO: This commented code was done as part of aligning layouting on mobile platforms.
				// We are reverting those changes as they require more changes, but keeping them
				// commented for future reference.
				//if (Panel is not ILayoutOptOut { ShouldUseMinSize: false })
				//{
				//	frameworkAvailableSize = frameworkAvailableSize.AtLeast(minSize);
				//}

				var desiredSize = MeasureOverride(frameworkAvailableSize);
				LayoutInformation.SetAvailableSize(Panel, availableSize);

				_logDebug?.Trace($"{this}.MeasureOverride(availableSize={availableSize}); frameworkAvailableSize={frameworkAvailableSize}; desiredSize={desiredSize}");

				if (
					double.IsNaN(desiredSize.Width)
					|| double.IsNaN(desiredSize.Height)
					|| double.IsInfinity(desiredSize.Width)
					|| double.IsInfinity(desiredSize.Height)
				)
				{
					throw new InvalidOperationException($"{this}: Invalid measured size {desiredSize}. NaN or Infinity are invalid desired size.");
				}

				desiredSize = desiredSize
					.AtLeast((Panel as ILayoutOptOut)?.ShouldUseMinSize == false ? Size.Empty : minSize)
					.AtLeastZero();

				_unclippedDesiredSize = desiredSize;

				var clippedDesiredSize = desiredSize
					// TODO: This commented code was done as part of aligning layouting on mobile platforms.
					// We are reverting those changes as they require more changes, but keeping them
					// commented for future reference.
					//.AtMost(maxSize)
					.AtMost(frameworkAvailableSize) // TODO: This line shouldn't be there (ie, frameworkAvailableSize should be maxSize).
					.Add(marginSize)
					// Making sure after adding margins that clipped DesiredSize is not bigger than the AvailableSize
					// TODO: This commented code was done as part of aligning layouting on mobile platforms.
					// We are reverting those changes as they require more changes, but keeping them
					// commented for future reference.
					//.AtMost(availableSize)
					// Margin may be negative
					.AtLeastZero();

				// DesiredSize must include margins
				// TODO: on UWP, it's not clipped. See test When_MinWidth_SmallerThan_AvailableSize
				LayoutInformation.SetDesiredSize(Panel, clippedDesiredSize);

				// We return "clipped" desiredSize to caller... the unclipped version stays internal
				return clippedDesiredSize;
			}
			finally
			{
				if (_elementAsUIElement?.IsVisualTreeRoot ?? false)
				{
					UIElement.IsLayoutingVisualTreeRoot = false;
				}
			}
		}

		private static bool IsCloseReal(double a, double b)
		{
			var x = Math.Abs((a - b) / (b == 0d ? 1d : b));
			return x < 1.85e-3d;
		}

		private static bool IsLessThanAndNotCloseTo(double a, double b)
		{
			return (a < b) && !IsCloseReal(a, b);
		}

		/// <summary>
		/// Places the children of the panel using a specific size, in logical pixels.
		/// </summary>
		public void Arrange(Rect finalRect)
		{
			using var traceActivity = _trace.IsEnabled
				? _trace.WriteEventActivity(
					FrameworkElement.TraceProvider.FrameworkElement_ArrangeStart,
					FrameworkElement.TraceProvider.FrameworkElement_ArrangeStop,
					new object[] { LoggingOwnerTypeName, Panel.GetDependencyObjectId() }
				)
				: null;

			try
			{
				if (_elementAsUIElement?.IsVisualTreeRoot ?? false)
				{
					UIElement.IsLayoutingVisualTreeRoot = true;
				}

				if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
				{
					this.Log().DebugFormat("[{0}/{1}] Arrange({2}/{3}/{4}/{5})", LoggingOwnerTypeName, Name, GetType(), Panel.Name, finalRect, Panel.Margin);
				}

				var clippedArrangeSize = _elementAsUIElement?.ClippedFrame is Rect clip && !_elementAsUIElement.IsArrangeDirty
					? clip.Size
					: finalRect.Size;

				bool allowClipToSlot;
				bool needsClipToSlot;

#if !IS_UNIT_TESTS
				if (Panel is ICustomClippingElement customClippingElement)
				{
					// Some controls may control itself how clipping is applied
					allowClipToSlot = customClippingElement.AllowClippingToLayoutSlot;
					needsClipToSlot = customClippingElement.ForceClippingToLayoutSlot;
				}
				else
#endif
				{
					allowClipToSlot = true;
					needsClipToSlot = false;
				}

				_logDebug?.Debug($"{this}: InnerArrangeCore({finalRect}) - allowClip={allowClipToSlot}, clippedArrangeSize={clippedArrangeSize}, _unclippedDesiredSize={_unclippedDesiredSize}, forcedClipping={needsClipToSlot}");

				var arrangeSize = finalRect
					.Size
					.AtLeastZero(); // 0.0,0.0

				if (allowClipToSlot && !needsClipToSlot)
				{
					if (IsLessThanAndNotCloseTo(clippedArrangeSize.Width, _unclippedDesiredSize.Width))
					{
						_logDebug?.Debug($"{this}: (arrangeSize.Width) {clippedArrangeSize.Width} < {_unclippedDesiredSize.Width}: NEEDS CLIPPING.");
						needsClipToSlot = true;
						// TODO: This commented code was done as part of aligning layouting on mobile platforms.
						// We are reverting those changes as they require more changes, but keeping them
						// commented for future reference.
						//arrangeSize.Width = _unclippedDesiredSize.Width;
					}

					if (IsLessThanAndNotCloseTo(clippedArrangeSize.Height, _unclippedDesiredSize.Height))
					{
						_logDebug?.Debug($"{this}: (arrangeSize.Height) {clippedArrangeSize.Height} < {_unclippedDesiredSize.Height}: NEEDS CLIPPING.");
						needsClipToSlot = true;
						// TODO: This commented code was done as part of aligning layouting on mobile platforms.
						// We are reverting those changes as they require more changes, but keeping them
						// commented for future reference.
						//arrangeSize.Height = _unclippedDesiredSize.Height;
					}
				}

				// Alignment==Stretch --> arrange at the slot size minus margins
				// Alignment!=Stretch --> arrange at the unclippedDesiredSize
				if (Panel is not Windows.UI.Xaml.Shapes.Shape and not ContentControl)
				{
					// Uno specific: Shapes arrange is relying on "wrong" layouter logic to be arranged properly
					// The "Panel is not Shape" check should be removed when we're removing the legacy shape measure/arrange
					// Also, it seems ContentControl is causing issues (probably related to content presenter bypass?)
					if (Panel.HorizontalAlignment != HorizontalAlignment.Stretch)
					{
						arrangeSize.Width = _unclippedDesiredSize.Width;
					}
					if (Panel.VerticalAlignment != VerticalAlignment.Stretch)
					{
						arrangeSize.Height = _unclippedDesiredSize.Height;
					}
				}

				var (_, maxSize) = this.Panel.GetMinMax();
				//var marginSize = this.Panel.GetMarginSize();

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

				var innerInkSize = ArrangeOverride(arrangeSize);
				var clippedInkSize = innerInkSize.AtMost(maxSize);

				// TODO: This commented code was done as part of aligning layouting on mobile platforms.
				// We are reverting those changes as they require more changes, but keeping them
				// commented for future reference.
				//if (IsLessThanAndNotCloseTo(clippedInkSize.Width, innerInkSize.Width) || IsLessThanAndNotCloseTo(clippedInkSize.Height, innerInkSize.Height))
				//{
				//	needsClipToSlot = true;
				//}

				//var clientSize = finalRect.Size
				//	.Subtract(marginSize)
				//	.AtLeastZero();

				//var (offset, overflow) = Panel.GetAlignmentOffset(clientSize, clippedInkSize);
				//var margin = Panel.Margin;

				//offset = new Point(
				//	offset.X + finalRect.X + margin.Left,
				//	offset.Y + finalRect.Y + margin.Top
				//);

				//if (overflow)
				//{
				//	needsClipToSlot = true;
				//}


				if (_elementAsUIElement != null)
				{
					//_elementAsUIElement.LayoutSlotWithMarginsAndAlignments = new Rect(offset, innerInkSize);
					//var layoutFrame = new Rect(offset, clippedInkSize);

					// Calculate clipped frame.
					//var clippedFrameWithParentOrigin = layoutFrame.IntersectWith(finalRect.DeflateBy(margin)) ?? Rect.Empty;

					// Rebase the origin of the clipped frame to layout
					//_elementAsUIElement.ClippedFrame = new Rect(
					//	clippedFrameWithParentOrigin.X - layoutFrame.X,
					//	clippedFrameWithParentOrigin.Y - layoutFrame.Y,
					//	clippedFrameWithParentOrigin.Width,
					//	clippedFrameWithParentOrigin.Height);

					_elementAsUIElement.RenderSize = clippedInkSize; // TODO: This should be innerInkSize
					_elementAsUIElement.NeedsClipToSlot = needsClipToSlot;
					_elementAsUIElement.ApplyClip();

					if (Panel is FrameworkElement fe)
					{
						fe.OnLayoutUpdated();
					}
				}
				else if (Panel is IFrameworkElement_EffectiveViewport evp)
				{
					evp.OnLayoutUpdated();
				}
			}
			finally
			{
				if (_elementAsUIElement?.IsVisualTreeRoot ?? false)
				{
					UIElement.IsLayoutingVisualTreeRoot = false;
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
		/// Provides the desired size of the element, from the last measure phase.
		/// </summary>
		/// <param name="view">The element to get the measured with</param>
		/// <returns>The measured size</returns>
		Size ILayouter.GetDesiredSize(View view)
			=> LayoutInformation.GetDesiredSize(view);

		protected Size MeasureChild(View view, Size slotSize)
		{
			var frameworkElement = view as IFrameworkElementInternal;
			var ret = default(Size);

			// NaN values are accepted as input for MeasureOverride, but are treated as Infinity.
			slotSize = slotSize.NumberOrDefault(MaxSize);

			if (frameworkElement?.Visibility == Visibility.Collapsed)
			{
				// By default iOS views measure to normal size, even if they're hidden.
				// We want the collapsed behavior, so we return a 0,0 size instead.

				// Note: Visibility is checked in both Measure and MeasureChild, since some IFrameworkElement children may not have their own Layouter
				LayoutInformation.SetDesiredSize(view, ret);

				if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
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
						Math.Max(0, slotSize.Width - margin.Left - margin.Right),
						Math.Max(0, slotSize.Height - margin.Top - margin.Bottom)
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
					var constrainedWidth = Math.Min(
						optionalMaxWidth ?? double.PositiveInfinity,
						optionalWidth ?? double.PositiveInfinity
					);

					slotSize.Width = Math.Min(slotSize.Width, constrainedWidth);
				}

				if (optionalMaxHeight != null || optionalHeight != null)
				{
					var constrainedHeight = Math.Min(
						optionalMaxHeight ?? double.PositiveInfinity,
						optionalHeight ?? double.PositiveInfinity
					);

					slotSize.Height = Math.Min(slotSize.Height, constrainedHeight);
				}
			}

			ret = MeasureChildOverride(view, slotSize);

			if (
				IsPositiveInfinity(ret.Height) || IsPositiveInfinity(ret.Width)
			)
			{
				if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Error))
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
				if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
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

			var hasLayouter = frameworkElement?.HasLayouter ?? false;
			if (!hasLayouter || frameworkElement.Visibility == Visibility.Collapsed)
			{
				// For native controls only - because it's already set in Layouter.Measure()
				// for Uno's managed controls
				LayoutInformation.SetDesiredSize(view, ret);
			}


			return ret;
		}

		/// <summary>
		/// Arranges the location a child in the current panel
		/// </summary>
		/// <param name="view">The child instance</param>
		/// <param name="frame">The rectangle to use, in Logical position</param>
		public void ArrangeChild(View view, Rect frame)
		{
			if ((view as IFrameworkElement)?.Visibility == Visibility.Collapsed)
			{
				return;
			}

			LayoutInformation.SetLayoutSlot(view, frame);

			// Note: This is not matching Windows.
			// Applying alignments should depend on what ArrangeOverride returns (as in Skia and Wasm).
			var (finalFrame, clippedFrame) = ApplyMarginAndAlignments(view, frame);
			if (view is UIElement elt)
			{
				elt.LayoutSlotWithMarginsAndAlignments = finalFrame;
				elt.ClippedFrame = clippedFrame;
			}


			ArrangeChildOverride(view, finalFrame);
		}

#if __ANDROID__ || __IOS__ || __MACOS__
		private void LogArrange(View view, Rect frame)
		{
			if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
			{
				var viewName = (view as IFrameworkElement).SelectOrDefault(f => f.Name, "NativeView");
				var margin = (view as IFrameworkElement).SelectOrDefault(f => f.Margin, Thickness.Empty);

				this.Log().DebugFormat("[{0}/{1}] ArrangeChild({2}/{3}/{4}/{5})", LoggingOwnerTypeName, Name, view.GetType(), viewName, frame, margin);
			}

		}
#endif

		protected abstract string Name { get; }

		private (Rect layoutFrame, Rect clippedFrame) ApplyMarginAndAlignments(View view, Rect frame)
		{
			// In this implementation, since we do not have the ability to intercept properly the measure and arrange
			// because of the type of hierarchy (inheriting from native views), we must apply the margins and alignments
			// from within the panel to its children. This makes the authoring of custom panels that do not inherit from
			// Panel that do not use this helper a bit more complex, but for all other panels that use this
			// layouter, the logic is implied.

			// The result "layoutFrame" gives the positioning of the element, relative to the origin of the parent's frame.
			// The result "clippedFrame" gives the resulting boundaries of the element.
			// If clipping is required, that's were it should occurs.

			if (view is IFrameworkElement frameworkElement)
			{
				// Apply the margin for framework elements, as if it were padding to the child.

				var (x, y, width, height) = frame;

				// capture the child's state to avoid getting DependencyProperties values multiple times.
				var childVerticalAlignment = frameworkElement.VerticalAlignment;
				var childHorizontalAlignment = frameworkElement.HorizontalAlignment;

				AdjustAlignment(view, ref childHorizontalAlignment, ref childVerticalAlignment);

				var childMaxHeight = frameworkElement.MaxHeight;
				var childMaxWidth = frameworkElement.MaxWidth;
				var (childMinHeight, childMinWidth) = (frameworkElement as ILayoutOptOut)?.ShouldUseMinSize == false
					? (0, 0)
					: (frameworkElement.MinHeight, frameworkElement.MinWidth);
				var childWidth = frameworkElement.Width;
				var childHeight = frameworkElement.Height;
				var childMargin = frameworkElement.Margin;

				var hasChildHeight = !IsNaN(childHeight);
				var hasChildWidth = !IsNaN(childWidth);
				var hasChildMaxWidth = !IsInfinity(childMaxWidth) && !IsNaN(childMaxWidth);
				var hasChildMaxHeight = !IsInfinity(childMaxHeight) && !IsNaN(childMaxHeight);
				var hasChildMinWidth = childMinWidth > 0.0;
				var hasChildMinHeight = childMinHeight > 0.0;

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
					var desiredSize = LayoutInformation.GetDesiredSize(view);

					// Apply vertical alignment
					if (
						childVerticalAlignment != VerticalAlignment.Stretch
						|| hasChildHeight
						|| hasChildMaxHeight
						|| hasChildMinHeight
					)
					{
						var actualHeight = GetActualSize(
							frame.Height,
							childVerticalAlignment == VerticalAlignment.Stretch,
							childMaxHeight,
							childMinHeight,
							childHeight,
							childMargin.Top + childMargin.Bottom,
							hasChildHeight,
							hasChildMaxHeight,
							hasChildMinHeight,
							desiredSize.Height);

						if (actualHeight == frame.Height)
						{
							y = frame.Y; // nothing to align: we're using exactly the available height

						}
						else
						{
							switch (childVerticalAlignment)
							{
								case VerticalAlignment.Top:
									y = frame.Y;
									break;

								case VerticalAlignment.Bottom:
									y = frame.Y + frame.Height - actualHeight;
									break;

								case VerticalAlignment.Stretch:
									// On UWP, when a control is taking more height than available from
									// parent, it will be top-aligned when its alignment is Stretch
									y = frame.Y + Math.Max((frame.Height - actualHeight) / 2d, 0d);
									break;
								case VerticalAlignment.Center:
									y = frame.Y + (frame.Height - actualHeight) / 2d;
									break;
							}
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
							frame.Width,
							childHorizontalAlignment == HorizontalAlignment.Stretch,
							childMaxWidth,
							childMinWidth,
							childWidth,
							childMargin.Left + childMargin.Right,
							hasChildWidth,
							hasChildMaxWidth,
							hasChildMinWidth,
							desiredSize.Width);

						if (actualWidth == frame.Width)
						{
							x = frame.X; // nothing to align: we're using exactly the available width
						}
						else
						{
							switch (childHorizontalAlignment)
							{
								case HorizontalAlignment.Left:
									x = frame.X;
									break;

								case HorizontalAlignment.Right:
									x = frame.X + frame.Width - actualWidth;
									break;

								case HorizontalAlignment.Stretch:
									// On UWP, when a control is taking more width than available from
									// parent, it will be left-aligned when its alignment is Stretch
									x = frame.X + Math.Max((frame.Width - actualWidth) / 2d, 0d);
									break;
								case HorizontalAlignment.Center:
									x = frame.X + (frame.Width - actualWidth) / 2d;
									break;
							}
						}

						width = actualWidth;
					}
				}

				// Calculate Create layoutFrame and apply child's margins
				var layoutFrame = new Rect(x, y, width, height).DeflateBy(childMargin);

				// Give opportunity to element to alter arranged size
				layoutFrame.Size = frameworkElement.AdjustArrange(layoutFrame.Size);

				// Calculate clipped frame.
				var clippedFrameWithParentOrigin =
					layoutFrame
						.IntersectWith(frame.DeflateBy(childMargin))
					?? Rect.Empty;

				// Rebase the origin of the clipped frame to layout
				var clippedFrame = new Rect(
					clippedFrameWithParentOrigin.X - layoutFrame.X,
					clippedFrameWithParentOrigin.Y - layoutFrame.Y,
					clippedFrameWithParentOrigin.Width,
					clippedFrameWithParentOrigin.Height);

				return (layoutFrame, clippedFrame);
			}
			else
			{
				var layoutFrame = new Rect(
					x: IsNaN(frame.X) ? 0 : frame.X,
					y: IsNaN(frame.Y) ? 0 : frame.Y,
					width: Math.Max(0, IsNaN(frame.Width) ? 0 : frame.Width),
					height: Math.Max(0, IsNaN(frame.Height) ? 0 : frame.Height)
				);

				// Clipped frame & layout frame are the same for native elements
				return (layoutFrame, layoutFrame);
			}
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
			double frameSize,
			bool isStretch,
			double childMaxSize,
			double childMinSize,
			double childSize,
			double childMarginSize,
			bool hasChildSize,
			bool hasChildMaxSize,
			bool hasChildMinSize,
			double desiredSize)
		{
			var min = hasChildMinSize ? childMinSize + childMarginSize : NegativeInfinity;
			var max = hasChildMaxSize ? childMaxSize + childMarginSize : PositiveInfinity;
			if (!hasChildSize)
			{
				childSize = isStretch
					? frameSize
					: desiredSize; // desired size always include margin, so no need to calculate it here
			}
			else
			{
				childSize += childMarginSize;
			}

			return Math.Max(
				Math.Min(
					Math.Min(childSize, frameSize),
					max),
				min);
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
