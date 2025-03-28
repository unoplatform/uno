// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using Windows.System;
using Windows.UI.Xaml.Automation.Peers;
using Windows.UI.Xaml.Input;
using DirectUI;
using DateTime = Windows.Foundation.WindowsFoundationDateTime;

namespace Windows.UI.Xaml.Controls
{
	internal partial class CalendarViewItem : CalendarViewBaseItem
	{
		// Called when a pointer makes a tap gesture on a CalendarViewBaseItem.
		protected override void OnTapped(
			TappedRoutedEventArgs pArgs)
		{
			bool isHandled = false;

			base.OnTapped(pArgs);

			isHandled = pArgs.Handled;

			if (!isHandled)
			{
				CalendarView spParentCalendarView = GetParentCalendarView();

				if (spParentCalendarView is { })
				{
					spParentCalendarView.OnSelectMonthYearItem(this, FocusState.Pointer);
					pArgs.Handled = true;

					var soundPlayerService = DXamlCore.Current.GetElementSoundPlayerServiceNoRef();
					soundPlayerService.RequestInteractionSoundForElement(ElementSoundKind.Invoke, this);
				}
			}

		}


		// Handles when a key is pressed down on the CalendarView.
		protected override void OnKeyDown(
			KeyRoutedEventArgs pArgs)
		{
			bool isHandled = false;

			base.OnKeyDown(pArgs);

			isHandled = pArgs.Handled;

			if (!isHandled)
			{
				CalendarView spParentCalendarView = GetParentCalendarView();

				if (spParentCalendarView is { })
				{
					VirtualKey key = VirtualKey.None;
					key = pArgs.Key;

					if (key == VirtualKey.Space || key == VirtualKey.Enter)
					{
						spParentCalendarView.OnSelectMonthYearItem(this, FocusState.Keyboard);
						pArgs.Handled = true;
						// note: though we are going to change the display mode and move the focus to the new item,
						// we still want to show a keyboard focus border before that happens (in case later we have an animation to change the display mode).
						SetIsKeyboardFocused(true);

						var soundPlayerService = DXamlCore.Current.GetElementSoundPlayerServiceNoRef();
						soundPlayerService.RequestInteractionSoundForElement(ElementSoundKind.Invoke, this);
					}
					else
					{
						// let CalendarView handle this event and tell calendarview the event comes from a MonthYearItem
						spParentCalendarView.SetKeyDownEventArgsFromCalendarItem(pArgs);
					}
				}
			}
		}

		/// <inheritdoc />
		internal override DateTime DateBase { get; set; }

#if DEBUG && false
		public override DateTime Date
		{
			set
			{
				SetDateForDebug(value);
				CalendarViewItemGenerated.Date = value;
			}
		}
#endif

		protected override AutomationPeer OnCreateAutomationPeer()
		{
			AutomationPeer ppAutomationPeer = null;

			CalendarViewItem.CalendarViewItemAutomationPeer spAutomationPeer;
			spAutomationPeer = new CalendarViewItemAutomationPeer(this);
			ppAutomationPeer = spAutomationPeer;
			return ppAutomationPeer;
		}
	}
}
