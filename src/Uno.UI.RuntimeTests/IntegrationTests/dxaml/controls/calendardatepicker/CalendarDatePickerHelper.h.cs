// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Tests.Enterprise;
using Uno.UI.RuntimeTests.Helpers;

using static Private.Infrastructure.TestServices;
using static Private.Infrastructure.CalendarHelper;
using Private.Infrastructure;

namespace Windows.UI.Xaml.Tests.Common
{

	internal class CalendarDatePickerHelper
	{
		// public
		internal CalendarDatePickerHelper()
		{
			m_loadedEvent = new Event();
			m_selectedDatesChangedEvent = new Event();
			m_cicEvent = new Event();
			m_openedEvent = new Event();
			m_closedEvent = new Event();
			m_addedDates = new CalendarHelper.DateCollection();
			m_removedDates = new CalendarHelper.DateCollection();
			m_loadedRegistration = CreateSafeEventRegistration<CalendarDatePicker, RoutedEventHandler>("Loaded");
			m_selectedDatesChangedRegistration = CreateSafeEventRegistration<CalendarDatePicker, TypedEventHandler<CalendarDatePicker, CalendarDatePickerDateChangedEventArgs>>("DateChanged");
			m_cicRegistration = CreateSafeEventRegistration<CalendarDatePicker, CalendarViewDayItemChangingEventHandler>("CalendarViewDayItemChanging");
			m_openedRegistration = CreateSafeEventRegistration<CalendarDatePicker, EventHandler<object>>("Opened");
			m_closedRegistration = CreateSafeEventRegistration<CalendarDatePicker, EventHandler<object>>("Closed");
		}

		internal async Task<CalendarDatePicker> GetCalendarDatePicker()
		{
			if (m_cp == null)
			{
				await RunOnUIThread(() =>
				{
					EnsurePickerCreated();
				});
			}

			return m_cp;
		}

		private void EnsurePickerCreated()
		{
			if (m_cp == null)
			{
				m_cp = new CalendarDatePicker();
			}

			VERIFY_IS_NOT_NULL(m_cp);
		}

		internal DependencyObject GetTemplateChild(string childName)
		{
			EnsurePickerCreated();
			return GetTemplateChild(m_cp, childName);
		}

		internal DependencyObject GetTemplateChild(DependencyObject root, string childName)
		{
			return CalendarHelper.GetTemplateChild(root, childName);
		}


		internal Task PrepareLoadedEvent()
		{
			return RunOnUIThread(() =>
			{
				EnsurePickerCreated();
				m_loadedRegistration.Attach(
					m_cp,
					(sender, e) =>
					{
						OnLoaded();
					});
			});
		}

		internal Task PrepareOpenedEvent()
		{
			return RunOnUIThread(() =>
			{
				EnsurePickerCreated();
				m_openedRegistration.Attach(
					m_cp,
					(sender, e) =>
					{
						OnOpened();
					});
			});
		}

		internal Task PrepareClosedEvent()
		{
			return RunOnUIThread(() =>
			{
				EnsurePickerCreated();
				m_closedRegistration.Attach(
					m_cp,
					(sender, e) =>
					{
						OnClosed();
					});
			});
		}

		internal Task PrepareSelectedDateChangedEvent()
		{
			m_addedDates.Clear();
			m_removedDates.Clear();

			return RunOnUIThread(() =>
			{
				EnsurePickerCreated();
				m_selectedDatesChangedRegistration.Attach(
					m_cp,
					(sender, e) =>
					{
						OnCalendarViewSelectedDateChanged(sender, e);
					});
			});
		}

		internal void ExpectAddedDate(DateTimeOffset date)
		{
			m_addedDates.Append(date);
		}

		internal void ExpectRemovedDate(DateTimeOffset date)
		{
			m_removedDates.Append(date);
		}

		// note: PrepareCICEvent must be called before item get realized
		// the best position is before CalendarDatePicker enters visual tree.
		internal Task PrepareCICEvent()
		{
			return RunOnUIThread(() =>
			{
				EnsurePickerCreated();
				m_cicRegistration.Attach(
					m_cp,
					(sender, e) =>
					{
						OnCalendarViewDayItemChanging(sender as CalendarView, e);
					});
			});
		}

		internal async Task WaitForLoaded()
		{
			await m_loadedEvent.WaitForDefault();
			VERIFY_IS_TRUE(m_loadedEvent.HasFired());
			m_loadedEvent.Reset();
			m_loadedRegistration.Detach();
		}

		internal async Task WaitForOpened()
		{
			await m_openedEvent.WaitForDefault();
			VERIFY_IS_TRUE(m_openedEvent.HasFired());
			m_openedEvent.Reset();
			m_openedRegistration.Detach();
		}

		internal async Task WaitForClosed()
		{
			await m_closedEvent.WaitForDefault();
			VERIFY_IS_TRUE(m_closedEvent.HasFired());
			m_closedEvent.Reset();
			m_closedRegistration.Detach();
		}

		internal async Task WaitForSelectedDatesChanged()
		{
			await m_selectedDatesChangedEvent.WaitForDefault();
			VERIFY_IS_TRUE(m_selectedDatesChangedEvent.HasFired());
			m_selectedDatesChangedEvent.Reset();
			m_selectedDatesChangedRegistration.Detach();
		}

		internal async Task WaitForCICEvent()
		{
			await m_cicEvent.WaitForDefault();
			VERIFY_IS_TRUE(m_cicEvent.HasFired());
			m_cicEvent.Reset();
			m_cicRegistration.Detach();
		}

		// private
		void OnLoaded()
		{
			LOG_OUTPUT("CalendarDatePickerIntegrationTests: CalendarDatePicker Loaded.");
			m_loadedEvent.Set();
		}

		void OnOpened()
		{
			LOG_OUTPUT("CalendarDatePickerIntegrationTests: CalendarDatePicker CalendarView flyout Opened.");
			m_openedEvent.Set();
		}

		void OnClosed()
		{
			LOG_OUTPUT("CalendarDatePickerIntegrationTests: CalendarDatePicker CalendarView flyout Closed.");
			m_closedEvent.Set();
		}

		void OnCalendarViewSelectedDateChanged(object sender, CalendarDatePickerDateChangedEventArgs e)
		{
			var addedSize = e.NewDate is { } ? 1 : 0;
			var removedSize = e.OldDate is { } ? 1 : 0;
			VERIFY_ARE_EQUAL(m_addedDates.Size, addedSize);
			VERIFY_ARE_EQUAL(m_removedDates.Size, removedSize);

			if (e.NewDate is { })
			{
				VERIFY_ARE_EQUAL(e.NewDate.Value.UniversalTime(), m_addedDates.GetAt(0).UniversalTime());
			}

			if (e.OldDate is { })
			{
				VERIFY_ARE_EQUAL(e.OldDate.Value.UniversalTime(), m_removedDates.GetAt(0).UniversalTime());
			}

			m_selectedDatesChangedEvent.Set();
		}

		void OnCalendarViewDayItemChanging(CalendarView sender, CalendarViewDayItemChangingEventArgs e)
		{
			// phase 2: set density bar
			// phase 5: blackout
			// phase 7: end cic event

			if (e.Phase == 2)
			{
				CalendarHelper.ColorCollection colors = new CalendarHelper.ColorCollection();
				colors.Append(Colors.Red);
				colors.Append(Colors.Green);
				colors.Append(Colors.Blue);
				colors.Append(Colors.Yellow);
				e.Item.SetDensityColors(colors);
			}
			else if (e.Phase == 5)
			{
				e.Item.IsBlackout = true;
			}
			else if (e.Phase == 7)
			{
				m_cicEvent.Set();
			}

			// keep subscribing cic event until phase 7.
			if (e.Phase < 7)
			{
				e.RegisterUpdateCallback(
					(sender, e) =>
					{
						OnCalendarViewDayItemChanging(sender, e);
					});
			}
		}

		// private
		CalendarDatePicker m_cp;

		SafeEventRegistration<CalendarDatePicker, RoutedEventHandler> m_loadedRegistration;

		SafeEventRegistration<CalendarDatePicker, TypedEventHandler<CalendarDatePicker, CalendarDatePickerDateChangedEventArgs>> m_selectedDatesChangedRegistration;
		SafeEventRegistration<CalendarDatePicker, CalendarViewDayItemChangingEventHandler> m_cicRegistration;
		SafeEventRegistration<CalendarDatePicker, EventHandler<object>> m_openedRegistration;
		SafeEventRegistration<CalendarDatePicker, EventHandler<object>> m_closedRegistration;
		Event m_loadedEvent;
		Event m_selectedDatesChangedEvent;
		Event m_cicEvent;
		Event m_openedEvent;
		Event m_closedEvent;
		CalendarHelper.DateCollection m_addedDates;
		CalendarHelper.DateCollection m_removedDates;
	}
}
