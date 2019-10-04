using System;
using System.Globalization;
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
		private Size _unclippedDesiredSize;
		private Point _visualOffset;

		private const double SIZE_EPSILON = 0.05;

		/// <summary>
		/// The origin of the view's bounds relative to its parent.
		/// </summary>
		internal Point RelativePosition => _visualOffset;

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
				.Subtract(marginSize)
				.AtLeast(new Size(0, 0))
				.AtMost(maxSize)
				.AtLeast(minSize);

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

			var clippedDesiredSize = desiredSize
				.Add(marginSize)
				.AtMost(availableSize);

			var retSize = clippedDesiredSize.AtLeast(new Size(0, 0));

			_logDebug?.Debug($"[{this}] Measure({Name}/{availableSize}/{Margin}) = {retSize}");

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

		private void InnerArrangeCore(Rect finalRect)
		{
			_logDebug?.Debug($"{this}: InnerArrangeCore({finalRect})");
			var arrangeSize = finalRect.Size;
			var needsClipping = false;

			var (minSize, maxSize) = this.GetMinMax();
			var marginSize = this.GetMarginSize();

			arrangeSize = arrangeSize
				.Subtract(marginSize)
				.AtLeast(new Size(0, 0));

			var allowClip = (this as ICustomClippingElement)?.AllowClippingToBounds ?? true; // Some controls may allow clipping
			_logDebug?.Debug($"{this}: InnerArrangeCore({finalRect}) - allowClip={allowClip}, arrangeSize={arrangeSize}, _unclippedDesiredSize={_unclippedDesiredSize}");

			if (allowClip)
			{
				if (arrangeSize.Width < _unclippedDesiredSize.Width - SIZE_EPSILON)
				{
					_logDebug?.Debug($">1>{this}: (Width) {arrangeSize.Width} < {_unclippedDesiredSize.Width}: NEEDS CLIPPING 1");
					needsClipping = true;
				}

				if (arrangeSize.Height < _unclippedDesiredSize.Height - SIZE_EPSILON)
				{
					_logDebug?.Debug($">2>{this}: (Height) {arrangeSize.Height} < {_unclippedDesiredSize.Height}: NEEDS CLIPPING 2");
					needsClipping = true;
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

			var effectiveMaxSize = Max(_unclippedDesiredSize, maxSize);
			arrangeSize = arrangeSize.AtMost(effectiveMaxSize);

			if (allowClip)
			{
				if (effectiveMaxSize.Width < arrangeSize.Width - SIZE_EPSILON)
				{
					_logDebug?.Debug($">3>{this}: (Width) {effectiveMaxSize.Width} < {arrangeSize.Width}: NEEDS CLIPPING 3");
					needsClipping = true;
				}

				if (effectiveMaxSize.Height < arrangeSize.Height - SIZE_EPSILON)
				{
					_logDebug?.Debug($">4>{this}: (Height) {effectiveMaxSize.Height} < {arrangeSize.Height}: NEEDS CLIPPING 4");
					needsClipping = true;
				}
			}

			var oldRenderSize = RenderSize;
			var innerInkSize = ArrangeOverride(arrangeSize);

			RenderSize = innerInkSize;

			var clippedInkSize = innerInkSize.AtMost(maxSize);

			var clientSize = finalRect.Size
				.Subtract(marginSize)
				.AtLeast(new Size(0, 0));

			var offset = this.GetAlignmentOffset(clientSize, clippedInkSize);
			offset = new Point(
				offset.X + finalRect.X + Margin.Left,
				offset.Y + finalRect.Y + Margin.Top
			);

			_logDebug?.Debug(
				$"[{this}] ArrangeChild(offset={offset}, margin={Margin}) [oldRenderSize={oldRenderSize}] [RequiresClipping={needsClipping}]");

			RequiresClipping = needsClipping;

			ArrangeNative(offset, oldRenderSize);
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

		private void ArrangeNative(Point offset, Size oldRenderSize)
		{
			var oldOffset = _visualOffset;
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

			_logDebug?.Trace($"{this}.ArrangeElementNative({newRect}, clip={clipRect} (RequiresClipping={RequiresClipping})");

			ArrangeElementNative(newRect, RequiresClipping, clipRect);
		}
	}
}
