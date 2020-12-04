// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.


using namespace DirectUI;
using namespace DirectUISynonyms;


private void GetPatternCore( xaml_automation_peers.PatternInterface patternInterface, out  DependencyObject ppReturnValue)
{
    IFCPTR_RETURN(ppReturnValue);
    ppReturnValue = null;

    bool isItemVisible = false;

    // For the GridItem pattern, make sure the item is visible otherwise we might end up returning a negative row value
    // for it.  An item may not be visible if it has been scrolled out of view..
    if (patternInterface == xaml_automation_peers.PatternInterface_GridItem && SUCCEEDED(IsItemVisible(isItemVisible)) && isItemVisible ||
        patternInterface == xaml_automation_peers.PatternInterface_ScrollItem)
    {
        ppReturnValue = ctl.as_iinspectable(this);
        ctl.addref_interface(this);
    }
    else
    {
        CalendarViewBaseItemAutomationPeerGenerated.GetPatternCore(patternInterface, ppReturnValue);
    }
    return;
}

private void GetNameCore(out HSTRING returnValue)
{
    IFCPTR_RETURN(returnValue);
    CalendarViewBaseItemAutomationPeerGenerated.GetNameCore(returnValue);
    if (returnValue == null)
    {
        UIElement spOwner;
        spOwner = Owner;
        spOwner as CalendarViewItem.GetMainText(returnValue);
    }
    return;
}

private void get_ColumnSpanImpl(out INT pValue)
{
    IFCPTR_RETURN(pValue);
    pValue = 1;
    return;
}

private void get_ContainingGridImpl(out result_maybenull_ xaml_automation.Provider.IIRawElementProviderSimple* ppValue)
{
    IFCPTR_RETURN(ppValue);
    ppValue = false;

    UIElement spOwner;
    spOwner = Owner;
    
    IAutomationPeer spAutomationPeer;
    CalendarView pParent = spOwner as CalendarViewBaseItem.GetParentCalendarView();
    IFCPTR_RETURN(pParent);

    spAutomationPeer = pParent.GetOrCreateAutomationPeer);
    ProviderFromPeer(spAutomationPeer, ppValue);
    return;
}

private void get_RowSpanImpl(out INT pValue)
{
    IFCPTR_RETURN(pValue);
    pValue = 1;
    return;
}

// Methods.

private void ScrollIntoViewImpl()
{
    UIElement spOwner;
    spOwner = Owner;

    DateTime date;
    date = spOwner as CalendarViewItem.GetDate);

    CalendarView pParent = spOwner as CalendarViewBaseItem.GetParentCalendarView();
    IFCPTR_RETURN(pParent);

    pParent.SetDisplayDate(date);
    
    return;
}

private void IsItemVisible(bool& isVisible)
{
    isVisible = false;

    UIElement owner;
    owner = Owner;

    var parent = owner as CalendarViewBaseItem.GetParentCalendarView();

    CalendarViewGeneratorHost host;
    host = parent.GetActiveGeneratorHost);

    var calendarPanel = host.GetPanel();
    if (calendarPanel)
    {
        DateTime date  = default;
        date = owner as CalendarViewBaseItem.GetDate);

        int itemIndex = 0;
        itemIndex = host.CalculateOffsetFromMinDate(date);

        int firstVisibleIndex = 0;
        firstVisibleIndex = calendarPanel.FirstVisibleIndex;

        int lastVisibleIndex = 0;
        lastVisibleIndex = calendarPanel.LastVisibleIndex;

        isVisible = (itemIndex >= firstVisibleIndex && itemIndex <= lastVisibleIndex);
    }

    return;
}
