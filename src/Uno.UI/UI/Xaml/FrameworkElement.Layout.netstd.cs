#if !__NETSTD_REFERENCE__
using System;
using System.Globalization;
using System.Linq;
using Uno.Diagnostics.Eventing;
using Uno.Extensions;
using Uno.Logging;
using Windows.Foundation;
using Microsoft.Extensions.Logging;
using Uno.UI;
using static System.Math;
using static Uno.UI.LayoutHelper;

namespace Windows.UI.Xaml
{
	public partial class FrameworkElement
	{
		/// <summary>
		/// DesiredSize from MeasureOverride, after clamping to min size but before being clipped by max size (from GetMinMax())
		/// </summary>
		private Size _unclippedDesiredSize;
		private Point _visualOffset;

		private const double SIZE_EPSILON = 0.05d;
		private readonly Size MaxSize = new Size(double.PositiveInfinity, double.PositiveInfinity);

		/// <summary>
		/// The origin of the view's bounds relative to its parent.
		/// </summary>
		internal Point RelativePosition => _visualOffset;

		private protected string DepthIndentation
		{
			get
			{
				if (Depth is int d)
				{
					return (Parent as FrameworkElement)?.DepthIndentation + $"-{d}>";
				}
				else
				{
					return "-?>";
				}
			}
		}

		internal sealed override void MeasureCore(Size availableSize)
		{
			if (_trace.IsEnabled)
			{
				var traceActivity = _trace.WriteEventActivity(
					TraceProvider.FrameworkElement_MeasureStart,
					TraceProvider.FrameworkElement_MeasureStop,
					new object[] { GetType().Name, this.GetDependencyObjectId(), Name, availableSize.ToString() }
				);

				using (traceActivity)
				{
					InnerMeasureCore(availableSize);
				}
			}
			else
			{
				// This method is split in two functions to avoid the dynCalls
				// invocations generation for mono-wasm AOT inside of try/catch/finally blocks.
				InnerMeasureCore(availableSize);
			}
		}

		private void InnerMeasureCore(Size availableSize)
		{
			var (minSize, maxSize) = this.GetMinMax();
			var marginSize = this.GetMarginSize();

			// NaN values are accepted as input here, particularly when coming from
			// SizeThatFits in Image or Scrollviewer. Clamp the value here as it is reused
			// below for the clipping value.
			availableSize = availableSize
				.NumberOrDefault(MaxSize);

			var frameworkAvailableSize = availableSize
				.Subtract(marginSize)
				.AtLeastZero()
				.AtMost(maxSize);

			var desiredSize = MeasureOverride(frameworkAvailableSize);

			_logDebug?.LogTrace($"{DepthIndentation}{this}.MeasureOverride(availableSize={frameworkAvailableSize}): desiredSize={desiredSize} minSize={minSize} maxSize={maxSize} marginSize={marginSize}");

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
				.AtLeast(minSize)
				.AtLeastZero();

			_unclippedDesiredSize = desiredSize;

			var clippedDesiredSize = desiredSize
				.AtMost(availableSize)
				.Add(marginSize)
				// Margin may be negative
				.AtLeastZero();

			// DesiredSize must include margins
			SetDesiredSize(clippedDesiredSize);

			_logDebug?.Debug($"{DepthIndentation}[{this}] Measure({Name}/{availableSize}/{Margin}) = {clippedDesiredSize} _unclippedDesiredSize={_unclippedDesiredSize}");
		}

		internal sealed override void ArrangeCore(Rect finalRect)
		{
			if (_trace.IsEnabled)
			{
				var traceActivity = _trace.WriteEventActivity(
					TraceProvider.FrameworkElement_ArrangeStart,
					TraceProvider.FrameworkElement_ArrangeStop,
					new object[] { GetType().Name, this.GetDependencyObjectId(), Name, finalRect.ToString() }
				);

				using (traceActivity)
				{
					InnerArrangeCore(finalRect);
				}
			}
			else
			{
				// This method is split in two functions to avoid the dynCalls
				// invocations generation for mono-wasm AOT inside of try/catch/finally blocks.
				InnerArrangeCore(finalRect);
			}
		}

		private static bool IsLessThanAndNotCloseTo(double a, double b) => a < (b - SIZE_EPSILON);

		private void InnerArrangeCore(Rect finalRect)
		{
			_logDebug?.Debug($"{DepthIndentation}{this}: InnerArrangeCore({finalRect})");
			var arrangeSize = finalRect.Size;

			var (_, maxSize) = this.GetMinMax();
			var marginSize = this.GetMarginSize();

			arrangeSize = arrangeSize
				.Subtract(marginSize)
				.AtLeastZero();

			var customClippingElement = (this as ICustomClippingElement);
			var allowClipToSlot = customClippingElement?.AllowClippingToLayoutSlot ?? true; // Some controls may control itself how clipping is applied
			var needsClipToSlot = customClippingElement?.ForceClippingToLayoutSlot ?? false;

			_logDebug?.Debug($"{DepthIndentation}{this}: InnerArrangeCore({finalRect}) - allowClip={allowClipToSlot}, arrangeSize={arrangeSize}, _unclippedDesiredSize={_unclippedDesiredSize}, forcedClipping={needsClipToSlot}");

			if (allowClipToSlot && !needsClipToSlot)
			{
				if (IsLessThanAndNotCloseTo(arrangeSize.Width, _unclippedDesiredSize.Width))
				{
					_logDebug?.Debug($"{DepthIndentation}{this}: (arrangeSize.Width) {arrangeSize.Width} < {_unclippedDesiredSize.Width}: NEEDS CLIPPING.");
					needsClipToSlot = true;
				}
				else if (IsLessThanAndNotCloseTo(arrangeSize.Height, _unclippedDesiredSize.Height))
				{
					_logDebug?.Debug($"{DepthIndentation}{this}: (arrangeSize.Height) {arrangeSize.Height} < {_unclippedDesiredSize.Height}: NEEDS CLIPPING.");
					needsClipToSlot = true;
				}
			}

			if (HorizontalAlignment != HorizontalAlignment.Stretch)
			{
				arrangeSize.Width = _unclippedDesiredSize.Width;
			}

			if (VerticalAlignment != VerticalAlignment.Stretch)
			{
				arrangeSize.Height = _unclippedDesiredSize.Height;
			}

			// We have to choose max between _unclippedDesiredSize and maxSize here, because
			// otherwise setting of max property could cause arrange at less then _unclippedDesiredSize.
			// Clipping by Max is needed to limit stretch here
			var effectiveMaxSize = Max(_unclippedDesiredSize, maxSize);

			_logDebug?.Debug($"{DepthIndentation}{this}: InnerArrangeCore({finalRect}) - effectiveMaxSize={effectiveMaxSize}, maxSize={maxSize}, _unclippedDesiredSize={_unclippedDesiredSize}, forcedClipping={needsClipToSlot}");

			if (allowClipToSlot)
			{
				if (IsLessThanAndNotCloseTo(effectiveMaxSize.Width, arrangeSize.Width))
				{
					_logDebug?.Debug($"{DepthIndentation}{this}: (effectiveMaxSize.Width) {effectiveMaxSize.Width} < {arrangeSize.Width}: NEEDS CLIPPING.");
					needsClipToSlot = true;
					arrangeSize.Width = effectiveMaxSize.Width;
				}
				if (IsLessThanAndNotCloseTo(effectiveMaxSize.Height, arrangeSize.Height))
				{
					_logDebug?.Debug($"{DepthIndentation}{this}: (effectiveMaxSize.Height) {effectiveMaxSize.Height} < {arrangeSize.Height}: NEEDS CLIPPING.");
					needsClipToSlot = true;
					arrangeSize.Height = effectiveMaxSize.Height;
				}
			}

			var oldRenderSize = RenderSize;
			var innerInkSize = ArrangeOverride(arrangeSize);

			var clippedInkSize = innerInkSize.AtMost(maxSize);

			RenderSize = needsClipToSlot ? clippedInkSize : innerInkSize;

			_logDebug?.Debug($"{DepthIndentation}{this}: ArrangeOverride({arrangeSize})={innerInkSize}, clipped={clippedInkSize} (max={maxSize}) needsClipToSlot={needsClipToSlot}");

			var clientSize = finalRect.Size
				.Subtract(marginSize)
				.AtLeastZero();

			// Give opportunity to element to alter arranged size
			clippedInkSize = AdjustArrange(clippedInkSize);

			var (offset, overflow) = this.GetAlignmentOffset(clientSize, clippedInkSize);
			var margin = Margin;

			offset = new Point(
				offset.X + finalRect.X + margin.Left,
				offset.Y + finalRect.Y + margin.Top
			);

			if (overflow)
			{
				needsClipToSlot = true;
			}

			_logDebug?.Debug(
				$"{DepthIndentation}[{this}] ArrangeChild(offset={offset}, margin={margin}) [oldRenderSize={oldRenderSize}] [RenderSize={RenderSize}] [clippedInkSize={clippedInkSize}] [RequiresClipping={needsClipToSlot}]");

			RequiresClipping = needsClipToSlot;

#if __WASM__
			if (FeatureConfiguration.UIElement.AssignDOMXamlProperties)
			{
				UpdateDOMXamlProperty(nameof(RequiresClipping), RequiresClipping);
			}
#endif

			if (needsClipToSlot)
			{
				var layoutFrame = new Rect(offset, clippedInkSize);

				// Calculate clipped frame.
				var clippedFrameWithParentOrigin =
					layoutFrame
						.IntersectWith(finalRect.DeflateBy(margin))
					?? Rect.Empty;

				// Rebase the origin of the clipped frame to layout
				var clippedFrame = new Rect(
					clippedFrameWithParentOrigin.X - layoutFrame.X,
					clippedFrameWithParentOrigin.Y - layoutFrame.Y,
					clippedFrameWithParentOrigin.Width,
					clippedFrameWithParentOrigin.Height);

				ArrangeNative(offset, clippedFrame);
			}
			else
			{
				ArrangeNative(offset);
			}

			OnLayoutUpdated();
		}

		/// <summary>
		/// Calculates and applies native arrange properties.
		/// </summary>
		/// <param name="offset">Offset of the view from its parent</param>
		/// <param name="clippedFrame">Zone to clip, if clipping is required</param>
		private void ArrangeNative(Point offset, Rect clippedFrame = default)
		{
			_visualOffset = offset;

			var newRect = new Rect(offset, RenderSize);

			if (
				newRect.Width < 0
				|| newRect.Height < 0
				|| double.IsNaN(newRect.Width)
				|| double.IsNaN(newRect.Height)
				|| double.IsNaN(newRect.X)
				|| double.IsNaN(newRect.Y)
			)
			{
				throw new InvalidOperationException($"{this}: Invalid frame size {newRect}. No dimension should be NaN or negative value.");
			}

			Rect? getClip()
			{
				// Clip transform not supported yet on Wasm

				if (RequiresClipping) // if control should be clipped by layout constrains
				{
					if (Clip != null)
					{
						return clippedFrame.IntersectWith(Clip.Rect);
					}
					return clippedFrame;
				}

				return Clip?.Rect;
			}

			var clipRect = getClip();

			_logDebug?.Trace($"{DepthIndentation}{this}.ArrangeElementNative({newRect}, clip={clipRect} (RequiresClipping={RequiresClipping})");

			ArrangeVisual(newRect, RequiresClipping, clipRect);
		}
	}
}
#endif
