// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.


using namespace DirectUI;
using namespace DirectUISynonyms;

// Work around disruptive max/min macros
#undef max
#undef min

// most codes are copied from ListViewBase_Partial_containerPhase.cpp
// ListViewBase CCC event is restricted with ListViewBase, and it considers about UIPlaceHolder
// the CalendarView version removes UIPlaceHolder and handles blackout state.
// Other logicals are still same as ListViewBase.

private void CalendarViewGeneratorMonthViewHost.SetupContainerContentChangingAfterPrepare(
     xaml.IDependencyObject pContainer,
     DependencyObject pItem,
     int itemIndex,
     wf.Size measureSize)
{
    // this is being called by modern panels after the prepare has occurred.
    // we will setup information that we will need during the lifetime of this container

    ICalendarViewDayItem spContainer;
    CalendarViewDayItemChangingEventArgs spArgsConcrete;

    // raw pointer, since we're not a refcounted object
    UIElement.VirtualizationInformation pVirtualizationInformation = null;

    spContainer = ctl.query_interface_cast<ICalendarViewDayItem>(pContainer);

    if (spContainer)
    {
        // is this a new style container? We can know by looking at the virtualizationInformation struct which is
        // a ModernCollectionBase concept
        xaml_controls.ICalendarViewDayItemChangingEventArgs spArgs;

        pVirtualizationInformation = spContainer as UIElement as UIElement.GetVirtualizationInformation();

        global::System.Diagnostics.Debug.Assert(pVirtualizationInformation);

        spArgs = spContainer as CalendarViewDayItem.GetBuildTreeArgs);
        spArgsConcrete = spArgs as CalendarViewDayItemChangingEventArgs;
    }

    // store the size we would measure with
    pVirtualizationInformation.SetMeasureSize(measureSize);

    // initialize values in the args
    spArgsConcrete.WantsCallBack = false;              // let them explicitly call-out if they want it

    spArgsConcrete.Item = spContainer;           // there is now a hard ref
    spArgsConcrete.InRecycleQueue = false;
    spArgsConcrete.Phase = 0;

    // raise the event. This is the synchronous version. we will raise it 'async' as well when we have time
    // but we guarantee calling it after prepare
    if (GetOwner().ShouldRaiseEvent(KnownEventIndex.CalendarView_CalendarViewDayItemChanging))   // app code hooks the event
    {
        CalendarView.CalendarViewDayItemChangingEventSourceType pEventSource = null;

        pEventSource = GetOwner().GetCalendarViewDayItemChangingEventSourceNoRef);

        // force measure. This will be no-op since content has not been set/changed
        // but we need it for the contenttemplateroot
        spContainer as CalendarViewDayItem.Measure(measureSize);

        pEventSource.Raise(GetOwner(), spArgsConcrete);
    }

    RegisterWorkFromCICArgs(spArgsConcrete);

}

private void CalendarViewGeneratorMonthViewHost.RegisterWorkForContainer(
     UIElement pContainer)
{
    xaml_controls.ICalendarViewDayItemChangingEventArgs spArgs;
    ICalendarViewDayItem spContainer;

    ctl.do_query_interface<ICalendarViewDayItem>(spContainer, pContainer);
    spArgs = spContainer as CalendarViewDayItem.GetBuildTreeArgs);

    RegisterWorkFromCICArgs(spArgs);

}

 HRESULT
CalendarViewGeneratorMonthViewHost.RemoveToBeClearedContainer( CalendarViewDayItem pContainer)
{
    // we might have been inserted into the list for deferred clear container calls.
    // the fact that we are now being prepared, means that we don't have to perform that clear call.
    // yay! that means we are going to not perform work that has quite a bit of perf overhead. 
    // ---
    // we happen to know that the clear will have been pushed to the back of the vector, so optimize
    // the panning scenario by checking in reverse order

    // special case the last element (since we push_back during when we called clear and we expect the next
    // action to be this prepare).
    Uint toBeClearedContainerCount = 0;
    EnsureToBeClearedContainers();
    toBeClearedContainerCount = m_toBeClearedContainers.Size;

    for (Uint current = toBeClearedContainerCount - 1; current >= 0 && toBeClearedContainerCount > 0; --current)
    {
        ICalendarViewDayItem spCurrentContainer;
        // go from back to front, since we're most likely in the back.
        spCurrentContainer = m_toBeClearedContainers.GetAt(current);

        if (spCurrentContainer == (ICalendarViewDayItem)(pContainer))
        {
            m_toBeClearedContainers.RemoveAt(current);
            break;
        }

        if (current == 0)
        {
            // UINT
            break;
        }
    }
}

CalendarViewGeneratorMonthViewHost.get_IsRegisteredForCallbacks(out BOOLEAN pValue)
{
    pValue = m_isRegisteredForCallbacks;
    return;
}

CalendarViewGeneratorMonthViewHost.IsRegisteredForCallbacks =  bool value
{
    m_isRegisteredForCallbacks = value;
    return;
}

private void CalendarViewGeneratorMonthViewHost.IsBuildTreeSuspended(out BOOLEAN pReturnValue)
{
    var pOwner = GetOwner().GetHandle();
    pReturnValue = pOwner.IsCollapsed() || !pOwner.AreAllAncestorsVisible();
    return;
}

// the async version of doWork that is being called by NWDrawTree
CalendarViewGeneratorMonthViewHost.BuildTree(out BOOLEAN pWorkLeft)
{
    int timeElapsedInMS = 0;
    BudgetManager spBudget;

    pWorkLeft = true;

    DXamlCore.GetCurrent().GetBudgetManager(spBudget);
    timeElapsedInMS = spBudget.GetElapsedMilliSecondsSinceLastUITick);

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
                ProcessIncrementalVisualization(spBudget, pCalendarPanel);
            }

            // 2. Clear containers
            // BUG#1331271 - make sure containers can be cleared even if CalendarView is not in live tree
            ClearContainers(spBudget);

            Uint containersToClearCount = 0;
            containersToClearCount = m_toBeClearedContainers.Size;

            // we have work left if we still have containers that need to finish their phases
            // or when we have containers that need to be cleared
            pWorkLeft = m_lowestPhaseInQueue != -1 || containersToClearCount > 0;
        }
    }

}

 CalendarViewGeneratorMonthViewHost.RaiseContainerContentChangingOnRecycle(
     UIElement pContainer,
     DependencyObject pItem)
{
    xaml_controls.ICalendarViewDayItemChangingEventArgs spArgs;
    CalendarViewDayItemChangingEventArgs spArgsConcrete;
    ICalendarViewDayItem spContainer;

    ctl.do_query_interface(spContainer, pContainer);
    spArgs = spContainer as CalendarViewDayItem.GetBuildTreeArgs);
    spArgsConcrete = spArgs as CalendarViewDayItemChangingEventArgs;

    if (GetOwner().ShouldRaiseEvent(KnownEventIndex.CalendarView_CalendarViewDayItemChanging))
    {
        bool wantCallback = false;
        CalendarView.CalendarViewDayItemChangingEventSourceType pEventSource = null;

        pEventSource = GetOwner().GetCalendarViewDayItemChangingEventSourceNoRef);

        spArgsConcrete.InRecycleQueue = true;
        spArgsConcrete.Phase = 0;
        spArgsConcrete.WantsCallBack = false;
        spArgsConcrete.Callback = null;
        spArgsConcrete.Item = spContainer;

        pEventSource.Raise(GetOwner(), spArgs);
        wantCallback = spArgs as CalendarViewDayItemChangingEventArgs.WantsCallBack;

        if (wantCallback)
        {
            if (m_lowestPhaseInQueue == -1)
            {
                Uint phaseArgs = 0;
                phaseArgs = spArgs as CalendarViewDayItemChangingEventArgs.Phase;

                // there was nothing registered
                m_lowestPhaseInQueue = phaseArgs;

                // that means we need to register ourselves with the buildtreeservice so that 
                // we can get called back to do some work
                if (!m_isRegisteredForCallbacks)
                {
                    BuildTreeService spBuildTree;
                    DXamlCore.GetCurrent().GetBuildTreeService(spBuildTree);
                    spBuildTree.RegisterWork(this);
                }
            }
        }
        else
        {
            spArgsConcrete.ResetLifetime();
        }
    }
    else
    {
        spArgsConcrete.ResetLifetime();
    }

}

 HRESULT
CalendarViewGeneratorMonthViewHost.ProcessIncrementalVisualization(
  BudgetManager& spBudget,
 CalendarPanel pCalendarPanel)
{
    if (m_lowestPhaseInQueue > -1)
    {
        int timeElapsedInMS = 0;
        IItemContainerMapping spMapping;
        // A block structure has been considered, but we do expect to continuously mutate the phase on containers, which would have 
        // cost us perf while reflecting that in the blocks. Instead, I keep an array of the size of the amount of containers i'm interested in.
        // The idea is that walking through that multiple times is still pretty darn fast.

        xaml_controls.PanelScrollingDirection direction = xaml_controls.PanelScrollingDirection.PanelScrollingDirection_None;

        // the following four indices will be fetched from the panel
        // notice how they are not guaranteed not to be stale: one scenario in which they are 
        // plain and simply wrong is when you collapse/remove a panel from the tree and start 
        // mutating the collection. Arrange will not get a chance to run after the mutation and
        // if we are still registered to do work, we will not be able to fetch the container.
        int cacheStart = -1;
        int visibleStart = -1;
        int visibleEnd = -1;
        int cacheEnd = -1;

        spMapping = pCalendarPanel.GetItemContainerMapping);
        cacheStart = pCalendarPanel.FirstCacheIndexBase;
        visibleStart = pCalendarPanel.FirstVisibleIndexBase;
        visibleEnd = pCalendarPanel.LastVisibleIndexBase;
        cacheEnd = pCalendarPanel.LastCacheIndexBase;

        // these four match the indices, except they are mapped into a lookup array.
        // notice however, that we understand how visibleindex could have been -1.
        int cacheStartInVector = -1;
        int visibleStartInVector = -1;
        int visibleEndInVector = -1;
        int cacheEndInVector = -1;

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
        int currentPositionInVector = -1;

        direction = pCalendarPanel.PanningDirectionBase;

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
            bool increasePhase = false;
            bool forward = direction != xaml_controls.PanelScrollingDirection.PanelScrollingDirection_Backward;

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
                        increasePhase = true;
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
                        increasePhase = true;
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
            xaml.IDependencyObject spContainer;
            xaml_controls.ICalendarViewDayItem spContainerAsCalendarViewDayItem;
            UIElement.VirtualizationInformation pVirtualizationInformation = null;
            xaml_controls.ICalendarViewDayItemChangingEventArgs spArgs;
            CalendarViewDayItemChangingEventArgs spArgsConcrete;
            // always update the current position when done, except when the phase requested is lower than current phase
            bool shouldUpdateCurrentPosition = true;

            // what is the phase?
            phase = lookup[currentPositionInVector];
            if (phase == -1)
            {
                // not initialized yet
                Uint argsPhase = 0;

                spContainer = spMapping.ContainerFromIndex(cacheStart + currentPositionInVector);
                if (!spContainer)
                {
                    // this is possible when mutations have occurred to the collection but we 
                    // cannot be reached through layout. This is very hard to figure out, so we just harden
                    // the code here to deal with nulls.
                    ProcessCurrentPosition();
                    continue;
                }

                spContainerAsCalendarViewDayItem = spContainer.As);

                pVirtualizationInformation = spContainer as UIElement as UIElement.GetVirtualizationInformation();
                spArgs = spContainerAsCalendarViewDayItem as CalendarViewDayItem.GetBuildTreeArgs);

                argsPhase = spArgs.Phase;
                phase = argsPhase;  // fits easily

                lookup[currentPositionInVector] = phase;
            }

            if (!spArgs)
            {
                // we might have skipped getting the args, let's do that now.
                spContainer = spMapping.ContainerFromIndex(cacheStart + currentPositionInVector);

                spContainerAsCalendarViewDayItem = spContainer.As);
                pVirtualizationInformation = spContainer as UIElement as UIElement.GetVirtualizationInformation();

                spArgs = spContainerAsCalendarViewDayItem as CalendarViewDayItem.GetBuildTreeArgs);
            }

            // guaranteed to have spArgs now
            spArgsConcrete = spArgs as CalendarViewDayItemChangingEventArgs;

            if (phase == processingPhase)
            {
                // processing this guy
                bool wantsCallBack = false;
                wf.Size measureSize  = default;

                wf.ITypedEventHandler<xaml_controls.CalendarView, xaml_controls.CalendarViewDayItemChangingEventArgs> spCallback;

                // guaranteed to have pVirtualizationInformation by now

                global::System.Diagnostics.Debug.Assert(pVirtualizationInformation);

                measureSize = pVirtualizationInformation.GetMeasureSize();

                // did we store a callback
                spCallback = spArgsConcrete.Callback;

                // raise event
                if (spCallback)
                {
                    spArgsConcrete.WantsCallBack = false;
                    // clear out the delegate
                    spArgsConcrete.Callback = null;

                    spCallback.Invoke(GetOwner(), spArgs);

                    // the invoke will cause them to call RegisterCallback which will overwrite the delegate (fine)
                    // and set the boolean below to true
                    wantsCallBack = spArgsConcrete.WantsCallBack;
                }

                // the user might have changed elements. In order to keep the budget fair, we need to try and incur
                // most of the cost right now.
                spContainerAsCalendarViewDayItem as CalendarViewDayItem.Measure(measureSize);

                // register callback
                if (wantsCallBack)
                {
                    Uint phaseFromArgs = 0;
                    phaseFromArgs = spArgsConcrete.Phase;
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
                        nextLowest = std.min(nextLowest, (INT64)(phase));
                    }
                }
                else
                {
                    // won't be called again for the lifetime of this container
                    spArgsConcrete.ResetLifetime();

                    // we do not have to update the next lowest. We are still processing this phase and will
                    // continue to do so (procesingPhase is still valid).
                }
            }
            else   //if (phase == processingPhase)
            {
                // if we hit a container that is registered for a callback (so he wants to iterate over phases)
                // but is currently at a different phase, we need to make sure that the next lowest is set.
                bool wantsCallBack = false;
                wantsCallBack = spArgsConcrete.WantsCallBack;

                if (wantsCallBack)
                {
                    global::System.Diagnostics.Debug.Assert(phase > processingPhase);
                    // update the next lowest, now that we have seen a phase that is higher than our current processing phase
                    nextLowest = std.min(nextLowest, (INT64)(phase));
                }
            }

            // updates the current position in the correct direction
            if (shouldUpdateCurrentPosition)
            {
                ProcessCurrentPosition();
            }
            // updates the time
            timeElapsedInMS = spBudget.GetElapsedMilliSecondsSinceLastUITick);
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
            global::System.Diagnostics.Debug.Assert(processingPhase < std.numeric_limits<INT>.max());
            m_lowestPhaseInQueue = (INT)(processingPhase);
        }
    }

}

private void CalendarViewGeneratorMonthViewHost.EnsureToBeClearedContainers()
{
    if (!m_toBeClearedContainers)
    {
        TrackerCollection<xaml_controls.CalendarViewDayItem> spContainersForClear;

        spContainersForClear = default;
        m_toBeClearedContainers = std.move(spContainersForClear);
    }

}

private void CalendarViewGeneratorMonthViewHost.ClearContainers(
      BudgetManager& spBudget)
{
    Uint containersToClearCount = 0;
    int timeElapsedInMS = 0;

    EnsureToBeClearedContainers();

    containersToClearCount = m_toBeClearedContainers.Size;
    for (Uint toClearIndex = containersToClearCount - 1; toClearIndex >= 0 && containersToClearCount > 0; --toClearIndex)
    {
        ICalendarViewDayItem spContainer;

        timeElapsedInMS = spBudget.GetElapsedMilliSecondsSinceLastUITick);

        if ((UINT)(timeElapsedInMS) > m_budget)
        {
            break;
        }

        spContainer = m_toBeClearedContainers.GetAt(toClearIndex);
        m_toBeClearedContainers.RemoveAtEnd();

        // execute the deferred work
        // apparently we were not going to reuse this container immediately again, so 
        // let's do the work now

        // we don't need the spItem because 1. we didn't save this information, 2. CalendarViewGeneratorHost.ClearContainerForItem is no-op for now
        // if we need this we could simple restore the spItem from the container.
        CalendarViewGeneratorHost.ClearContainerForItem(spContainer as CalendarViewDayItem, null /spItem/);

        // potentially raise the event
        if (spContainer)
        {
            RaiseContainerContentChangingOnRecycle(spContainer as UIElement, null);
        }

        if (toClearIndex == 0)
        {
            // UINT
            break;
        }
    }

}

CalendarViewGeneratorMonthViewHost.ShutDownDeferredWork()
{
    IItemContainerMapping spMapping;

    var pCalendarPanel = GetPanel();

    if (pCalendarPanel)
    {
        // go through everyone that might have work registered for a prepare
        int cacheStart, cacheEnd = 0;
        cacheStart = pCalendarPanel.FirstCacheIndexBase;
        cacheEnd = pCalendarPanel.LastCacheIndexBase;
        spMapping = pCalendarPanel.GetItemContainerMapping);

        for (int i = cacheStart; i < cacheEnd; ++i)
        {
            xaml.IDependencyObject spContainer;
            xaml_controls.ICalendarViewDayItemChangingEventArgs spArgs;
            spContainer = spMapping.ContainerFromIndex(i);

            if (!spContainer)
            {
                // apparently a sentinel. This should not occur, however, during shutdown we could
                // run into this since measure might not have been processed yet
                continue;
            }

            spArgs = spContainer as CalendarViewDayItem.GetBuildTreeArgs);

            spArgs as CalendarViewDayItemChangingEventArgs.ResetLifetime();
        }
    }

}

private void CalendarViewGeneratorMonthViewHost.RegisterWorkFromCICArgs(
     xaml_controls.ICalendarViewDayItemChangingEventArgs pArgs)
{
    bool wantsCallback = false;
    ICalendarViewDayItem spCalendarViewDayItem;
    CalendarViewDayItemChangingEventArgs pConcreteArgsNoRef = (CalendarViewDayItemChangingEventArgs)(pArgs);

    wantsCallback = pConcreteArgsNoRef.WantsCallBack;
    spCalendarViewDayItem = pConcreteArgsNoRef.Item;

    // we are going to want to be called back if:
    // 1. we are still showing the placeholder
    // 2. app code registered to be called back
    if (wantsCallback)
    {
        Uint phase = 0;

        phase = pConcreteArgsNoRef.Phase;

        // keep this state on the listviewbase
        if (m_lowestPhaseInQueue == -1)
        {
            // there was nothing registered
            m_lowestPhaseInQueue = phase;

            // that means we need to register ourselves with the buildtreeservice so that 
            // we can get called back to do some work
            if (!m_isRegisteredForCallbacks)
            {
                BuildTreeService spBuildTree;
                DXamlCore.GetCurrent().GetBuildTreeService(spBuildTree);
                spBuildTree.RegisterWork(this);
            }

            global::System.Diagnostics.Debug.Assert(m_isRegisteredForCallbacks);
        }
        else if (m_lowestPhaseInQueue > phase)
        {
            m_lowestPhaseInQueue = phase;
        }
    }
    else
    {
        // well, app code doesn't want a callback so cleanup the args
        pConcreteArgsNoRef.ResetLifetime();
    }

}
