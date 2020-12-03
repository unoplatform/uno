// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#include "precomp.h"
#include "Grid.g.h"
#include "CalendarViewBaseItemAutomationPeer.g.h"
#include "CalendarViewItem.g.h"
#include "CalendarView.g.h"
#include "CalendarViewGeneratorHost.h"
#include "CalendarPanel.g.h"

using namespace DirectUI;
using namespace DirectUISynonyms;


IFACEMETHODIMP CalendarViewBaseItemAutomationPeer.GetPatternCore( xaml_automation_peers.PatternInterface patternInterface, _Outptr_ IInspectable** ppReturnValue)
{
    IFCPTR_RETURN(ppReturnValue);
    *ppReturnValue = null;

    bool isItemVisible = false;

    // For the GridItem pattern, make sure the item is visible otherwise we might end up returning a negative row value
    // for it.  An item may not be visible if it has been scrolled out of view..
    if (patternInterface == xaml_automation_peers.PatternInterface_GridItem && SUCCEEDED(IsItemVisible(isItemVisible)) && isItemVisible ||
        patternInterface == xaml_automation_peers.PatternInterface_ScrollItem)
    {
        *ppReturnValue = ctl.as_iinspectable(this);
        ctl.addref_interface(this);
    }
    else
    {
        IFC_RETURN(CalendarViewBaseItemAutomationPeerGenerated.GetPatternCore(patternInterface, ppReturnValue));
    }
    return S_OK;
}

IFACEMETHODIMP CalendarViewBaseItemAutomationPeer.GetNameCore(out HSTRING* returnValue)
{
    IFCPTR_RETURN(returnValue);
    IFC_RETURN(CalendarViewBaseItemAutomationPeerGenerated.GetNameCore(returnValue));
    if (*returnValue == null)
    {
        ctl.ComPtr<xaml.IUIElement> spOwner;
        IFC_RETURN(get_Owner(&spOwner));
        IFC_RETURN(spOwner.Cast<CalendarViewItem>().GetMainText(returnValue));
    }
    return S_OK;
}

_Check_return_ HRESULT CalendarViewBaseItemAutomationPeer.get_ColumnSpanImpl(out INT* pValue)
{
    IFCPTR_RETURN(pValue);
    *pValue = 1;
    return S_OK;
}

_Check_return_ HRESULT CalendarViewBaseItemAutomationPeer.get_ContainingGridImpl(_Outptr_result_maybenull_ xaml_automation.Provider.IIRawElementProviderSimple** ppValue)
{
    IFCPTR_RETURN(ppValue);
    *ppValue = FALSE;

    ctl.ComPtr<xaml.IUIElement> spOwner;
    IFC_RETURN(get_Owner(&spOwner));
    
    ctl.ComPtr<IAutomationPeer> spAutomationPeer;
    CalendarView *pParent = spOwner.Cast<CalendarViewBaseItem>().GetParentCalendarView();
    IFCPTR_RETURN(pParent);

    IFC_RETURN(pParent.GetOrCreateAutomationPeer(&spAutomationPeer));
    IFC_RETURN(ProviderFromPeer(spAutomationPeer.Get(), ppValue));
    return S_OK;
}

_Check_return_ HRESULT CalendarViewBaseItemAutomationPeer.get_RowSpanImpl(out INT* pValue)
{
    IFCPTR_RETURN(pValue);
    *pValue = 1;
    return S_OK;
}

// Methods.

_Check_return_ HRESULT CalendarViewBaseItemAutomationPeer.ScrollIntoViewImpl()
{
    ctl.ComPtr<xaml.IUIElement> spOwner;
    IFC_RETURN(get_Owner(&spOwner));

    wf.DateTime date;
    IFC_RETURN(spOwner.Cast<CalendarViewItem>().GetDate(&date));

    CalendarView *pParent = spOwner.Cast<CalendarViewBaseItem>().GetParentCalendarView();
    IFCPTR_RETURN(pParent);

    IFC_RETURN(pParent.SetDisplayDate(date));
    
    return S_OK;
}

_Check_return_ HRESULT CalendarViewBaseItemAutomationPeer.IsItemVisible(bool& isVisible)
{
    isVisible = false;

    ctl.ComPtr<xaml.IUIElement> owner;
    IFC_RETURN(get_Owner(&owner));

    var parent = owner.Cast<CalendarViewBaseItem>().GetParentCalendarView();

    ctl.ComPtr<CalendarViewGeneratorHost> host;
    IFC_RETURN(parent.GetActiveGeneratorHost(&host));

    var calendarPanel = host.GetPanel();
    if (calendarPanel)
    {
        wf.DateTime date = {};
        IFC_RETURN(owner.Cast<CalendarViewBaseItem>().GetDate(&date));

        int itemIndex = 0;
        IFC_RETURN(host.CalculateOffsetFromMinDate(date, &itemIndex));

        int firstVisibleIndex = 0;
        IFC_RETURN(calendarPanel.get_FirstVisibleIndex(&firstVisibleIndex));

        int lastVisibleIndex = 0;
        IFC_RETURN(calendarPanel.get_LastVisibleIndex(&lastVisibleIndex));

        isVisible = (itemIndex >= firstVisibleIndex && itemIndex <= lastVisibleIndex);
    }

    return S_OK;
}
