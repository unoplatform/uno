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
		{
#if __SKIA__
			// MUX Reference ScrollViewer_Partial.cpp:3385 ChangeViewInternal — before
			// applying the new view, the WinUI port raises ViewChanging with the
			// computed target view so app code can observe the in-flight change.
			// On Skia the rendered scroll is driven by SCP.Set(...) below; this
			// block sources the same NextView/FinalView args the C++ port would.
			// Phase-4 ChangeViewInternal will own this entirely; until then, this
			// is the bridge.

			// MUX Reference: when a programmatic ChangeView arrives during inertia,
			// the inertia is aborted. This matches C++ ScrollViewer behavior where
			// ChangeViewInternal calls StopInertialManipulation as a side effect.
			if (IsInDirectManipulation && m_isInertial)
			{
				StopInertialManipulation();
			}

			var targetH = horizontalOffset ?? HorizontalOffset;
			var targetV = verticalOffset ?? VerticalOffset;
			var targetZ = zoomFactor.HasValue ? (float)zoomFactor.Value : ZoomFactor;
			if (zoomFactor.HasValue)
			{
				NotifyZoomFactorChanging(targetZ);
				// MUX Reference ScrollViewer_Partial.cpp:3385 — ChangeViewInternal
				// processes a zoom factor change by routing through ZoomToFactorInternal,
				// which coerces against MinZoomFactor/MaxZoomFactor and updates the
				// public ZoomFactor DP. Without this, the SV's ZoomFactor property
				// stays at its old value after a ChangeView call that requested zoom.
				ZoomToFactorInternal(targetZ, delayAndFlushViewChanged: false, out _);
			}
			if (horizontalOffset.HasValue)
			{
				NotifyHorizontalOffsetChanging(targetH, targetV);
			}
			if (verticalOffset.HasValue)
			{
				NotifyVerticalOffsetChanging(targetH, targetV);
			}
#endif
			return (_presenter as ScrollContentPresenter)?.Set(horizontalOffset, verticalOffset, disableAnimation: disableAnimation) ?? true;
		}

		private partial void OnLoadedPartial()
		{
#if __SKIA__
			OnLoadedCore();
#endif
		}

		private partial void OnUnloadedPartial()
		{
#if __SKIA__
			OnUnloadedCore();
#endif
		}

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
	}
}
#endif
