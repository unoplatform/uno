#nullable enable

using System;
using DirectUI;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Uno.Disposables;
using Uno.UI.DataBinding;
using Uno.UI.Xaml.Controls;
using Uno.UI.Xaml.Core;
using Uno.UI.Xaml.Input;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls
{
	partial class ScrollViewer
	{
		private IDisposable? _directManipulationHandlerSubscription;

		// MUX Reference ScrollViewer_Partial.cpp, IsContentScrollable, tag winui3/release/2.5.1
		// Determine if content can be scrolled.
		// It can, if for either dimension scrolling is enabled AND content size is greater than size of viewport.
		internal void IsContentScrollable(
			bool ignoreScrollMode,
			bool ignoreScrollBarVisibility,
			out bool isContentHorizontallyScrollable,
			out bool isContentVerticallyScrollable)
		{
			isContentHorizontallyScrollable = false;
			isContentVerticallyScrollable = false;

			if (IsEnabled)
			{
				// TODO Uno: no IScrollInfo, the viewport and extent always come from the ScrollViewer itself.
				// TODO Uno: GetEffective*ScrollMode is not ported, Auto counts as enabled.
				double minOffset = 0.0;

				var horizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
				var verticalScrollBarVisibility = ScrollBarVisibility.Disabled;

				if (!ignoreScrollBarVisibility)
				{
					horizontalScrollBarVisibility = HorizontalScrollBarVisibility;
					verticalScrollBarVisibility = VerticalScrollBarVisibility;
				}

				if ((ignoreScrollMode || VerticalScrollMode != ScrollMode.Disabled) &&
					(ignoreScrollBarVisibility || verticalScrollBarVisibility != ScrollBarVisibility.Disabled) &&
					(ExtentHeight - minOffset) > ViewportHeight)
				{
					isContentVerticallyScrollable = true;
				}

				if ((ignoreScrollMode || HorizontalScrollMode != ScrollMode.Disabled) &&
					(ignoreScrollBarVisibility || horizontalScrollBarVisibility != ScrollBarVisibility.Disabled) &&
					(ExtentWidth - minOffset) > ViewportWidth)
				{
					isContentHorizontallyScrollable = true;
				}
			}
		}

		private bool m_isPointerLeftButtonPressed;

		internal bool m_templatedParentHandlesMouseButton;

		// Allow to set focus on ScrollViewer itself. For example, Flyout inner ScrollViewer.
		internal bool m_isFocusableOnFlyoutScrollViewer;

		// Indicates whether ScrollViewer should ignore mouse wheel scroll events (not zoom).
		internal bool ArePointerWheelEventsIgnored { get; set; }
		// Uno drives touch scrolling, its inertia and snapping from the presenter rather than through DirectManipulation.
		internal bool IsInManipulation => IsInDirectManipulation || m_isInConstantVelocityPan || (_presenter as ScrollContentPresenter)?.IsInMotion == true;

		/// <summary>
		/// Gets or set whether the <see cref="ScrollViewer"/> will allow scrolling outside of the ScrollViewer's Child bound.
		/// </summary>
		///
		private bool _forceChangeToCurrentView;
		internal bool ForceChangeToCurrentView
		{
			get => _forceChangeToCurrentView;
			set
			{
				_forceChangeToCurrentView = value;

#if __SKIA__
				if (_presenter != null)
				{
					_presenter.ForceChangeToCurrentView = value;
				}
#endif
			}
		}
		internal bool IsInDirectManipulation { get; }
		internal bool TemplatedParentHandlesScrolling { get; set; }
		internal Func<AutomationPeer>? AutomationPeerFactoryIndex { get; set; }

		internal bool BringIntoViewport(Rect bounds,
			bool skipDuringTouchContact,
			bool skipAnimationWhileRunning,
			bool animate)
		{
			return ChangeView(bounds.X, bounds.Y, null, !animate);
		}

		internal void SetDirectManipulationStateChangeHandler(IDirectManipulationStateChangeHandler? handler)
		{
			_directManipulationHandlerSubscription?.Dispose();

			if (handler is null)
			{
				return;
			}

			var weakHandler = WeakReferencePool.RentWeakReference(this, handler);
			UpdatesMode = ScrollViewerUpdatesMode.Synchronous;
			ViewChanged += OnViewChanged;
			_directManipulationHandlerSubscription = Disposable.Create(() =>
			{
				ViewChanged -= OnViewChanged;
				WeakReferencePool.ReturnWeakReference(this, weakHandler);
			});

			void OnViewChanged(object? sender, ScrollViewerViewChangedEventArgs args)
			{
				if (args.IsIntermediate)
				{
					return;
				}

				if (weakHandler.Target is IDirectManipulationStateChangeHandler h)
				{
					h.NotifyStateChange(DMManipulationState.DMManipulationCompleted, default, default, default, default, default, default, default, default);
				}
			}
		}

		protected override void OnPointerPressed(PointerRoutedEventArgs e)
		{
			// If our templated parent is handling mouse button, we should not take
			// focus away.  They're handling it, not us.
			if (m_templatedParentHandlesMouseButton)
			{
				return;
			}

			var spPointerPoint = e.GetCurrentPoint(this);
			var spPointerProperties = spPointerPoint.Properties;
			m_isPointerLeftButtonPressed = spPointerProperties.IsLeftButtonPressed;

			// Don't handle PointerPressed event to raise up
		}

		protected override void OnPointerReleased(PointerRoutedEventArgs e)
		{
			base.OnPointerReleased(e);
			var handled = e.Handled;
			if (handled)
			{
				return;
			}

			if (m_isPointerLeftButtonPressed)
			{
				//GestureModes gestureFollowing = GestureModes.None;

				// Reset the pointer left button pressed state
				m_isPointerLeftButtonPressed = false;

				//var gestureFollowing = e.GestureFollowing;

				//if (gestureFollowing == GestureModes.RightTapped)
				//{
				//	// Schedule the focus change for OnRightTappedUnhandled.
				//	m_shouldFocusOnRightTapUnhandled = true;
				//}
				//else
				{
					bool isFocusedOnLightDismissPopupOfFlyout = false;

					// Set focus on the Flyout inner ScrollViewer to dismiss IHM.
					if (m_isFocusableOnFlyoutScrollViewer)
					{
						isFocusedOnLightDismissPopupOfFlyout = ScrollContentControl_SetFocusOnFlyoutLightDismissPopupByPointer(this);
					}
					if (isFocusedOnLightDismissPopupOfFlyout)
					{
						e.Handled = true;
					}
					else
					{
						bool scrollViewerIsFocusAncestor = false;
						var scrollViewerIsTabStop = IsTabStop;

						if (VisualTree.GetFocusManagerForElement(this) is FocusManager focusManager)
						{
							if (focusManager.FocusedElement is DependencyObject currentFocusedElement)
							{
								scrollViewerIsFocusAncestor = this.IsAncestorOf(currentFocusedElement);
							}
						}

						if (!scrollViewerIsTabStop && scrollViewerIsFocusAncestor)
						{
							// Do not take focus when:
							// - ScrollViewer.IsTabStop is False and
							// - Currently focused element is within this ScrollViewer.
							// In that case, leave the focus as is by marking this event handled.
							e.Handled = true;
						}
						else
						{
							// Focus now.
							// Set focus to the ScrollViewer to capture key input for scrolling
							var focused = Focus(FocusState.Pointer);
							e.Handled = focused;
						}
					}
				}
			}
		}

		private static bool ScrollContentControl_SetFocusOnFlyoutLightDismissPopupByPointer(UIElement pScrollContentControl)
		{
			PopupRoot? pPopupRoot = null;
			Popup? pPopup = null;

			if (pScrollContentControl.GetRootOfPopupSubTree() is not null)
			{
				pPopupRoot = VisualTree.GetPopupRootForElement(pScrollContentControl);
				if (pPopupRoot is not null)
				{
					pPopup = pPopupRoot.GetTopmostPopup(PopupRoot.PopupFilter.LightDismissOnly);
					// Uno-specific: We don't yet have GetSavedFocusState()
					if (pPopup is not null && FocusSelection.ShouldUpdateFocus(pPopup.Child, pPopup.FocusState/*.GetSavedFocusState()*/) && pPopup.IsForFlyout)
					{
						bool wasFocusUpdated = pPopup.Focus(FocusState.Pointer, false /*animateIfBringIntoView*/);
						return wasFocusUpdated;
					}
				}
			}

			return false;
		}
	}

	internal interface IDirectManipulationStateChangeHandler
	{
		// zCumulativeFactor: if the zoom factor was 1.5 at the beginning of the manipulation,
		// and the current zoom factor is 3.0, then zCumulativeFactor is 2.0.
		// xCenter/yCenter: these coordinates are in relation to the top/left corner of the
		// manipulated element. They might be negative if the ScrollViewer content is smaller
		// than the viewport.
		void NotifyStateChange(
			DMManipulationState state,
			float xCumulativeTranslation,
			float yCumulativeTranslation,
			float zCumulativeFactor,
			float xCenter,
			float yCenter,
			bool isInertial,
			bool isTouchConfigurationActivated,
			bool isBringIntoViewportConfigurationActivated);
	}
}
