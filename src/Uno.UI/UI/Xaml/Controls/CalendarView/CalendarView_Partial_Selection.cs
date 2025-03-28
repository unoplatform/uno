// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Linq;
using Windows.Foundation.Collections;
using Windows.UI.Xaml.Automation.Peers;
using DirectUI;
using Uno.Extensions;
using DateTime = Windows.Foundation.WindowsFoundationDateTime;
using SelectedDatesChangedEventSourceType = Windows.Foundation.TypedEventHandler<Windows.UI.Xaml.Controls.CalendarView, Windows.UI.Xaml.Controls.CalendarViewSelectedDatesChangedEventArgs>;

namespace Windows.UI.Xaml.Controls
{
	partial class CalendarView
	{
		private CalendarViewDayItem GetContainerByDate(
			DateTime datetime)
		{
			CalendarViewDayItem ppItem = null;

			var pMonthpanel = m_tpMonthViewItemHost.Panel;
			if (pMonthpanel is { })
			{
				int index = -1;
				DependencyObject spChildAsI;

				index = m_tpMonthViewItemHost.CalculateOffsetFromMinDate(datetime);

				if (index >= 0)
				{
					spChildAsI = pMonthpanel.ContainerFromIndex(index);
					if (spChildAsI is { })
					{
						CalendarViewDayItem spContainer;

						spContainer = (CalendarViewDayItem)spChildAsI;
						ppItem = spContainer;
					}
				}
			}

			return ppItem;
		}

		internal DateTime GetMaxDate()
			=> m_maxDate;
		internal DateTime GetMinDate()
			=> m_minDate;

		internal void OnSelectDayItem(CalendarViewDayItem pItem)
		{
			try
			{
				CalendarViewSelectionMode selectionMode = CalendarViewSelectionMode.None;

				global::System.Diagnostics.Debug.Assert(m_tpMonthViewItemHost.Panel is { });
				selectionMode = SelectionMode;

				if (selectionMode != CalendarViewSelectionMode.None)
				{
					bool isBlackout = false;

					isBlackout = pItem.IsBlackout;
					if (!isBlackout) // can't select a blackout item.
					{
						uint size = 0;
						DateTime date;
						uint index = 0;
						bool found = false;

						size = m_tpSelectedDates.Size;

						m_isSelectedDatesChangingInternally = true;

						global::System.Diagnostics.Debug.Assert(size <= 1 ||
																selectionMode == CalendarViewSelectionMode.Multiple);

						date = pItem.Date;

						m_tpSelectedDates.IndexOf(date, out index, out found);
						if (found)
						{
							// when user deselect an item, we remove all equivalent dates from selectedDates.
							// so the item will be unselected.
							// (the opposite case is when developer removes a date from selectedDates,
							// we only remove that date from selectedDates, so the corresponding item
							// will be still selected until all equivalent dates are removed from selectedDates)

							(m_tpSelectedDates as TrackableDateCollection).RemoveAll(date); // out index
						}
						else
						{
							if (selectionMode == CalendarViewSelectionMode.Single && size == 1)
							{
								// there was one selected date, remove it.
								m_tpSelectedDates.Clear();
							}

							m_tpSelectedDates.Append(date);
						}

						RaiseSelectionChangedEventIfChanged();
					}
				}
			}
			finally
			{
				//Cleanup:
				m_isSelectedDatesChangingInternally = false;
			}
		}

		// when we select a monthitem or yearitem, we changed to the corresponding view.
		internal void OnSelectMonthYearItem(
			CalendarViewItem pItem,
			FocusState focusState)
		{
			DateTime date = default;

			CalendarViewDisplayMode displayMode = CalendarViewDisplayMode.Month;

			displayMode = DisplayMode;
			date = pItem.DateBase;

			// after display mode changed, we'll focus a new item, we want that item to be focused by the specified state.
#if false // Fix for Uno - not supported feature
			m_focusItemAfterDisplayModeChanged = true;
#endif
			m_focusStateAfterDisplayModeChanged = focusState;

			if (displayMode == CalendarViewDisplayMode.Year && m_tpMonthViewItemHost.Panel is { })
			{
				// when we switch back to MonthView, we try to keep the same day and use the selected month and year (and era)
				CopyDate(
					displayMode,
					date,
					ref m_lastDisplayedDate);
				DisplayMode = CalendarViewDisplayMode.Month;
			}
			else if (displayMode == CalendarViewDisplayMode.Decade && m_tpYearViewItemHost.Panel is { })
			{
				// when we switch back to YearView, we try to keep the same day and same month and use the selected year (and era)
				CopyDate(
					displayMode,
					date,
					ref m_lastDisplayedDate);
				DisplayMode = CalendarViewDisplayMode.Year;
			}
			else
			{
				global::System.Diagnostics.Debug.Assert(false); // corresponding panel part is missing.
			}

		}

		private void OnSelectionModeChanged()
		{
			CalendarViewSelectionMode selectionMode = CalendarViewSelectionMode.None;

			selectionMode = SelectionMode;

			try
			{
				// when selection mode is changed, e.g. from Multiple . Single or from Single . None
				// we need to deselect some or all items and raise SelectedDates changed event
				m_isSelectedDatesChangingInternally = true;

				if (selectionMode == CalendarViewSelectionMode.None)
				{
					m_tpSelectedDates.Clear();
				}
				else if (selectionMode == CalendarViewSelectionMode.Single)
				{
					int size = 0;

					// remove all but keep the first selected item.
					size = m_tpSelectedDates.Count;

					while (size > 1)
					{
						m_tpSelectedDates.RemoveAt(size - 1);
						size--;
					}
				}

				RaiseSelectionChangedEventIfChanged();
			}
			finally
			{
				m_isSelectedDatesChangingInternally = false;
			}
		}

		private void RaiseSelectionChangedEventIfChanged()
		{
			var lessThanComparer = m_dateComparer.LessThanComparer;
			TrackableDateCollection.DateSetType addedDates = new TrackableDateCollection.DateSetType(lessThanComparer);
			TrackableDateCollection.DateSetType removedDates = new TrackableDateCollection.DateSetType(lessThanComparer);

			var pSelectedDates = m_tpSelectedDates as TrackableDateCollection;

			// grab all the changes since last time SelectedDates changed.
			pSelectedDates.FetchAndResetChange(addedDates, removedDates);

			// we don't support extended selection mode, so we should have only up to one added date.
			global::System.Diagnostics.Debug.Assert(addedDates.Count <= 1);

			if (addedDates.Count == 1)
			{
				uint count = 0;
				DateTime date = addedDates.First();

				pSelectedDates.CountOf(date, out count);

				// given that we have one date in addedDates, so it must exist in SelectedDates.
				global::System.Diagnostics.Debug.Assert(count >= 1);

				if (count > 1)
				{
					// we had this date in SelectedDates before, adding this date will not affect the
					// selection state on this item, so actually we haven't added this date into SelectedDates.
					addedDates.Remove(date);
				}
				else if (count == 1)
				{
					// this date doesn't exist in SelectedDates before,
					// which means we change the selection state on this item from Not Selected to Selected.
					CalendarViewDayItem spChild;

					spChild = GetContainerByDate(date);
					if (spChild is { })
					{
#if DEBUG
						bool isBlackout = false;

						isBlackout = spChild.IsBlackout;
						// we already handle blackout in CollectionChanging, so here the date must not be blackout.
						global::System.Diagnostics.Debug.Assert(!isBlackout);

#endif
						spChild.SetIsSelected(true);
					}

					// else this item is not realized yet, we'll update the selection state when this item is prepared.
				}

			}

			// now handle removedDates

			// we'll check all dates in RemovedDates, to see if there is still an equivalent date
			// in SelectedDates, if yes, this date is still selected and actually not being removed,
			// if no we need update selection state and raise selectedDatesChanged event.

			if (removedDates.Count > 0)
			{
				// removedDates is sorted and unique, so let's search all SelectedDates from removedDates. time cost O(M x lg(N))
				uint size = 0;
				size = pSelectedDates.Size;

				for (uint i = 0; i < size; ++i)
				{
					DateTime date = default;
					//KeyValuePair<TrackableDateCollection.DateSetType.iterator, TrackableDateCollection.DateSetType.iterator> result;

					date = pSelectedDates.GetAt(i);

					//// binary_search only tells us if the item exists or not, it doesn't tell us the position:(
					//result = removedDates.equal_range(date);
					//if (result.first != result.second)
					//{
					//	// because removedDates is unique and sorted, so we should have only up to 1 record.
					//	global::System.Diagnostics.Debug.Assert(std.distance(result.first, result.second) == 1);
					//	removedDates.erase(result.first);
					//}
					removedDates.Remove(date);
				}

				// now removedDates contains all the dates that we finally removed and we are going to
				// mark them as un-selected (if they are realized)

				foreach (var it in removedDates)
				{
					CalendarViewDayItem spChild;

					spChild = GetContainerByDate(it);
					if (spChild is { })
					{
						spChild.SetIsSelected(false);
					}
				}
			}

			// developer could change SelectedDates in SelectedDatesChanged event
			// it is the good time allow they do so now.
			m_isSelectedDatesChangingInternally = false;

			// now raise selectedDatesChanged event if there are any actual changes
			if (!addedDates.Empty() || !removedDates.Empty())
			{
				SelectedDatesChangedEventSourceType pEventSource = null;
				CalendarViewSelectedDatesChangedEventArgs spEventArgs;
				ValueTypeCollection<DateTimeOffset> spAddedDates;
				ValueTypeCollection<DateTimeOffset> spRemovedDates;

				spAddedDates = new ValueTypeCollection<DateTimeOffset>();
				spRemovedDates = new ValueTypeCollection<DateTimeOffset>();

				foreach (var it in addedDates)
				{
					spAddedDates.Append(it);
				}

				foreach (var it in removedDates)
				{
					spRemovedDates.Append(it);
				}

				spEventArgs = new CalendarViewSelectedDatesChangedEventArgs();
				spEventArgs.AddedDates = spAddedDates as IVectorView<DateTimeOffset>;
				spEventArgs.RemovedDates = spRemovedDates as IVectorView<DateTimeOffset>;
				GetSelectedDatesChangedEventSourceNoRef(out pEventSource);
				pEventSource?.Invoke(this, spEventArgs);


				bool bAutomationListener = false;
				// TODO UNO
				//bAutomationListener = AutomationPeer.ListenerExistsHelper(AutomationEvents.SelectionPatternOnInvalidated);
				//if (!bAutomationListener)
				//{
				//	bAutomationListener = AutomationPeer.ListenerExistsHelper(AutomationEvents.SelectionItemPatternOnElementSelected);
				//}

				//if (!bAutomationListener)
				//{
				//	bAutomationListener = AutomationPeer.ListenerExistsHelper(AutomationEvents.SelectionItemPatternOnElementAddedToSelection);
				//}

				//if (!bAutomationListener)
				//{
				//	bAutomationListener = AutomationPeer.ListenerExistsHelper(AutomationEvents.SelectionItemPatternOnElementRemovedFromSelection);
				//}

				if (bAutomationListener)
				{
					AutomationPeer spAutomationPeer;
					spAutomationPeer = GetAutomationPeer();
					if (spAutomationPeer is { })
					{
						(spAutomationPeer as CalendarViewAutomationPeer).RaiseSelectionEvents(spEventArgs);
					}
				}
			}

			return;
		}

		internal void OnDayItemBlackoutChanged(CalendarViewDayItem pItem, bool isBlackOut)
		{
			try
			{
				if (isBlackOut)
				{
					DateTime date;
					uint index = 0;
					bool found = false;

					date = pItem.Date;
					m_tpSelectedDates.IndexOf(date, out index, out found);

					if (found)
					{
						// this item is selected, remove the selection and raise event.
						m_isSelectedDatesChangingInternally = true;

						(m_tpSelectedDates as TrackableDateCollection).RemoveAll(date);

						RaiseSelectionChangedEventIfChanged();
					}
				}
			}
			finally
			{
				m_isSelectedDatesChangingInternally = false;
			}
		}

		internal void IsSelected(DateTime date, out bool pIsSelected)
		{
			uint index = 0;
			m_tpSelectedDates.IndexOf(date, out index, out pIsSelected);
		}

		private void OnSelectedDatesChanged(
			IObservableVector<DateTimeOffset> pSender,
			IVectorChangedEventArgs e)
		{
			// only raise event for the changes from external.
			if (!m_isSelectedDatesChangingInternally)
			{
				RaiseSelectionChangedEventIfChanged();
			}

			return;
		}

		private void OnSelectedDatesChanging(
			TrackableDateCollection.CollectionChanging action,
			DateTime addingDate)
		{
			switch (action)
			{
				case DirectUI.TrackableDateCollection.CollectionChanging.ItemInserting:
					{
						// when inserting an item, we should verify the new adding date is not blackout.
						// also we need to verify this adding operation doesn't break the limition of Selection mode.
						ValidateSelectingDateIsNotBlackout(addingDate);

						uint size = 0;
						CalendarViewSelectionMode selectionMode = CalendarViewSelectionMode.None;

						selectionMode = SelectionMode;
						size = (uint)m_tpSelectedDates.Count;

						// if we already have 1 item selected in Single mode, or the selection mode is None, we can't select any more dates.
						if ((selectionMode == CalendarViewSelectionMode.Single && size > 0)
							|| (selectionMode == CalendarViewSelectionMode.None))
						{
							//ErrorHelper.OriginateErrorUsingResourceID(E_FAIL, ERROR_CALENDAR_CANNOT_SELECT_MORE_DATES);
							throw new InvalidOperationException("ERROR_CALENDAR_CANNOT_SELECT_MORE_DATES");
						}
					}
					break;
				case DirectUI.TrackableDateCollection.CollectionChanging.ItemChanging:
					// when item is changing, we don't change the total number of selected dates, so we
					// don't need to verify Selection mode. Here we only need to check if
					// the new addingDate is blackout or not.
					ValidateSelectingDateIsNotBlackout(addingDate);
					break;
				default:
					break;
			}

			return;
		}

		private void ValidateSelectingDateIsNotBlackout(DateTime date)
		{
			CalendarViewDayItem spChild;

			spChild = GetContainerByDate(date);
			if (spChild is { })
			{
				bool isBlackout = false;

				isBlackout = spChild.IsBlackout;
				if (isBlackout)
				{
					//ErrorHelper.OriginateErrorUsingResourceID(E_FAIL, ERROR_CALENDAR_CANNOT_SELECT_BLACKOUT_DATE);
					throw new InvalidOperationException("ERROR_CALENDAR_CANNOT_SELECT_BLACKOUT_DATE");
				}
			}

			return;
		}
	}
}
