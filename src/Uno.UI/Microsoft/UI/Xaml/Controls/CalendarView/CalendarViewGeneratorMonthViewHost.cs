// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.


using namespace DirectUI;
using namespace DirectUISynonyms;

// Work around disruptive max/min macros
#undef max
#undef min


CalendarViewGeneratorMonthViewHost()
    : m_lowestPhaseInQueue(-1)
    , m_isRegisteredForCallbacks()
    , m_budget(BUDGET_MANAGER_DEFAULT_LIMIT)
{
}

private void GetContainer(
     DependencyObject pItem,
     xaml.IDependencyObject pRecycledContainer,
    out  CalendarViewBaseItem* ppContainer)
{
    CalendarViewDayItem spContainer;

    spContainer = ctl.new CalendarViewDayItem);

    spContainer.CopyTo(ppContainer);

}

 private void PrepareItemContainer(
     xaml.IDependencyObject pContainer,
     DependencyObject pItem)
{
    DateTime date;
    CalendarViewDayItem spContainer;

    spContainer = (CalendarViewDayItem)(pContainer);

    // first let's check if this container is already in the tobecleared queue
    // the container might be already marked as to be cleared but not cleared yet, 
    // if we pick up this container we don't need to clear it up.
    RemoveToBeClearedContainer(spContainer);

    // now prepare the container    

    ctl.do_get_value(date, pItem);
    GetCalendar().SetDateTime(date);

    spContainer.Date = date;

    // main text
    {
        string mainText;

        GetCalendar().DayAsString(mainText());
        spContainer.UpdateMainText(mainText);
    }

    // label text
    {
        bool isLabelVisible = false;

        isLabelVisible = GetOwner().IsGroupLabelVisible;

        UpdateLabel(spContainer, !!isLabelVisible);
    }

    // today state will be updated in CalendarViewGeneratorHost.PrepareItemContainer

    // clear the blackout state and set correct selection state.
    {
        // we don't have a copy of blackout items (that could be too many), instead developer needs to tell us
        // about the blackout and densitybar properties when in CIC event. As both properties are on the container (not like selected dates) so when
        // the container is being reused, we'll need to clear these properties and developer may set them again if they want.
        // there is a discussion that who should clear the flags - the developer clear them in phase 0 (in CIC phase 0 event) 
        // or we clear them in phase 0 (before CIC phase 0 event). the result is we do this to make the logical simple.

        // reset the blackout state
        spContainer.IsBlackout = false;

        // set selection state.
        bool isSelected = false;
        isSelected = GetOwner().IsSelected(date);
        spContainer.SetIsSelected(isSelected);
    }

    // clear the density bar as well.
    spContainer.SetDensityColors(null);

    // apply style to CalendarViewDayItem if any.
    {
        IStyle spStyle;

        spStyle = GetOwner().CalendarViewDayItemStyle;
        CalendarView.SetDayItemStyle(spContainer, spStyle);
    }

    CalendarViewGeneratorHost.PrepareItemContainer(pContainer, pItem);

}

private void UpdateLabel( CalendarViewBaseItem pItem,  bool isLabelVisible)
{
    bool showLabel = false;
    if (isLabelVisible)
    {
        DateTime date;
        var pCalendar = GetCalendar();
        int day = 0;
        int firstDayInThisMonth = 0;

        // TODO: consider caching the firstday flag because we also need this information when determining snap points 
        // (however Decadeview doesn't need this for Label).
        date = pItem.GetDate);
        pCalendar.SetDateTime(date);
        firstDayInThisMonth = pCalendar.FirstDayInThisMonth;
        day = pCalendar.Day;
        showLabel = firstDayInThisMonth == day;

        if (showLabel)
        {
            string labelText;
            IFC_RETURN(pCalendar.MonthAsString(
                0, /idealLength, set to 0 to get the abbreviated string/
                labelText()));
            pItem.UpdateLabelText(labelText);
        }
    }
    pItem.ShowLabelText(showLabel);
    return;
}

// reset CIC event if the container is being cleared.
 private void ClearContainerForItem(
     xaml.IDependencyObject pContainer,
     DependencyObject pItem)
{
    CalendarViewDayItem spContainer = (CalendarViewDayItem)(pContainer);

    // There is much perf involved with doing a clear, and usually it is going to be
    // a waste of time since we are going to immediately follow up with a prepare. 
    // Perf traces have found this to be about 8 to 12% during a full virtualization pass (!!)
    // Although with other optimizations we would expect that to go down, it is unlikely to go 
    // down to 0. Therefore we are deferring the impl work here to later.
    // We have decided to do this only for the new panels.

    // also, do not defer items that are uielement. They need to be cleared straight away so that
    // they can be messed with again.
    m_toBeClearedContainers.Append(spContainer);

    // note that if we are being cleared, we are not going to be in the 
    // visible index, or the caches. And thus we will never be called in the 
    // prepare queuing part.

    if (!m_isRegisteredForCallbacks)
    {
        BuildTreeService spBuildTree;
        DXamlCore.GetCurrent().GetBuildTreeService(spBuildTree);
        spBuildTree.RegisterWork(this);
    }
    global::System.Diagnostics.Debug.Assert(m_isRegisteredForCallbacks);


}

private void GetIsFirstItemInScope( int index, out bool pIsFirstItemInScope)
{
    pIsFirstItemInScope = false;
    if (index == 0)
    {
        pIsFirstItemInScope = true;
    }
    else
    {
        DateTime date  = default;
        int day = 0;
        int firstDay = 0;

        date = GetDateAt(index);
        var pCalendar = GetCalendar();
        pCalendar.SetDateTime(date);
        day = pCalendar.Day;
        firstDay = pCalendar.FirstDayInThisMonth;
        pIsFirstItemInScope = day == firstDay;
    }

}

private void GetUnit(out int pValue)
{
    return GetCalendar().get_Day(pValue);
}

private void SetUnit( int value)
{
    return GetCalendar().Day = value;
}

private void AddUnits( int value)
{
    return GetCalendar().AddDays(value);
}

private void AddScopes( int value)
{
    return GetCalendar().AddMonths(value);
}

private void GetFirstUnitInThisScope(out int pValue)
{
    return GetCalendar().get_FirstDayInThisMonth(pValue);
}
private void GetLastUnitInThisScope(out int pValue)
{
    return GetCalendar().get_LastDayInThisMonth(pValue);
}

private void OnScopeChanged()
{
    return GetOwner().FormatMonthYearName(m_minDateOfCurrentScope, m_pHeaderText.ReleaseAn());
}

private void GetPossibleItemStrings(out   std.CalculatorList<string>** ppStrings)
{
    ppStrings = &m_possibleItemStrings;

    if (m_possibleItemStrings.empty())
    {
        // for all known calendar identifiers so far (10 different calendar identifiers), we can find the longest month in no more than 3 months
        // if we start from min date of this calendar.

        // below are the longest month and the lowest index of that month we found for each calendar identifier. 
        // we hope that any new calendar in the future don't break this rule.

        // PersianCalendar, maxLength = 31 @ index 0
        // GregorianCalendar, maxLength = 31 @ index 0
        // HebrewCalendar, maxLength = 30 @ index 1
        // HijriCalendar, maxLength = 30 @ index 0
        // JapaneseCalendar, maxLength = 31 @ index 0
        // JulianCalendar, maxLength = 31 @ index 2             
        // KoreanCalendar, maxLength = 31 @ index 0
        // TaiwanCalendar, maxLength = 31 @ index 0
        // ThaiCalendar, maxLength = 31 @ index 0
        // UmAlQuraCalendar, maxLength = 30 @ index 1

        {
             int MaxNumberOfMonthsToBeChecked = 3;
            DateTime longestMonth;
            int lengthOfLongestMonth = 0;
            int numberOfDays = 0;
            int day = 0;

            var pCalendar = GetCalendar();

            pCalendar.SetToMin();
            for (int i = 0; i < MaxNumberOfMonthsToBeChecked; i++)
            {
                numberOfDays = pCalendar.NumberOfDaysInThisMonth;
                if (numberOfDays > lengthOfLongestMonth)
                {
                    lengthOfLongestMonth = numberOfDays;
                    longestMonth = pCalendar.GetDateTime);
                }
                pCalendar.AddMonths(1);
            }

            global::System.Diagnostics.Debug.Assert(lengthOfLongestMonth == 30 || lengthOfLongestMonth == 31);
            pCalendar.SetDateTime(longestMonth);
            day = pCalendar.FirstDayInThisMonth;
            pCalendar.Day = day;

            m_possibleItemStrings.reserve(lengthOfLongestMonth);

            for (int i = 0; i < lengthOfLongestMonth; i++)
            {
                string string;
                pCalendar.DayAsString(string());
                m_possibleItemStrings.emplace_back(std.move(string));
                pCalendar.AddDays(1);
            }
        }
    }

    return;
}

private void CompareDate( DateTime lhs,  DateTime rhs, out int pResult)
{
    return GetOwner().GetDateComparer().CompareDay(lhs, rhs, pResult);
}

INT64 GetAverageTicksPerUnit()
{
    // this is being used to estimate the distance between two dates,
    // it doesn't need to be (and it can't be) the exact value
    return CalendarConstants.s_ticksPerDay;
}
