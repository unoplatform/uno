#if !__NETSTD_REFERENCE__
#nullable enable
using System;
using System.Globalization;
using System.Linq;
using Uno.Diagnostics.Eventing;
using Uno.Extensions;
using Uno.Foundation.Logging;
using Windows.Foundation;
using Windows.UI.Xaml.Controls.Primitives;

using Uno.UI;
using static System.Math;
using static Uno.UI.LayoutHelper;

namespace Windows.UI.Xaml
{
	public partial class FrameworkElement
	{
		private readonly static IEventProvider _trace = Tracing.Get(FrameworkElement.TraceProvider.Id);

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
				/// <remarks>
				/// This method contains or is called by a try/catch containing method and
				/// can be significantly slower than other methods as a result on WebAssembly.
				/// See https://github.com/dotnet/runtime/issues/56309
				/// </remarks>
				void MeasureCoreWithTrace(Size availableSize)
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

				MeasureCoreWithTrace(availableSize);
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
				.AtMost(maxSize)
				.AtLeast(minSize);

			var desiredSize = MeasureOverride(frameworkAvailableSize);

			_logDebug?.Trace($"{DepthIndentation}{FormatDebugName()}.MeasureOverride(availableSize={frameworkAvailableSize}): desiredSize={desiredSize} minSize={minSize} maxSize={maxSize} marginSize={marginSize}");

			if (
				double.IsNaN(desiredSize.Width)
				|| double.IsNaN(desiredSize.Height)
				|| double.IsInfinity(desiredSize.Width)
				|| double.IsInfinity(desiredSize.Height)
			)
			{
				throw new InvalidOperationException($"{FormatDebugName()}: Invalid measured size {desiredSize}. NaN or Infinity are invalid desired size.");
			}

			desiredSize = desiredSize
				.AtLeast(minSize)
				.AtLeastZero();

			_unclippedDesiredSize = desiredSize;

			var clippedDesiredSize = desiredSize
				.AtMost(maxSize)
				.Add(marginSize)
				// Making sure after adding margins that clipped DesiredSize is not bigger than the AvailableSize
				.AtMost(availableSize)
				// Margin may be negative
				.AtLeastZero();

			// DesiredSize must include margins
			LayoutInformation.SetDesiredSize(this, clippedDesiredSize);

			_logDebug?.Debug($"{DepthIndentation}[{FormatDebugName()}] Measure({Name}/{availableSize}/{Margin}) = {clippedDesiredSize} _unclippedDesiredSize={_unclippedDesiredSize}");
		}

		private string FormatDebugName()
			=> $"[{this}/{Name}";

		internal sealed override void ArrangeCore(Rect finalRect)
		{
			if (_trace.IsEnabled)
			{
				void ArrangeCoreWithTrace(Rect finalRect)
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

				ArrangeCoreWithTrace(finalRect);
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
			_logDebug?.Debug($"{DepthIndentation}{FormatDebugName()}: InnerArrangeCore({finalRect})");
			var arrangeSize = finalRect.Size;

			var (_, maxSize) = this.GetMinMax();
			var marginSize = this.GetMarginSize();

			arrangeSize = arrangeSize
				.Subtract(marginSize)
				.AtLeastZero();

			var customClippingElement = (this as ICustomClippingElement);
			var allowClipToSlot = customClippingElement?.AllowClippingToLayoutSlot ?? true; // Some controls may control itself how clipping is applied
			var needsClipToSlot = customClippingElement?.ForceClippingToLayoutSlot ?? false;

			_logDebug?.Debug($"{DepthIndentation}{FormatDebugName()}: InnerArrangeCore({finalRect}) - allowClip={allowClipToSlot}, arrangeSize={arrangeSize}, _unclippedDesiredSize={_unclippedDesiredSize}, forcedClipping={needsClipToSlot}");

			if (allowClipToSlot && !needsClipToSlot)
			{
				if (IsLessThanAndNotCloseTo(arrangeSize.Width, _unclippedDesiredSize.Width))
				{
					_logDebug?.Trace($"{DepthIndentation}{FormatDebugName()}: (arrangeSize.Width) {arrangeSize.Width} < {_unclippedDesiredSize.Width}: NEEDS CLIPPING.");
					needsClipToSlot = true;
					arrangeSize.Width = _unclippedDesiredSize.Width;
				}

				if (IsLessThanAndNotCloseTo(arrangeSize.Height, _unclippedDesiredSize.Height))
				{
					_logDebug?.Trace($"{DepthIndentation}{FormatDebugName()}: (arrangeSize.Height) {arrangeSize.Height} < {_unclippedDesiredSize.Height}: NEEDS CLIPPING.");
					needsClipToSlot = true;
					arrangeSize.Height = _unclippedDesiredSize.Height;
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

			_logDebug?.Debug($"{DepthIndentation}{FormatDebugName()}: InnerArrangeCore({finalRect}) - effectiveMaxSize={effectiveMaxSize}, maxSize={maxSize}, _unclippedDesiredSize={_unclippedDesiredSize}, forcedClipping={needsClipToSlot}");

			if (IsLessThanAndNotCloseTo(effectiveMaxSize.Width, arrangeSize.Width))
			{
				_logDebug?.Trace($"{DepthIndentation}{FormatDebugName()}: (effectiveMaxSize.Width) {effectiveMaxSize.Width} < {arrangeSize.Width}: NEEDS CLIPPING.");
				needsClipToSlot = allowClipToSlot;
				arrangeSize.Width = effectiveMaxSize.Width;
			}
			if (IsLessThanAndNotCloseTo(effectiveMaxSize.Height, arrangeSize.Height))
			{
				_logDebug?.Trace($"{DepthIndentation}{FormatDebugName()}: (effectiveMaxSize.Height) {effectiveMaxSize.Height} < {arrangeSize.Height}: NEEDS CLIPPING.");
				needsClipToSlot = allowClipToSlot;
				arrangeSize.Height = effectiveMaxSize.Height;
			}

			var oldRenderSize = RenderSize;
			var innerInkSize = ArrangeOverride(arrangeSize);

			var clippedInkSize = innerInkSize.AtMost(maxSize);

			RenderSize = innerInkSize;

			_logDebug?.Debug($"{DepthIndentation}{FormatDebugName()}: ArrangeOverride({arrangeSize})={innerInkSize}, clipped={clippedInkSize} (max={maxSize}) needsClipToSlot={needsClipToSlot}");

			var clientSize = finalRect.Size
				.Subtract(marginSize)
				.AtLeastZero();

			// Give opportunity to element to alter arranged size
			clippedInkSize = AdjustArrange(clippedInkSize);

			if (allowClipToSlot &&
				(IsLessThanAndNotCloseTo(clippedInkSize.Width, innerInkSize.Width) ||
				IsLessThanAndNotCloseTo(clippedInkSize.Height, innerInkSize.Height)))
			{
				needsClipToSlot = true;
			}

			if (allowClipToSlot &&
				(IsLessThanAndNotCloseTo(clientSize.Width, clippedInkSize.Width) ||
				IsLessThanAndNotCloseTo(clientSize.Height, clippedInkSize.Height)))
			{
				needsClipToSlot = true;
			}

			var offset = this.GetAlignmentOffset(clientSize, clippedInkSize);
			var margin = Margin;

			offset = new Point(
				offset.X + finalRect.X + margin.Left,
				offset.Y + finalRect.Y + margin.Top
			);

			_logDebug?.Debug(
				$"{DepthIndentation}[{FormatDebugName()}] ArrangeChild(offset={offset}, margin={margin}) [oldRenderSize={oldRenderSize}] [RenderSize={RenderSize}] [clippedInkSize={clippedInkSize}] [RequiresClipping={needsClipToSlot}]");

			NeedsClipToSlot = needsClipToSlot;

#if __WASM__
			if (FeatureConfiguration.UIElement.AssignDOMXamlProperties)
			{
				UpdateDOMXamlProperty(nameof(NeedsClipToSlot), NeedsClipToSlot);
			}
#endif

			var clippedFrame = GetClipRect(needsClipToSlot, finalRect, maxSize, margin, offset);
			if (clippedFrame is null)
			{
				ArrangeNative(offset, false);
			}
			else
			{
				ArrangeNative(offset, true, clippedFrame.Value);
			}

			OnLayoutUpdated();
		}

		// Part of this code originates from https://github.com/dotnet/wpf/blob/b9b48871d457fc1f78fa9526c0570dae8e34b488/src/Microsoft.DotNet.Wpf/src/PresentationFramework/System/Windows/FrameworkElement.cs#L4877
		private Rect? GetClipRect(bool needsClipToSlot, Rect finalRect, Size maxSize, Thickness margin, Point actualOffset)
		{
			if (needsClipToSlot)
			{
				Rect clippedFrame = default;
				var inkSize = RenderSize;

				var maxClip = maxSize.FiniteOrDefault(inkSize);

				//need to clip because the computed sizes exceed MaxWidth/MaxHeight/Width/Height
				bool needToClipLocally = IsLessThanAndNotCloseTo(maxClip.Width, inkSize.Width) || IsLessThanAndNotCloseTo(maxClip.Height, inkSize.Height);

				inkSize = inkSize.AtMost(maxSize);

				var marginSize = new Size(margin.Left + margin.Right, margin.Top + margin.Bottom);
				Size clippingSize = finalRect.Size.Subtract(marginSize).AtLeastZero();
				bool needToClipSlot = IsLessThanAndNotCloseTo(clippingSize.Width, inkSize.Width) || IsLessThanAndNotCloseTo(clippingSize.Height, inkSize.Height);

				if (needToClipSlot)
				{
					var offset = LayoutHelper.GetAlignmentOffset(this, clippingSize, inkSize);
					clippedFrame = new Rect(-offset.X, -offset.Y, clippingSize.Width, clippingSize.Height);
					if (needToClipLocally)
					{
						clippedFrame = clippedFrame.IntersectWith(new Rect(default, maxClip)) ?? Rect.Empty;
					}
				}
				else if (needToClipLocally)
				{
					clippedFrame = new Rect(default, maxClip);
				}

				if (needToClipSlot || needToClipLocally)
				{
					return clippedFrame;
				}
			}

			return null;
		}

		/// <summary>
		/// Calculates and applies native arrange properties.
		/// </summary>
		/// <param name="offset">Offset of the view from its parent</param>
		/// <param name="needsClipToSlot">If the control should be clip to its bounds</param>
		/// <param name="clippedFrame">Zone to clip, if clipping is required</param>
		private void ArrangeNative(Point offset, bool needsClipToSlot, Rect clippedFrame = default)
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
				throw new InvalidOperationException($"{FormatDebugName()}: Invalid frame size {newRect}. No dimension should be NaN or negative value.");
			}

			var clipRect = Clip?.Rect ?? (needsClipToSlot ? clippedFrame : default(Rect?));

			_logDebug?.Trace($"{DepthIndentation}{FormatDebugName()}.ArrangeElementNative({newRect}, clip={clipRect} (NeedsClipToSlot={NeedsClipToSlot})");

			ArrangeVisual(newRect, clipRect);
		}
	}
}
#endif
