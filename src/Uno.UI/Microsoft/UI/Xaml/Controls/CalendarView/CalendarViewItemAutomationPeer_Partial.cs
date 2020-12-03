// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#include "precomp.h"
#include "CalendarViewItemAutomationPeer.g.h"
#include "CalendarViewItem.g.h"
#include "CalendarView.g.h"
#include "CalendarViewGeneratorHost.h"
#include "CalendarPanel.g.h"

using namespace DirectUI;
using namespace DirectUISynonyms;

IFACEMETHODIMP CalendarViewItemAutomationPeer.GetPatternCore( xaml_automation_peers.PatternInterface patternInterface, _Outptr_ IInspectable** ppReturnValue)
{
    IFCPTR_RETURN(ppReturnValue);
    *ppReturnValue = null;

    if (patternInterface == xaml_automation_peers.PatternInterface_Invoke ||
        patternInterface == xaml_automation_peers.PatternInterface_TableItem)
    {
        *ppReturnValue = ctl.as_iinspectable(this);
        ctl.addref_interface(this);
    }
    else
    {
        IFC_RETURN(CalendarViewItemAutomationPeerGenerated.GetPatternCore(patternInterface, ppReturnValue));
    }
    return S_OK;
}

IFACEMETHODIMP CalendarViewItemAutomationPeer.GetClassNameCore(out HSTRING* pReturnValue)
{
    IFCPTR_RETURN(pReturnValue);
    IFC_RETURN(wrl_wrappers.Hstring(STR_LEN_PAIR("CalendarViewItem")).CopyTo(pReturnValue));
    return S_OK;
}

IFACEMETHODIMP CalendarViewItemAutomationPeer.GetAutomationControlTypeCore(out xaml_automation_peers.AutomationControlType* pReturnValue)
{
    IFCPTR_RETURN(pReturnValue);
    *pReturnValue = xaml_automation_peers.AutomationControlType_Button;

    return S_OK;
}

_Check_return_ HRESULT CalendarViewItemAutomationPeer.InvokeImpl()
{
    ctl.ComPtr<xaml.IUIElement> spOwner;
    IFC_RETURN(get_Owner(&spOwner));
    
    CalendarView *pParent = spOwner.Cast<CalendarViewItem>().GetParentCalendarView();
    IFCPTR_RETURN(pParent);
    IFC_RETURN(pParent.OnSelectMonthYearItem(spOwner.Cast<CalendarViewItem>(), xaml.FocusState.FocusState_Keyboard));

    return S_OK;
}

_Check_return_ HRESULT CalendarViewItemAutomationPeer.get_ColumnImpl(out INT* pValue)
{
    IFCPTR_RETURN(pValue);
    *pValue = 0;

    ctl.ComPtr<xaml.IUIElement> spOwner;
    IFC_RETURN(get_Owner(&spOwner));

    wf.DateTime date;
    IFC_RETURN(spOwner.Cast<CalendarViewItem>().GetDate(&date));

    CalendarView *pParent = spOwner.Cast<CalendarViewItem>().GetParentCalendarView();
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

        *pValue = (itemIndex - firstVisibleIndex) % cols;
        if (*pValue < 0)
        {
            *pValue += cols;
        }
    }
    return S_OK;
}

_Check_return_ HRESULT CalendarViewItemAutomationPeer.get_RowImpl(out INT* pValue)
{
    IFCPTR_RETURN(pValue);
    *pValue = 0;

    ctl.ComPtr<xaml.IUIElement> spOwner;
    IFC_RETURN(get_Owner(&spOwner));
    
    wf.DateTime date;
    IFC_RETURN(spOwner.Cast<CalendarViewItem>().GetDate(&date));

    CalendarView *pParent = spOwner.Cast<CalendarViewItem>().GetParentCalendarView();
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

        // Find the relative row position w.r.to visible rows
        *pValue = (itemIndex - firstVisibleIndex) / cols;
        // the element is not visible and we can't define row
        if (*pValue < 0)
        {
            return E_NOT_SUPPORTED;
        }
    }
    return S_OK;
}

_Check_return_ HRESULT CalendarViewItemAutomationPeer.GetColumnHeaderItemsImpl(out UINT* pReturnValueCount, _Out_writes_to_ptr_(*pReturnValueCount) xaml_automation.Provider.IIRawElementProviderSimple*** ppReturnValue)
{
    *pReturnValueCount = 0;
    return S_OK;
}

_Check_return_ HRESULT CalendarViewItemAutomationPeer.GetRowHeaderItemsImpl(out UINT* pReturnValueCount, _Out_writes_to_ptr_(*pReturnValueCount) xaml_automation.Provider.IIRawElementProviderSimple*** ppReturnValue)
{
    ctl.ComPtr<xaml.IUIElement> spOwner;
    IFC_RETURN(get_Owner(&spOwner));

    CalendarViewItem* item = spOwner.Cast<CalendarViewItem>();
    CalendarView *pParent = item.GetParentCalendarView();
    IFCPTR_RETURN(pParent);

    wf.DateTime itemDate;
    IFC_RETURN(item.GetDate(&itemDate));

    // Currently we only want this row header read in year mode, not in decade mode.
    IFC_RETURN(pParent.GetRowHeaderForItemAutomationPeer(itemDate, xaml_controls.CalendarViewDisplayMode_Year, pReturnValueCount, ppReturnValue));

    return S_OK;
}