// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Windows.System;
using Windows.UI;
using Windows.UI.Input;
using Windows.UI.Xaml.Automation.Peers;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Printing;
using DirectUI;
using DateTime = Windows.Foundation.WindowsFoundationDateTime;
using CCalendarViewBaseItemChrome = Windows.UI.Xaml.Controls.CalendarViewBaseItem;

namespace Windows.UI.Xaml.Controls
{
	public partial class CalendarViewDayItem : CalendarViewBaseItem
	{
		private CalendarViewDayItemChangingEventArgs m_tpBuildTreeArgs;

		// Handle the custom property changed event and call the OnPropertyChanged methods.
		internal override void OnPropertyChanged2(
			DependencyPropertyChangedEventArgs args)
		{
			base.OnPropertyChanged2(args);

			//if (args.m_pDP.GetIndex() == KnownPropertyIndex.CalendarViewDayItem_IsBlackout)
			if (args.Property == IsBlackoutProperty)
			{
				bool isBlackout = false;

				isBlackout = (bool)args.NewValue;

				SetIsBlackout(isBlackout);

				// when setting an item to blackout, we need remove it from selectedDates (if it exists)
				if (isBlackout)
				{
					CalendarView spParentCalendarView = GetParentCalendarView();
					if (spParentCalendarView is { })
					{
						spParentCalendarView.OnDayItemBlackoutChanged(this, isBlackout);
					}
				}
			}
		}

#if false
		private void SetDensityColorsImpl(IIterable<Color> pColors)
		{
			((CCalendarViewBaseItemChrome)(GetHandle())).SetDensityColors(pColors);
		}
#endif

		internal CalendarViewDayItemChangingEventArgs GetBuildTreeArgs()
		{
			CalendarViewDayItemChangingEventArgs pspArgs;
			if (m_tpBuildTreeArgs is null)
			{
				CalendarViewDayItemChangingEventArgs spArgs;

				spArgs = new CalendarViewDayItemChangingEventArgs();
				m_tpBuildTreeArgs = spArgs;
				pspArgs = spArgs;
			}
			else
			{
				pspArgs = m_tpBuildTreeArgs;
			}

			return pspArgs;
		}



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
					bool ignored = false;
					ignored = FocusSelfOrChild(FocusState.Pointer);
					spParentCalendarView.OnSelectDayItem(this);
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
						spParentCalendarView.OnSelectDayItem(this);
						pArgs.Handled = true;
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

#if DEBUG
		private void put_Date(DateTime value)
		{
			SetDateForDebug(value);
			//CalendarViewDayItemGenerated.Date = value;

		}
#endif

		protected override AutomationPeer OnCreateAutomationPeer()
		{
			AutomationPeer ppAutomationPeer = null;

			CalendarViewDayItemAutomationPeer spAutomationPeer;
			spAutomationPeer = new CalendarViewDayItemAutomationPeer(this);
			ppAutomationPeer = spAutomationPeer;
			return ppAutomationPeer;
		}
	}
}
