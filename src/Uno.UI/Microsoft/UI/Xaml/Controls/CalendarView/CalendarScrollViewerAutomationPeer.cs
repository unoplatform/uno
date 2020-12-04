// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.


using namespace DirectUI;
using namespace DirectUISynonyms;

private void GetClassNameCore(out HSTRING pReturnValue)
{
    IFCPTR_RETURN(pReturnValue);
    stringReference("CalendarScrollViewer").CopyTo(pReturnValue);

    return;
}

private void GetChildrenCore(out  wfc.IVector<xaml_automation_peers.AutomationPeer>** ppReturnValue)
{
    IFCPTR_RETURN(ppReturnValue);

    UIElement spOwner;
    xaml.FrameworkElement spOwnerAsFrameworkElement;

    spOwner = Owner;
    spOwnerAsFrameworkElement = spOwner.As);

    DependencyObject spTemplatedParent;
    spTemplatedParent = (spOwnerAsFrameworkElement as FrameworkElement).TemplatedParent;

    if (spTemplatedParent)
    {
        xaml_controls.ICalendarView
            spCalendarView = spTemplatedParent as xaml_controls.ICalendarView;

        if (spCalendarView)
        {
            CalendarViewGeneratorHost spGeneratorHost;
            spGeneratorHost = (spCalendarView as CalendarView).GetActiveGeneratorHost);
            var pCalendarPanel = spGeneratorHost.GetPanel();

            if (pCalendarPanel)
            {
                int firstIndex = -1;
                int lastIndex = -1;

                firstIndex = pCalendarPanel.FirstVisibleIndex;
                lastIndex = pCalendarPanel.LastVisibleIndex;

                // This ScrollViewer automation peer ensures that for CalendarViews, accessible Items are restricted
                // to visible Items. To go to next unit view, user scenario is to utilize next and previous button.
                // Utilizing realized Items has a side effect due to bufferring, the first Item is a few months
                // back then current Item leading to an awkward state.
                if (firstIndex != -1 && lastIndex != -1)
                {
                    TrackerCollection<xaml_automation_peers.AutomationPeer> spAPChildren;
                    spAPChildren = ctl.new TrackerCollection<xaml_automation_peers.AutomationPeer>);

                    for (int index = firstIndex; index <= lastIndex; ++index)
                    {
                        IDependencyObject spChildAsIDO;
                        ICalendarViewBaseItem spChildAsItem;
                        spChildAsIDO = pCalendarPanel.ContainerFromIndex(index);
                        spChildAsItem = spChildAsIDO.As);

                        IAutomationPeer spAutomationPeer;
                        spAutomationPeer = (spChildAsItem as CalendarViewBaseItem).GetOrCreateAutomationPeer);

                        if (spAutomationPeer)
                        {
                            spAPChildren.Append(spAutomationPeer);
                        }
                    }

                    spAPChildren.CopyTo(ppReturnValue);
                }
            }
        }
    }

    return;
}