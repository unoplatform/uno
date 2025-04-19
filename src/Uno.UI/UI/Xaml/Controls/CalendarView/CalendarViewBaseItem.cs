// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using CCalendarViewBaseItemChrome = Microsoft.UI.Xaml.Controls.CalendarViewBaseItem;
using DateTime = Windows.Foundation.WindowsFoundationDateTime;
using Uno.UI.Xaml;
using Uno.UI.Extensions;


#if HAS_UNO_WINUI
using Microsoft.UI.Input;
#else
using Windows.Devices.Input;
using Windows.UI.Input;
#endif

namespace Microsoft.UI.Xaml.Controls
{
	partial class CalendarViewBaseItem
	{
		// Called when the user presses a pointer down over the CalendarViewBaseItem.
		protected override void OnPointerPressed(
			PointerRoutedEventArgs pArgs)
		{
			bool isHandled = false;

			base.OnPointerPressed(pArgs);

			isHandled = pArgs.Handled;
			if (!isHandled)
			{
				SetIsPressed(true);
				UpdateVisualStateInternal();
			}

		}

		// Called when the user releases a pointer over the CalendarViewBaseItem.
		protected override void OnPointerReleased(
			PointerRoutedEventArgs pArgs)
		{
			bool isHandled = false;

			base.OnPointerReleased(pArgs);

			isHandled = pArgs.Handled;
			if (!isHandled)
			{
				SetIsPressed(false);
				UpdateVisualStateInternal();
			}

		}

		// Called when a pointer enters a CalendarViewBaseItem.
		protected override void OnPointerEntered(
			PointerRoutedEventArgs pArgs)
		{
			Pointer spPointer;
			PointerDeviceType pointerDeviceType = PointerDeviceType.Touch;

			base.OnPointerEntered(pArgs);

			// Only update hover state if the pointer type isn't touch
			spPointer = pArgs.Pointer;
			pointerDeviceType = spPointer.PointerDeviceType;
			if (pointerDeviceType != PointerDeviceType.Touch)
			{
				SetIsHovered(true);
				UpdateVisualStateInternal();
			}

		}

		// Called when a pointer leaves a CalendarViewBaseItem.
		protected override void OnPointerExited(
			PointerRoutedEventArgs pArgs)
		{
			base.OnPointerExited(pArgs);

			SetIsHovered(false);
			SetIsPressed(false);
			UpdateVisualStateInternal();

		}

		// Called when the CalendarViewBaseItem or its children lose pointer capture.
		protected override void OnPointerCaptureLost(
			PointerRoutedEventArgs pArgs)
		{
			base.OnPointerCaptureLost(pArgs);

			SetIsHovered(false);
			SetIsPressed(false);
			UpdateVisualStateInternal();

		}

		// Called when the CalendarViewBaseItem receives focus.
		protected override void OnGotFocus(
			RoutedEventArgs pArgs)
		{
			FocusState focusState = FocusState.Unfocused;

			base.OnGotFocus(pArgs);

			var pCalendarView = GetParentCalendarView();
			if (pCalendarView is { })
			{
				pCalendarView.OnItemFocused(this);
			}

			focusState = FocusState;

			SetIsKeyboardFocused(focusState == FocusState.Keyboard);


		}

		// Called when the CalendarViewBaseItem loses focus.
		protected override void OnLostFocus(
			RoutedEventArgs pArgs)
		{
			base.OnLostFocus(pArgs);

			// remove keyboard focused state
			SetIsKeyboardFocused(false);

		}

		protected override void OnRightTapped(
			RightTappedRoutedEventArgs pArgs)
		{
			base.OnRightTapped(pArgs);

			bool isHandled = false;

			isHandled = pArgs.Handled;

			if (!isHandled)
			{
				bool ignored = false;
				ignored = FocusSelfOrChild(FocusState.Pointer);
				pArgs.Handled = true;
			}

			return;
		}

		private protected override void OnIsEnabledChanged(IsEnabledChangedEventArgs pArgs)
		{
			UpdateTextBlockForeground();
		}

#if !__NETSTD_REFERENCE__

#if UNO_HAS_ENHANCED_LIFECYCLE
		private protected override void EnterImpl(bool live)
		{
			base.EnterImpl(live);
#else
		private void EnterImpl()
		{
#endif
			// In case any of the TextBlock properties have been updated while
			// we were out of the visual tree, we should update them in order to ensure
			// that we always have the most up-to-date values.
			// An example where this can happen is if the theme changes while
			// the flyout holding the CalendarView for a CalendarDatePicker is closed.
			UpdateTextBlockForeground();
			UpdateTextBlockFontProperties();
			UpdateTextBlockAlignments();
			UpdateVisualStateInternal();

			//TODO:Uno specific: Ensure chrome of the calendar item is updated (e.g. the background color, border color, etc.)
			InvalidateRender();
		}

#endif
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private protected CCalendarViewBaseItemChrome GetHandle() => this;

		internal void SetParentCalendarView(CalendarView pCalendarView)
		{
			m_pParentCalendarView = pCalendarView;
			CCalendarViewBaseItemChrome pChrome = (CCalendarViewBaseItemChrome)(GetHandle());

			pChrome.SetOwner(pCalendarView);
		}

		internal CalendarView GetParentCalendarView()
		{
			return m_pParentCalendarView;
		}

		/* Chrome is a partial file in Uno, no needs to re-route methods
		internal void UpdateMainText(string mainText)
		{
			CCalendarViewBaseItemChrome pChrome = (CCalendarViewBaseItemChrome)(GetHandle());
			return pChrome.UpdateMainText(mainText);
		}

		private void UpdateLabelText(string labelText)
		{
			CCalendarViewBaseItemChrome pChrome = (CCalendarViewBaseItemChrome)(GetHandle());
			return pChrome.UpdateLabelText(labelText);
		}

		private void ShowLabelText(bool showLabel)
		{
			CCalendarViewBaseItemChrome pChrome = (CCalendarViewBaseItemChrome)(GetHandle());
			return pChrome.ShowLabelText(showLabel);
		}

		private string GetMainText()
		{
			CCalendarViewBaseItemChrome pChrome = (CCalendarViewBaseItemChrome)(GetHandle());
			return pChrome.GetMainText(pMainText);
		}

		internal void SetIsToday(bool state)
		{
			CCalendarViewBaseItemChrome pChrome = (CCalendarViewBaseItemChrome)(GetHandle());
			return pChrome.SetIsToday(state);
		}

		protected void SetIsKeyboardFocused(bool state)
		{
			CCalendarViewBaseItemChrome pChrome = (CCalendarViewBaseItemChrome)(GetHandle());
			return pChrome.SetIsKeyboardFocused(state);
		}

		private void SetIsSelected(bool state)
		{
			CCalendarViewBaseItemChrome pChrome = (CCalendarViewBaseItemChrome)(GetHandle());
			return pChrome.SetIsSelected(state);
		}

		private void SetIsBlackout(bool state)
		{
			CCalendarViewBaseItemChrome pChrome = (CCalendarViewBaseItemChrome)(GetHandle());
			return pChrome.SetIsBlackout(state);
		}

		private void SetIsHovered(bool state)
		{
			CCalendarViewBaseItemChrome pChrome = (CCalendarViewBaseItemChrome)(GetHandle());
			return pChrome.SetIsHovered(state);
		}

		private void SetIsPressed(bool state)
		{
			CCalendarViewBaseItemChrome pChrome = (CCalendarViewBaseItemChrome)(GetHandle());
			return pChrome.SetIsPressed(state);
		}

		internal void SetIsOutOfScope(bool state)
		{
			CCalendarViewBaseItemChrome pChrome = (CCalendarViewBaseItemChrome)(GetHandle());
			return pChrome.SetIsOutOfScope(state);
		}
		*/

		// If this item is unfocused, sets focus on the CalendarViewBaseItem.
		// Otherwise, sets focus to whichever element currently has focus
		// (so focusState can be propagated).
		internal bool FocusSelfOrChild(
			FocusState focusState,
			FocusNavigationDirection focusNavigationDirection = default)
		{
			bool isItemAlreadyFocused = false;
			DependencyObject spItemToFocus = null;

			var pFocused = false;

			isItemAlreadyFocused = FocusState != FocusState.Unfocused;
			if (isItemAlreadyFocused)
			{
				// Re-focus the currently focused item to propagate focusState (the item might be focused
				// under a different FocusState value).
				var focusedElement = XamlRoot is { } xamlRoot ?
					FocusManager.GetFocusedElement(xamlRoot) :
					null;
				spItemToFocus = focusedElement as DependencyObject;
			}
			else
			{
				spItemToFocus = this;
			}

			if (spItemToFocus is { })
			{
				var focused = this.SetFocusedElementWithDirection(spItemToFocus, focusState, false /* animateIfBringIntoView */, focusNavigationDirection);
				pFocused = focused;
			}

			return pFocused;
		}

#if DEBUG
		// DateTime has an int64 member which is not intutive enough. This method will convert it
		// into numbers that we can easily read.
		private protected void SetDateForDebug(DateTime value)
		{
			var pCalendarView = GetParentCalendarView();
			if (pCalendarView is { })
			{
				var pCalendar = pCalendarView.Calendar;
				pCalendar.SetDateTime(value);
				m_eraForDebug = pCalendar.Era;
				m_yearForDebug = pCalendar.Year;
				m_monthForDebug = pCalendar.Month;
				m_dayForDebug = pCalendar.Day;
			}
		}
#endif

		/* Chrome is a partial file in Uno, no needs to re-route methods
		private void InvalidateRender()
		{
			CCalendarViewBaseItemChrome pChrome = (CCalendarViewBaseItemChrome)(GetHandle());
			pChrome.InvalidateRender();
			return;
		}*/

		internal void UpdateTextBlockForeground()
		{
			CCalendarViewBaseItemChrome pChrome = (CCalendarViewBaseItemChrome)(GetHandle());
			pChrome.UpdateTextBlocksForeground();
		}

		internal void UpdateTextBlockFontProperties()
		{
			CCalendarViewBaseItemChrome pChrome = (CCalendarViewBaseItemChrome)(GetHandle());
			pChrome.UpdateTextBlocksFontProperties();
		}

		internal void UpdateTextBlockAlignments()
		{
			CCalendarViewBaseItemChrome pChrome = (CCalendarViewBaseItemChrome)(GetHandle());
			pChrome.UpdateTextBlocksAlignments();
		}

		// Change to the correct visual state for the CalendarViewBaseItem.
		private protected override void ChangeVisualState(
			// true to use transitions when updating the visual state, false
			// to snap directly to the new visual state.
			bool bUseTransitions)
		{
			base.ChangeVisualState(bUseTransitions);

			CCalendarViewBaseItemChrome chrome = (CCalendarViewBaseItemChrome)(GetHandle());
			bool ignored = false;
			bool isPointerOver = chrome.IsHovered();
			bool isPressed = chrome.IsPressed();

			// Common States Group
			if (isPressed)
			{
				ignored = GoToState(bUseTransitions, "Pressed");
			}
			else if (isPointerOver)
			{
				ignored = GoToState(bUseTransitions, "PointerOver");
			}
			else
			{
				ignored = GoToState(bUseTransitions, "Normal");
			}

			return;
		}

		private void UpdateVisualStateInternal()
		{
			CCalendarViewBaseItemChrome chrome = (CCalendarViewBaseItemChrome)(GetHandle());
			if (chrome.HasTemplateChild()) // If !HasTemplateChild, then there is no visual in ControlTemplate for CalendarViewDayItemStyle
										   // There should be no VisualStateGroup defined, so ignore UpdateVisualState
			{
				UpdateVisualState(false /* fUseTransitions */);
			}

			return;
		}
	}
}
