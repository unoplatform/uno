// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using Windows.UI.Xaml.Automation.Peers;
using Windows.UI.Xaml.Automation.Provider;
using DateTime = Windows.Foundation.WindowsFoundationDateTime;

namespace Windows.UI.Xaml.Controls
{
	partial class CalendarViewBaseItem
	{
		internal class CalendarViewBaseItemAutomationPeer : FrameworkElementAutomationPeer
		{
			internal CalendarViewBaseItemAutomationPeer(object owner) : base(owner)
			{

			}

			protected override object GetPatternCore(PatternInterface patternInterface)
			{
				object ppReturnValue = null;

				bool isItemVisible = false;

				isItemVisible = IsItemVisible();

				// For the GridItem pattern, make sure the item is visible otherwise we might end up returning a negative row value
				// for it.  An item may not be visible if it has been scrolled out of view..
				if (patternInterface == PatternInterface.GridItem && isItemVisible ||
					patternInterface == PatternInterface.ScrollItem)
				{
					ppReturnValue = this;
				}
				else
				{
					ppReturnValue = base.GetPatternCore(patternInterface);
				}

				return ppReturnValue;
			}

			protected override string GetNameCore()
			{
				var returnValue = base.GetNameCore();
				if (returnValue == null)
				{
					UIElement spOwner;
					spOwner = Owner;
					returnValue = (spOwner as CalendarViewItem).GetMainText();
				}

				return returnValue;
			}

#if false
			private int ColumnSpanImpl()
			{
				var pValue = 1;
				return pValue;
			}
#endif

			protected IRawElementProviderSimple ContainingGridImpl()
			{
				IRawElementProviderSimple ppValue = default;

				UIElement spOwner;
				spOwner = Owner;

				AutomationPeer spAutomationPeer;
				CalendarView pParent = (spOwner as CalendarViewBaseItem).GetParentCalendarView();

				spAutomationPeer = pParent.GetAutomationPeer();
				ppValue = ProviderFromPeer(spAutomationPeer);
				return ppValue;
			}

#if false
			private int RowSpanImpl()
			{
				var pValue = 1;
				return pValue;
			}

			// Methods.

			private void ScrollIntoViewImpl()
			{
				UIElement spOwner;
				spOwner = Owner;

				DateTime date;
				date = (spOwner as CalendarViewItem).DateBase;

				CalendarView pParent = (spOwner as CalendarViewBaseItem).GetParentCalendarView();

				pParent.SetDisplayDate(date);

				return;
			}
#endif

			private bool IsItemVisible()
			{
				var isVisible = false;

				UIElement owner;
				owner = Owner;

				var parent = (owner as CalendarViewBaseItem).GetParentCalendarView();

				CalendarViewGeneratorHost host;
				parent.GetActiveGeneratorHost(out host);

				var calendarPanel = host.Panel;
				if (calendarPanel is { })
				{
					DateTime date = default;
					date = (owner as CalendarViewBaseItem).DateBase;

					int itemIndex = 0;
					itemIndex = host.CalculateOffsetFromMinDate(date);

					int firstVisibleIndex = 0;
					firstVisibleIndex = calendarPanel.FirstVisibleIndex;

					int lastVisibleIndex = 0;
					lastVisibleIndex = calendarPanel.LastVisibleIndex;

					isVisible = (itemIndex >= firstVisibleIndex && itemIndex <= lastVisibleIndex);
				}

				return isVisible;
			}
		}
	}
}
