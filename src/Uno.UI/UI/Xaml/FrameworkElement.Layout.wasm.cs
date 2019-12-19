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

		internal sealed override Size MeasureCore(Size availableSize)
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
					return InnerMeasureCore(availableSize);
				}
			}
			else
			{
				// This method is split in two functions to avoid the dynCalls
				// invocations generation for mono-wasm AOT inside of try/catch/finally blocks.
				return InnerMeasureCore(availableSize);
			}
		}

		private Size InnerMeasureCore(Size availableSize)
		{
			var (minSize, maxSize) = this.GetMinMax();
			var marginSize = this.GetMarginSize();

			var frameworkAvailableSize = availableSize
				.NumberOrDefault(MaxSize)
				.Subtract(marginSize)
				.AtLeast(new Size(0, 0))
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

			desiredSize = desiredSize.AtLeast(minSize);

			_unclippedDesiredSize = desiredSize;

			desiredSize = desiredSize.AtMost(maxSize);

			var clippedDesiredSize = desiredSize
				.Add(marginSize)
				.AtMost(availableSize);

			var retSize = clippedDesiredSize.AtLeast(new Size(0, 0));

			_logDebug?.Debug($"{DepthIndentation}[{this}] Measure({Name}/{availableSize}/{Margin}) = {retSize}");

			return retSize;
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

			var (minSize, maxSize) = this.GetMinMax();
			var marginSize = this.GetMarginSize();

			arrangeSize = arrangeSize
				.Subtract(marginSize)
				.AtLeast(new Size(0, 0));

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
				.AtLeast(new Size(0, 0));

			var offset = this.GetAlignmentOffset(clientSize, clippedInkSize);
			offset = new Point(
				offset.X + finalRect.X + Margin.Left,
				offset.Y + finalRect.Y + Margin.Top
			);

			_logDebug?.Debug(
				$"{DepthIndentation}[{this}] ArrangeChild(offset={offset}, margin={Margin}) [oldRenderSize={oldRenderSize}] [RenderSize={RenderSize}] [clippedInkSize={clippedInkSize}] [RequiresClipping={needsClipToSlot}]");

			RequiresClipping = needsClipToSlot;

			ArrangeNative(offset);
		}

		internal Thickness GetThicknessAdjust()
		{
			switch (this)
			{
				case Controls.Border b:
					return b.BorderThickness;

				case Controls.Panel g:
					return g.BorderThickness;

				case Controls.ContentPresenter p:
					return p.BorderThickness;

				default:
					return Thickness.Empty;
			}
		}

		/// <summary>
		/// Calculates and applies native arrange properties.
		/// </summary>
		/// <param name="offset">Offset of the view from its parent</param>
		private void ArrangeNative(Point offset)
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
				if (Clip != null)
				{
					return Clip.Rect;
				}

				if (RequiresClipping)
				{
					return new Rect(0, 0, newRect.Width, newRect.Height);
				}

				return null;
			}

			var clipRect = getClip();

			_logDebug?.Trace($"{DepthIndentation}{this}.ArrangeElementNative({newRect}, clip={clipRect} (RequiresClipping={RequiresClipping})");

			ArrangeElementNative(newRect, RequiresClipping, clipRect);
		}
	}
}
