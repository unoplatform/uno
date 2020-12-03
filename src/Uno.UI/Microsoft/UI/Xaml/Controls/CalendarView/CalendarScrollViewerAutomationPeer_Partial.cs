// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#include "precomp.h"
#include "CalendarScrollViewerAutomationPeer.g.h"
#include "CalendarView.g.h"
#include "CalendarViewGeneratorHost.h"
#include "CalendarPanel.g.h"
#include "CalendarViewBaseItem.g.h"
#include "CalendarViewBaseItemAutomationPeer.g.h"

using namespace DirectUI;
using namespace DirectUISynonyms;

IFACEMETHODIMP CalendarScrollViewerAutomationPeer.GetClassNameCore(out HSTRING* pReturnValue)
{
    IFCPTR_RETURN(pReturnValue);
    IFC_RETURN(wrl_wrappers.Hstring(STR_LEN_PAIR("CalendarScrollViewer")).CopyTo(pReturnValue));

    return S_OK;
}

IFACEMETHODIMP CalendarScrollViewerAutomationPeer.GetChildrenCore(_Outptr_ wfc.IList<xaml_automation_peers.AutomationPeer*>** ppReturnValue)
{
    IFCPTR_RETURN(ppReturnValue);

    ctl.ComPtr<xaml.IUIElement> spOwner;
    ctl.ComPtr<xaml.IFrameworkElement> spOwnerAsFrameworkElement;

    IFC_RETURN(get_Owner(&spOwner));
    IFC_RETURN(spOwner.As(&spOwnerAsFrameworkElement));

    ctl.ComPtr<DependencyObject> spTemplatedParent;
    IFC_RETURN((spOwnerAsFrameworkElement.Cast<FrameworkElement>()).get_TemplatedParent(&spTemplatedParent));

    if (spTemplatedParent)
    {
        ctl.ComPtr<xaml_controls.ICalendarView>
            spCalendarView = spTemplatedParent.AsOrNull<xaml_controls.ICalendarView>();

        if (spCalendarView)
        {
            ctl.ComPtr<CalendarViewGeneratorHost> spGeneratorHost;
            IFC_RETURN((spCalendarView.Cast<CalendarView>()).GetActiveGeneratorHost(&spGeneratorHost));
            var pCalendarPanel = spGeneratorHost.GetPanel();

            if (pCalendarPanel)
            {
                int firstIndex = -1;
                int lastIndex = -1;

                IFC_RETURN(pCalendarPanel.get_FirstVisibleIndex(&firstIndex));
                IFC_RETURN(pCalendarPanel.get_LastVisibleIndex(&lastIndex));

                // This ScrollViewer automation peer ensures that for CalendarViews, accessible Items are restricted
                // to visible Items. To go to next unit view, user scenario is to utilize next and previous button.
                // Utilizing realized Items has a side effect due to bufferring, the first Item is a few months
                // back then current Item leading to an awkward state.
                if (firstIndex != -1 && lastIndex != -1)
                {
                    ctl.ComPtr<TrackerCollection<xaml_automation_peers.AutomationPeer*>> spAPChildren;
                    IFC_RETURN(ctl.new TrackerCollection<xaml_automation_peers.AutomationPeer*>(&spAPChildren));

                    for (int index = firstIndex; index <= lastIndex; ++index)
                    {
                        ctl.ComPtr<IDependencyObject> spChildAsIDO;
                        ctl.ComPtr<ICalendarViewBaseItem> spChildAsItem;
                        IFC_RETURN(pCalendarPanel.ContainerFromIndex(index, &spChildAsIDO));
                        IFC_RETURN(spChildAsIDO.As(&spChildAsItem));

                        ctl.ComPtr<IAutomationPeer> spAutomationPeer;
                        IFC_RETURN((spChildAsItem.Cast<CalendarViewBaseItem>()).GetOrCreateAutomationPeer(&spAutomationPeer));

                        if (spAutomationPeer)
                        {
                            IFC_RETURN(spAPChildren.Append(spAutomationPeer.Get()));
                        }
                    }

                    IFC_RETURN(spAPChildren.CopyTo(ppReturnValue));
                }
            }
        }
    }

    return S_OK;
}