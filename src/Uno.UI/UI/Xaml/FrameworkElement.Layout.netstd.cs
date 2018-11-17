using System;
using System.Globalization;
using Uno.Diagnostics.Eventing;
using Uno.Extensions;
using Uno.Logging;
using Windows.Foundation;
using Uno.UI;
using static System.Math;
using static Uno.UI.LayoutHelper;

namespace Windows.UI.Xaml
{
	public partial class FrameworkElement
	{
		private Size _unclippedDesiredSize;
		private Point _visualOffset;
		/// <summary>
		/// The origin of the view's bounds relative to its parent.
		/// </summary>
		internal Point RelativePosition => _visualOffset;

		internal sealed override Size MeasureCore(Size availableSize)
		{
			IDisposable traceActivity = null;
			if (_trace.IsEnabled)
			{
				traceActivity = _trace.WriteEventActivity(
					TraceProvider.FrameworkElement_MeasureStart,
					TraceProvider.FrameworkElement_MeasureStop,
					new object[] { GetType().Name, this.GetDependencyObjectId(), Name, availableSize.ToString() }
				);
			}

			using (traceActivity)
			{
				var (minSize, maxSize) = this.GetMinMax();
				var marginSize = this.GetMarginSize();

				var frameworkAvailableSize = availableSize
					.Subtract(marginSize)
					.AtLeast(new Size(0, 0))
					.AtMost(maxSize)
					.AtLeast(minSize);

				var desiredSize = MeasureOverride(frameworkAvailableSize);

				if(
					double.IsNaN(desiredSize.Width)
					|| double.IsNaN(desiredSize.Height)
					|| double.IsInfinity(desiredSize.Width)
					|| double.IsInfinity(desiredSize.Height)
				)
				{
					throw new InvalidOperationException($"Invalid measured size {desiredSize}/{GetType()}/{Name}");
				}

				desiredSize = desiredSize.AtLeast(minSize);
				
				_unclippedDesiredSize = desiredSize;

				desiredSize = desiredSize.AtMost(maxSize);

				var clippedDesiredSize = desiredSize
					.Add(marginSize)
					.AtMost(availableSize);

				var retSize = clippedDesiredSize.AtLeast(new Size(0, 0));

				if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
				{
					this.Log().DebugFormat(
						$"[{GetType()}/{Name}] Measure({Name}/{availableSize}/{Margin}) = {retSize}"
					);
				}

				return retSize;
			}
		}

		internal sealed override void ArrangeCore(Rect finalRect)
		{
			IDisposable traceActivity = null;
			if (_trace.IsEnabled)
			{
				traceActivity = _trace.WriteEventActivity(
					TraceProvider.FrameworkElement_ArrangeStart,
					TraceProvider.FrameworkElement_ArrangeStop,
					new object[] { GetType().Name, this.GetDependencyObjectId(), Name, finalRect.ToString() }
				);
			}

			using (traceActivity)
			{
				var arrangeSize = finalRect.Size;

				var (minSize, maxSize) = this.GetMinMax();
				var marginSize = this.GetMarginSize();

				arrangeSize = arrangeSize
					.Subtract(marginSize)
					.AtLeast(new Size(0, 0));

				arrangeSize = arrangeSize.AtLeast(_unclippedDesiredSize);
				
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

				ArrangeNative(offset, oldRenderSize);

				if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
				{
					this.Log().DebugFormat(
						$"[{GetType()}/{Name}] ArrangeChild({Name}/{offset}/{Margin})"
					);
				}
			}
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
			
			var oldRect = new Rect(oldOffset, oldRenderSize); 
			var newRect = new Rect(offset, RenderSize);
			if (oldRect != newRect)
			{
				if(
					newRect.Width < 0
					|| newRect.Height < 0
					|| double.IsNaN(newRect.Width)
					|| double.IsNaN(newRect.Height)
					|| double.IsNaN(newRect.X)
					|| double.IsNaN(newRect.Y)
				)
				{
					throw new InvalidOperationException($"Invalid frame size {newRect} for {HtmlId}/{GetType()}/{Name}");
				}

				// Disable clipping for Scrollviewer (edge seems to disable scrolling if 
				// the clipping is enabled to the size of the scrollviewer, even if overflow-y is auto)
				var clip = this is Controls.ScrollViewer 
					? "" 
					: "rect(0px," + newRect.Width.ToString(CultureInfo.InvariantCulture) + "px, " + newRect.Height.ToString(CultureInfo.InvariantCulture) + "0px)";

				SetStyleArranged(
					("position", "absolute"),
					("top", newRect.Top.ToString(CultureInfo.InvariantCulture) + "px"),
					("left", newRect.Left.ToString(CultureInfo.InvariantCulture) + "px"),
					("width", newRect.Width.ToString(CultureInfo.InvariantCulture) + "px"),
					("height", newRect.Height.ToString(CultureInfo.InvariantCulture) + "px"),
					("clip", clip)
				);
			}
		}
	}
}
