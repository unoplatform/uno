// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#include "precomp.h"
#include "CalendarViewAutomationPeer.g.h"
#include "Grid.g.h"
#include "CalendarView.g.h"
#include "CalendarPanel.g.h"
#include "CalendarViewBaseItem.g.h"
#include "CalendarViewGeneratorHost.h"
#include "CalendarViewSelectedDatesChangedEventArgs.g.h"
#include <windows.globalization.datetimeformatting.h>

using namespace DirectUI;
using namespace DirectUISynonyms;

IFACEMETHODIMP CalendarViewAutomationPeer.GetPatternCore( xaml_automation_peers.PatternInterface patternInterface, _Outptr_ IInspectable** ppReturnValue)
{
    IFCPTR_RETURN(ppReturnValue);
    *ppReturnValue = null;

    if (patternInterface == xaml_automation_peers.PatternInterface_Table ||
        patternInterface == xaml_automation_peers.PatternInterface_Grid ||
        patternInterface == xaml_automation_peers.PatternInterface_Value ||
        patternInterface == xaml_automation_peers.PatternInterface_Selection)
    {
        *ppReturnValue = ctl.as_iinspectable(this);
        ctl.addref_interface(this);
    }
    else
    {
        IFC_RETURN(CalendarViewAutomationPeerGenerated.GetPatternCore(patternInterface, ppReturnValue));
    }

    return S_OK;
}

IFACEMETHODIMP CalendarViewAutomationPeer.GetClassNameCore(out HSTRING* pReturnValue)
{
    IFCPTR_RETURN(pReturnValue);
    IFC_RETURN(wrl_wrappers.Hstring(STR_LEN_PAIR("CalendarView")).CopyTo(pReturnValue));

    return S_OK;
}

IFACEMETHODIMP CalendarViewAutomationPeer.GetAutomationControlTypeCore(out xaml_automation_peers.AutomationControlType* pReturnValue)
{
    IFCPTR_RETURN(pReturnValue);
    *pReturnValue = xaml_automation_peers.AutomationControlType_Calendar;

    return S_OK;
}

IFACEMETHODIMP CalendarViewAutomationPeer.GetChildrenCore(_Outptr_ wfc.IList<xaml_automation_peers.AutomationPeer*>** ppReturnValue)
{
    IFCPTR_RETURN(ppReturnValue);

    ctl.ComPtr<xaml.IUIElement> spOwner;
    IFC_RETURN(get_Owner(&spOwner));

    IFC_RETURN(CalendarViewAutomationPeerGenerated.GetChildrenCore(ppReturnValue));
    if (*ppReturnValue != null)
    {
        IFC_RETURN(RemoveAPs(*ppReturnValue));
    }
    return S_OK;
}

// This function removes views that are not active.
HRESULT CalendarViewAutomationPeer.RemoveAPs(__inout wfc.IList<xaml_automation_peers.AutomationPeer*>* pAPCollection)
{
    ctl.ComPtr<xaml.IUIElement> spOwner;
    IFC_RETURN(get_Owner(&spOwner));

    UINT count = 0;
    IFC_RETURN(pAPCollection.get_Size(&count));
    for (int index = (int)count - 1; index >= 0; index--)
    {
        ctl.ComPtr<xaml_automation_peers.IAutomationPeer> spAutomationPeer;
        IFC_RETURN(pAPCollection.GetAt(index, &spAutomationPeer));
        if (spAutomationPeer != null)
        {
            ctl.ComPtr<xaml.IUIElement> spCurrent;
            IFC_RETURN(spAutomationPeer.Cast<FrameworkElementAutomationPeer>().get_Owner(&spCurrent));

            while (spCurrent != null && spCurrent.Get() != spOwner.Get())
            {
                DOUBLE opacity = 1.0;
                IFC_RETURN(spCurrent.get_Opacity(&opacity));
                if (opacity == 0.0)
                {
                    IFC_RETURN(pAPCollection.RemoveAt(index));
                    break;
                }
                ctl.ComPtr<xaml.IDependencyObject> spParent;
                IFC_RETURN(spCurrent.AsOrNull<IFrameworkElement>().get_Parent(&spParent));
                spCurrent = spParent.AsOrNull<xaml.IUIElement>();
            }
        }
    }

    return S_OK;
}

// Properties.
_Check_return_ HRESULT CalendarViewAutomationPeer.get_CanSelectMultipleImpl(out BOOLEAN* pValue)
{
    IFCPTR_RETURN(pValue);
    *pValue = FALSE;

    ctl.ComPtr<xaml.IUIElement> spOwner;
    IFC_RETURN(get_Owner(&spOwner));


    xaml_controls.CalendarViewSelectionMode selectionMode = xaml_controls.CalendarViewSelectionMode.CalendarViewSelectionMode_None;
    IFC_RETURN(spOwner.Cast<CalendarView>().get_SelectionMode(&selectionMode));

    if (selectionMode == xaml_controls.CalendarViewSelectionMode.CalendarViewSelectionMode_Multiple)
    {
        *pValue = TRUE;
    }
    return S_OK;
}

_Check_return_ HRESULT CalendarViewAutomationPeer.get_IsSelectionRequiredImpl(out BOOLEAN* pValue)
{
    IFCPTR_RETURN(pValue);
    *pValue = FALSE;
    return S_OK;
}

_Check_return_ HRESULT CalendarViewAutomationPeer.get_IsReadOnlyImpl(out BOOLEAN* pValue)
{
    IFCPTR_RETURN(pValue);
    *pValue = TRUE;
    return S_OK;
}

// This will be date string if single date is selected otherwise the name of header of view
_Check_return_ HRESULT CalendarViewAutomationPeer.get_ValueImpl(out HSTRING* pValue)
{
    ctl.ComPtr<xaml.IUIElement> spOwner;
    IFC_RETURN(get_Owner(&spOwner));

    UINT count = 0;
    IFC_RETURN(spOwner.Cast<CalendarView>().m_tpSelectedDates.get_Size(&count));
    if (count == 1)
    {
        wrl_wrappers.HString string;
        ctl.ComPtr<wg.DateTimeFormatting.IDateTimeFormatter> spFormatter;
        IFC_RETURN(spOwner.Cast<CalendarView>().CreateDateTimeFormatter(wrl_wrappers.Hstring(STR_LEN_PAIR("day month.full year")).Get(), &spFormatter));

        wf.DateTime date;
        IFC_RETURN(spOwner.Cast<CalendarView>().m_tpSelectedDates.GetAt(0, &date));

        IFC_RETURN(spFormatter.Format(date, string.GetAddressOf()));
        IFC_RETURN(string.CopyTo(pValue));
    }
    else
    {
        ctl.ComPtr<CalendarViewGeneratorHost> spHost;
        IFC_RETURN(spOwner.Cast<CalendarView>().GetActiveGeneratorHost(&spHost));
        IFC_RETURN(spHost.GetHeaderTextOfCurrentScope().CopyTo(pValue));
    }

    return S_OK;
}

_Check_return_ HRESULT CalendarViewAutomationPeer.get_RowOrColumnMajorImpl(out xaml_automation.RowOrColumnMajor* pValue)
{
    IFCPTR_RETURN(pValue);
    *pValue = xaml_automation.RowOrColumnMajor.RowOrColumnMajor_RowMajor;
    return S_OK;
}

_Check_return_ HRESULT CalendarViewAutomationPeer.get_ColumnCountImpl(out INT* pValue)
{
    IFCPTR_RETURN(pValue);
    *pValue = 0;

    ctl.ComPtr<xaml.IUIElement> spOwner;
    IFC_RETURN(get_Owner(&spOwner));

    ctl.ComPtr<CalendarViewGeneratorHost> spHost;
    IFC_RETURN(spOwner.Cast<CalendarView>().GetActiveGeneratorHost(&spHost));
    CalendarPanel* pCalendarPanel = spHost.GetPanel();
    if (pCalendarPanel)
    {
        IFC_RETURN(pCalendarPanel.get_Cols(pValue));
    }
    return S_OK;
}

_Check_return_ HRESULT CalendarViewAutomationPeer.get_RowCountImpl(out INT* pValue)
{
    IFCPTR_RETURN(pValue);
    *pValue = 0;

    ctl.ComPtr<xaml.IUIElement> spOwner;
    IFC_RETURN(get_Owner(&spOwner));

    ctl.ComPtr<CalendarViewGeneratorHost> spHost;
    IFC_RETURN(spOwner.Cast<CalendarView>().GetActiveGeneratorHost(&spHost));
    CalendarPanel* pCalendarPanel = spHost.GetPanel();
    if (pCalendarPanel)
    {
        IFC_RETURN(pCalendarPanel.get_Rows(pValue));
    }
    return S_OK;
}

// This will be visible rows in the view, real number of rows will not provide any value
_Check_return_ HRESULT CalendarViewAutomationPeer.GetSelectionImpl(out UINT* pReturnValueCount, _Out_writes_to_ptr_(*pReturnValueCount) xaml_automation.Provider.IIRawElementProviderSimple*** ppReturnValue)
{
    IFCPTR_RETURN(pReturnValueCount);
    ctl.ComPtr<xaml.IUIElement> spOwner;
    IFC_RETURN(get_Owner(&spOwner));
    *pReturnValueCount = 0;


    xaml_controls.CalendarViewDisplayMode mode = xaml_controls.CalendarViewDisplayMode_Month;
    IFC_RETURN(spOwner.Cast<CalendarView>().get_DisplayMode(&mode));
    if (mode == xaml_controls.CalendarViewDisplayMode_Month)
    {
        UINT count = 0;
        UINT realizedCount = 0;

        IFC_RETURN(spOwner.Cast<CalendarView>().m_tpSelectedDates.get_Size(&count));
        if (count > 0)
        {
            ctl.ComPtr<CalendarViewGeneratorHost> spHost;
            IFC_RETURN(spOwner.Cast<CalendarView>().GetActiveGeneratorHost(&spHost));

            ctl.ComPtr<wfc.IList<xaml_automation_peers.AutomationPeer*>> spAPChildren;
            IFC_RETURN(ctl.ComObject<TrackerCollection<xaml_automation_peers.AutomationPeer*>>.CreateInstance(spAPChildren.ReleaseAndGetAddressOf()));

            var pPanel = spHost.GetPanel();
            if (pPanel)
            {
                for (UINT i = 0; i < count; i++)
                {
                    wf.DateTime date;
                    int itemIndex = 0;
                    ctl.ComPtr<IDependencyObject> spItemAsI;
                    ctl.ComPtr<CalendarViewBaseItem> spItem;
                    ctl.ComPtr<xaml_automation_peers.IAutomationPeer> spItemPeerAsAP;

                    IFC_RETURN(spOwner.Cast<CalendarView>().m_tpSelectedDates.GetAt(i, &date));
                    IFC_RETURN(spHost.CalculateOffsetFromMinDate(date, &itemIndex));

                    IFC_RETURN(pPanel.ContainerFromIndex(itemIndex, &spItemAsI));
                    if (spItemAsI)
                    {
                        IFC_RETURN(spItemAsI.As(&spItem));
                        IFC_RETURN(spItem.GetOrCreateAutomationPeer(&spItemPeerAsAP));
                        IFC_RETURN(spAPChildren.Append(spItemPeerAsAP.Get()));
                    }
                }

                IFC_RETURN(spAPChildren.get_Size(&realizedCount));
                if (realizedCount > 0)
                {
                    UINT allocSize = sizeof(IIRawElementProviderSimple*) * realizedCount;
                    *ppReturnValue = (IIRawElementProviderSimple**)(CoTaskMemAlloc(allocSize));
                    IFCOOMFAILFAST(*ppReturnValue);
                    ZeroMemory(*ppReturnValue, allocSize);

                    for (UINT index = 0; index < realizedCount; index++)
                    {
                        ctl.ComPtr<xaml_automation_peers.IAutomationPeer> spItemPeerAsAP;
                        ctl.ComPtr<xaml_automation.Provider.IIRawElementProviderSimple> spProvider;
                        IFC_RETURN(spAPChildren.GetAt(index, &spItemPeerAsAP));
                        IFC_RETURN(ProviderFromPeer(spItemPeerAsAP.Get(), &spProvider));
                        (*ppReturnValue)[index] = spProvider.Detach();
                    }
                }
                *pReturnValueCount = realizedCount;
            }
        }
    }
    return S_OK;
}

_Check_return_ HRESULT CalendarViewAutomationPeer.SetValueImpl( HSTRING value)
{
    return E_NOTIMPL;
}

// This will returns the header text labels on the top only in the case of monthView
_Check_return_ HRESULT CalendarViewAutomationPeer.GetColumnHeadersImpl(out UINT* pReturnValueCount, _Out_writes_to_ptr_(*pReturnValueCount) xaml_automation.Provider.IIRawElementProviderSimple*** ppReturnValue)
{
    *pReturnValueCount = 0;
    ctl.ComPtr<xaml.IUIElement> spOwner;
    IFC_RETURN(get_Owner(&spOwner));

    xaml_controls.CalendarViewDisplayMode mode = xaml_controls.CalendarViewDisplayMode_Month;
    IFC_RETURN(spOwner.Cast<CalendarView>().get_DisplayMode(&mode));
    if (mode == xaml_controls.CalendarViewDisplayMode_Month)
    {
        UINT nCount = 0;
        ctl.ComPtr<xaml_controls.IGrid> spWeekDayNames;
        IFC_RETURN(spOwner.Cast<CalendarView>().GetTemplatePart<xaml_controls.IGrid>(STR_LEN_PAIR("WeekDayNames"), spWeekDayNames.ReleaseAndGetAddressOf()));
        if (spWeekDayNames)
        {
            ctl.ComPtr<wfc.IList<xaml.UIElement*>> spChildren;
            IFC_RETURN(spWeekDayNames.Cast<Grid>().get_Children(&spChildren));
            IFC_RETURN(spChildren.get_Size(&nCount));

            UINT allocSize = sizeof(IIRawElementProviderSimple*) * nCount;
            *ppReturnValue = (IIRawElementProviderSimple**)(CoTaskMemAlloc(allocSize));
            IFCOOMFAILFAST(*ppReturnValue);
            ZeroMemory(*ppReturnValue, allocSize);
            for (UINT i = 0; i < nCount; i++)
            {
                ctl.ComPtr<xaml_automation_peers.IAutomationPeer> spItemPeerAsAP;
                ctl.ComPtr<xaml_automation.Provider.IIRawElementProviderSimple> spProvider;
                ctl.ComPtr<xaml.IUIElement> spChild;
                IFC_RETURN(spChildren.GetAt(i, &spChild));

                IFC_RETURN(spChild.Cast<UIElement>().GetOrCreateAutomationPeer(&spItemPeerAsAP));
                IFC_RETURN(ProviderFromPeer(spItemPeerAsAP.Get(), &spProvider));
                (*ppReturnValue)[i] = spProvider.Detach();
            }
            *pReturnValueCount = nCount;
        }
    }

    return S_OK;
}

_Check_return_ HRESULT CalendarViewAutomationPeer.GetRowHeadersImpl(out UINT* pReturnValueCount, _Out_writes_to_ptr_(*pReturnValueCount) xaml_automation.Provider.IIRawElementProviderSimple*** ppReturnValue)
{
    *pReturnValueCount = 0;
    return S_OK;
}


_Check_return_ HRESULT CalendarViewAutomationPeer.GetItemImpl( INT row,  INT column, _Outptr_ xaml_automation.Provider.IIRawElementProviderSimple** ppReturnValue)
{
    *ppReturnValue = null;

    ctl.ComPtr<xaml.IUIElement> spOwner;
    IFC_RETURN(get_Owner(&spOwner));

    ctl.ComPtr<CalendarViewGeneratorHost> spHost;
    IFC_RETURN(spOwner.Cast<CalendarView>().GetActiveGeneratorHost(&spHost));

    ctl.ComPtr<IDependencyObject> spItemAsI;
    ctl.ComPtr<CalendarViewBaseItem> spItem;
    ctl.ComPtr<xaml_automation_peers.IAutomationPeer> spItemPeerAsAP;
    ctl.ComPtr<xaml_automation.Provider.IIRawElementProviderSimple> spProvider;

    int colCount = 0;
    IFC_RETURN(get_ColumnCountImpl(&colCount));

    int firstVisibleIndex = 0;
    CalendarPanel* pCalendarPanel = spHost.GetPanel();
    if (pCalendarPanel)
    {
        IFC_RETURN(pCalendarPanel.get_FirstVisibleIndex(&firstVisibleIndex));

        int itemIndex = firstVisibleIndex + row * colCount + column;
        // firstVisibleIndex is on the first row, we need to count how many space before the firstVisibleIndex.
        if (firstVisibleIndex < colCount)
        {
            int startIndex = 0;
            IFC_RETURN(pCalendarPanel.get_StartIndex(&startIndex));
            itemIndex -= startIndex;
        }

        if (itemIndex >= 0)
        {
            IFC_RETURN(pCalendarPanel.ContainerFromIndex(itemIndex, &spItemAsI));
            // This can be a virtualized item or item does not exist, check for null
            if (spItemAsI)
            {
                IFC_RETURN(spItemAsI.As(&spItem));

                IFC_RETURN(spItem.GetOrCreateAutomationPeer(&spItemPeerAsAP));
                IFC_RETURN(ProviderFromPeer(spItemPeerAsAP.Get(), &spProvider));
                *ppReturnValue = spProvider.Detach();
            }
        }
    }
    return S_OK;
}

_Check_return_ HRESULT CalendarViewAutomationPeer.RaiseSelectionEvents( xaml_controls.ICalendarViewSelectedDatesChangedEventArgs* pSelectionChangedEventArgs)
{
    ctl.ComPtr<xaml.IUIElement> spOwner;
    IFC_RETURN(get_Owner(&spOwner));

    xaml_controls.CalendarViewDisplayMode mode = xaml_controls.CalendarViewDisplayMode_Month;
    IFC_RETURN(spOwner.Cast<CalendarView>().get_DisplayMode(&mode));

    // No header for year or decade view
    if (mode == xaml_controls.CalendarViewDisplayMode_Month)
    {
        ctl.ComPtr<CalendarViewGeneratorHost> spHost;
        IFC_RETURN(spOwner.Cast<CalendarView>().GetActiveGeneratorHost(&spHost));

        var pPanel = spHost.GetPanel();
        if (pPanel)
        {
            wf.DateTime date;
            ctl.ComPtr<IDependencyObject> spItemAsI;
            ctl.ComPtr<CalendarViewBaseItem> spItem;
            ctl.ComPtr<xaml_automation_peers.IAutomationPeer> spItemPeerAsAP;
            int itemIndex = 0;
            unsigned selectedCount = 0;

            IFC_RETURN(spOwner.Cast<CalendarView>().m_tpSelectedDates.get_Size(&selectedCount));

            ctl.ComPtr<wfc.IVectorView<wf.DateTime>> spAddedDates;
            ctl.ComPtr<wfc.IVectorView<wf.DateTime>> spRemovedDates;
            unsigned addedDatesSize = 0;
            unsigned removedDatesSize = 0;

            IFC_RETURN(pSelectionChangedEventArgs.get_AddedDates(spAddedDates.ReleaseAndGetAddressOf()));
            IFC_RETURN(spAddedDates.get_Size(&addedDatesSize));

            IFC_RETURN(pSelectionChangedEventArgs.get_RemovedDates(spRemovedDates.ReleaseAndGetAddressOf()));
            IFC_RETURN(spRemovedDates.get_Size(&removedDatesSize));

            // One selection added and that is the only selection
            if (addedDatesSize == 1 && selectedCount == 1)
            {

                IFC_RETURN(spAddedDates.GetAt(0, &date));
				IFC_RETURN(spHost.CalculateOffsetFromMinDate(date, &itemIndex));
                IFC_RETURN(pPanel.ContainerFromIndex(itemIndex, &spItemAsI));
                if (spItemAsI)
                {
                    IFC_RETURN(spItemAsI.As(&spItem));
                    IFC_RETURN(spItem.GetOrCreateAutomationPeer(&spItemPeerAsAP));
                    if (spItemPeerAsAP)
                    {
                        IFC_RETURN(spItemPeerAsAP.RaiseAutomationEvent(xaml_automation_peers.AutomationEvents_SelectionItemPatternOnElementSelected));
                    }
                }

                if (removedDatesSize == 1)
                {
                    IFC_RETURN(spRemovedDates.GetAt(0, &date));
                    IFC_RETURN(spHost.CalculateOffsetFromMinDate(date, &itemIndex));
                    IFC_RETURN(pPanel.ContainerFromIndex(itemIndex, &spItemAsI));
                    if (spItemAsI)
                    {
                        IFC_RETURN(spItemAsI.As(&spItem));
                        IFC_RETURN(spItem.GetOrCreateAutomationPeer(&spItemPeerAsAP));
                        if (spItemPeerAsAP)
                        {
                            IFC_RETURN(spItemPeerAsAP.RaiseAutomationEvent(xaml_automation_peers.AutomationEvents_SelectionItemPatternOnElementRemovedFromSelection));
                        }
                    }
                }
            }
            else
            {
                if (addedDatesSize + removedDatesSize > BulkChildrenLimit)
                {
                    IFC_RETURN(RaiseAutomationEvent(xaml_automation_peers.AutomationEvents_SelectionPatternOnInvalidated));
                }
                else
                {
                    unsigned i = 0;
                    for (i = 0; i < addedDatesSize; i++)
                    {
                        IFC_RETURN(spAddedDates.GetAt(i, &date));
                        IFC_RETURN(spHost.CalculateOffsetFromMinDate(date, &itemIndex));
                        IFC_RETURN(pPanel.ContainerFromIndex(itemIndex, &spItemAsI));
                        if (spItemAsI)
                        {
                            IFC_RETURN(spItemAsI.As(&spItem));
                            IFC_RETURN(spItem.GetOrCreateAutomationPeer(&spItemPeerAsAP));
                            if (spItemPeerAsAP)
                            {
                                IFC_RETURN(spItemPeerAsAP.RaiseAutomationEvent(xaml_automation_peers.AutomationEvents_SelectionItemPatternOnElementAddedToSelection));
                            }
                        }
                    }

                    for (i = 0; i < removedDatesSize; i++)
                    {
                        IFC_RETURN(spRemovedDates.GetAt(i, &date));
                        IFC_RETURN(spHost.CalculateOffsetFromMinDate(date, &itemIndex));
                        IFC_RETURN(pPanel.ContainerFromIndex(itemIndex, &spItemAsI));
                        if (spItemAsI)
                        {
                            IFC_RETURN(spItemAsI.As(&spItem));
                            IFC_RETURN(spItem.GetOrCreateAutomationPeer(&spItemPeerAsAP));
                            if (spItemPeerAsAP)
                            {
                                IFC_RETURN(spItemPeerAsAP.RaiseAutomationEvent(xaml_automation_peers.AutomationEvents_SelectionItemPatternOnElementRemovedFromSelection));
                            }
                        }
                    }
                }
            }
        }
    }
    return S_OK;
}
