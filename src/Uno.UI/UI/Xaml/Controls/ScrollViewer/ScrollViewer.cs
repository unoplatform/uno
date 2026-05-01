#if IS_UNIT_TESTS
#pragma warning disable CS0067
#endif

#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Uno.Disposables;
using System.Runtime.CompilerServices;
using System.Text;
using Uno.UI;
using Uno.UI.DataBinding;
using Microsoft.UI.Xaml.Controls.Primitives;
using Windows.Foundation;
using Windows.System;
using Windows.UI.Core;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Uno;
using Uno.Extensions;
using Uno.Foundation.Logging;
using Uno.UI.Extensions;
using Windows.Foundation.Metadata;
using Uno.UI.Xaml.Core;

#if __ANDROID__
using View = Android.Views.View;
using Font = Android.Graphics.Typeface;
#elif __APPLE_UIKIT__
using UIKit;
using View = UIKit.UIView;
using Color = UIKit.UIColor;
using Font = UIKit.UIFont;
#else
using View = Microsoft.UI.Xaml.UIElement;
#endif

#if UNO_HAS_MANAGED_SCROLL_PRESENTER
using _ScrollContentPresenter = Microsoft.UI.Xaml.Controls.ScrollContentPresenter;
#else
using _ScrollContentPresenter = Microsoft.UI.Xaml.Controls.IScrollContentPresenter;
#endif

using Microsoft.UI.Input;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class ScrollViewer : ContentControl, IFrameworkTemplatePoolAware
	{
#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value
		private bool m_isInConstantVelocityPan;
#pragma warning restore CS0649 // Field is never assigned to, and will always have its default value

		private static class Parts
		{
			public static class Uwp
			{
				public const string ScrollContentPresenter = "ScrollContentPresenter";
				public const string VerticalScrollBar = "VerticalScrollBar";
				public const string HorizontalScrollBar = "HorizontalScrollBar";
			}

			public static class WinUI3
			{
				public const string Scroller = "PART_Scroller";
				public const string VerticalScrollBar = "PART_VerticalScrollBar";
				public const string HorizontalScrollBar = "PART_HorizontalScrollBar";
			}
		}

		private static class VisualStates
		{
			public static class ScrollingIndicator
			{
				public const string None = "NoIndicator";
				public const string Touch = "TouchIndicator";
				public const string Mouse = "MouseIndicator";
				// public const string MouseFull = "MouseIndicatorFull"; // No supported yet
			}

			public static class ScrollBarsSeparator
			{
				public const string Collapsed = "ScrollBarSeparatorCollapsed";
				public const string Expanded = "ScrollBarSeparatorExpanded";
				public const string ExpandedWithoutAnimation = "ScrollBarSeparatorExpandedWithoutAnimation";
				public const string CollapsedWithoutAnimation = "ScrollBarSeparatorCollapsedWithoutAnimation";

				// On WinUI3 visuals states are prefixed with "ScrollBar***s***" (with a trailing 's')
				//public const string Collapsed = "ScrollBarsSeparatorCollapsed";
				//public const string CollapsedDisabled = "ScrollBarsSeparatorCollapsedDisabled"; // Not supported yet
				//public const string Expanded = "ScrollBarsSeparatorExpanded";
				//public const string DisplayedWithoutAnimation = "ScrollBarsSeparatorDisplayedWithoutAnimation"; // Not supported yet
				//public const string ExpandedWithoutAnimation = "ScrollBarsSeparatorExpandedWithoutAnimation";
				//public const string CollapsedWithoutAnimation = "ScrollBarsSeparatorCollapsedWithoutAnimation";
			}
		}

		private static bool IsAnimationEnabled => Uno.UI.Helpers.WinUI.SharedHelpers.IsAnimationsEnabled();

		/// <summary>
		/// Occurs when manipulations such as scrolling and zooming have caused the view to change.
		/// </summary>
		public event EventHandler<ScrollViewerViewChangedEventArgs>? ViewChanged;

		internal event SizeChangedEventHandler? ExtentSizeChanged;

		public ScrollViewer()
		{
			DefaultStyleKey = typeof(ScrollViewer);

			UpdatesMode = Uno.UI.Xaml.Controls.ScrollViewer.GetUpdatesMode(this);
			InitializePartial();
		}

		partial void InitializePartial();

		private protected override void OnLoaded()
		{
			base.OnLoaded();

			EnsureAttachScrollBars();

			OnLoadedPartial();
		}

		private partial void OnLoadedPartial();

		private protected override void OnUnloaded()
		{
			base.OnUnloaded();

			DetachScrollBars();
			ResetScrollIndicator();

			OnUnloadedPartial();
		}
		private partial void OnUnloadedPartial();

		protected override AutomationPeer OnCreateAutomationPeer() => new ScrollViewerAutomationPeer(this);

		// Note: All DependencyProperty declarations have been moved to ScrollViewer.Properties.cs
		// as a sideline foundation step. ScrollViewer.cs retains only behavioral logic and field state.


		private readonly SerialDisposable _sizeChangedSubscription = new SerialDisposable();

#pragma warning disable 649 // unused member for Unit tests
		private _ScrollContentPresenter? _presenter;
#pragma warning restore 649 // unused member for Unit tests

		/// <summary>
		/// Gets the ScrollContentPresenter resolved from the template.
		/// Be aware that on iOS and Android this might be only a wrapper onto the NativeScrollContentPresenter.
		/// </summary>
		/// <remarks>
		/// This is a temporary workaround until the NativeSCP knows its managed SCP and will most probably been removed in a near .
		/// Try to avoid usage of this property as much as possible!
		/// </remarks>
		internal ScrollContentPresenter? Presenter { get; private set; }

		/// <summary>
		/// Gets the size of the Viewport used in the **CURRENT** (cf. remarks) or last measure
		/// </summary>
		/// <remarks>Unlike the LayoutInformation.GetAvailableSize(), this property is set **BEFORE** measuring the children of the ScrollViewer</remarks>
		internal Size ViewportMeasureSize { get; private set; }

		/// <summary>
		/// Gets the size of the Viewport used in the **CURRENT** (cf. remarks) or last arrange
		/// </summary>
		/// <remarks>Unlike the LayoutInformation.GetLayoutSlot(), this property is set **BEFORE** arranging the children of the ScrollViewer</remarks>
		internal Size ViewportArrangeSize { get; private set; }

		/// <summary>
		/// Cached value of <see cref="Uno.UI.Xaml.Controls.ScrollViewer.UpdatesModeProperty"/>,
		/// in order to not access the DP on each scroll (perf considerations)
		/// </summary>
		internal Uno.UI.Xaml.Controls.ScrollViewerUpdatesMode UpdatesMode { get; set; }

		/// <summary>
		/// If this flag is enabled, the ScrollViewer will report offsets less than 0 and greater than <see cref="ScrollableHeight"/> when
		/// 'overscrolling' on iOS. By default this is false, matching Windows behaviour.
		/// </summary>
		[UnoOnly]
		public bool ShouldReportNegativeOffsets { get; set; } = false;

		/// <summary>
		/// Determines if the vertical scrolling is allowed or not.
		/// Unlike the Visibility of the scroll bar, this will also applies to the mousewheel!
		/// </summary>
		internal bool ComputedIsHorizontalScrollEnabled { get; private set; }

		/// <summary>
		/// Determines if the vertical scrolling is allowed or not.
		/// Unlike the Visibility of the scroll bar, this will also applies to the mousewheel!
		/// </summary>
		internal bool ComputedIsVerticalScrollEnabled { get; private set; }

		internal double MinHorizontalOffset => 0;

		internal double MinVerticalOffset => 0;

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


		/// <summary>
		/// Sets the content of the ScrollViewer
		/// </summary>
		/// <param name="view"></param>
		/// <remarks>Used in the context of member initialization</remarks>
		public
#if !UNO_REFERENCE_API && !IS_UNIT_TESTS
			new
#endif
			void Add(View view)
		{
			Content = view;
		}

		protected override void OnApplyTemplate()
		{
			// Cleanup previous template
			DetachScrollBars();

			base.OnApplyTemplate();


			var scpTemplatePart = GetTemplateChild(Parts.WinUI3.Scroller) ?? GetTemplateChild(Parts.Uwp.ScrollContentPresenter);
			_presenter = scpTemplatePart as _ScrollContentPresenter;

			_isTemplateApplied = _presenter != null;

#if __WASM__ || __SKIA__
			if (_presenter != null && ForceChangeToCurrentView)
			{
				_presenter.ForceChangeToCurrentView = ForceChangeToCurrentView;
			}
#endif
			// Load new template
			_verticalScrollbar = null;
			_isVerticalScrollBarMaterialized = false;
			_horizontalScrollbar = null;
			_isHorizontalScrollBarMaterialized = false;

#if __APPLE_UIKIT__ || __ANDROID__
			if (scpTemplatePart is ScrollContentPresenter scp && scp.Native is null)
			{
				// For Android and iOS, ensure that the ScrollContentPresenter contains a native SCP,
				// which will handle the actual scrolling.
				var nativeSCP = new NativeScrollContentPresenter(this);
				scp.Content = scp.Native = nativeSCP;
				_presenter = nativeSCP;
			}
#endif

			if (scpTemplatePart is ScrollContentPresenter presenter)
			{
				presenter.ScrollOwner = this;
				Presenter = presenter;
			}
			else
			{
				Presenter = null;
			}

			// We update the scrollability properties here in order to make sure to set the right scrollbar visibility
			// on the _presenter as soon as possible
			UpdateComputedVerticalScrollability(invalidate: false);
			UpdateComputedHorizontalScrollability(invalidate: false);

			ApplyScrollContentPresenterContent(Content);

			OnApplyTemplatePartial();

			// Apply correct initial zoom settings
			OnZoomModeChanged(ZoomMode);

			OnBringIntoViewOnFocusChangeChangedPartial(BringIntoViewOnFocusChange);

			PrepareScrollIndicator();
		}

		partial void OnApplyTemplatePartial();

		void IFrameworkTemplatePoolAware.OnTemplateRecycled()
		{
			if (VerticalOffset != 0 || HorizontalOffset != 0 || ZoomFactor != 1)
			{
				ChangeView(
					horizontalOffset: 0,
					verticalOffset: 0,
					zoomFactor: 1,
					disableAnimation: true
				);
			}
		}


		/// <summary>
		/// Determines whether this ScrollViewer is pannable.
		/// Returns false only if both vertical and horizontal scrolling are disabled.
		/// </summary>
		internal override bool IsDraggableOrPannable()
		{
			// If both vertical and horizontal scrolling are disabled, return false.
			// This matches WinUI's ScrollViewer::IsDraggableOrPannableImpl.
			return !(
				(VerticalScrollBarVisibility == ScrollBarVisibility.Disabled ||
					VerticalScrollMode == ScrollMode.Disabled)
				&&
				(HorizontalScrollBarVisibility == ScrollBarVisibility.Disabled ||
					HorizontalScrollMode == ScrollMode.Disabled)
			);
		}
	}
}
