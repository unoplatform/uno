// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using DateTime = Windows.Foundation.WindowsFoundationDateTime;

namespace Microsoft.UI.Xaml.Controls
{
	partial class CalendarViewBaseItem
	{
		internal class CalendarViewBaseItemAutomationPeer : FrameworkElementAutomationPeer,
			IGridItemProvider, IScrollItemProvider
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

			// IGridItemProvider
			public int ColumnSpan => 1;

			public IRawElementProviderSimple ContainingGrid
			{
				get
				{
					UIElement spOwner;
					spOwner = Owner;

					AutomationPeer spAutomationPeer;
					CalendarView pParent = (spOwner as CalendarViewBaseItem).GetParentCalendarView();

					spAutomationPeer = pParent.GetAutomationPeer();
					return ProviderFromPeer(spAutomationPeer);
				}
			}

			public int RowSpan => 1;

			// Column and Row are virtual-like - overridden in derived classes
			public virtual int Column => 0;

			public virtual int Row => 0;

			// IScrollItemProvider
			public void ScrollIntoView()
			{
				UIElement spOwner;
				spOwner = Owner;

				DateTime date;
				date = (spOwner as CalendarViewItem).DateBase;

				CalendarView pParent = (spOwner as CalendarViewBaseItem).GetParentCalendarView();

				pParent.SetDisplayDate(date);
			}

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
