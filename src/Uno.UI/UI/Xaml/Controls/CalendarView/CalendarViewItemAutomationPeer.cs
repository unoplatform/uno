// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using Windows.UI.Xaml.Automation.Peers;
using Windows.UI.Xaml.Automation.Provider;
using Windows.UI.Xaml.Controls.Primitives;
using DateTime = Windows.Foundation.WindowsFoundationDateTime;

namespace Windows.UI.Xaml.Controls
{
	partial class CalendarViewItem
	{
		internal partial class CalendarViewItemAutomationPeer : CalendarViewBaseItemAutomationPeer
		{
			internal CalendarViewItemAutomationPeer(CalendarViewItem owner) : base(owner)
			{
			}

			protected override object GetPatternCore(PatternInterface patternInterface)
			{
				object ppReturnValue = null;

				if (patternInterface == PatternInterface.Invoke ||
					patternInterface == PatternInterface.TableItem)
				{
					ppReturnValue = this;
				}
				else
				{
					ppReturnValue = base.GetPatternCore(patternInterface);
				}

				return ppReturnValue;
			}

			protected override string GetClassNameCore()
			{
				return "CalendarViewItem";
			}

			protected override AutomationControlType GetAutomationControlTypeCore()
			{
				AutomationControlType pReturnValue = AutomationControlType.Button;

				return pReturnValue;
			}

#if false
			private void InvokeImpl()
			{
				UIElement spOwner;
				spOwner = Owner;

				CalendarView pParent = (spOwner as CalendarViewItem).GetParentCalendarView();
				pParent.OnSelectMonthYearItem(spOwner as CalendarViewItem, FocusState.Keyboard);
			}

			private int ColumnImpl()
			{
				var pValue = 0;

				UIElement spOwner;
				spOwner = Owner;

				DateTime date;
				date = (spOwner as CalendarViewItem).DateBase;

				CalendarView pParent = (spOwner as CalendarViewItem).GetParentCalendarView();

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

					pValue = (itemIndex - firstVisibleIndex) % cols;
					if (pValue < 0)
					{
						pValue += cols;
					}
				}

				return pValue;
			}

			private int RowImpl()
			{
				var pValue = 0;

				UIElement spOwner;
				spOwner = Owner;

				DateTime date;
				date = (spOwner as CalendarViewItem).DateBase;

				CalendarView pParent = (spOwner as CalendarViewItem).GetParentCalendarView();

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

					// Find the relative row position w.r.to visible rows
					pValue = (itemIndex - firstVisibleIndex) / cols;
					// the element is not visible and we can't define row
					if (pValue < 0)
					{
						throw new NotSupportedException();
					}
				}

				return pValue;
			}

			private void GetColumnHeaderItemsImpl(out uint pReturnValueCount, out IRawElementProviderSimple[] ppReturnValue)
			{
				pReturnValueCount = 0;
				ppReturnValue = default;
				return;
			}

			private void GetRowHeaderItemsImpl(out uint pReturnValueCount, out IRawElementProviderSimple[] ppReturnValue)
			{
				UIElement spOwner;
				spOwner = Owner;

				CalendarViewItem item = spOwner as CalendarViewItem;
				CalendarView pParent = item.GetParentCalendarView();

				DateTime itemDate;
				itemDate = item.DateBase;

				// Currently we only want this row header read in year mode, not in decade mode.
				pParent.GetRowHeaderForItemAutomationPeer(itemDate, CalendarViewDisplayMode.Year, out pReturnValueCount, out ppReturnValue);

				return;
			}
#endif
		}
	}
}
