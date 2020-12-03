// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#include "precomp.h"
#include "CalendarViewGeneratorMonthViewHost.h"
#include "CalendarView.g.h"
#include "CalendarViewItem.g.h"
#include "CalendarViewDayItem_Partial.h"
#include "CalendarViewDayItemChangingEventArgs.g.h"
#include "CalendarPanel.g.h"
#include "BuildTreeService.g.h"
#include "BudgetManager.g.h"

using namespace DirectUI;
using namespace DirectUISynonyms;

// Work around disruptive max/min macros
#undef max
#undef min

// most codes are copied from ListViewBase_Partial_containerPhase.cpp
// ListViewBase CCC event is restricted with ListViewBase, and it considers about UIPlaceHolder
// the CalendarView version removes UIPlaceHolder and handles blackout state.
// Other logicals are still same as ListViewBase.

_Check_return_ HRESULT CalendarViewGeneratorMonthViewHost.SetupContainerContentChangingAfterPrepare(
     xaml.IDependencyObject* pContainer,
     IInspectable* pItem,
     INT itemIndex,
     wf.Size measureSize)
{
    HRESULT hr = S_OK;

    // this is being called by modern panels after the prepare has occurred.
    // we will setup information that we will need during the lifetime of this container

    ctl.ComPtr<ICalendarViewDayItem> spContainer;
    ctl.ComPtr<CalendarViewDayItemChangingEventArgs> spArgsConcrete;

    // raw pointer, since we're not a refcounted object
    UIElement.VirtualizationInformation* pVirtualizationInformation = null;

    spContainer = ctl.query_interface_cast<ICalendarViewDayItem>(pContainer);

    if (spContainer)
    {
        // is this a new style container? We can know by looking at the virtualizationInformation struct which is
        // a ModernCollectionBase concept
        ctl.ComPtr<xaml_controls.ICalendarViewDayItemChangingEventArgs> spArgs;

        pVirtualizationInformation = spContainer.AsOrNull<IUIElement>().Cast<UIElement>().GetVirtualizationInformation();

        ASSERT(pVirtualizationInformation);

        IFC(spContainer.Cast<CalendarViewDayItem>().GetBuildTreeArgs(&spArgs));
        spArgsConcrete = spArgs.Cast<CalendarViewDayItemChangingEventArgs>();
    }

    // store the size we would measure with
    pVirtualizationInformation.SetMeasureSize(measureSize);

    // initialize values in the args
    IFC(spArgsConcrete.put_WantsCallBack(FALSE));              // let them explicitly call-out if they want it

    IFC(spArgsConcrete.put_Item(spContainer.Get()));           // there is now a hard ref
    IFC(spArgsConcrete.put_InRecycleQueue(FALSE));
    IFC(spArgsConcrete.put_Phase(0));

    // raise the event. This is the synchronous version. we will raise it 'async' as well when we have time
    // but we guarantee calling it after prepare
    if (GetOwner().ShouldRaiseEvent(KnownEventIndex.CalendarView_CalendarViewDayItemChanging))   // app code hooks the event
    {
        CalendarView.CalendarViewDayItemChangingEventSourceType* pEventSource = null;

        IFC(GetOwner().GetCalendarViewDayItemChangingEventSourceNoRef(&pEventSource));

        // force measure. This will be no-op since content has not been set/changed
        // but we need it for the contenttemplateroot
        IFC(spContainer.Cast<CalendarViewDayItem>().Measure(measureSize));

        IFC(pEventSource.Raise(GetOwner(), spArgsConcrete.Get()));
    }

    IFC(RegisterWorkFromCICArgs(spArgsConcrete.Get()));

Cleanup:
    return hr;
}

_Check_return_ HRESULT CalendarViewGeneratorMonthViewHost.RegisterWorkForContainer(
     xaml.IUIElement* pContainer)
{
    HRESULT hr = S_OK;
    ctl.ComPtr<xaml_controls.ICalendarViewDayItemChangingEventArgs> spArgs;
    ctl.ComPtr<ICalendarViewDayItem> spContainer;

    IFC(ctl.do_query_interface<ICalendarViewDayItem>(spContainer, pContainer));
    IFC(spContainer.Cast<CalendarViewDayItem>().GetBuildTreeArgs(&spArgs));

    IFC(RegisterWorkFromCICArgs(spArgs.Get()));

Cleanup:
    return hr;
}

_Check_return_ HRESULT
CalendarViewGeneratorMonthViewHost.RemoveToBeClearedContainer( CalendarViewDayItem* pContainer)
{
    HRESULT hr = S_OK;

    // we might have been inserted into the list for deferred clear container calls.
    // the fact that we are now being prepared, means that we don't have to perform that clear call.
    // yay! that means we are going to not perform work that has quite a bit of perf overhead. 
    // ---
    // we happen to know that the clear will have been pushed to the back of the vector, so optimize
    // the panning scenario by checking in reverse order

    // special case the last element (since we push_back during when we called clear and we expect the next
    // action to be this prepare).
    UINT toBeClearedContainerCount = 0;
    IFC(EnsureToBeClearedContainers());
    IFC(m_toBeClearedContainers.get_Size(&toBeClearedContainerCount));

    for (UINT current = toBeClearedContainerCount - 1; current >= 0 && toBeClearedContainerCount > 0; --current)
    {
        ctl.ComPtr<ICalendarViewDayItem> spCurrentContainer;
        // go from back to front, since we're most likely in the back.
        IFC(m_toBeClearedContainers.GetAt(current, &spCurrentContainer));

        if (spCurrentContainer.Get() == (ICalendarViewDayItem*)(pContainer))
        {
            IFC(m_toBeClearedContainers.RemoveAt(current));
            break;
        }

        if (current == 0)
        {
            // UINT
            break;
        }
    }
Cleanup:
    return hr;
}

IFACEMETHODIMP CalendarViewGeneratorMonthViewHost.get_IsRegisteredForCallbacks(out BOOLEAN* pValue)
{
    *pValue = m_isRegisteredForCallbacks;
    return S_OK;
}

IFACEMETHODIMP CalendarViewGeneratorMonthViewHost.put_IsRegisteredForCallbacks( BOOLEAN value)
{
    m_isRegisteredForCallbacks = value;
    return S_OK;
}

_Check_return_ HRESULT CalendarViewGeneratorMonthViewHost.IsBuildTreeSuspended(out BOOLEAN* pReturnValue)
{
    var pOwner = GetOwner().GetHandle();
    *pReturnValue = pOwner.IsCollapsed() || !pOwner.AreAllAncestorsVisible();
    return S_OK;
}

// the async version of doWork that is being called by NWDrawTree
IFACEMETHODIMP CalendarViewGeneratorMonthViewHost.BuildTree(out BOOLEAN* pWorkLeft)
{
    HRESULT hr = S_OK;
    INT timeElapsedInMS = 0;
    ctl.ComPtr<BudgetManager> spBudget;

    *pWorkLeft = TRUE;

    IFC(DXamlCore.GetCurrent().GetBudgetManager(spBudget));
    IFC(spBudget.GetElapsedMilliSecondsSinceLastUITick(&timeElapsedInMS));

    if ((UINT)(timeElapsedInMS) <= m_budget)
    {
        var pCalendarPanel = GetPanel();

        if (pCalendarPanel)
        {
            // we are going to do different types of work:
            // 1. process incremental visualization
            // 2. process deferred clear container work 
            // 3. process extra cache

            // at this point, cache indices are set correctly
            // We might be going several passes over the containers. Currently we are not keeping those containers 
            // in a particular datastructure, but we are re-using the children collection on our moderncollectionbasepanel
            // We also have a nice hint (m_lowestPhaseInQueue) that tells us what phase to look out for. While we are doing our
            // walk, we're going to build up a structure that allows us to do the second walks much faster.
            // When we are done, we'll throw it away.

            // we do not want to do incremental loading when we are not in the live tree.
            // 1. process incremental visualization
            if (GetOwner().IsInLiveTree() && pCalendarPanel.IsInLiveTree())
            {
                IFC(ProcessIncrementalVisualization(spBudget, pCalendarPanel));
            }

            // 2. Clear containers
            // BUG#1331271 - make sure containers can be cleared even if CalendarView is not in live tree
            IFC(ClearContainers(spBudget));

            UINT containersToClearCount = 0;
            IFC(m_toBeClearedContainers.get_Size(&containersToClearCount));

            // we have work left if we still have containers that need to finish their phases
            // or when we have containers that need to be cleared
            *pWorkLeft = m_lowestPhaseInQueue != -1 || containersToClearCount > 0;
        }
    }

Cleanup:
    return hr;
}

_Check_return_ IFACEMETHODIMP CalendarViewGeneratorMonthViewHost.RaiseContainerContentChangingOnRecycle(
     xaml.IUIElement* pContainer,
     IInspectable* pItem)
{
    HRESULT hr = S_OK;

    ctl.ComPtr<xaml_controls.ICalendarViewDayItemChangingEventArgs> spArgs;
    ctl.ComPtr<CalendarViewDayItemChangingEventArgs> spArgsConcrete;
    ctl.ComPtr<ICalendarViewDayItem> spContainer;

    IFC(ctl.do_query_interface(spContainer, pContainer));
    IFC(spContainer.Cast<CalendarViewDayItem>().GetBuildTreeArgs(&spArgs));
    spArgsConcrete = spArgs.Cast<CalendarViewDayItemChangingEventArgs>();

    if (GetOwner().ShouldRaiseEvent(KnownEventIndex.CalendarView_CalendarViewDayItemChanging))
    {
        BOOLEAN wantCallback = FALSE;
        CalendarView.CalendarViewDayItemChangingEventSourceType* pEventSource = null;

        IFC(GetOwner().GetCalendarViewDayItemChangingEventSourceNoRef(&pEventSource));

        IFC(spArgsConcrete.put_InRecycleQueue(TRUE));
        IFC(spArgsConcrete.put_Phase(0));
        IFC(spArgsConcrete.put_WantsCallBack(FALSE));
        IFC(spArgsConcrete.put_Callback(null));
        IFC(spArgsConcrete.put_Item(spContainer.Get()));

        IFC(pEventSource.Raise(GetOwner(), spArgs.Get()));
        IFC(spArgs.Cast<CalendarViewDayItemChangingEventArgs>().get_WantsCallBack(&wantCallback));

        if (wantCallback)
        {
            if (m_lowestPhaseInQueue == -1)
            {
                UINT phaseArgs = 0;
                IFC(spArgs.Cast<CalendarViewDayItemChangingEventArgs>().get_Phase(&phaseArgs));

                // there was nothing registered
                m_lowestPhaseInQueue = phaseArgs;

                // that means we need to register ourselves with the buildtreeservice so that 
                // we can get called back to do some work
                if (!m_isRegisteredForCallbacks)
                {
                    ctl.ComPtr<BuildTreeService> spBuildTree;
                    IFC(DXamlCore.GetCurrent().GetBuildTreeService(spBuildTree));
                    IFC(spBuildTree.RegisterWork(this));
                }
            }
        }
        else
        {
            IFC(spArgsConcrete.ResetLifetime());
        }
    }
    else
    {
        IFC(spArgsConcrete.ResetLifetime());
    }

Cleanup:
    return hr;
}

_Check_return_ HRESULT
CalendarViewGeneratorMonthViewHost.ProcessIncrementalVisualization(
 const ctl.ComPtr<BudgetManager>& spBudget,
 CalendarPanel* pCalendarPanel)
{
    HRESULT hr = S_OK;

    if (m_lowestPhaseInQueue > -1)
    {
        INT timeElapsedInMS = 0;
        ctl.ComPtr<IItemContainerMapping> spMapping;
        // A block structure has been considered, but we do expect to continuously mutate the phase on containers, which would have 
        // cost us perf while reflecting that in the blocks. Instead, I keep an array of the size of the amount of containers i'm interested in.
        // The idea is that walking through that multiple times is still pretty darn fast.

        xaml_controls.PanelScrollingDirection direction = xaml_controls.PanelScrollingDirection.PanelScrollingDirection_None;

        // the following four indices will be fetched from the panel
        // notice how they are not guaranteed not to be stale: one scenario in which they are 
        // plain and simply wrong is when you collapse/remove a panel from the tree and start 
        // mutating the collection. Arrange will not get a chance to run after the mutation and
        // if we are still registered to do work, we will not be able to fetch the container.
        INT cacheStart = -1;
        INT visibleStart = -1;
        INT visibleEnd = -1;
        INT cacheEnd = -1;

        IFC(pCalendarPanel.GetItemContainerMapping(&spMapping));
        IFC(pCalendarPanel.get_FirstCacheIndexBase(&cacheStart));
        IFC(pCalendarPanel.get_FirstVisibleIndexBase(&visibleStart));
        IFC(pCalendarPanel.get_LastVisibleIndexBase(&visibleEnd));
        IFC(pCalendarPanel.get_LastCacheIndexBase(&cacheEnd));

        // these four match the indices, except they are mapped into a lookup array.
        // notice however, that we understand how visibleindex could have been -1.
        INT cacheStartInVector = -1;
        INT visibleStartInVector = -1;
        INT visibleEndInVector = -1;
        INT cacheEndInVector = -1;

        // translate to array indices
        if (cacheEnd > -1)  // -1 means there is no thing.. no visible or cached containers
        {
            cacheStartInVector = 0;
            visibleStartInVector = visibleStart > -1 ? visibleStart - cacheStart : 0;
            visibleEndInVector = visibleEnd > -1 ? visibleEnd - cacheStart : visibleStartInVector;
            cacheEndInVector = cacheEnd - cacheStart;

        }
        else
        {
            // well, nothing to do, 
            m_lowestPhaseInQueue = -1;
        }

        // start off uninitialized
        INT currentPositionInVector = -1;

        IFC(pCalendarPanel.get_PanningDirectionBase(&direction));

        // trying to find containers in this phase
        INT64 processingPhase = m_lowestPhaseInQueue;
        // when we are done with a full iteration, will be looking for the nextlowest phase
        // note: INT64 here so we can set it to a max that is out of range of the public phase property which is INT
        // max is used to indicate there is nothing left to work through.
        INT64 nextLowest = std.numeric_limits<INT64>.max();

        // policy is to go through visible children first, then buffer in panning direction, then buffer in non-panning direction
        // this method will help us do that
        var ProcessCurrentPosition = [&]()
        {
            BOOLEAN increasePhase = FALSE;
            BOOLEAN forward = direction != xaml_controls.PanelScrollingDirection.PanelScrollingDirection_Backward;

            // initialize
            if (currentPositionInVector == -1)
            {
                currentPositionInVector = forward ? visibleStartInVector : visibleEndInVector;
            }
            else
            {
                if (forward)
                {
                    // go forward until you reach the end of the forward buffer, and start in the opposite buffer
                    // going in the opposite direction
                    if (currentPositionInVector >= visibleStartInVector)
                    {
                        ++currentPositionInVector;
                        if (currentPositionInVector > cacheEndInVector)
                        {
                            currentPositionInVector = visibleStartInVector - 1; // set to the start of the left section
                        }
                    }
                    else
                    {
                        // processing to the left
                        --currentPositionInVector;
                    }

                    // run off or no cache on left side
                    if (currentPositionInVector < 0)
                    {
                        currentPositionInVector = visibleStartInVector;
                        increasePhase = TRUE;
                    }
                }
                else
                {
                    if (currentPositionInVector <= visibleEndInVector)
                    {
                        --currentPositionInVector;
                        if (currentPositionInVector < 0)
                        {
                            currentPositionInVector = visibleEndInVector + 1; // set to the start of the right section
                        }
                    }
                    else
                    {
                        // processing to the right
                        ++currentPositionInVector;
                    }

                    // run off or no cache on right side
                    if (currentPositionInVector > cacheEndInVector)
                    {
                        currentPositionInVector = visibleEndInVector;
                        increasePhase = TRUE;
                    }
                }

                if (increasePhase)
                {
                    processingPhase = nextLowest;
                    // when increasing, signal using max() that we can stop after this iteration. 
                    // this will be undone by the loop, resetting it to the actual value
                    nextLowest = std.numeric_limits<INT64>.max();
                }
            }
        };

        // the array that we keep iterating until we're done
        // -1 means, not fetched yet
        std.vector<INT64> lookup;
        lookup.assign(cacheEndInVector + 1, -1);
        // initialize before going into the loop
        ProcessCurrentPosition();

        while (processingPhase != std.numeric_limits<INT64>.max()
            && (UINT)(timeElapsedInMS) < m_budget
            && !lookup.empty())
        {
            INT64 phase = 0;
            ctl.ComPtr<xaml.IDependencyObject> spContainer;
            ctl.ComPtr<xaml_controls.ICalendarViewDayItem> spContainerAsCalendarViewDayItem;
            UIElement.VirtualizationInformation* pVirtualizationInformation = null;
            ctl.ComPtr<xaml_controls.ICalendarViewDayItemChangingEventArgs> spArgs;
            ctl.ComPtr<CalendarViewDayItemChangingEventArgs> spArgsConcrete;
            // always update the current position when done, except when the phase requested is lower than current phase
            bool shouldUpdateCurrentPosition = true;

            // what is the phase?
            phase = lookup[currentPositionInVector];
            if (phase == -1)
            {
                // not initialized yet
                UINT argsPhase = 0;

                IFC(spMapping.ContainerFromIndex(cacheStart + currentPositionInVector, &spContainer));
                if (!spContainer)
                {
                    // this is possible when mutations have occurred to the collection but we 
                    // cannot be reached through layout. This is very hard to figure out, so we just harden
                    // the code here to deal with nulls.
                    ProcessCurrentPosition();
                    continue;
                }

                IFC(spContainer.As(&spContainerAsCalendarViewDayItem));

                pVirtualizationInformation = spContainer.AsOrNull<IUIElement>().Cast<UIElement>().GetVirtualizationInformation();
                IFC(spContainerAsCalendarViewDayItem.Cast<CalendarViewDayItem>().GetBuildTreeArgs(&spArgs));

                IFC(spArgs.get_Phase(&argsPhase));
                phase = argsPhase;  // fits easily

                lookup[currentPositionInVector] = phase;
            }

            if (!spArgs)
            {
                // we might have skipped getting the args, let's do that now.
                IFC(spMapping.ContainerFromIndex(cacheStart + currentPositionInVector, &spContainer));

                IFC(spContainer.As(&spContainerAsCalendarViewDayItem));
                pVirtualizationInformation = spContainer.AsOrNull<IUIElement>().Cast<UIElement>().GetVirtualizationInformation();

                IFC(spContainerAsCalendarViewDayItem.Cast<CalendarViewDayItem>().GetBuildTreeArgs(&spArgs));
            }

            // guaranteed to have spArgs now
            spArgsConcrete = spArgs.Cast<CalendarViewDayItemChangingEventArgs>();

            if (phase == processingPhase)
            {
                // processing this guy
                BOOLEAN wantsCallBack = FALSE;
                wf.Size measureSize = {};

                ctl.ComPtr<wf.ITypedEventHandler<xaml_controls.CalendarView*, xaml_controls.CalendarViewDayItemChangingEventArgs*>> spCallback;

                // guaranteed to have pVirtualizationInformation by now

                ASSERT(pVirtualizationInformation);

                measureSize = pVirtualizationInformation.GetMeasureSize();

                // did we store a callback
                IFC(spArgsConcrete.get_Callback(&spCallback));

                // raise event
                if (spCallback.Get())
                {
                    IFC(spArgsConcrete.put_WantsCallBack(FALSE));
                    // clear out the delegate
                    IFC(spArgsConcrete.put_Callback(null));

                    IFC(spCallback.Invoke(GetOwner(), spArgs.Get()));

                    // the invoke will cause them to call RegisterCallback which will overwrite the delegate (fine)
                    // and set the boolean below to true
                    IFC(spArgsConcrete.get_WantsCallBack(&wantsCallBack));
                }

                // the user might have changed elements. In order to keep the budget fair, we need to try and incur
                // most of the cost right now.
                IFC(spContainerAsCalendarViewDayItem.Cast<CalendarViewDayItem>().Measure(measureSize));

                // register callback
                if (wantsCallBack)
                {
                    UINT phaseFromArgs = 0;
                    IFC(spArgsConcrete.get_Phase(&phaseFromArgs));
                    phase = phaseFromArgs;
                    lookup[currentPositionInVector] = phase;

                    // if the appcode requested a phase that is lower than the current processing phase, it is kind of weird
                    if (phase < processingPhase)
                    {
                        // after we change the processingphase, our next lowest is going to be the current phase (we didn't finish it yet)
                        nextLowest = processingPhase;

                        // change our processing phase to the requested phase. It is going to be the one we work on next
                        processingPhase = phase;
                        m_lowestPhaseInQueue = processingPhase;

                        // the pointer is pointing to the current container which is great
                        shouldUpdateCurrentPosition = false;
                    }
                    else
                    {
                        // update the next lowest to the best of our current understanding
                        nextLowest = Math.Min(nextLowest, (INT64)(phase));
                    }
                }
                else
                {
                    // won't be called again for the lifetime of this container
                    IFC(spArgsConcrete.ResetLifetime());

                    // we do not have to update the next lowest. We are still processing this phase and will
                    // continue to do so (procesingPhase is still valid).
                }
            }
            else   //if (phase == processingPhase)
            {
                // if we hit a container that is registered for a callback (so he wants to iterate over phases)
                // but is currently at a different phase, we need to make sure that the next lowest is set.
                BOOLEAN wantsCallBack = FALSE;
                IFC(spArgsConcrete.get_WantsCallBack(&wantsCallBack));

                if (wantsCallBack)
                {
                    ASSERT(phase > processingPhase);
                    // update the next lowest, now that we have seen a phase that is higher than our current processing phase
                    nextLowest = Math.Min(nextLowest, (INT64)(phase));
                }
            }

            // updates the current position in the correct direction
            if (shouldUpdateCurrentPosition)
            {
                ProcessCurrentPosition();
            }
            // updates the time
            IFC(spBudget.GetElapsedMilliSecondsSinceLastUITick(&timeElapsedInMS));
        }

        if (processingPhase == std.numeric_limits<INT64>.max())
        {
            // nothing left to process
            m_lowestPhaseInQueue = -1;
        }
        else
        {
            // we broke out of the loop for some other reason (policy)
            // should be safe at this point
            ASSERT(processingPhase < std.numeric_limits<INT>.max());
            m_lowestPhaseInQueue = (INT)(processingPhase);
        }
    }

Cleanup:
    return hr;
}

_Check_return_ HRESULT CalendarViewGeneratorMonthViewHost.EnsureToBeClearedContainers()
{
    HRESULT hr = S_OK;

    if (!m_toBeClearedContainers)
    {
        ctl.ComPtr<TrackerCollection<xaml_controls.CalendarViewDayItem*>> spContainersForClear;

        IFC(ctl.make(&spContainersForClear));
        SetPtrValue(m_toBeClearedContainers, std.move(spContainersForClear));
    }

Cleanup:
    return hr;
}

_Check_return_ HRESULT CalendarViewGeneratorMonthViewHost.ClearContainers(
     const ctl.ComPtr<BudgetManager>& spBudget)
{
    HRESULT hr = S_OK;
    UINT containersToClearCount = 0;
    INT timeElapsedInMS = 0;

    IFC(EnsureToBeClearedContainers());

    IFC(m_toBeClearedContainers.get_Size(&containersToClearCount));
    for (UINT toClearIndex = containersToClearCount - 1; toClearIndex >= 0 && containersToClearCount > 0; --toClearIndex)
    {
        ctl.ComPtr<ICalendarViewDayItem> spContainer;

        IFC(spBudget.GetElapsedMilliSecondsSinceLastUITick(&timeElapsedInMS));

        if ((UINT)(timeElapsedInMS) > m_budget)
        {
            break;
        }

        IFC(m_toBeClearedContainers.GetAt(toClearIndex, &spContainer));
        IFC(m_toBeClearedContainers.RemoveAtEnd());

        // execute the deferred work
        // apparently we were not going to reuse this container immediately again, so 
        // let's do the work now

        // we don't need the spItem because 1. we didn't save this information, 2. CalendarViewGeneratorHost.ClearContainerForItem is no-op for now
        // if we need this we could simple restore the spItem from the container.
        IFC(CalendarViewGeneratorHost.ClearContainerForItem(spContainer.Cast<CalendarViewDayItem>(), null /*spItem*/));

        // potentially raise the event
        if (spContainer)
        {
            IFC(RaiseContainerContentChangingOnRecycle(spContainer.AsOrNull<IUIElement>().Get(), null));
        }

        if (toClearIndex == 0)
        {
            // UINT
            break;
        }
    }

Cleanup:
    return hr;
}

IFACEMETHODIMP CalendarViewGeneratorMonthViewHost.ShutDownDeferredWork()
{
    HRESULT hr = S_OK;

    ctl.ComPtr<IItemContainerMapping> spMapping;

    var pCalendarPanel = GetPanel();

    if (pCalendarPanel)
    {
        // go through everyone that might have work registered for a prepare
        INT cacheStart, cacheEnd = 0;
        IFC(pCalendarPanel.get_FirstCacheIndexBase(&cacheStart));
        IFC(pCalendarPanel.get_LastCacheIndexBase(&cacheEnd));
        IFC(pCalendarPanel.GetItemContainerMapping(&spMapping));

        for (INT i = cacheStart; i < cacheEnd; ++i)
        {
            ctl.ComPtr<xaml.IDependencyObject> spContainer;
            ctl.ComPtr<xaml_controls.ICalendarViewDayItemChangingEventArgs> spArgs;
            IFC(spMapping.ContainerFromIndex(i, &spContainer));

            if (!spContainer)
            {
                // apparently a sentinel. This should not occur, however, during shutdown we could
                // run into this since measure might not have been processed yet
                continue;
            }

            IFC(spContainer.Cast<CalendarViewDayItem>().GetBuildTreeArgs(&spArgs));

            IFC(spArgs.Cast<CalendarViewDayItemChangingEventArgs>().ResetLifetime());
        }
    }

Cleanup:
    return hr;
}

_Check_return_ HRESULT CalendarViewGeneratorMonthViewHost.RegisterWorkFromCICArgs(
     xaml_controls.ICalendarViewDayItemChangingEventArgs* pArgs)
{
    HRESULT hr = S_OK;

    BOOLEAN wantsCallback = FALSE;
    ctl.ComPtr<ICalendarViewDayItem> spCalendarViewDayItem;
    CalendarViewDayItemChangingEventArgs* pConcreteArgsNoRef = (CalendarViewDayItemChangingEventArgs*)(pArgs);

    IFC(pConcreteArgsNoRef.get_WantsCallBack(&wantsCallback));
    IFC(pConcreteArgsNoRef.get_Item(&spCalendarViewDayItem));

    // we are going to want to be called back if:
    // 1. we are still showing the placeholder
    // 2. app code registered to be called back
    if (wantsCallback)
    {
        UINT phase = 0;

        IFC(pConcreteArgsNoRef.get_Phase(&phase));

        // keep this state on the listviewbase
        if (m_lowestPhaseInQueue == -1)
        {
            // there was nothing registered
            m_lowestPhaseInQueue = phase;

            // that means we need to register ourselves with the buildtreeservice so that 
            // we can get called back to do some work
            if (!m_isRegisteredForCallbacks)
            {
                ctl.ComPtr<BuildTreeService> spBuildTree;
                IFC(DXamlCore.GetCurrent().GetBuildTreeService(spBuildTree));
                IFC(spBuildTree.RegisterWork(this));
            }

            ASSERT(m_isRegisteredForCallbacks);
        }
        else if (m_lowestPhaseInQueue > phase)
        {
            m_lowestPhaseInQueue = phase;
        }
    }
    else
    {
        // well, app code doesn't want a callback so cleanup the args
        IFC(pConcreteArgsNoRef.ResetLifetime());
    }

Cleanup:
    return hr;
}
