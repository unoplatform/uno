// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.


using namespace DirectUI;
using namespace DirectUISynonyms;

// Work around disruptive max/min macros
#undef max
#undef min

// IGeneratorHost

 private void get_View(
    out  wfc.IVector<DependencyObject>** ppView)
{
    CalendarViewGeneratorHost spThis(this);

    return spThis.CopyTo(ppView);
}

 private void get_CollectionView(
    out  xaml_data.ICollectionView* ppCollectionView)
{
    // return null so MCBP knows there is no group.
    ppCollectionView = null;

    return;
}

 private void IsItemItsOwnContainer(
     DependencyObject pItem,
    out BOOLEAN pIsOwnContainer)
{
    // our item is DateTime, not the container
    pIsOwnContainer = false;
    return;
}

 private void GetContainerForItem(
     DependencyObject pItem,
     xaml.IDependencyObject pRecycledContainer,
    out  xaml.IDependencyObject* ppContainer)
{
    CalendarViewBaseItem spContainer;

    spContainer = GetContainer(pItem, pRecycledContainer);
    spContainer.SetParentCalendarView(GetOwner());

    spContainer.MoveTo(ppContainer);

}

 private void PrepareItemContainer(
     xaml.IDependencyObject pContainer,
     DependencyObject pItem)
{
    // All calendar items have same scope logical, handle it here:
    CalendarViewItem spContainer((CalendarViewItem)(pContainer));

    spContainer.SetIsOutOfScope(false);

    // today state
    {
        DateTime date;
        bool isToday = false;
        int result = 0;

        ctl.do_get_value(date, pItem);

        result = CompareDate(date, GetOwner().GetToday());
        if (result == 0)
        {
            bool isTodayHighlighted = false;

            isTodayHighlighted = GetOwner().IsTodayHighlighted;

            isToday = !!isTodayHighlighted;
        }

        spContainer.SetIsToday(isToday);
    }

    return;
}

 private void ClearContainerForItem(
     xaml.IDependencyObject pContainer,
     DependencyObject pItem)
{
    return;
}

 private void IsHostForItemContainer(
     xaml.IDependencyObject pContainer,
    out BOOLEAN pIsHost)
{
    throw new NotImplementedException();
}

 private void GetGroupStyle(
     xaml_data.ICollectionViewGroup pGroup,
     Uint level,
    out xaml_controls.IGroupStyle* ppGroupStyle)
{
    // The modern panel is always going to ask for a GroupStyle.
    // Fortunately, it's perfectly valid to return null
    ppGroupStyle = null;
    return;
}

 private void SetIsGrouping(
     bool isGrouping)
{
    global::System.Diagnostics.Debug.Assert(!isGrouping);
    return;
}

// we don't expose this publicly, there is an override for our own controls
// to mirror the public api
 private void GetHeaderForGroup(
     DependencyObject pGroup,
    out  xaml.IDependencyObject* ppContainer)
{
    throw new NotImplementedException();
}

 private void PrepareGroupContainer(
     xaml.IDependencyObject pContainer,
     xaml_data.ICollectionViewGroup pGroup)
{
    throw new NotImplementedException();
}

 private void ClearGroupContainerForGroup(
     xaml.IDependencyObject pContainer,
     xaml_data.ICollectionViewGroup pItem)
{
    throw new NotImplementedException();
}

 private void CanRecycleContainer(
     xaml.IDependencyObject pContainer,
    out BOOLEAN pCanRecycleContainer)
{
    pCanRecycleContainer = true;
    return;
}

 private void SuggestContainerForContainerFromItemLookup(
    out  xaml.IDependencyObject* ppContainer)
{
    // CalendarViewGeneratorHost has no clue
    ppContainer = null;
    return;
}


CalendarViewGeneratorHost()
    : m_size(0)
    , m_pOwnerNoRef(null)
{
    ResetScope();
}

CalendarViewGeneratorHost.~CalendarViewGeneratorHost()
{
    /VERIFYHR/(DetachScrollViewerFocusEngagedEvent());
    /VERIFYHR/(DetachVisibleIndicesUpdatedEvent());
    IModernCollectionBasePanel panel;
    if (m_tpPanel.TryGetSafeReference(&panel))
    {
        /VERIFYHR/(panel as CalendarPanel.SetOwner(null));
        /VERIFYHR/(panel as CalendarPanel.SetSnapPointFilterFunction(null));
    }

    IScrollViewer scrollviewer;
    if (m_tpScrollViewer.TryGetSafeReference(&scrollviewer))
    {
        /VERIFYHR/(scrollviewer as ScrollViewer.SetDirectManipulationStateChangeHandler(null));
    }
}


wg.ICalendar GetCalendar()
{
    return GetOwner().GetCalendar();
}

void ResetScope()
{
    // when scope is enabled, the current scope means the current Month for monthview, current year for yearView and current decade for decadeview
    m_minDateOfCurrentScope.UniversalTime = 0;
    m_maxDateOfCurrentScope.UniversalTime = 0;
    m_pHeaderText.Release();
    m_lastVisibleIndicesPair[0] = -1;
    m_lastVisibleIndicesPair[1] = -1;
    m_lastVisitedDateAndIndex.first.UniversalTime = 0;
    m_lastVisitedDateAndIndex.second = -1;

}

// compute how many items we have in this view, basically the number of items equals to the index of max date + 1
private void ComputeSize()
{
    int index = 0;

    m_lastVisitedDateAndIndex.first = GetOwner().GetMinDate();
    m_lastVisitedDateAndIndex.second = 0;

    global::System.Diagnostics.Debug.Assert(!GetOwner().GetDateComparer().LessThan(GetOwner().GetMaxDate(), GetOwner().GetMinDate()));

    index = CalculateOffsetFromMinDate(GetOwner().GetMaxDate());

    m_size = (UINT)(index)+1;

}

// Add scopes to the given date.
private void AddScopes( DateTime& date,  int scopes)
{
    var pCalendar = GetCalendar();

    pCalendar.SetDateTime(date);
    AddScopes(scopes);
    date = pCalendar.GetDateTime);

    // We coerce and check if the date is in Calendar's limit where this gets called.

}

private void AddUnits( DateTime& date,  int units)
{
    var pCalendar = GetCalendar();

    pCalendar.SetDateTime(date);
    AddUnits(units);
    date = pCalendar.GetDateTime);

    // We coerce and check if the date is in Calendar's limit where this gets called.

    return;
}

// AddDays/AddMonths/AddYears takes O(N) time but given that at most time we
// generate the items continuously so we can cache the result from last call and
// call AddUnits from the cache - this way N is small enough
// time cost: amortized O(1)

private void GetDateAt( Uint index, out DateTime pDate)
{
    DateTime date  = default;
    var pCalendar = GetCalendar();

    global::System.Diagnostics.Debug.Assert(m_lastVisitedDateAndIndex.second != -1);

    pCalendar.SetDateTime(m_lastVisitedDateAndIndex.first);
    AddUnits((int)(index) - m_lastVisitedDateAndIndex.second);
    date = pCalendar.GetDateTime);
    m_lastVisitedDateAndIndex.first = date;
    m_lastVisitedDateAndIndex.second = (int)(index);
    pDate.UniversalTime = date.UniversalTime;

    return;
}

// to get the distance of two days, here are the amortized O(1) method
//1. Estimate the offset of Date2 from Date1 by dividing their UTC difference by 24 hours
//2. Call Globalization API AddDays(Date1, offset) to get an estimated date, let’s say EstimatedDate, here offset comes from step1
//3. Compute the distance between EstimatedDate and Date2(keep adding 1 day on the smaller one, until we hit the another date), 
//   if this distance is still big, we can do step 1 and 2 one more time
//4. Return the sum of results from step1 and step3.

private void CalculateOffsetFromMinDate( DateTime date, out int pIndex)
{
    pIndex = 0;
    DateTime estimatedDate = { m_lastVisitedDateAndIndex.first.UniversalTime };
    var pCalendar = GetCalendar();
    global::System.Diagnostics.Debug.Assert(m_lastVisitedDateAndIndex.second != -1);

    int estimatedOffset = 0;
    INT64 diffInUTC = 0;
    int diffInUnit = 0;

     int maxEstimationRetryCount = 3;  // the max times that we should estimate
     int maxReboundCount = 3;          // the max times that we should reduce the step when the estimation is over the boundary.
     int minDistanceToEstimate = 3;    // the min estimated distance that we should do estimation.

    pCalendar.SetDateTime(estimatedDate);

    // step 1: estimation. mostly we only need to up to 2 times, but if we are targeting the calendar's boundaries
    // we could need more times (uncommon scenario)
    var averageTicksPerUnit = GetAverageTicksPerUnit();
#if DEBUG
    int estimationCount = 0;
#endif
    while (true)
    {
        diffInUTC = date.UniversalTime - estimatedDate.UniversalTime;

        // round to the nearest integer
        diffInUnit = (int)(diffInUTC / averageTicksPerUnit);

        if (std.abs(diffInUnit) < minDistanceToEstimate)
        {
            // if two dates are close enough, we can start to check if a correction is needed.
            break;
        }
#if DEBUG
        if (estimationCount++ > maxEstimationRetryCount)
        {
            IGNOREHR(DebugTrace(XCP_TRACE_WARNING, "CalendarViewGeneartorHost.CalculateOffsetFromMinDate[0x%p]:  estimationCount = %d.", this, estimationCount));
            global::System.Diagnostics.Debug.Assert(false);
        }
#endif

        // when we are targeting the calendar's boundaries, it is possible the estimation will
        // cross the boundary, in this case we should reduce the length of step.
#if DEBUG
        int retryCount = 0;
#endif
        while (true)
        {
            HRESULT hr = AddUnits(diffInUnit);

            if (SUCCEEDED(hr))
                break;
#if DEBUG
            if (retryCount++ > maxReboundCount)
            {
                IGNOREHR(DebugTrace(XCP_TRACE_WARNING, "CalendarViewGeneartorHost.CalculateOffsetFromMinDate[0x%p]: over boundary, retryCount = %d.", this, retryCount));
                global::System.Diagnostics.Debug.Assert(false);
            }
#endif
            // we crossed the boundary! reduce the length and restart from estimatedDate
            //
            // mostly a bad estimation could happen on two dates that have a huge difference (e.g. jump to 100 years ago),
            // to fix the estimation we only need to slightly reduce the diff.

            pCalendar.SetDateTime(estimatedDate);
            diffInUnit = diffInUnit * 99 / 100;
            global::System.Diagnostics.Debug.Assert(diffInUnit != 0);
        } //while (true)

        estimatedOffset += diffInUnit;

        estimatedDate = pCalendar.GetDateTime);
    } //while (true)

    // step 2: after estimation, we'll check if a correction is needed or not.
    // this will be done in O(N) time but given that we have a good enough
    // estimation, here N will be very small (most likely <= 2)
    int offsetCorrection = 0;
    while (true)
    {
        int result = 0;
        int step = 1;
        result = CompareDate(estimatedDate, date);
        if (result == 0)
        {
            // end the loop when meeting the target date
            break;
        }
        else if (result > 0)
        {
            step = -1;
        }
        AddUnits(step);
        offsetCorrection += step;
        estimatedDate = pCalendar.GetDateTime);
    }

    // base + estimatedDiff + correction
    pIndex = m_lastVisitedDateAndIndex.second + estimatedOffset + offsetCorrection;

    return;
}

// return the first date of next scope.
// parameter dateOfFirstVisibleItem is the first visible item, it could be in
// current scope, or in previous scope.
private void GetFirstDateOfNextScope(
     DateTime dateOfFirstVisibleItem,
     bool forward,
    out DateTime pFirstDateOfNextScope)
{
    int adjustScopes = 0;
    DateTime firstDateOfNextScope  = default;

    // set to the first date of current scope
    GetCalendar().SetDateTime(m_minDateOfCurrentScope);

    if (!GetOwner().GetDateComparer().LessThan(m_minDateOfCurrentScope, dateOfFirstVisibleItem))
    {
        // current scope starts from the first visible line
        // in this case, we simply jump to previous or next scope
        adjustScopes = forward ? 1 : -1;
    }
    else
    {
        // current scope starts before the first visible line,
        // so when we go backwards, we go to the beginning of this scope.
        // when go forwards, we still go to the next scope
        adjustScopes = forward ? 1 : 0;
    }

    if (adjustScopes != 0)
    {
        AddScopes(adjustScopes);

        int firstUnit = 0;
        firstUnit = GetFirstUnitInThisScope);
        SetUnit(firstUnit);
    }

    firstDateOfNextScope = GetCalendar().GetDateTime);

    // when the navigation button is enabled, we should always be able to navigate to the desired scope.
    global::System.Diagnostics.Debug.Assert(!GetOwner().GetDateComparer().LessThan(firstDateOfNextScope, GetOwner().GetMinDate()));
    global::System.Diagnostics.Debug.Assert(!GetOwner().GetDateComparer().LessThan(GetOwner().GetMaxDate(), firstDateOfNextScope));

Cleanup:
    pFirstDateOfNextScope = firstDateOfNextScope;
    return hr;
}


// Give a date range (it may contain multiple scopes, the scope is a month for MonthView),
// find the scope that has higher item coverage percentage, and use it as current scope.
private void UpdateScope(
     DateTime firstDate,
     DateTime lastDate,
    out bool isScopeChanged)
{
    DateTime lastDateOfFirstScope;
    DateTime minDateOfCurrentScope;
    DateTime maxDateOfCurrentScope;
    int firstUnit = 0;
    int firstUnitOfFirstScope = 0;
    int lastUnitOfFirstScope = 0;

    isScopeChanged = false;
    var pCalendar = GetCalendar();

    global::System.Diagnostics.Debug.Assert(!GetOwner().GetDateComparer().LessThan(lastDate, firstDate));

    pCalendar.SetDateTime(firstDate);
    firstUnit = GetUnit);
    lastUnitOfFirstScope = AdjustToLastUnitInThisScope(&lastDateOfFirstScope);

    if (!GetOwner().GetDateComparer().LessThan(lastDateOfFirstScope, lastDate))
    {
        // The given range has only one scope, so this is the current scope
        maxDateOfCurrentScope.UniversalTime = lastDateOfFirstScope.UniversalTime;
        minDateOfCurrentScope = AdjustToFirstUnitInThisScope);
    }
    else
    {
        // The given range has more than one scopes, let's check the first one and second one.
        DateTime lastDateOfSecondScope;
        int itemCountOfFirstScope = lastUnitOfFirstScope - firstUnit + 1;
        int itemCountOfSecondScope = 0;

        DateTime dateToDetermineCurrentScope;   // we'll pick a date from first scope or second scope to determine the current scope.
        int firstUnitOfSecondScope = 0;
        int lastUnitOfSecondScope = 0;

        firstUnitOfFirstScope = GetFirstUnitInThisScope);

        // We are on the last unit of first scope, add 1 unit will move to the second scope
        AddUnits(1);

        firstUnitOfSecondScope = GetFirstUnitInThisScope);

        // Read the last date of second scope, check if it is inside the given range.
        lastUnitOfSecondScope = AdjustToLastUnitInThisScope(&lastDateOfSecondScope);

        if (!GetOwner().GetDateComparer().LessThan(lastDate, lastDateOfSecondScope))
        {
            // The given range has the whole 2nd scope
            itemCountOfSecondScope = lastUnitOfSecondScope - firstUnitOfSecondScope + 1;
        }
        else
        {
            // The given range has only a part of the 2nd scope
            int lastUnit = 0;
            pCalendar.SetDateTime(lastDate);
            lastUnit = GetUnit);
            itemCountOfSecondScope = lastUnit - firstUnitOfSecondScope + 1;
        }

        double firstScopePercentage = (double)itemCountOfFirstScope / (lastUnitOfFirstScope-firstUnitOfFirstScope+1);
        double secondScopePercentage = (double)itemCountOfSecondScope / (lastUnitOfSecondScope-firstUnitOfSecondScope+1);

        if (firstScopePercentage < secondScopePercentage)
        {
            // second scope wins
            dateToDetermineCurrentScope.UniversalTime = lastDateOfSecondScope.UniversalTime;
        }
        else
        {
            // first scope wins
            dateToDetermineCurrentScope.UniversalTime = firstDate.UniversalTime;
        }

        pCalendar.SetDateTime(dateToDetermineCurrentScope);
        minDateOfCurrentScope = AdjustToFirstUnitInThisScope);
        maxDateOfCurrentScope = AdjustToLastUnitInThisScope);
    }

    // in case we start from a day other than first day, we need to adjust the scope.
    // in case we end at a day other than the last day of this month, we need to adjust the scope.
    GetOwner().CoerceDate(minDateOfCurrentScope);
    GetOwner().CoerceDate(maxDateOfCurrentScope);

    if (minDateOfCurrentScope.UniversalTime != m_minDateOfCurrentScope.UniversalTime ||
        maxDateOfCurrentScope.UniversalTime != m_maxDateOfCurrentScope.UniversalTime)
    {
        m_minDateOfCurrentScope = minDateOfCurrentScope;
        m_maxDateOfCurrentScope = maxDateOfCurrentScope;
        isScopeChanged = true;

        OnScopeChanged();
    }

}

private void AdjustToFirstUnitInThisScope(out DateTime pDate, _Out_opt_ int pUnit /* = null */)
{
    int firstUnit = 0;

    if (pUnit)
    {
        pUnit = 0;
    }
    pDate.UniversalTime = 0;

    firstUnit = GetFirstUnitInThisScope);
    SetUnit(firstUnit);
    GetCalendar().GetDateTime(pDate);

    if (pUnit)
    {
        pUnit = firstUnit;
    }

    return;
}

private void AdjustToLastUnitInThisScope(out DateTime pDate, _Out_opt_ int pUnit /* = null */)
{
    int lastUnit = 0;

    if (pUnit)
    {
        pUnit = 0;
    }
    pDate.UniversalTime = 0;

    lastUnit = GetLastUnitInThisScope);
    SetUnit(lastUnit);
    GetCalendar().GetDateTime(pDate);

    if (pUnit)
    {
        pUnit = lastUnit;
    }

    return;
}

private void NotifyStateChange(
     DMManipulationState state,
     FLOAT xCumulativeTranslation,
     FLOAT yCumulativeTranslation,
     FLOAT zCumulativeFactor,
     FLOAT xCenter,
     FLOAT yCenter,
     bool isInertial,
     bool isTouchConfigurationActivated,
     bool isBringIntoViewportConfigurationActivated)
{
    switch (state)
    {
        // we change items' scope state to InScope when DMManipulation is in progress to achieve better visual effect.
        // note we only change when there is an actual move (e.g. Manipulation Started, not Starting), because user
        // tapping to select an item also causes Manipulation starting, in this case we should not change scope state.
    case DirectUI.DMManipulationStarted:
        IFC_RETURN(GetOwner().UpdateItemsScopeState(this,
            true, /ignoreWhenIsOutOfScopeDisabled/
            false /ignoreInDirectManipulation/));
        break;
    case DirectUI.DMManipulationCompleted:
        IFC_RETURN(GetOwner().UpdateItemsScopeState(this,
            false, /ignoreWhenIsOutOfScopeDisabled/ // in case we changed IsOutOfScopeEnabled to false during DManipulation
            false /ignoreInDirectManipulation/));
        break;
    default:
        break;
    }
    return;
}

private void AttachVisibleIndicesUpdatedEvent()
{
    if (m_tpPanel)
    {
        IFC_RETURN(m_epVisibleIndicesUpdatedHandler.AttachEventHandler(m_tpPanel as CalendarPanel,
            (DependencyObject pSender, DependencyObject pArgs) =>
        {
            return GetOwner().OnVisibleIndicesUpdated(this);
        }));
    }
    return;
}

private void DetachVisibleIndicesUpdatedEvent()
{
    return DetachHandler(m_epVisibleIndicesUpdatedHandler, m_tpPanel);
}

private void AttachScrollViewerFocusEngagedEvent()
{
    if (m_tpPanel)
    {
        DirectUI.ScrollViewer sv(m_tpScrollViewer as DirectUI.ScrollViewer);
        IFC_RETURN(m_epScrollViewerFocusEngagedEventHandler.AttachEventHandler(sv as xaml_controls.IControl,
            (xaml_controls.IControl pSender,
                 xaml_controls.IFocusEngagedEventArgs pArgs) =>
        {
            return GetOwner().OnScrollViewerFocusEngaged(pArgs);
        }));
    }
    return;
}

private void DetachScrollViewerFocusEngagedEvent()
{
    return DetachHandler(m_epScrollViewerFocusEngagedEventHandler, m_tpScrollViewer);
}

private void SetPanel( xaml_primitives.ICalendarPanel pPanel)
{
    if (pPanel)
    {
        m_tpPanel = pPanel;
        m_tpPanel as CalendarPanel.SetOwner(this);
    }
    else if (m_tpPanel)
    {
        m_tpPanel as CalendarPanel.SetOwner(null);
        m_tpPanel.Clear();
    }
    return;
}


private void SetScrollViewer( xaml_controls.IScrollViewer pScrollViewer)
{
    if (pScrollViewer)
    {
        m_tpScrollViewer = pScrollViewer;
    }
    else
    {
        m_tpScrollViewer.Clear();
    }
    return;
}


CalendarPanel GetPanel()
{
    return m_tpPanel as CalendarPanel;
}

ScrollViewer GetScrollViewer()
{
    return m_tpScrollViewer as ScrollViewer;
}

private void OnPrimaryPanelDesiredSizeChanged()
{
    return GetOwner().OnPrimaryPanelDesiredSizeChanged(this);
}
