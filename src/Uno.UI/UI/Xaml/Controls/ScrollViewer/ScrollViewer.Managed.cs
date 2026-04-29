#nullable enable
#if UNO_HAS_MANAGED_SCROLL_PRESENTER
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Windows.Foundation;
using Windows.UI;
using Uno;
using Uno.UI;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class ScrollViewer
	{
		#region IsScrollInertiaEnabled (Attached DP)
		public static bool GetIsScrollInertiaEnabled(DependencyObject element)
			=> (bool)element.GetValue(IsScrollInertiaEnabledProperty);

		public static void SetIsScrollInertiaEnabled(DependencyObject element, bool isScrollInertiaEnabled)
			=> element.SetValue(IsScrollInertiaEnabledProperty, isScrollInertiaEnabled);

		public bool IsScrollInertiaEnabled
		{
			get => (bool)GetValue(IsScrollInertiaEnabledProperty);
			set => SetValue(IsScrollInertiaEnabledProperty, value);
		}

		public static DependencyProperty IsScrollInertiaEnabledProperty
		{
			[DynamicDependency(nameof(GetIsScrollInertiaEnabled))]
			[DynamicDependency(nameof(SetIsScrollInertiaEnabled))]
			get;
		} = DependencyProperty.RegisterAttached(
				nameof(IsScrollInertiaEnabled),
				typeof(bool),
				typeof(ScrollViewer),
				new FrameworkPropertyMetadata(true));
		#endregion

		#region IsHorizontalScrollChainingEnabled (Attached DP)
		public static bool GetIsHorizontalScrollChainingEnabled(DependencyObject element)
			=> (bool)element.GetValue(IsHorizontalScrollChainingEnabledProperty);

		public static void SetIsHorizontalScrollChainingEnabled(DependencyObject element, bool isHorizontalScrollChainingEnabled)
			=> element.SetValue(IsHorizontalScrollChainingEnabledProperty, isHorizontalScrollChainingEnabled);

		public bool IsHorizontalScrollChainingEnabled
		{
			get => (bool)GetValue(IsHorizontalScrollChainingEnabledProperty);
			set => SetValue(IsHorizontalScrollChainingEnabledProperty, value);
		}

		public static DependencyProperty IsHorizontalScrollChainingEnabledProperty
		{
			[DynamicDependency(nameof(GetIsHorizontalScrollChainingEnabled))]
			[DynamicDependency(nameof(SetIsHorizontalScrollChainingEnabled))]
			get;
		} = DependencyProperty.RegisterAttached(
				"IsHorizontalScrollChainingEnabled",
				typeof(bool),
				typeof(ScrollViewer),
				new FrameworkPropertyMetadata(true));
		#endregion

		#region IsHorizontalRailEnabled (Attached DP)
		public static bool GetIsHorizontalRailEnabled(DependencyObject element)
			=> (bool)element.GetValue(IsHorizontalRailEnabledProperty);

		public static void SetIsHorizontalRailEnabled(DependencyObject element, bool isHorizontalRailEnabled)
			=> element.SetValue(IsHorizontalRailEnabledProperty, isHorizontalRailEnabled);

		public bool IsHorizontalRailEnabled
		{
			get => (bool)GetValue(IsHorizontalRailEnabledProperty);
			set => SetValue(IsHorizontalRailEnabledProperty, value);
		}

		public static DependencyProperty IsHorizontalRailEnabledProperty
		{
			[DynamicDependency(nameof(GetIsHorizontalRailEnabled))]
			[DynamicDependency(nameof(SetIsHorizontalRailEnabled))]
			get;
		} = DependencyProperty.RegisterAttached(
				"IsHorizontalRailEnabled",
				typeof(bool),
				typeof(ScrollViewer),
				new FrameworkPropertyMetadata(true));
		#endregion

		#region IsVerticalScrollChainingEnabled (Attached DP)
		public static bool GetIsVerticalScrollChainingEnabled(DependencyObject element)
			=> (bool)element.GetValue(IsVerticalScrollChainingEnabledProperty);

		public static void SetIsVerticalScrollChainingEnabled(DependencyObject element, bool isVerticalScrollChainingEnabled)
			=> element.SetValue(IsVerticalScrollChainingEnabledProperty, isVerticalScrollChainingEnabled);

		public bool IsVerticalScrollChainingEnabled
		{
			get => (bool)GetValue(IsVerticalScrollChainingEnabledProperty);
			set => SetValue(IsVerticalScrollChainingEnabledProperty, value);
		}

		public static DependencyProperty IsVerticalScrollChainingEnabledProperty
		{

			[DynamicDependency(nameof(GetIsVerticalScrollChainingEnabled))]
			[DynamicDependency(nameof(SetIsVerticalScrollChainingEnabled))]
			get;
		} = DependencyProperty.RegisterAttached(
				"IsVerticalScrollChainingEnabled",
				typeof(bool),
				typeof(ScrollViewer),
				new FrameworkPropertyMetadata(true));
		#endregion

		#region IsVerticalRailEnabled (Attached DP)
		public static bool GetIsVerticalRailEnabled(DependencyObject element)
			=> (bool)element.GetValue(IsVerticalRailEnabledProperty);

		public static void SetIsVerticalRailEnabled(DependencyObject element, bool isVerticalRailEnabled)
			=> element.SetValue(IsVerticalRailEnabledProperty, isVerticalRailEnabled);

		public bool IsVerticalRailEnabled
		{
			get => (bool)GetValue(IsVerticalRailEnabledProperty);
			set => SetValue(IsVerticalRailEnabledProperty, value);
		}

		public static DependencyProperty IsVerticalRailEnabledProperty
		{
			[DynamicDependency(nameof(GetIsVerticalRailEnabled))]
			[DynamicDependency(nameof(SetIsVerticalRailEnabled))]
			get;
		} = DependencyProperty.RegisterAttached(
				"IsVerticalRailEnabled",
				typeof(bool),
				typeof(ScrollViewer),
				new FrameworkPropertyMetadata(true));
		#endregion

		internal Size ScrollBarSize => (_presenter as ScrollContentPresenter)?.ScrollBarSize ?? default;

		private bool ChangeViewNative(double? horizontalOffset, double? verticalOffset, double? zoomFactor, bool disableAnimation)
			=> (_presenter as ScrollContentPresenter)?.Set(horizontalOffset, verticalOffset, disableAnimation: disableAnimation) ?? true;

		private partial void OnLoadedPartial() { }
		private partial void OnUnloadedPartial() { }

		#region Over scroll support
		/// <summary>
		/// Trim excess scroll, which can be present if the content size is reduced.
		/// </summary>
		partial void TrimOverscroll(Orientation orientation)
		{
			if (_presenter is not null)
			{
				var (contentExtent, presenterViewportSize, offset) = orientation switch
				{
					Orientation.Vertical => (ExtentHeight, ViewportHeight, VerticalOffset),
					_ => (ExtentWidth, ViewportWidth, HorizontalOffset),
				};
				var viewportEnd = offset + presenterViewportSize;
				var overscroll = contentExtent - viewportEnd;
				if (offset > 0 && overscroll < -0.5)
				{
					ChangeViewForOrientation(orientation, overscroll);
				}
			}
		}

		private void ChangeViewForOrientation(Orientation orientation, double scrollAdjustment)
		{
			if (orientation == Orientation.Vertical)
			{
				ChangeView(null, VerticalOffset + scrollAdjustment, null, disableAnimation: true);
			}
			else
			{
				ChangeView(HorizontalOffset + scrollAdjustment, null, null, disableAnimation: true);
			}
		}
		#endregion

		#region End-anchor tracking on extent growth

		// Forces synchronous convergence to the trailing edge when a programmatic ChangeView lands
		// against an under-estimated extent (e.g. ItemsRepeater virtualizing a high-variance tail).
		// Native WinUI converges via element-anchor-driven origin tracking over multiple
		// EffectiveViewportChanged → measure → arrange cycles; the managed presenter does not iterate
		// the same way, so without this hook the user would see content several items short of the
		// end until additional layout passes ran. Gated by ArmFollowExtentGrowth so this only runs
		// for programmatic "scroll to end" calls — never for wheel/touch input, which would
		// otherwise fight the user when re-realization momentarily inflates the average size.
		private bool _isFollowingExtentGrowth;
		private bool _followExtentGrowthArmedVertical;
		private bool _followExtentGrowthArmedHorizontal;

		partial void ArmFollowExtentGrowth(Orientation orientation)
		{
			if (orientation == Orientation.Vertical)
			{
				_followExtentGrowthArmedVertical = true;
			}
			else
			{
				_followExtentGrowthArmedHorizontal = true;
			}
		}

		partial void FollowExtentGrowthIfAtEnd(Orientation orientation, double oldOffset, double oldScrollable)
		{
			if (_presenter is null || _isFollowingExtentGrowth)
			{
				return;
			}

			// Consume the per-orientation arm token. If the workaround re-issues ChangeView below,
			// that re-issuance arms the next arrange via the public ChangeView path.
			bool wasArmed;
			if (orientation == Orientation.Vertical)
			{
				wasArmed = _followExtentGrowthArmedVertical;
				_followExtentGrowthArmedVertical = false;
			}
			else
			{
				wasArmed = _followExtentGrowthArmedHorizontal;
				_followExtentGrowthArmedHorizontal = false;
			}

			if (!wasArmed)
			{
				return;
			}

			const double Tolerance = 0.5;

			var (newScrollable, newOffset) = orientation switch
			{
				Orientation.Vertical => (ScrollableHeight, VerticalOffset),
				_ => (ScrollableWidth, HorizontalOffset),
			};

			// Only follow when the previous state was "scrolled to the end of a non-trivially scrollable
			// region". oldScrollable > Tolerance excludes the initial empty state where extent==viewport
			// (otherwise, async content insertion would auto-scroll to the bottom on first measure).
			if (oldScrollable <= Tolerance ||
				oldOffset < oldScrollable - Tolerance ||
				newScrollable <= oldScrollable + Tolerance ||
				newScrollable <= newOffset + Tolerance)
			{
				return;
			}

			try
			{
				_isFollowingExtentGrowth = true;
				if (orientation == Orientation.Vertical)
				{
					ChangeView(null, newScrollable, null, disableAnimation: true);
				}
				else
				{
					ChangeView(newScrollable, null, null, disableAnimation: true);
				}
			}
			finally
			{
				_isFollowingExtentGrowth = false;
			}
		}
		#endregion
	}
}
#endif
