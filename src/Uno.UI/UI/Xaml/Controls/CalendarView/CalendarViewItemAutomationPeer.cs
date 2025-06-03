// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference src\dxaml\xcp\dxaml\lib\CalendarViewItemAutomationPeer_Partial.cpp, tag winui3/release/1.7.1, commit 5f27a786ac96

using System;
using System.Reflection.Metadata.Ecma335;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using DateTime = Windows.Foundation.WindowsFoundationDateTime;

namespace Microsoft.UI.Xaml.Automation.Peers;

internal partial class CalendarViewItemAutomationPeer : CalendarViewBaseItemAutomationPeer, IInvokeProvider, ITableItemProvider
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

	protected override string GetClassNameCore() => nameof(CalendarViewItem);

	protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Button;

	public void Invoke()
	{
		UIElement spOwner = Owner;

		CalendarView pParent = (spOwner as CalendarViewItem).GetParentCalendarView();
		pParent.OnSelectMonthYearItem(spOwner as CalendarViewItem, FocusState.Keyboard);
	}

	public override int Column
	{
		get
		{
			var pValue = 0;

			UIElement spOwner = Owner;

			DateTime date = (spOwner as CalendarViewItem).GetDate();

			CalendarView pParent = (spOwner as CalendarViewItem).GetParentCalendarView();

			CalendarViewGeneratorHost spHost;
			pParent.GetActiveGeneratorHost(out spHost);

			CalendarPanel pCalendarPanel = spHost.Panel;
			if (pCalendarPanel is { })
			{
				int itemIndex = spHost.CalculateOffsetFromMinDate(date);

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
	}

	public override int Row
	{
		get
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
	}

	public IRawElementProviderSimple[] GetColumnHeaderItems() => null;

	public IRawElementProviderSimple[] GetRowHeaderItems()
	{
		UIElement spOwner;
		spOwner = Owner;

		CalendarViewItem item = spOwner as CalendarViewItem;
		CalendarView pParent = item.GetParentCalendarView();

		DateTime itemDate = item.DateBase;

		// Currently we only want this row header read in year mode, not in decade mode.
		pParent.GetRowHeaderForItemAutomationPeer(itemDate, CalendarViewDisplayMode.Year, out var pReturnValueCount, out var ppReturnValue);

		return ppReturnValue;
	}
}
