#if __WASM__ || __SKIA__
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Uno.Extensions;
using Uno.Extensions.Specialized;
using Microsoft.UI.Xaml.Controls.Primitives;
using Uno;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Windows.Foundation.Collections;
using Windows.System;
using Microsoft.UI.Xaml.Automation.Peers;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI.Xaml.Automation;
using Uno.UI.Xaml;
using Uno.Disposables;
using DirectUI;
using Uno.UI.Xaml.Core;
using Uno.UI.Xaml.Input;

using Microsoft.UI.Input;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class FlipView : Selector
	{
		const string UIA_FLIPVIEW_PREVIOUS = nameof(UIA_FLIPVIEW_PREVIOUS);
		const string UIA_FLIPVIEW_NEXT = nameof(UIA_FLIPVIEW_NEXT);

		event EventHandler<ScrollViewerViewChangedEventArgs> m_epScrollViewerViewChangedHandler;

		const int TICKS_PER_MILLISECOND = 10000;

		// Minimum required time between mouse wheel inputs for triggering successive flips
		const int FLIP_VIEW_DISTINCT_SCROLL_WHEEL_DELAY_MS = 200;

		// How long the FlipView's navigation buttons show before fading out.
		const int FLIP_VIEW_BUTTONS_SHOW_DURATION_MS = 3000;

		static int s_scrollWheelDelayMS = FLIP_VIEW_DISTINCT_SCROLL_WHEEL_DELAY_MS;

		// Dispatcher timer to set correct offset values after size changed
		DispatcherTimer m_tpFixOffsetTimer;

		// Stores a reference to the ButtonLayer part.
		//Panel* m_pButtonLayerPart;

		// Stores a reference to the  PreviousButtonHorizontalpart.
		ButtonBase m_tpPreviousButtonHorizontalPart;

		// Stores a reference to the NextButtonHorizontal part.
		ButtonBase m_tpNextButtonHorizontalPart;

		// Stores a reference to the PreviousButtonVertical part.
		ButtonBase m_tpPreviousButtonVerticalPart;

		// Stores a reference to the NextButtonVertical part.
		ButtonBase m_tpNextButtonVerticalPart;

		// Stores a value indicating whether we should show next/prev navigation buttons.
		bool m_showNavigationButtons;

		// Stores a value to whether a FocusRect should be shown.
		bool m_ShouldShowFocusRect;

		// Dispatcher timer causing the buttons to fade out after FLIP_VIEW_BUTTONS_SHOW_DURATION_MS.
		DispatcherTimer m_tpButtonsFadeOutTimer;

		// TRUE if new SelectedIndex is being animated into view.
		bool m_animateNewIndex;
		// TRUE if selection change is due to a touch manipulation.
		bool m_skipAnimationOnce;

		bool m_itemsAreSized;

		// True if we are in a Measure/Arrange pass. We need this to make sure that we don't change the SelectedIndex due to
		// the scroll position changing during a resize.
		bool m_inMeasure;
		bool m_inArrange;

		// Saved SnapPointsTypes. These are saved in the beginning of an animation and restored when the animation is completed.
		SnapPointsType m_verticalSnapPointsType;
		SnapPointsType m_horizontalSnapPointsType;

		// A value indicating the last time a scroll wheel event occurred.
		long m_lastScrollWheelTime;

		// A value indicating the last wheel delta a scroll wheel event contained.
		int m_lastScrollWheelDelta;

		bool m_keepNavigationButtonsVisible;

		bool m_moveFocusToSelectedItem;

		private readonly SerialDisposable _fixOffsetSubscription = new SerialDisposable();
		private readonly SerialDisposable _buttonsFadeOutTimerSubscription = new SerialDisposable();

		partial void InitializePartial()
		{
			m_showNavigationButtons = false;
			m_ShouldShowFocusRect = false;
			m_animateNewIndex = false;
			m_verticalSnapPointsType = SnapPointsType.None;
			m_horizontalSnapPointsType = SnapPointsType.None;
			m_skipAnimationOnce = false;
			m_lastScrollWheelDelta = 0;
			m_lastScrollWheelTime = 0;
			m_keepNavigationButtonsVisible = false;
			m_itemsAreSized = false;
		}

		internal override void EnterImpl(EnterParams @params, int depth)
		{
			base.EnterImpl(@params, depth);

			HookTemplate();
		}

		internal override void LeaveImpl(LeaveParams @params)
		{
			base.LeaveImpl(@params);

			UnhookTemplate();
		}

		protected override void OnApplyTemplate()
		{
			var oldSV = m_tpScrollViewer;
			base.OnApplyTemplate();

			HookTemplate();

			// Uno docs: Due to differences in Uno's FlipView and WinUI's FlipView (i.e, when the ScrollViewer is
			// initialized - related to OnItemsHostAvailable which is missing in Uno), the base OnApplyTemplate can
			// mess up with m_tpScrollViewer. So, we bring it back.
			m_tpScrollViewer ??= oldSV;
		}

		private void HookTemplate()
		{
			// Unhook from old Template
			UnhookTemplate();

			InitializeScrollViewer();

			string strAutomationName;
			ButtonBase spPreviousButtonHorizontalPart;
			ButtonBase spNextButtonHorizontalPart;
			ButtonBase spPreviousButtonVerticalPart;
			ButtonBase spNextButtonVerticalPart;

			//Set event handlers for the 4 FlipView buttons
			spPreviousButtonHorizontalPart = CreateButtonClickEventHandler("PreviousButtonHorizontal", OnPreviousButtonPartClick);

			m_tpPreviousButtonHorizontalPart = spPreviousButtonHorizontalPart;

			if (m_tpPreviousButtonHorizontalPart != null)
			{
				m_tpPreviousButtonHorizontalPart.PointerEntered += OnPointerEnteredNavigationButtons;
				m_tpPreviousButtonHorizontalPart.PointerExited += OnPointerExitedNavigationButtons;

				strAutomationName = AutomationProperties.GetName(m_tpPreviousButtonHorizontalPart);

				if (strAutomationName == null)
				{
					strAutomationName = DXamlCore.GetCurrentNoCreate().GetLocalizedResourceString(UIA_FLIPVIEW_PREVIOUS);
					AutomationProperties.SetName(m_tpPreviousButtonHorizontalPart, strAutomationName);
				}
			}

			spNextButtonHorizontalPart = CreateButtonClickEventHandler("NextButtonHorizontal", OnNextButtonPartClick);

			m_tpNextButtonHorizontalPart = spNextButtonHorizontalPart;

			if (m_tpNextButtonHorizontalPart != null)
			{
				m_tpNextButtonHorizontalPart.PointerEntered += OnPointerEnteredNavigationButtons;
				m_tpNextButtonHorizontalPart.PointerExited += OnPointerExitedNavigationButtons;

				strAutomationName = AutomationProperties.GetName(m_tpNextButtonHorizontalPart);

				if (strAutomationName == null)
				{
					strAutomationName = DXamlCore.GetCurrentNoCreate().GetLocalizedResourceString(UIA_FLIPVIEW_NEXT);
					AutomationProperties.SetName(m_tpNextButtonHorizontalPart, strAutomationName);
				}
			}


			spPreviousButtonVerticalPart = CreateButtonClickEventHandler("PreviousButtonVertical", OnPreviousButtonPartClick);

			m_tpPreviousButtonVerticalPart = spPreviousButtonVerticalPart;

			if (m_tpPreviousButtonVerticalPart != null)
			{
				m_tpPreviousButtonVerticalPart.PointerEntered += OnPointerEnteredNavigationButtons;
				m_tpPreviousButtonVerticalPart.PointerExited += OnPointerExitedNavigationButtons;

				strAutomationName = AutomationProperties.GetName(m_tpPreviousButtonVerticalPart);

				if (strAutomationName == null)
				{
					strAutomationName = DXamlCore.GetCurrentNoCreate().GetLocalizedResourceString(UIA_FLIPVIEW_PREVIOUS);
					AutomationProperties.SetName(m_tpPreviousButtonVerticalPart, strAutomationName);
				}
			}

			spNextButtonVerticalPart = CreateButtonClickEventHandler("NextButtonVertical", OnNextButtonPartClick);

			m_tpNextButtonVerticalPart = spNextButtonVerticalPart;

			if (m_tpNextButtonVerticalPart != null)
			{
				m_tpNextButtonVerticalPart.PointerEntered += OnPointerEnteredNavigationButtons;
				m_tpNextButtonVerticalPart.PointerExited += OnPointerExitedNavigationButtons;

				strAutomationName = AutomationProperties.GetName(m_tpNextButtonVerticalPart);

				if (strAutomationName == null)
				{
					strAutomationName = DXamlCore.GetCurrentNoCreate().GetLocalizedResourceString(UIA_FLIPVIEW_NEXT);
					AutomationProperties.SetName(m_tpNextButtonVerticalPart, strAutomationName);

				}
			}

			// Sync the logical and visual states of the control
			UpdateVisualState(false);
		}

		// Destroys an instance of the FlipView class.
		~FlipView()
		{
			//// Release and unhook from template parts/events
			UnhookTemplate();

			//// Makes sure the timers are stopped.
			//{
			//	var spButtonsFadeOutTimer = m_tpButtonsFadeOutTimer.GetSafeReference();
			//	if (spButtonsFadeOutTimer)
			//	{
			//		IGNOREHR(spButtonsFadeOutTimer.Stop());
			//	}

			//	var spFixOffsetTimer = m_tpFixOffsetTimer.GetSafeReference();
			//	if (spFixOffsetTimer)
			//	{
			//		IGNOREHR(spFixOffsetTimer.Stop());
			//	}
			//}
			// Cleanup
			/// VERIFYHR / (hr);
		}

		// Saves the Vertical/HorizontalSnapPointsTypes and clears them. This is necessary for us to
		// be able to chain animate FlipViewItems.
		void SaveAndClearSnapPointsTypes()
		{
			// Saved snap points will be restored asynchronously, so if we are still in the animation we do not want to
			// save the current values since they are barely the default values.
			if (m_tpScrollViewer != null && !m_animateNewIndex)
			{
				m_verticalSnapPointsType = m_tpScrollViewer.VerticalSnapPointsType;

				m_horizontalSnapPointsType = m_tpScrollViewer.HorizontalSnapPointsType;

				m_tpScrollViewer.ClearValue(ScrollViewer.VerticalSnapPointsTypeProperty, DependencyPropertyValuePrecedences.Local);

				m_tpScrollViewer.ClearValue(ScrollViewer.HorizontalSnapPointsTypeProperty, DependencyPropertyValuePrecedences.Local);
			}
		}

		// Restore the saved Vertical/HorizontalSnapPointsTypes
		void RestoreSnapPointsTypes()
		{
			if (m_tpScrollViewer != null)
			{
				m_tpScrollViewer.VerticalSnapPointsType = m_verticalSnapPointsType;
				m_tpScrollViewer.HorizontalSnapPointsType = m_horizontalSnapPointsType;
			}
		}

		private bool MoveNext()
		{
			int nSelectedIndex = 0;
			int nCount = 0;

			bool successfullyMoved = false;

			// Select the next index
			var spItems = Items;

			nCount = spItems.Count;
			nSelectedIndex = SelectedIndex;

			if (nSelectedIndex < (nCount - 1))
			{
				++nSelectedIndex;
				successfullyMoved = true;
			}
			else
			{
				nSelectedIndex = nCount - 1;
			}

			UpdateSelectedIndex(nSelectedIndex);

			if (successfullyMoved)
			{
				ElementSoundPlayerService.RequestInteractionSoundForElementStatic(ElementSoundKind.MoveNext, this);
			}

			return successfullyMoved;
		}

		private bool MovePrevious()
		{
			bool successfullyMoved = false;

			// Select the previous index
			var nSelectedIndex = SelectedIndex;

			if (nSelectedIndex > 0)
			{
				--nSelectedIndex;
				successfullyMoved = true;
			}

			UpdateSelectedIndex(nSelectedIndex);

			if (successfullyMoved)
			{
				// Play MovePrevious sound:
				ElementSoundPlayerService.RequestInteractionSoundForElementStatic(ElementSoundKind.MovePrevious, this);
			}

			return successfullyMoved;
		}


		// Calculates the rectangle to be brought into view from the index.
		private Rect CalculateBounds(int index)
		{
			Rect emptyRect = default;
			bool isVertical = false;
			Orientation physicalOrientation = Orientation.Vertical;
			double width = 0;
			double height = 0;

			var bounds = emptyRect;

			height = GetDesiredItemHeight();

			bounds.Height = (float)(height);

			width = GetDesiredItemWidth();

			bounds.Width = (float)(width);

			(physicalOrientation, _) = GetItemsHostOrientations();

			isVertical = physicalOrientation == Orientation.Vertical;

			if (isVertical)
			{
				bounds.Y = bounds.Height * index;
				global::System.Diagnostics.Debug.Assert(bounds.X == 0);
			}
			else
			{
				bounds.X = bounds.Width * index;
				global::System.Diagnostics.Debug.Assert(bounds.Y == 0);
			}

			return bounds;
		}

		private void UpdateSelectedIndex(int index)
		{
			//The following statement will trigger OnSelectionChanged
			SelectedIndex = index;

			//Focus state needs to be keyboard when using navigation keys
			SetFocusedItem(index, false, true, FocusState.Pointer, false);
		}

		void UnhookTemplate()
		{
			if (m_tpPreviousButtonHorizontalPart != null)
			{
				m_tpPreviousButtonHorizontalPart.PointerEntered -= OnPointerEnteredNavigationButtons;
				m_tpPreviousButtonHorizontalPart.PointerExited -= OnPointerExitedNavigationButtons;

				m_tpPreviousButtonHorizontalPart.Click -= OnPreviousButtonPartClick;
			}

			if (m_tpNextButtonHorizontalPart != null)
			{
				m_tpNextButtonHorizontalPart.PointerEntered -= OnPointerEnteredNavigationButtons;
				m_tpNextButtonHorizontalPart.PointerExited -= OnPointerExitedNavigationButtons;

				m_tpNextButtonHorizontalPart.Click -= OnNextButtonPartClick;
			}

			if (m_tpPreviousButtonVerticalPart != null)
			{
				m_tpPreviousButtonVerticalPart.PointerEntered -= OnPointerEnteredNavigationButtons;
				m_tpPreviousButtonVerticalPart.PointerExited -= OnPointerExitedNavigationButtons;

				m_tpPreviousButtonVerticalPart.Click -= OnPreviousButtonPartClick;
			}

			if (m_tpNextButtonVerticalPart != null)
			{
				m_tpNextButtonVerticalPart.PointerEntered -= OnPointerEnteredNavigationButtons;
				m_tpNextButtonVerticalPart.PointerExited -= OnPointerExitedNavigationButtons;

				m_tpNextButtonVerticalPart.Click -= OnNextButtonPartClick;
			}

			if (m_tpScrollViewer != null)
			{
				m_tpScrollViewer.SizeChanged -= OnScrollingHostPartSizeChanged;

				m_tpScrollViewer.ViewChanged -= OnScrollViewerViewChanged;
			}

			m_tpButtonsFadeOutTimer?.Stop();
			m_tpFixOffsetTimer?.Stop();

			_fixOffsetSubscription.Disposable = null;
			_buttonsFadeOutTimerSubscription.Disposable = null;
		}

		ButtonBase CreateButtonClickEventHandler(string buttonName, RoutedEventHandler eventHandler)
		{
			ButtonBase spButtonBasePart;

			GetTemplatePart<ButtonBase>(buttonName, out spButtonBasePart);

			if (spButtonBasePart != null)
			{
				spButtonBasePart.Click += eventHandler;
			}

			return spButtonBasePart;
		}

		void InitializeScrollViewer()
		{
			// Uno-specific: The way we call InitializeScrollViewer is different from WinUI.
			// In WinUI, this is called in OnItemsHostAvailable, which isn't available in Uno.
			// In Uno, we call this in HookTemplate, which is called in Enter.
			// If the FlipView enters the visual tree, then leaves it, then enters again, what will happen is:
			// 1) The first Enter will set the m_tpScrollViewer and subscribe to SizeChanged and ViewChanged.
			// 2) The leave will UnhookTemplate which will unsubscribe from SizeChanged and ViewChanged.
			// 3) The second Enter will call InitializeScrollViewer again, but this time m_tpScrollViewer is not null.
			// 4) This means we won't subscribe to SizeChanged and ViewChanged again if we have the following null check.
			//if (m_tpScrollViewer == null)
			{
				ScrollViewer spScrollViewer;

				GetTemplatePart<ScrollViewer>("ScrollingHost", out spScrollViewer);
				if (spScrollViewer is null)
				{
					return;
				}

				m_tpScrollViewer = spScrollViewer;
				m_tpScrollViewer.ForceChangeToCurrentView = true;
				m_tpScrollViewer.IsHorizontalScrollChainingEnabled = false;

				if (m_tpScrollViewer != null)
				{
					Orientation physicalOrientation = Orientation.Vertical;

					// Ignore mouse wheel scroll events to route them to the parent
					m_tpScrollViewer.ArePointerWheelEventsIgnored = true;

					// Set ScrollViewer's properties based o ItemsHost orientation
					(physicalOrientation, _) = GetItemsHostOrientations();

					if (physicalOrientation == Orientation.Vertical)
					{
						// Disable horizontal scrolling and enable vertical scrolling
						m_tpScrollViewer.HorizontalScrollMode = ScrollMode.Disabled;
						m_tpScrollViewer.VerticalScrollMode = ScrollMode.Enabled;
					}
					else
					{
						// Disable vertical scrolling and enable horizontal scrolling
						m_tpScrollViewer.VerticalScrollMode = ScrollMode.Disabled;
						m_tpScrollViewer.HorizontalScrollMode = ScrollMode.Enabled;
					}

					m_tpScrollViewer.SizeChanged += OnScrollingHostPartSizeChanged;

					//(m_tpScrollViewer as ScrollViewer)?.SetDirectManipulationStateChangeHandler((DirectManipulationStateChangeHandler)(this));

					//(m_epScrollViewerViewChangedHandler.AttachEventHandler((m_tpScrollViewer as ScrollViewer) ?,


					//	[this](DependencyObject pSender, IScrollViewerViewChangedEventArgs pArgs)

					//{
					//	return OnScrollViewerViewChanged(pSender, pArgs);
					//}));

					m_tpScrollViewer.ViewChanged += OnScrollViewerViewChanged;
				}
			}
		}

#pragma warning disable IDE0051 // Private member 'FlipView.OnScrollViewerViewChanged' is unused
		void OnScrollViewerViewChanged(object pSender, ScrollViewerViewChangedEventArgs pArgs)
		{
			bool isIntermediate = true;
			isIntermediate = pArgs.IsIntermediate;

			if (!isIntermediate && m_itemsAreSized && !m_inMeasure && !m_inArrange)
			{
				int nSelectedIndex = 0;
				double offset = 0;
				double viewportSize = 0;
				int nCount = 0;

				var spItems = Items;

				nCount = spItems.Count;

				if (nCount > 0)
				{
					// FlipView contains at least 1 FlipViewItem
					// Update SelectedIndex
					Orientation physicalOrientation = Orientation.Vertical;

					(physicalOrientation, _) = GetItemsHostOrientations();

					if (physicalOrientation == Orientation.Vertical)
					{
						offset = m_tpScrollViewer.VerticalOffset;
						viewportSize = m_tpScrollViewer.ViewportHeight;
					}
					else
					{
						offset = m_tpScrollViewer.HorizontalOffset;
						viewportSize = m_tpScrollViewer.ViewportWidth;
					}

					if (viewportSize > 0)
					{
						bool bIsVirtualizing = false;
						bIsVirtualizing = (bool)this.GetValue(VirtualizingStackPanel.IsVirtualizingProperty);

						if (!m_animateNewIndex)
						{
							bool hasPendingSelectionChange = false;

							// When the m_tpFixOffsetTimer was started and thus there is a pending view change to sync
							// up with the current SelectedIndex, do not change that SelectedIndex. This situation occurs
							// when the SelectedIndex is changed during a view animation caused by a prior SelectedIndex change.
							if (m_tpFixOffsetTimer != null)
							{
								hasPendingSelectionChange = m_tpFixOffsetTimer.IsEnabled;
							}
							if (!hasPendingSelectionChange)
							{
								// Round to the nearest FlipViewItem
								m_skipAnimationOnce = true;
								nSelectedIndex = (int)(Math.Round(bIsVirtualizing ? ItemsPresenter.OffsetToIndex(offset) : (offset / viewportSize), 0));
								SelectedIndex = nSelectedIndex;
								m_skipAnimationOnce = false;
							}
						}
						else
						{
							// No need to update the selected index since it is already at the correct value.
							m_animateNewIndex = false;
							RestoreSnapPointsTypes();
						}

						if (m_moveFocusToSelectedItem)
						{
							int selectedIndex = 0;
							selectedIndex = SelectedIndex;
							if (selectedIndex >= 0)
							{
								DependencyObject spContainer;
								spContainer = ContainerFromIndex(selectedIndex);
								if (spContainer != null)
								{
									// We previously received focus on a non-selected item at a time when the container for the selected item
									// was not realized. We move focus to the selected item now that it is available.
									// We do this in the ScrollViewerChanged event since there is no other callback that we use to determine when a
									// particular container has been realized by the virtualizing panel.
									m_moveFocusToSelectedItem = false;
									SetFocusedItem(selectedIndex,
										shouldScrollIntoView: false,
										forceFocus: false,
										FocusState.Programmatic,
										animateIfBringIntoView: false);
								}
							}
						}
					}
				}
			}
		}
#pragma warning restore IDE0051 // Private member 'FlipView.OnScrollViewerViewChanged' is unused

#if false
		void OnItemsHostAvailable()
		{
			InitializeScrollViewer();
		}
#endif

		protected override void OnPointerWheelChanged(PointerRoutedEventArgs pArgs)
		{
			bool handled = false;

			base.OnPointerWheelChanged(pArgs);

			handled = pArgs.Handled;

			if (!handled)
			{
				VirtualKeyModifiers keyModifiers = VirtualKeyModifiers.None;
				bool isCtrlPressed = false;

				keyModifiers = PlatformHelpers.GetKeyboardModifiers();

				isCtrlPressed = keyModifiers.HasFlag(VirtualKeyModifiers.Control);

				if (!isCtrlPressed)
				{
					long lTimeCurrent = default;
					bool canFlip = false;
					bool queryCounterSuccess = false;

					queryCounterSuccess = QueryPerformanceCounter(out lTimeCurrent);

					if (queryCounterSuccess)
					{
						PointerPoint spPointerPoint;
						PointerPointProperties spPointerProperties;
						int mouseWheelDelta = 0;

						spPointerPoint = pArgs.GetCurrentPoint(this);
						if (spPointerPoint == null) throw new ArgumentNullException();
						spPointerProperties = spPointerPoint.Properties;
						if (spPointerProperties == null) throw new ArgumentNullException();
						mouseWheelDelta = spPointerProperties.MouseWheelDelta;
						if ((mouseWheelDelta < 0 && m_lastScrollWheelDelta >= 0) ||
							(mouseWheelDelta > 0 && m_lastScrollWheelDelta <= 0))
						{
							// Direction change so we can flip.
							canFlip = true;
						}
						else
						{
							long frequency;
							bool queryFrequencySuccess;

							queryFrequencySuccess = QueryPerformanceFrequency(out frequency);

							if (queryFrequencySuccess && ((lTimeCurrent - m_lastScrollWheelTime) / (double)(frequency) * 1000) > s_scrollWheelDelayMS)
							{
								// Enough time has passed so we can flip.
								canFlip = true;
							}
						}

						// Whether a flip is performed or not, the time of this mouse wheel delta is being recorded. This is to avoid a single touch pad
						// flick to trigger multiple flips in a row, every 200ms. Touch pad flicks are indeed translated into pointer wheel deltas and spread
						// over multiple seconds, which is much larger than s_scrollWheelDelayMS==200ms. So a pause of 200ms since the last
						// wheel delta, or a change in direction, is required to trigger a new flip. Unfortunately that may require the user to wait a few seconds
						// before being able to trigger a new flip with the touch pad.
						m_lastScrollWheelTime = lTimeCurrent;

						if (canFlip)
						{
							bool successfullyMoved = false;

							if (mouseWheelDelta < 0)
							{
								successfullyMoved = MoveNext();
							}
							else
							{
								successfullyMoved = MovePrevious();
							}

							if (successfullyMoved)
							{
								// An actual flip occurred.
								m_lastScrollWheelDelta = mouseWheelDelta;

								// Since we flipped we ignore mouse wheel in current and parent scrollers.
								pArgs.Handled = true;
							}
							// Do not set handled in the else so we can scroll chain since we were already at the first/last item.
						}
					}

					if (!canFlip)
					{
						// Ignore mouse wheel in current and parent scrollers.
						pArgs.Handled = true;
					}
				}
			}
		}

		protected override void OnPointerEntered(PointerRoutedEventArgs pArgs)
		{
			Pointer spPointer;
			PointerDeviceType pointerDeviceType = PointerDeviceType.Touch;

			base.OnPointerEntered(pArgs);

			if (pArgs == null) throw new ArgumentNullException();

			spPointer = pArgs.Pointer;
			if (spPointer == null) throw new ArgumentNullException();
			pointerDeviceType = (PointerDeviceType)spPointer.PointerDeviceType;

			if (pointerDeviceType == PointerDeviceType.Touch)
			{
				HideButtonsImmediately();
			}
			else
			{
				ResetButtonsFadeOutTimer();
			}
		}

		protected override void OnPointerMoved(PointerRoutedEventArgs pArgs)
		{
			Pointer spPointer;
			PointerDeviceType pointerDeviceType = PointerDeviceType.Touch;

			base.OnPointerMoved(pArgs);

			if (pArgs == null) throw new ArgumentNullException();

			spPointer = pArgs.Pointer;
			if (spPointer == null) throw new ArgumentNullException();
			pointerDeviceType = (PointerDeviceType)spPointer.PointerDeviceType;

			if (PointerDeviceType.Touch != pointerDeviceType)
			{
				ResetButtonsFadeOutTimer();
			}
		}

		// Creates or identifies the element that is used to display the given item.
		protected override DependencyObject GetContainerForItemOverride()
		{
			var spFlipViewItem = new FlipViewItem();

			return spFlipViewItem;
		}

		protected override void OnItemsChanged(object e)
		{
			int currentSelectedIndex = 0;
			int previousSelectedIndex = 0;
			bool savedSkipAnimationOnce = m_skipAnimationOnce;

			previousSelectedIndex = SelectedIndex;

			base.OnItemsChanged(e); // TODO: this currently will never modify SelectedIndex, because the base method doesn't do anything. As part of work to align selection handling better with WinUI, the logic to update SelectedIndex on collection changes should move to Selector.OnItemsChanged() (which currently doesn't exist).

			currentSelectedIndex = SelectedIndex;
			if (previousSelectedIndex < 0 ||
				previousSelectedIndex != currentSelectedIndex)
			{
				int nCount = 0;
				int newSelectedIndex = -1;

				var spItems = Items;

				nCount = spItems.Count;
				if (nCount > 0)
				{
					if (previousSelectedIndex < 0 &&
						currentSelectedIndex < 0)
					{
						// Initial case is special to be in parity with old behavior
						// Default to selecting the first element.
						newSelectedIndex = 0;
					}
					else if (previousSelectedIndex < 0 ||
						(currentSelectedIndex >= 0 && currentSelectedIndex < previousSelectedIndex))
					{
						// If no item was selected previously, or an item got removed before the previously selected index
						// ensure selected index and offset is correctly set to new selected index.
						newSelectedIndex = currentSelectedIndex;
					}
					else
					{
						newSelectedIndex = previousSelectedIndex;
					}

					// Upper bound check to select new last element after last one was removed.
					if (newSelectedIndex >= nCount)
					{
						newSelectedIndex = nCount - 1;
					}
				}

				m_skipAnimationOnce = true;
				SelectedIndex = newSelectedIndex;
			}

			// Set the correct offset to selected item so that during Arrange items are laid out correctly.
			if (!m_animateNewIndex)
			{
				SetOffsetToSelectedIndex();
			}

			m_skipAnimationOnce = savedSkipAnimationOnce;
		}

		protected override void OnItemsSourceChanged(DependencyPropertyChangedEventArgs args)
		{
			base.OnItemsSourceChanged(args);

			int nSelectedIndex = 0;
			int nCount = 0;

			var spItems = Items;

			nCount = spItems.Count;

			if (args.NewValue != null && nCount > 0)
			{
				nSelectedIndex = SelectedIndex;
				if (nSelectedIndex < 0)
				{
					// Reset selected index to 0 if nothing is selected.
					SelectedIndex = 0;
				}

				SetOffsetToSelectedIndex();
			}
		}

#if false
		// TODO: This should override a base class method!
		void NotifyOfSourceChanged(IObservableVector<DependencyObject> pSender, IVectorChangedEventArgs e)
		{
			CollectionChange action = CollectionChange.Reset;
			int nSelectedIndex = 0;
			int nPreviousSelectedIndex = 0;

			// If we animate due to change in items, we may end up showing a wrong index due to entering and leaving items.
			// So we skip animations for index changes resulting from source changes.
			m_skipAnimationOnce = true;

			nPreviousSelectedIndex = SelectedIndex;
			if (nPreviousSelectedIndex < 0)
			{
				nPreviousSelectedIndex = 0;
			}

			//FlipViewGenerated.NotifyOfSourceChanged(pSender, e);

			action = e.CollectionChange;
			if (action == CollectionChange.ItemChanged)
			{
				nSelectedIndex = SelectedIndex;
				if (nSelectedIndex < 0)
				{
					// Reset selected index to 0 if nothing is selected.
					SelectedIndex = nPreviousSelectedIndex;
				}

				SetOffsetToSelectedIndex();
			}

			m_skipAnimationOnce = false;
		}
#endif

		// Sets vertical/horizontal offset corresponding to the selected item.
		// If the panel inside is a virtualizing panel selected index is used since virtualizing panels use item based scrolling
		// otherwise pixel offset is set calculated based on flipview's size since all items are of the same size.
		void SetOffsetToSelectedIndex()
		{
			int selectedIndex = 0;
			Orientation physicalOrientation = Orientation.Vertical;
			bool bIsVertical = false;
			bool bIsVirtualizing = false;
			double offset = 0.0;

			selectedIndex = SelectedIndex;

			if (selectedIndex < 0)
			{
				return;
			}

			(physicalOrientation, _) = GetItemsHostOrientations();

			bIsVertical = (physicalOrientation == Orientation.Vertical);

			bIsVirtualizing = (bool)this.GetValue(VirtualizingStackPanel.IsVirtualizingProperty);

			if (bIsVirtualizing)
			{
				// Item based scrolling
				offset = ItemsPresenter.IndexToOffset(selectedIndex);
			}
			else if (bIsVertical) // pixel based scrolling
			{
				offset = GetDesiredItemHeight();
				offset *= selectedIndex;
			}
			else
			{
				offset = GetDesiredItemWidth();
				offset *= selectedIndex;
			}

			if (m_tpScrollViewer != null)
			{
				if (bIsVertical)
				{
					m_tpScrollViewer.ScrollToVerticalOffset(offset);
				}
				else
				{
					m_tpScrollViewer.ScrollToHorizontalOffset(offset);
				}

				//m_tpScrollViewer.InvalidateScrollInfo();
			}
		}

		protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
		{
			base.PrepareContainerForItemOverride(element, item);

			DependencyObject pElement = (DependencyObject)(element);
			FlipViewItem pFlipViewItem = null;
			double value = 0.0;
			Thickness flipViewItemMargin = Thickness.Empty;

			// Cast container to known type
			pFlipViewItem = (FlipViewItem)(pElement);

			flipViewItemMargin = pFlipViewItem.Margin;

			value = GetDesiredItemWidth();

			value -= (flipViewItemMargin.Left + flipViewItemMargin.Right);
			pFlipViewItem.Width = value;

			value = GetDesiredItemHeight();

			value -= (flipViewItemMargin.Top + flipViewItemMargin.Bottom);
			pFlipViewItem.Height = value;
		}

		// Create FlipViewAutomationPeer to represent the FlipView.
		protected override AutomationPeer OnCreateAutomationPeer(/*out xaml_automation_peers.IAutomationPeer ppAutomationPeer*/)
		{
			// TODO: Should return FlipViewAutomationPeer
			//FlipViewAutomationPeer spFlipViewAutomationPeer;
			//IFlipViewAutomationPeerFactory spFlipViewAPFactory;
			//IActivationFactory spActivationFactory;
			//DependencyObject spInner;

			//spActivationFactory.Attach(ctl.ActivationFactoryCreator<FlipViewAutomationPeerFactory>.CreateActivationFactory());

			//spActivationFactory.As(spFlipViewAPFactory);

			//((spFlipViewAPFactory as FlipViewAutomationPeerFactory)?.CreateInstanceWithOwner(this,
			//	null,
			//	&spInner,
			//	&spFlipViewAutomationPeer));
			//spFlipViewAutomationPeer.CopyTo(ppAutomationPeer);

			return new AutomationPeer();
		}

		void OnScrollingHostPartSizeChanged(object pSender, SizeChangedEventArgs pArgs)
		{
			double width = 0.0;
			double height = 0.0;
			double value = 0.0;
			int nCount = 0;
			Thickness flipViewItemMargin = Thickness.Empty;

			// Get desired container width/height
			width = GetDesiredItemWidth();

			height = GetDesiredItemHeight();

			// Iterate through all indices
			var spItems = Items;

			nCount = spItems.Count;

			for (var i = 0; i < nCount; i++)
			{
				DependencyObject spContainer;

				spContainer = ContainerFromIndex(i);

				if (spContainer != null)
				{
					FlipViewItem pFlipViewItemNoRef = null;
					// Update the container's width/height to match
					pFlipViewItemNoRef = spContainer as FlipViewItem;
					flipViewItemMargin = pFlipViewItemNoRef.Margin;
					// NOTE: The following two assignments don't currently seem to have an effect (known layout bug)
					value = width - (flipViewItemMargin.Left + flipViewItemMargin.Right);
					pFlipViewItemNoRef.Width = value;
					value = height - (flipViewItemMargin.Top + flipViewItemMargin.Bottom);
					pFlipViewItemNoRef.Height = value;
				}
			}

			m_itemsAreSized = true;

			SetFixOffsetTimer();

			m_epScrollViewerViewChangedHandler?.Invoke(this, new ScrollViewerViewChangedEventArgs());
		}

		void OnPreviousButtonPartClick(object pSender, RoutedEventArgs pArgs)
		{
			MovePrevious();
			ResetButtonsFadeOutTimer();
		}

		void OnNextButtonPartClick(object pSender, RoutedEventArgs pArgs)
		{
			MoveNext();
			ResetButtonsFadeOutTimer();
		}

		// Invoked when the PointerEntered for the navigation buttons event is raised.
		void OnPointerEnteredNavigationButtons(object pSender, PointerRoutedEventArgs pArgs)
		{
			m_showNavigationButtons = true;
			m_keepNavigationButtonsVisible = true;

			UpdateVisualState();

			return;
		}

		// Invoked when the PointerExited for the navigation buttons event is raised.
		void OnPointerExitedNavigationButtons(object pSender, PointerRoutedEventArgs pArgs)
		{
			m_keepNavigationButtonsVisible = false;
			ResetButtonsFadeOutTimer();
		}

		// DirectManipulationStateChangeHandler implementation
		//void NotifyStateChange(
		//	DMManipulationState state,
		//	float xCumulativeTranslation,
		//	float yCumulativeTranslation,
		//	float zCumulativeFactor,
		//	float xCenter,
		//	float yCenter,
		//	bool isInertial,
		//	bool isTouchConfigurationActivated,
		//	bool isBringIntoViewportConfigurationActivated)
		//{
		//	// If while we are using ScrollViewer to animate the selected FlipViewItem into view the user begins a manipulation, this effectively
		//	// forces us to cancel the ScrollIntoView.
		//	if (isTouchConfigurationActivated && m_animateNewIndex)
		//	{
		//		RestoreSnapPointsTypes();
		//		m_animateNewIndex = false;
		//	}

		//	return;
		//}

		private protected override void ChangeVisualState(bool bUseTransitions)
		{
			int nSelectedIndex = 0;
			int nCount = 0;
			bool nothingPrevious = false;
			bool nothingNext = false;
			Orientation physicalOrientation = Orientation.Vertical;
			bool isVertical = false;

			// Determine the correct button/previous/next visibility
			var spItems = Items;

			nCount = spItems.Count;
			nSelectedIndex = SelectedIndex;
			if ((0 == nSelectedIndex) || (nCount <= 1))
			{
				nothingPrevious = true;
			}
			if ((nCount - 1 == nSelectedIndex) || (nCount <= 1))
			{
				nothingNext = true;
			}

			(physicalOrientation, _) = GetItemsHostOrientations();

			isVertical = (physicalOrientation == Orientation.Vertical);

			// Apply the new visibilities
			if (m_tpPreviousButtonHorizontalPart != null)
			{
				if (m_tpPreviousButtonHorizontalPart is ButtonBase previousButtonHorizontalPart)
				{
					previousButtonHorizontalPart.Visibility = (!m_showNavigationButtons || nothingPrevious || isVertical)
						? Visibility.Collapsed
						: Visibility.Visible;
				}
			}
			if (m_tpPreviousButtonVerticalPart != null)
			{
				if (m_tpPreviousButtonVerticalPart is ButtonBase previousButtonVerticalPart)
				{
					previousButtonVerticalPart.Visibility = (!m_showNavigationButtons || nothingPrevious || !isVertical)
						? Visibility.Collapsed : Visibility.Visible;
				}
			}
			if (m_tpNextButtonHorizontalPart != null)
			{
				if (m_tpNextButtonHorizontalPart is ButtonBase nextButtonHorizontalPart)
				{
					nextButtonHorizontalPart.Visibility = (!m_showNavigationButtons || nothingNext || isVertical)
						? Visibility.Collapsed
						: Visibility.Visible;
				}
			}
			if (m_tpNextButtonVerticalPart != null)
			{
				if (m_tpNextButtonVerticalPart is ButtonBase nextButtonVerticalPart)
				{
					nextButtonVerticalPart.Visibility = (!m_showNavigationButtons || nothingNext || !isVertical)
								? Visibility.Collapsed
								: Visibility.Visible;
				}
			}

			var bHasFocus = HasFocus();
			if (bHasFocus)
			{
				if (m_ShouldShowFocusRect)
				{
					GoToState(bUseTransitions, "Focused");
				}
				else
				{
					GoToState(bUseTransitions, "PointerFocused");
				}
			}
			else
			{
				GoToState(bUseTransitions, "Unfocused");
			}
		}

		double GetDesiredItemWidth()
		{
			Panel spPanel;
			IVirtualizingPanel spVirtualizingPanel;
			double width = 0.0;

			spPanel = ItemsPanelRoot;
			spVirtualizingPanel = spPanel as IVirtualizingPanel;

			if (spVirtualizingPanel != null)
			{
				width = LayoutInformation.GetAvailableSize(spVirtualizingPanel as VirtualizingPanel).Width;
			}

			double pWidth;
			if (double.IsInfinity(width) || width <= 0)
			{
				// Desired container width matches the width of the ScrollingHost part (or FlipView)
				pWidth = m_tpScrollViewer != null ? m_tpScrollViewer.ActualWidth : ActualWidth;
			}
			else
			{
				pWidth = width;
			}

			// If flipview has never been measured yet - scroll viewer will not have its size set.
			// Use flipview size set by developer in that case.
			if (pWidth <= 0)
			{
				pWidth = Width;
			}

			return pWidth;
		}

		double GetDesiredItemHeight()
		{
			Panel spPanel;
			IVirtualizingPanel spVirtualizingPanel;
			double height = 0.0;

			spPanel = ItemsPanelRoot;

			spVirtualizingPanel = spPanel as IVirtualizingPanel;

			if (spVirtualizingPanel != null)
			{
				height = LayoutInformation.GetAvailableSize(spVirtualizingPanel as VirtualizingPanel).Height;
			}

			double pHeight;
			if (double.IsInfinity(height) || height <= 0)
			{
				// Desired container height matches the height of the ScrollingHost part (or FlipView)
				pHeight = m_tpScrollViewer != null ? m_tpScrollViewer.ActualHeight : ActualHeight;
			}
			else
			{
				pHeight = height;
			}

			// If flipview has never been measured yet - scroll viewer will not have its size set.
			// Use flipview size set by developer in that case.
			if (pHeight <= 0)
			{
				pHeight = Height;
			}

			return pHeight;
		}


		protected override Size MeasureOverride(Size availableSize)
		{
			try
			{
				double height = 0;
				double width = 0;

				m_inMeasure = true;

				// FlipView requires to set its size on the FlipViewItems in order to show each item taking the whole space.
				// If no size is defined on the FlipView we get into a measure cycle which causes the application to not respond.
				// Fix is to use min of availableSize and window size as available size for measuring FlipView.
				//DXamlCore.GetCurrent().GetContentBoundsForElement(GetHandle(), &rootRect);

				var rootSize = XamlRoot.Size;

				height = Height;

				if (double.IsNaN(height))
				{
					availableSize.Height = Math.Min(rootSize.Height, availableSize.Height);
				}

				width = Width;
				if (double.IsNaN(width))
				{
					availableSize.Width = Math.Min(rootSize.Width, availableSize.Width);
				}

				return base.MeasureOverride(availableSize);
			}
			finally
			{
				m_inMeasure = false;
			}
		}


		protected override Size ArrangeOverride(Size arrangeSize)
		{
			try
			{
				m_inArrange = true;
				return base.ArrangeOverride(arrangeSize);
			}
			finally
			{
				m_inArrange = false;
			}
		}

		// GotFocus event handler.
		protected override void OnGotFocus(RoutedEventArgs pArgs)
		{
			FocusState focusState = FocusState.Unfocused;

			var pControl = this;

			base.OnGotFocus(pArgs);

			m_moveFocusToSelectedItem = false;

			if (!pControl.IsFocusEngagementEnabled || pControl.IsFocusEngaged)
			{
				var hasFocus = HasFocus();

				if (hasFocus)
				{
					DependencyObject spOriginalSource;

					spOriginalSource = pArgs.OriginalSource as DependencyObject;
					//Need to show Focus visual for FlipView whenever an item in a FlipView item has focus.
					if (spOriginalSource != null)
					{
						UIElement spFocusedElement;

						//Need focus state to show focus rect only when keyboard focus.
						spFocusedElement = spOriginalSource as UIElement;

						if (spFocusedElement != null)
						{
							focusState = spFocusedElement.FocusState;
						}

						if (FocusState.Keyboard == focusState)
						{
							var contentRoot = VisualTree.GetContentRootForElement(this);
							if (contentRoot.InputManager.LastInputDeviceType == InputDeviceType.GamepadOrRemote)
							{
								m_keepNavigationButtonsVisible = false;
							}

							// Keyboard should show buttons
							m_ShouldShowFocusRect = true;
							ResetButtonsFadeOutTimer();
						}
						else
						{
							m_ShouldShowFocusRect = false;
						}

						//ctl.are_equal(spOriginalSource, this, &isOriginalSource);

						//If the FlipView is the item getting the focus then focus needs to be set to one of its items.
						if (spOriginalSource == this)
						{
							IsSelectionActive = hasFocus;
							SetFocusedItem(m_iFocusedIndex < 0 ? 0 : m_iFocusedIndex, true);
						}
						else
						{
							int selectedIndex = 0;
							selectedIndex = SelectedIndex;
							if (selectedIndex >= 0)
							{
								DependencyObject spContainer;
								spContainer = ContainerFromIndex(selectedIndex);
								if (spContainer == null)
								{
									// If we get focus when there is no container for the current item, we want to move focus
									// to the selected item once it is available.
									m_moveFocusToSelectedItem = true;
								}
							}
						}
						UpdateVisualState(true);
					}
				}
			}
		}

		// LostFocus event handler.
		protected override void OnLostFocus(RoutedEventArgs pArgs)
		{
			base.OnLostFocus(pArgs);

			DependencyObject spOriginalSource;

			var hasFocus = HasFocus();

			if (pArgs == null) throw new ArgumentNullException();

			spOriginalSource = pArgs.OriginalSource as DependencyObject;

			//ctl.are_equal(spOriginalSource, this, &isOriginalSource);

			if (spOriginalSource == this)
			{
				IsSelectionActive = hasFocus;
				if (hasFocus)
				{
					SetFocusedItem(m_iFocusedIndex < 0 ? 0 : m_iFocusedIndex, true);
				}
			}

			if (!hasFocus)
			{
				m_moveFocusToSelectedItem = false;
				m_ShouldShowFocusRect = false;
				UpdateVisualState(true);
			}
		}

		protected override void OnPointerCaptureLost(PointerRoutedEventArgs pArgs)
		{
			base.OnPointerCaptureLost(pArgs);
			HandlePointerLostOrCanceled(pArgs);
		}

		protected override void OnPointerCanceled(PointerRoutedEventArgs pArgs)
		{
			base.OnPointerCanceled(pArgs);

			HandlePointerLostOrCanceled(pArgs);
			HideButtonsImmediately();
		}

		void HandlePointerLostOrCanceled(PointerRoutedEventArgs pArgs)
		{
			PointerPoint spPointerPoint;
			global::Windows.Devices.Input.PointerDevice spPointerDevice;
			PointerDeviceType nPointerDeviceType = PointerDeviceType.Touch;

			if (pArgs == null) throw new ArgumentNullException();

			spPointerPoint = pArgs.GetCurrentPoint(this);

			if (spPointerPoint == null) throw new ArgumentNullException();
			spPointerDevice = spPointerPoint.PointerDevice;
			if (spPointerDevice == null) throw new ArgumentNullException();
			nPointerDeviceType = (PointerDeviceType)spPointerDevice.PointerDeviceType;
			if (nPointerDeviceType == PointerDeviceType.Touch)
			{
				m_ShouldShowFocusRect = false;
				UpdateVisualState(true);
			}
		}

		internal override void OnPropertyChanged2(DependencyPropertyChangedEventArgs args)
		{
			switch (args.Property.Name)
			{
				case "Visibility":
					OnVisibilityChanged();
					break;
			}

			base.OnPropertyChanged2(args);
		}


		internal override void OnSelectedIndexChanged(int oldSelectedIndex, int newSelectedIndex)
		{
			base.OnSelectedIndexChanged(oldSelectedIndex, newSelectedIndex);

			int newIndex = newSelectedIndex;
			int oldIndex = oldSelectedIndex;
			bool shouldUseAnimation = false;

			//if (IsInit())
			//{
			//	goto Cleanup;
			//}

			shouldUseAnimation = UseTouchAnimationsForAllNavigation;

			//&& m_tpScrollViewer?.IsManipulationHandlerReady()

			if ((oldIndex == newIndex + 1 || oldIndex == newIndex - 1) && m_tpScrollViewer != null
				&& shouldUseAnimation && !m_skipAnimationOnce && oldIndex != -1 && newIndex != -1)
			{
				Rect bounds = default;
				bool handled = false;

				bounds = CalculateBounds(newIndex);

				SaveAndClearSnapPointsTypes();

				m_skipScrollIntoView = true;

				m_animateNewIndex = true;

				if (m_tpScrollViewer != null)
				{
					double svOffset = 0;
					//double siOffset = 0;
					Orientation physicalOrientation = Orientation.Horizontal;
					bool isVertical = false;

					(physicalOrientation, _) = GetItemsHostOrientations();

					isVertical = (physicalOrientation == Orientation.Vertical);

					// We do not want the measure pass and DManip try to set contradicting offsets on the scrollviewer in the next tick.
					//So, if the scrollviewer is waiting to be updated for the changes we made at this pass, update its layout.
					if (isVertical)
					{
						//siOffset = Scro.VerticalOffset;
						svOffset = (double)(m_tpScrollViewer?.VerticalOffset);
					}
					else
					{
						//siOffset = spScrollInfo.HorizontalOffset;
						svOffset = (double)(m_tpScrollViewer?.HorizontalOffset);
					}

					//if (svOffset != siOffset)
					//{
					//	m_tpScrollViewer?.UpdateLayout();
					//}

					m_tpScrollViewer?.UpdateLayout();

					m_tpFixOffsetTimer?.Stop();

					// UNO: force previous/next buttons' visibility to update immediately
					UpdateVisualState();

					//m_tpScrollViewer?.InvalidateScrollInfo();
					// When the application is not being rendered, there is no need to animate
					// the view change. Instead, a view jump is performed to minimize the CPU cost.

					handled = (bool)(m_tpScrollViewer?.BringIntoViewport(bounds,
						true,
						false,
						true //DXamlCore.GetCurrent().IsRenderingFrames() / animate /
				));
				}
			}
			else
			{
				// During an animated view change, cancel the user input if present, and do not change
				// the view according to the new SelectedIndex immediately, with SetOffsetToSelectedIndex.
				// Instead, wait for the animation to complete and then only sync the view to the new index
				// with SetFixOffsetTimer.
				if (m_animateNewIndex)
				{
					// UIElement.CancelDirectManipulations is not yet implemented in uno at the time of writing.
					if (ApiInformation.IsMethodPresent(typeof(UIElement), "CancelDirectManipulations"))
					{
						bool succeeded = (bool)(m_tpScrollViewer?.CancelDirectManipulations());
					}

					RestoreSnapPointsTypes();
					m_animateNewIndex = false;
					SetFixOffsetTimer();
				}
				else
				{
					SetOffsetToSelectedIndex();
				}

				m_skipScrollIntoView = false;
			}

			m_skipAnimationOnce = false;

			var smallChange = Math.Abs(oldSelectedIndex - newSelectedIndex) <= 1;
			//OnSelectedIndexChangedPartial(oldSelectedIndex, newSelectedIndex, smallChange && UseTouchAnimationsForAllNavigation);
		}

		// Called when the IsEnabled property changes.
		private protected override void OnIsEnabledChanged(IsEnabledChangedEventArgs e)
		{
			base.OnIsEnabledChanged(e);

			var bIsEnabled = IsEnabled;
			if (!bIsEnabled)
			{
				m_ShouldShowFocusRect = false;
				HideButtonsImmediately();
			}

			UpdateVisualState();
		}

		// Update the visual states when the Visibility property is changed.
		private protected override void OnVisibilityChanged()
		{
			Visibility visibility = Visibility.Collapsed;

			visibility = this.Visibility;

			if (Visibility.Visible != visibility)
			{
				m_ShouldShowFocusRect = false;
				HideButtonsImmediately();
			}

			UpdateVisualState();
		}

		// When size changes we need to reset the offsets on next tick.
		// In current tick the IScrollInfo might not have the correct extent because it hasn't been measured yet.
		void SetFixOffsetTimer()
		{
			if (m_tpFixOffsetTimer != null)
			{
				m_tpFixOffsetTimer.Stop();
			}
			else
			{
				DispatcherTimer spNewDispatcherTimer;

				spNewDispatcherTimer = new DispatcherTimer();

				_fixOffsetSubscription.Disposable = Disposable.Create(() =>
				{
					spNewDispatcherTimer.Stop();
					spNewDispatcherTimer.Tick -= FixOffset;
				});
				spNewDispatcherTimer.Tick += FixOffset;

				TimeSpan showDurationTimeSpan = TimeSpan.Zero;
				spNewDispatcherTimer.Interval = showDurationTimeSpan;
				m_tpFixOffsetTimer = spNewDispatcherTimer;

				if (m_tpFixOffsetTimer == null) throw new ArgumentNullException();
			}

			m_tpFixOffsetTimer.Start();
		}

		void FixOffset(object sender, object e)
		{
			m_tpFixOffsetTimer.Stop();

			if (m_tpScrollViewer != null && !m_tpScrollViewer.IsInManipulation)
			{
				SetOffsetToSelectedIndex();
			}
			else
			{
				// If we are in the middle of a manipulation (during the animation from one selected item to another) we
				// don't want to modify the offset. Postpone the fix via timer.
				SetFixOffsetTimer();
			}

			return;
		}

		// Creates a DispatcherTimer for fading out the FlipView's buttons.
		void EnsureButtonsFadeOutTimer()
		{
			if (m_tpButtonsFadeOutTimer == null)
			{
				DispatcherTimer spNewDispatcherTimer;
				var showDurationTimeSpan = TimeSpan.FromMilliseconds(FLIP_VIEW_BUTTONS_SHOW_DURATION_MS * TICKS_PER_MILLISECOND);

				spNewDispatcherTimer = new DispatcherTimer();

				_buttonsFadeOutTimerSubscription.Disposable = Disposable.Create(() =>
				{
					spNewDispatcherTimer.Stop();
					spNewDispatcherTimer.Tick -= ButtonsFadeOutTimerTickHandler;
				});

				spNewDispatcherTimer.Tick += ButtonsFadeOutTimerTickHandler;

				spNewDispatcherTimer.Interval = showDurationTimeSpan;
				m_tpButtonsFadeOutTimer = spNewDispatcherTimer;
			}
		}

		// Starts the fade out timer if not yet started.  Otherwise, resets it back to the original duration.
		void ResetButtonsFadeOutTimer()
		{
			if (!m_showNavigationButtons)
			{
				EnsureButtonsFadeOutTimer();
				m_showNavigationButtons = true;
				UpdateVisualState();
			}

			// Bug# 1292039: in the CanFlipWithMouse test, the mouse enters the the flipper button
			// directly which will set m_showNavigationButtons to true
			// this will not initialize m_tpButtonsFadeOutTimer
			m_tpButtonsFadeOutTimer?.Start();
		}

		// Hides the navigation buttons immediately.
		void HideButtonsImmediately()
		{
			if (m_showNavigationButtons)
			{
				m_tpButtonsFadeOutTimer?.Stop();

				if (!m_keepNavigationButtonsVisible)
				{
					m_showNavigationButtons = false;
					UpdateVisualState();
				}
			}
		}

		// Handler for the Tick event on m_tpButtonsFadeOutTimer.
		void ButtonsFadeOutTimerTickHandler(object sender, object e)
		{
			HideButtonsImmediately();
		}

		private void GetTemplatePart<T>(string name, out T element) where T : class
		{
			element = GetTemplateChild(name) as T;
		}
	}
}
#endif
