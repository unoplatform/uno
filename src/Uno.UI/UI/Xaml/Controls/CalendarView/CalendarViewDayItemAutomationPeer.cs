// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Graphics.Printing.OptionDetails;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls.Primitives;
using DirectUI;
using DateTime = Windows.Foundation.WindowsFoundationDateTime;
using DayOfWeek = Windows.Globalization.DayOfWeek;

namespace Microsoft.UI.Xaml.Controls
{
	partial class CalendarViewDayItem
	{
		internal partial class CalendarViewDayItemAutomationPeer : CalendarViewBaseItemAutomationPeer,
			ITableItemProvider, ISelectionItemProvider
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

			// ITableItemProvider
			public IRawElementProviderSimple[] GetColumnHeaderItems()
			{
				UIElement spOwner;
				spOwner = Owner;

				DateTime date;
				date = ((CalendarViewDayItem)spOwner).Date;

				CalendarView pParent = (spOwner as CalendarViewDayItem).GetParentCalendarView();

				Grid spWeekDayNames;

				// Gets the 'WeekDayNames' part of the container template and find weekindex position as element
				spWeekDayNames = pParent.GetTemplateChild("WeekDayNames") as Grid;
				if (spWeekDayNames is { })
				{
					IList<UIElement> spChildren;
					spChildren = (spWeekDayNames as Grid).Children;

					int weekindex = 0;
					weekindex = Column;

					UIElement spChild;
					spChild = spChildren.ElementAtOrDefault(weekindex);
					if (spChild is { })
					{
						AutomationPeer spItemPeerAsAP;
						spItemPeerAsAP = (spChild as FrameworkElement).GetAutomationPeer();
						if (spItemPeerAsAP is { })
						{
							IRawElementProviderSimple spProvider;
							spProvider = ProviderFromPeer(spItemPeerAsAP);
							return new IRawElementProviderSimple[] { spProvider };
						}
					}
				}

				return Array.Empty<IRawElementProviderSimple>();
			}

			public IRawElementProviderSimple[] GetRowHeaderItems()
			{
				UIElement spOwner;
				spOwner = Owner;

				CalendarViewDayItem item = spOwner as CalendarViewDayItem;
				CalendarView pParent = item.GetParentCalendarView();

				DateTime itemDate;
				itemDate = item.Date;

				pParent.GetRowHeaderForItemAutomationPeer(itemDate, CalendarViewDisplayMode.Month, out _, out var result);

				return result ?? Array.Empty<IRawElementProviderSimple>();
			}

			// ISelectionItemProvider
			public IRawElementProviderSimple SelectionContainer
			{
				get => ContainingGrid;
			}

			public void AddToSelection()
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
			}

			public void RemoveFromSelection()
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
			}

			public bool IsSelected
			{
				get
				{
					UIElement spOwner;
					spOwner = Owner;

					DateTime date;
					date = (spOwner as CalendarViewDayItem).Date;
					CalendarView pParent = (spOwner as CalendarViewDayItem).GetParentCalendarView();

					pParent.IsSelected(date, out var isSelected);
					return isSelected;
				}
			}

			public void Select()
			{
				UIElement spOwner;
				spOwner = Owner;

				DateTime date;
				date = (spOwner as CalendarViewDayItem).Date;

				CalendarView pParent = (spOwner as CalendarViewDayItem).GetParentCalendarView();

				pParent.SelectedDates.Clear();

				pParent.OnSelectDayItem(spOwner as CalendarViewDayItem);
			}

			// IGridItemProvider overrides (calculate visible column/row from index)
			public override int Column
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

						// Calculate the relative position w.r.to the visible elements from item index
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

			public override int Row
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
		}
	}
}
