// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Graphics.Printing.OptionDetails;
using Windows.UI.Xaml.Automation.Peers;
using Windows.UI.Xaml.Automation.Provider;
using Windows.UI.Xaml.Controls.Primitives;
using DirectUI;
using DateTime = Windows.Foundation.WindowsFoundationDateTime;
using DayOfWeek = Windows.Globalization.DayOfWeek;

namespace Windows.UI.Xaml.Controls
{
	partial class CalendarViewDayItem
	{
		internal partial class CalendarViewDayItemAutomationPeer : CalendarViewBaseItemAutomationPeer
		{
			public CalendarViewDayItemAutomationPeer(CalendarViewDayItem owner) : base(owner)
			{
			}

			protected override object GetPatternCore(PatternInterface patternInterface)
			{
				object ppReturnValue = null;

				if (patternInterface == PatternInterface.TableItem ||
					patternInterface == PatternInterface.SelectionItem)
				{
					ppReturnValue = this;
				}
				else
				{
					ppReturnValue = base.GetPatternCore(patternInterface);
				}

				return ppReturnValue;
			}

			protected override AutomationControlType GetAutomationControlTypeCore()
			{
				var pReturnValue = AutomationControlType.DataItem;

				return pReturnValue;
			}

			protected override string GetClassNameCore()
			{
				return "CalendarViewDayItem";
			}

			protected override bool IsEnabledCore()
			{
				bool pReturnValue;
				UIElement spOwner;
				spOwner = Owner;

				bool isBlackout = false;
				isBlackout = ((CalendarViewDayItem)spOwner).IsBlackout;

				// This property also takes in consideration of a day is 'blackout' or not. Blackout dates are disabled
				if (isBlackout)
				{
					pReturnValue = false;
				}
				else
				{
					pReturnValue = base.IsEnabledCore();
				}

				return pReturnValue;
			}

#if false
			private void GetColumnHeaderItemsImpl(out uint pReturnValueCount, out IRawElementProviderSimple[] ppReturnValue)
			{
				pReturnValueCount = 0;
				ppReturnValue = default;
				UIElement spOwner;
				spOwner = Owner;

				DateTime date;
				date = ((CalendarViewDayItem)spOwner).Date;

				CalendarView pParent = (spOwner as CalendarViewDayItem).GetParentCalendarView();

				uint nCount = 0;
				Grid spWeekDayNames;

				// Gets the 'WeekDayNames' part of the container template and find weekindex position as elment
				spWeekDayNames = pParent.GetTemplateChild("WeekDayNames") as Grid;
				if (spWeekDayNames is { })
				{
					IList<UIElement> spChildren;
					spChildren = (spWeekDayNames as Grid).Children;
					nCount = (uint)spChildren.Count;

					int weekindex = 0;
					weekindex = ColumnImpl;

					AutomationPeer spItemPeerAsAP;
					IRawElementProviderSimple spProvider;
					UIElement spChild;
					spChild = spChildren.ElementAtOrDefault(weekindex);
					if (spChild is { })
					{
						spItemPeerAsAP = (spChild as FrameworkElement).GetAutomationPeer();
						if (spItemPeerAsAP is { })
						{
							//uint allocSize = sizeof(IRawElementProviderSimple);
							//ppReturnValue = (IRawElementProviderSimple)(CoTaskMemAlloc(allocSize));
							//ZeroMemory(ppReturnValue, allocSize);
							ppReturnValue = new IRawElementProviderSimple[1];

							spProvider = ProviderFromPeer(spItemPeerAsAP);
							(ppReturnValue)[0] = spProvider;
							pReturnValueCount = 1;
						}
					}
				}

				return;
			}

			private void GetRowHeaderItemsImpl(out uint pReturnValueCount, out IRawElementProviderSimple[] ppReturnValue)
			{
				UIElement spOwner;
				spOwner = Owner;

				CalendarViewDayItem item = spOwner as CalendarViewDayItem;
				CalendarView pParent = item.GetParentCalendarView();

				DateTime itemDate;
				itemDate = item.Date;

				pParent.GetRowHeaderForItemAutomationPeer(itemDate, CalendarViewDisplayMode.Month, out pReturnValueCount, out ppReturnValue);

				return;
			}

			private void SelectionContainerImpl(out IRawElementProviderSimple ppValue)
			{
				ppValue = ContainingGridImpl();
			}

			private void AddToSelectionImpl()
			{
				UIElement spOwner;
				spOwner = Owner;

				DateTime date;
				date = (spOwner as CalendarViewDayItem).Date;

				CalendarView pParent = (spOwner as CalendarViewDayItem).GetParentCalendarView();

				bool isSelected = false;
				pParent.IsSelected(date, out isSelected);
				if (!isSelected)
				{
					pParent.OnSelectDayItem(spOwner as CalendarViewDayItem);
				}

				return;
			}

			private void RemoveFromSelectionImpl()
			{
				UIElement spOwner;
				spOwner = Owner;

				DateTime date;
				date = (spOwner as CalendarViewDayItem).Date;

				CalendarView pParent = (spOwner as CalendarViewDayItem).GetParentCalendarView();

				bool isSelected = false;
				pParent.IsSelected(date, out isSelected);
				if (isSelected)
				{
					pParent.OnSelectDayItem(spOwner as CalendarViewDayItem);
				}

				return;
			}

			private bool IsSelectedImpl
			{
				get
				{
					var pValue = false;

					UIElement spOwner;
					spOwner = Owner;

					DateTime date;
					date = (spOwner as CalendarViewDayItem).Date;
					bool isSelected = false;
					CalendarView pParent = (spOwner as CalendarViewDayItem).GetParentCalendarView();

					pParent.IsSelected(date, out isSelected);
					if (isSelected)
					{
						pValue = true;
					}

					return pValue;
				}
			}

			private void SelectImpl()
			{
				UIElement spOwner;
				spOwner = Owner;

				DateTime date;
				date = (spOwner as CalendarViewDayItem).Date;

				CalendarView pParent = (spOwner as CalendarViewDayItem).GetParentCalendarView();

				IList<DateTime> spSelectedItems;
				spSelectedItems = pParent.SelectedDates;
				spSelectedItems.Clear();

				pParent.OnSelectDayItem(spOwner as CalendarViewDayItem);

				return;
			}

			// calculate visible column from index of the item
			private int ColumnImpl
			{
				get
				{
					var pValue = 0;

					UIElement spOwner;
					spOwner = Owner;

					DateTime date;
					date = (spOwner as CalendarViewDayItem).Date;

					CalendarView pParent = (spOwner as CalendarViewDayItem).GetParentCalendarView();

					CalendarViewGeneratorHost spHost;
					pParent.GetActiveGeneratorHost(out spHost);

					CalendarPanel pCalendarPanel = spHost.Panel;
					if (pCalendarPanel is { })
					{
						int itemIndex = 0;

						// Get realized item index
						itemIndex = spHost.CalculateOffsetFromMinDate(date);

						int cols = 1;
						cols = pCalendarPanel.Cols;
						int firstVisibleIndex = 0;
						firstVisibleIndex = pCalendarPanel.FirstVisibleIndex;

						// Calculate the relative positon w.r.to the visible elements from item index
						int relativePos = (itemIndex - firstVisibleIndex);
						if (firstVisibleIndex < cols)
						{
							int monthViewStartIndex = 0;
							monthViewStartIndex = pCalendarPanel.StartIndex;
							relativePos += monthViewStartIndex;
						}

						pValue = relativePos % cols;
						if (pValue < 0)
						{
							pValue += cols;
						}
					}

					return pValue;
				}
			}

			private int RowImpl
			{
				get
				{
					var pValue = 0;

					UIElement spOwner;
					spOwner = Owner;

					DateTime date;
					date = (spOwner as CalendarViewDayItem).Date;

					CalendarView pParent = (spOwner as CalendarViewDayItem).GetParentCalendarView();

					CalendarViewGeneratorHost spHost;
					pParent.GetActiveGeneratorHost(out spHost);

					CalendarPanel pCalendarPanel = spHost.Panel;
					if (pCalendarPanel is { })
					{
						int itemIndex = 0;
						itemIndex = spHost.CalculateOffsetFromMinDate(date);

						int cols = 1;
						cols = pCalendarPanel.Cols;
						int firstVisibleIndex = 0;
						firstVisibleIndex = pCalendarPanel.FirstVisibleIndex;

						DayOfWeek firstDayOfWeek = DayOfWeek.Sunday;
						firstDayOfWeek = pParent.FirstDayOfWeek;

						// Find the relative row position w.r.to visible rows
						int relativePos = (itemIndex - firstVisibleIndex);
						if (firstVisibleIndex < cols)
						{
							int monthViewStartIndex = 0;
							monthViewStartIndex = pCalendarPanel.StartIndex;
							relativePos += monthViewStartIndex;
						}

						pValue = relativePos / cols;
						// the element is not visible and we can't define row
						if (pValue < 0)
						{
							throw new NotSupportedException();
						}
					}

					return pValue;
				}
			}
#endif
		}
	}
}
