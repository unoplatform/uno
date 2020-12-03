// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#include "precomp.h"
#include "Grid.g.h"
#include "CalendarViewDayItemAutomationPeer.g.h"
#include "CalendarViewDayItem.g.h"
#include "CalendarView.g.h"
#include "CalendarViewGeneratorHost.h"
#include "CalendarPanel.g.h"

using namespace DirectUI;
using namespace DirectUISynonyms;

IFACEMETHODIMP CalendarViewDayItemAutomationPeer.GetPatternCore( xaml_automation_peers.PatternInterface patternInterface, _Outptr_ IInspectable** ppReturnValue)
{
    IFCPTR_RETURN(ppReturnValue);
    *ppReturnValue = null;

    if (patternInterface == xaml_automation_peers.PatternInterface_TableItem ||
        patternInterface == xaml_automation_peers.PatternInterface_SelectionItem)
    {
        *ppReturnValue = ctl.as_iinspectable(this);
        ctl.addref_interface(this);
    }
    else
    {
        IFC_RETURN(CalendarViewDayItemAutomationPeerGenerated.GetPatternCore(patternInterface, ppReturnValue));
    }
    return S_OK;
}

IFACEMETHODIMP CalendarViewDayItemAutomationPeer.GetAutomationControlTypeCore(out xaml_automation_peers.AutomationControlType* pReturnValue)
{
    IFCPTR_RETURN(pReturnValue);
    *pReturnValue = xaml_automation_peers.AutomationControlType_DataItem;

    return S_OK;
}

IFACEMETHODIMP CalendarViewDayItemAutomationPeer.GetClassNameCore(out HSTRING* pReturnValue)
{
    IFCPTR_RETURN(pReturnValue);
    IFC_RETURN(wrl_wrappers.Hstring(STR_LEN_PAIR("CalendarViewDayItem")).CopyTo(pReturnValue));
    return S_OK;
}

IFACEMETHODIMP CalendarViewDayItemAutomationPeer.IsEnabledCore(out BOOLEAN* pReturnValue)
{
    IFCPTR_RETURN(pReturnValue);
    ctl.ComPtr<xaml.IUIElement> spOwner;
    IFC_RETURN(get_Owner(&spOwner));

    BOOLEAN isBlackout = FALSE;
    IFC_RETURN(spOwner.Cast<CalendarViewDayItem>().get_IsBlackout(&isBlackout));

    // This property also takes in consideration of a day is 'blackout' or not. Blackout dates are disabled
    if (isBlackout)
    {
        *pReturnValue = FALSE;
    }
    else
    {
        IFC_RETURN(CalendarViewDayItemAutomationPeerGenerated.IsEnabledCore(pReturnValue));
    }
    return S_OK;
}

_Check_return_ HRESULT CalendarViewDayItemAutomationPeer.GetColumnHeaderItemsImpl(out UINT* pReturnValueCount, _Out_writes_to_ptr_(*pReturnValueCount) xaml_automation.Provider.IIRawElementProviderSimple*** ppReturnValue)
{
    *pReturnValueCount = 0;
    ctl.ComPtr<xaml.IUIElement> spOwner;
    IFC_RETURN(get_Owner(&spOwner));

    wf.DateTime date;
    IFC_RETURN(spOwner.Cast<CalendarViewDayItem>().GetDate(&date));

    CalendarView *pParent = spOwner.Cast<CalendarViewDayItem>().GetParentCalendarView();
    IFCPTR_RETURN(pParent);

    UINT nCount = 0;
    ctl.ComPtr<xaml_controls.IGrid> spWeekDayNames;

    // Gets the 'WeekDayNames' part of the container template and find weekindex position as elment
    IFC_RETURN(pParent.GetTemplatePart<xaml_controls.IGrid>(STR_LEN_PAIR("WeekDayNames"), spWeekDayNames.ReleaseAndGetAddressOf()));
    if (spWeekDayNames)
    {
        ctl.ComPtr<wfc.IList<xaml.UIElement*>> spChildren;
        IFC_RETURN(spWeekDayNames.Cast<Grid>().get_Children(&spChildren));
        IFC_RETURN(spChildren.get_Size(&nCount));

		int weekindex = 0;
        IFC_RETURN(get_ColumnImpl(&weekindex));

        ctl.ComPtr<xaml_automation_peers.IAutomationPeer> spItemPeerAsAP;
        ctl.ComPtr<xaml_automation.Provider.IIRawElementProviderSimple> spProvider;
        ctl.ComPtr<xaml.IUIElement> spChild;
        IFC_RETURN(spChildren.GetAt(weekindex, &spChild));
		if (spChild)
		{
			IFC_RETURN(spChild.Cast<UIElement>().GetOrCreateAutomationPeer(&spItemPeerAsAP));
			if (spItemPeerAsAP)
			{
				UINT allocSize = sizeof(IIRawElementProviderSimple*);
				*ppReturnValue = (IIRawElementProviderSimple**)(CoTaskMemAlloc(allocSize));
                IFCOOMFAILFAST(*ppReturnValue);
				ZeroMemory(*ppReturnValue, allocSize);

				IFC_RETURN(ProviderFromPeer(spItemPeerAsAP.Get(), &spProvider));
				(*ppReturnValue)[0] = spProvider.Detach();
				*pReturnValueCount = 1;
			}
		}
    }
    return S_OK;
}

_Check_return_ HRESULT CalendarViewDayItemAutomationPeer.GetRowHeaderItemsImpl(out UINT* pReturnValueCount, _Out_writes_to_ptr_(*pReturnValueCount) xaml_automation.Provider.IIRawElementProviderSimple*** ppReturnValue)
{
    ctl.ComPtr<xaml.IUIElement> spOwner;
    IFC_RETURN(get_Owner(&spOwner));

    CalendarViewDayItem* item = spOwner.Cast<CalendarViewDayItem>();
    CalendarView *pParent = item.GetParentCalendarView();
    IFCPTR_RETURN(pParent);

    wf.DateTime itemDate;
    IFC_RETURN(item.GetDate(&itemDate));

    IFC_RETURN(pParent.GetRowHeaderForItemAutomationPeer(itemDate, xaml_controls.CalendarViewDisplayMode_Month, pReturnValueCount, ppReturnValue));

    return S_OK;
}

_Check_return_ HRESULT CalendarViewDayItemAutomationPeer.get_SelectionContainerImpl(_Outptr_result_maybenull_ xaml_automation.Provider.IIRawElementProviderSimple** ppValue)
{
    return get_ContainingGridImpl(ppValue);
}

_Check_return_ HRESULT CalendarViewDayItemAutomationPeer.AddToSelectionImpl()
{
    ctl.ComPtr<xaml.IUIElement> spOwner;
    IFC_RETURN(get_Owner(&spOwner));

    wf.DateTime date;
    IFC_RETURN(spOwner.Cast<CalendarViewDayItem>().GetDate(&date));

    CalendarView *pParent = spOwner.Cast<CalendarViewDayItem>().GetParentCalendarView();
    IFCPTR_RETURN(pParent);

    bool isSelected = false;
    IFC_RETURN(pParent.IsSelected(date, &isSelected));
    if (!isSelected)
    {
        IFC_RETURN(pParent.OnSelectDayItem(spOwner.Cast<CalendarViewDayItem>()));
    }

    return S_OK;
}

_Check_return_ HRESULT CalendarViewDayItemAutomationPeer.RemoveFromSelectionImpl()
{
    ctl.ComPtr<xaml.IUIElement> spOwner;
    IFC_RETURN(get_Owner(&spOwner));

    wf.DateTime date;
    IFC_RETURN(spOwner.Cast<CalendarViewDayItem>().GetDate(&date));

    CalendarView *pParent = spOwner.Cast<CalendarViewDayItem>().GetParentCalendarView();
    IFCPTR_RETURN(pParent);

    bool isSelected = false;
    IFC_RETURN(pParent.IsSelected(date, &isSelected));
    if (isSelected)
    {
        IFC_RETURN(pParent.OnSelectDayItem(spOwner.Cast<CalendarViewDayItem>()));
    }

    return S_OK;
}

_Check_return_ HRESULT CalendarViewDayItemAutomationPeer.get_IsSelectedImpl(out BOOLEAN* pValue)
{
    IFCPTR_RETURN(pValue);
    *pValue = FALSE;

    ctl.ComPtr<xaml.IUIElement> spOwner;
    IFC_RETURN(get_Owner(&spOwner));

    wf.DateTime date;
    IFC_RETURN(spOwner.Cast<CalendarViewDayItem>().GetDate(&date));
    bool isSelected = false;
    CalendarView *pParent = spOwner.Cast<CalendarViewDayItem>().GetParentCalendarView();
    IFCPTR_RETURN(pParent);

    IFC_RETURN(pParent.IsSelected(date, &isSelected));
    if (isSelected)
    {
        *pValue = TRUE;
    }
    return S_OK;
}

_Check_return_ HRESULT CalendarViewDayItemAutomationPeer.SelectImpl()
{
	ctl.ComPtr<xaml.IUIElement> spOwner;
	IFC_RETURN(get_Owner(&spOwner));

	wf.DateTime date;
	IFC_RETURN(spOwner.Cast<CalendarViewDayItem>().GetDate(&date));

	CalendarView *pParent = spOwner.Cast<CalendarViewDayItem>().GetParentCalendarView();
	IFCPTR_RETURN(pParent);

	ctl.ComPtr<wfc.IList<wf.DateTime>> spSelectedItems;
	IFC_RETURN(pParent.get_SelectedDates(&spSelectedItems));
	IFC_RETURN(spSelectedItems.Clear());

	IFC_RETURN(pParent.OnSelectDayItem(spOwner.Cast<CalendarViewDayItem>()));

	return S_OK;
}

// calculate visible column from index of the item
_Check_return_ HRESULT CalendarViewDayItemAutomationPeer.get_ColumnImpl(out INT* pValue)
{
    IFCPTR_RETURN(pValue);
    *pValue = 0;

    ctl.ComPtr<xaml.IUIElement> spOwner;
    IFC_RETURN(get_Owner(&spOwner));

    wf.DateTime date;
    IFC_RETURN(spOwner.Cast<CalendarViewDayItem>().GetDate(&date));

    CalendarView *pParent = spOwner.Cast<CalendarViewDayItem>().GetParentCalendarView();
    IFCPTR_RETURN(pParent);

    ctl.ComPtr<CalendarViewGeneratorHost> spHost;
    IFC_RETURN(pParent.GetActiveGeneratorHost(&spHost));

    CalendarPanel* pCalendarPanel = spHost.GetPanel();
    if (pCalendarPanel)
    {
        int itemIndex = 0;

        // Get realized item index
        IFC_RETURN(spHost.CalculateOffsetFromMinDate(date, &itemIndex));

        int cols = 1;
        IFC_RETURN(pCalendarPanel.get_Cols(&cols));
        int firstVisibleIndex = 0;
        IFC_RETURN(pCalendarPanel.get_FirstVisibleIndex(&firstVisibleIndex));

        // Calculate the relative positon w.r.to the visible elements from item index
        int relativePos = (itemIndex - firstVisibleIndex);
        if (firstVisibleIndex < cols)
        {
            int monthViewStartIndex = 0;
            IFC_RETURN(pCalendarPanel.get_StartIndex(&monthViewStartIndex));
            relativePos += monthViewStartIndex;
        }

        *pValue = relativePos % cols;
        if (*pValue < 0)
        {
            *pValue += cols;
        }
    }
    return S_OK;
}

_Check_return_ HRESULT CalendarViewDayItemAutomationPeer.get_RowImpl(out INT* pValue)
{
    IFCPTR_RETURN(pValue);
    *pValue = 0;

    ctl.ComPtr<xaml.IUIElement> spOwner;
    IFC_RETURN(get_Owner(&spOwner));

    wf.DateTime date;
    IFC_RETURN(spOwner.Cast<CalendarViewDayItem>().GetDate(&date));

    CalendarView *pParent = spOwner.Cast<CalendarViewDayItem>().GetParentCalendarView();
    IFCPTR_RETURN(pParent);

    ctl.ComPtr<CalendarViewGeneratorHost> spHost;
    IFC_RETURN(pParent.GetActiveGeneratorHost(&spHost));

    CalendarPanel* pCalendarPanel = spHost.GetPanel();
    if (pCalendarPanel)
    {
        int itemIndex = 0;
        IFC_RETURN(spHost.CalculateOffsetFromMinDate(date, &itemIndex));

        int cols = 1;
        IFC_RETURN(pCalendarPanel.get_Cols(&cols));
        int firstVisibleIndex = 0;
        IFC_RETURN(pCalendarPanel.get_FirstVisibleIndex(&firstVisibleIndex));

        wg.DayOfWeek firstDayOfWeek = wg.DayOfWeek_Sunday;
        IFC_RETURN(pParent.get_FirstDayOfWeek(&firstDayOfWeek));

        // Find the relative row position w.r.to visible rows
        int relativePos = (itemIndex - firstVisibleIndex);
        if (firstVisibleIndex < cols)
        {
            int monthViewStartIndex = 0;
            IFC_RETURN(pCalendarPanel.get_StartIndex(&monthViewStartIndex));
            relativePos += monthViewStartIndex;
        }

        *pValue = relativePos / cols;
        // the element is not visible and we can't define row
        if (*pValue < 0)
        {
            return E_NOT_SUPPORTED;
        }
    }
    return S_OK;
}
