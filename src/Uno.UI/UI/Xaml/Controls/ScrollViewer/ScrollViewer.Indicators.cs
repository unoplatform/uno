#nullable enable

using System;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Input;
using Uno.UI;
using Uno.UI.DataBinding;
using Windows.System;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class ScrollViewer
	{
		#region Scroll indicators visual states (Managed scroll bars only)

		private static readonly TimeSpan _indicatorResetDelay = FeatureConfiguration.ScrollViewer.DefaultAutoHideDelay ?? TimeSpan.FromSeconds(4);
		private static readonly bool _indicatorResetDisabled = _indicatorResetDelay == TimeSpan.MaxValue;
		private DispatcherQueueTimer? _indicatorResetTimer;
		private string? _indicatorState;
		//private bool m_isInIntermediateViewChangedMode;
		//private bool m_isViewChangedRaisedInIntermediateMode;
		//private bool m_isDraggingThumb;

		private void PrepareScrollIndicator() // OnApplyTemplate
		{
			if (_indicatorResetDisabled)
			{
				ShowScrollIndicator(PointerDeviceType.Mouse, forced: true);
			}
			else
			{
				ResetScrollIndicator(forced: true);
			}
		}

		private static void ShowScrollIndicator(object sender, PointerRoutedEventArgs e) // OnPointerMove
			=> (sender as ScrollViewer)?.ShowScrollIndicator(e.Pointer.PointerDeviceType);

		private void ShowScrollIndicator(PointerDeviceType type, bool forced = false)
		{
			if (!forced && !ComputedIsVerticalScrollEnabled && !ComputedIsHorizontalScrollEnabled)
			{
				return;
			}

			var indicatorState = type switch
			{
				PointerDeviceType.Touch => VisualStates.ScrollingIndicator.Touch,
				_ => VisualStates.ScrollingIndicator.Mouse // Mouse and pen are using the MouseIndicator
			};
			if (_indicatorState != indicatorState) // Avoid costly GoToState if useless
			{
				VisualStateManager.GoToState(this, indicatorState, true);
				_indicatorState = indicatorState;
			}

			if (_indicatorResetDisabled)
			{
				return;
			}

			// Automatically hide the scroll indicator after a delay without any interaction
			if (_indicatorResetTimer == null)
			{
				var weakRef = WeakReferencePool.RentSelfWeakReference(this);
				_indicatorResetTimer = new DispatcherQueueTimer
				{
					Interval = _indicatorResetDelay,
					IsRepeating = false
				};
				_indicatorResetTimer.Tick += (snd, e) => (weakRef.Target as ScrollViewer)?.ResetScrollIndicator();
			}
			_indicatorResetTimer.Start(); // Starts or restarts the reset timer
		}

		private static void ResetScrollIndicator(object sender, RoutedEventArgs _) // OnUnloaded
			=> (sender as ScrollViewer)?.ResetScrollIndicator(forced: true);

		private void ResetScrollIndicator(bool forced = false)
		{
			if (_indicatorResetDisabled)
			{
				return;
			}

			_indicatorResetTimer?.Stop();

			if (!forced && ((_horizontalScrollbar?.IsPointerOver ?? false) || (_verticalScrollbar?.IsPointerOver ?? false)))
			{
				// We don't auto hide the indicators if the pointer is over it!
				// Note: the pointer has to move over this ScrollViewer to exit the ScrollBar, so we will restart the reset timer!
				return;
			}

			VisualStateManager.GoToState(this, VisualStates.ScrollingIndicator.None, true);
			_indicatorState = VisualStates.ScrollingIndicator.None;

			VisualStateManager.GoToState(this, VisualStates.ScrollBarsSeparator.Collapsed, true);
		}

		private void ShowScrollBarSeparator(object sender, PointerRoutedEventArgs e) // ScrollBar.OnPointerEntered
		{
			if (e.Pointer.PointerDeviceType == PointerDeviceType.Touch)
			{
				return; // The separator is needed only for the MouseIndicator (Mouse and Pen)
			}

			if (IsAnimationEnabled || !VisualStateManager.GoToState(this, VisualStates.ScrollBarsSeparator.ExpandedWithoutAnimation, true))
			{
				VisualStateManager.GoToState(this, VisualStates.ScrollBarsSeparator.Expanded, true);
			}
		}

		private void HideScrollBarSeparator(object sender, PointerRoutedEventArgs e) // ScrollBar.OnPointerExited
		{
			if (IsAnimationEnabled || !VisualStateManager.GoToState(this, VisualStates.ScrollBarsSeparator.CollapsedWithoutAnimation, true))
			{
				VisualStateManager.GoToState(this, VisualStates.ScrollBarsSeparator.Collapsed, true);
			}
		}
		#endregion
	}
}
