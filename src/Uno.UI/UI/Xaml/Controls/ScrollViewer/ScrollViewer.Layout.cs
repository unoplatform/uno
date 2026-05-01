#nullable enable

using System;
using Windows.Foundation;
using Microsoft.UI.Xaml.Controls.Primitives;
using Uno.Extensions;
using Uno.Foundation.Logging;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class ScrollViewer
	{
		protected override Size MeasureOverride(Size availableSize)
		{
			ViewportMeasureSize = availableSize;

			var size = base.MeasureOverride(availableSize);

			return size;
		}

		protected override Size ArrangeOverride(Size finalSize)
		{
			ViewportArrangeSize = finalSize;

			return AnchoringArrangeOverride(finalSize, size =>
			{
				var arranged = base.ArrangeOverride(size);
				TrimOverscroll(Orientation.Horizontal);
				TrimOverscroll(Orientation.Vertical);
				return arranged;
			});
		}

		partial void TrimOverscroll(Orientation orientation);

		// TODO: Revisit if this can use SizeChanged += (_, _) => OnControlsBoundsChanged(); on all platforms.
#if UNO_HAS_ENHANCED_LIFECYCLE
		internal override void AfterArrange()
		{
			base.AfterArrange();
#else
		internal override void OnLayoutUpdated()
		{
			base.OnLayoutUpdated();
#endif
			if (m_dimensionsUpdatedInArrange)
			{
				m_dimensionsUpdatedInArrange = false;
			}
			else
			{
				UpdateDimensionProperties();
			}
			UpdateZoomedContentAlignment();
		}

		private double LayoutRoundIfNeeded(FrameworkElement fe, double value)
		{
			return this.GetUseLayoutRounding() ? fe.LayoutRound(value) : value;
		}

#if __APPLE_UIKIT__
		internal
#else
		private
#endif
			void UpdateDimensionProperties()
		{
			// The dimensions of the presenter (which are often but not always the same as the ScrollViewer) determine the viewport size
			var vpHeight = (_presenter as IFrameworkElement)?.ActualHeight ?? ActualHeight;
			var vpWidth = (_presenter as IFrameworkElement)?.ActualWidth ?? ActualWidth;

			if (vpHeight == 0 || vpWidth == 0)
			{
				// Do not update properties if we don't have any valid size yet.
				// This is useful essentially for the first size changed on the Content,
				// where it already have its final size while the SV doesn't.
				// This would cause a Scrollable<Width|Height> greater than 0,
				// which will cause the materialization of the managed scrollbar
				// which might not be needed after next layout pass.
				return;
			}

			if ((ActualHeight != vpHeight || ActualWidth != vpWidth) &&
				this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().LogDebug($"ScrollViewer setting ViewportHeight={ActualHeight}, ViewportWidth={ActualWidth}");
			}

			ViewportHeight = vpHeight;
			ViewportWidth = vpWidth;

			var oldSize = new Size(ExtentWidth, ExtentHeight);

			if (_presenter?.CustomContentExtent is { } customExtent)
			{
				ExtentHeight = customExtent.Height;
				ExtentWidth = customExtent.Width;
			}
			else if (Content is FrameworkElement fe)
			{
				ExtentHeight = CalculateExtent(this, fe, isHorizontal: false);
				ExtentWidth = CalculateExtent(this, fe, isHorizontal: true);

				static double CalculateExtent(ScrollViewer sv, FrameworkElement fe, bool isHorizontal)
				{
					var margin = isHorizontal ? GetEffectiveMargin(fe.Margin.Left, fe.Margin.Right) : GetEffectiveMargin(fe.Margin.Top, fe.Margin.Bottom);
					var @explicit = isHorizontal ? fe.Width : fe.Height;
					if (@explicit.IsFinite())
					{
						return sv.LayoutRoundIfNeeded(fe, @explicit + margin);
					}

					var isStretchAlign = isHorizontal ? fe.HorizontalAlignment == HorizontalAlignment.Stretch : fe.VerticalAlignment == VerticalAlignment.Stretch;
					var actual = isHorizontal ? fe.ActualWidth : fe.ActualHeight;
					if (actual > 0 && isStretchAlign &&
						// Due to #2269, TextBlock ActualSize is implemented via DesiredSize
						// which includes the Margin already. We just let it flow to the next block
						// to avoid including margin twice here.
						fe is not TextBlock
					)
					{
						return sv.LayoutRoundIfNeeded(fe, actual + margin);
					}

					// DesiredSize includes the margin already, so we don't need to add it again.
					var desired = isHorizontal ? fe.DesiredSize.Width : fe.DesiredSize.Height;
					return sv.LayoutRoundIfNeeded(fe, desired);
				}
				static double GetEffectiveMargin(double leadingMargin, double trailingMargin)
				{
#if !__WASM__
					return leadingMargin + trailingMargin;
#else
					// Issue needs to be fixed first for WASM for missing trailing Margin
					// Details here: https://github.com/unoplatform/uno/issues/7000
					return leadingMargin;
#endif
				}
			}
			else
			{
				ExtentHeight = 0;
				ExtentWidth = 0;
			}

			// For scrollable height and scrollable width we apply rounding
			// to ensure there is no unwanted difference caused by double
			// precision, which could then cause the scroll bars to appear
			// for no reason.

			var scrollableHeight = Math.Max(Math.Round(ExtentHeight - ViewportHeight, 4), 0);

			ScrollableHeight = scrollableHeight;

			var scrollableWidth = Math.Max(Math.Round(ExtentWidth - ViewportWidth, 4), 0);

			ScrollableWidth = scrollableWidth;

			if (Presenter is not null)
			{
				Presenter.ExtentHeight = ExtentHeight;
				Presenter.ExtentWidth = ExtentWidth;
			}

			UpdateComputedVerticalScrollability(invalidate: false);
			UpdateComputedHorizontalScrollability(invalidate: false);

			TrimOverscroll(Orientation.Vertical);
			TrimOverscroll(Orientation.Horizontal);

			var newSize = new Size(ExtentWidth, ExtentWidth);
			if (oldSize != newSize)
			{
				ExtentSizeChanged?.Invoke(this, new(this, oldSize, newSize));
			}
		}
	}
}
