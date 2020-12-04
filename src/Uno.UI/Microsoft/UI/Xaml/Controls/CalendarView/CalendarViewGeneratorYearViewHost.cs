// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.


using namespace DirectUI;
using namespace DirectUISynonyms;

// Work around disruptive max/min macros
#undef max
#undef min

private void GetContainer(
     DependencyObject pItem,
     xaml.IDependencyObject pRecycledContainer,
    out  CalendarViewBaseItem* ppContainer)
{
    CalendarViewItem spContainer;

    spContainer = ctl.new CalendarViewItem);

    spContainer.CopyTo(ppContainer);

}


 private void PrepareItemContainer(
     xaml.IDependencyObject pContainer,
     DependencyObject pItem)
{
    DateTime date;
    CalendarViewItem spContainer;

    spContainer = (CalendarViewItem)(pContainer);

    ctl.do_get_value(date, pItem);
    GetCalendar().SetDateTime(date);
    spContainer.Date = date;

    // maintext
    {
        string mainText;
        string automationName;

        IFC(GetCalendar().MonthAsFullString(
            automationName()));

        AutomationProperties.SetNameStatic(spContainer as FrameworkElement, automationName);

        IFC(GetCalendar().MonthAsString(
            0, /idealLength, set to 0 to get the abbreviated string/
            mainText()));

        spContainer.UpdateMainText(mainText);
    }

    // label text
    {
        bool isLabelVisible = false;

        isLabelVisible = GetOwner().IsGroupLabelVisible;

        UpdateLabel(spContainer, !!isLabelVisible);
    }

    // today state will be updated in CalendarViewGeneratorHost.PrepareItemContainer

    // YearView doesn't have selection state
    
    // Make a grid effect on YearView.
    // For MonthView, we put a margin on CalendarViewDayItem in the template to achieve the grid effect.
    // For YearView and DecadeView, we can't do the same because there is no template for MonthItem and YearItem
    {
        xaml.Thickness margin{ 1.0, 1.0, 1.0, 1.0 };
        spContainer.Margin = margin;
    }

    //This code enables the focus visuals on the CalendarViewItems in the Year Pane in the correct position.
    {
         xaml.Thickness focusMargin{ -2.0, -2.0, -2.0, -2.0 };
        spContainer.FocusVisualMargin = focusMargin;

        spContainer.UseSystemFocusVisuals = true;
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
        int month = 0;
        int firstMonthOfThisYear = 0;

        // TODO: consider caching the firstday flag because we also need this information when determining snap points 
        // (however Decadeview doesn't need this for Label).
        date = pItem.GetDate);
        pCalendar.SetDateTime(date);
        firstMonthOfThisYear = pCalendar.FirstMonthInThisYear;
        month = pCalendar.Month;

        showLabel = firstMonthOfThisYear == month;

        if (showLabel)
        {
            string labelText;
            pCalendar.YearAsString(labelText());
            pItem.UpdateLabelText(labelText);
        }
    }
    pItem.ShowLabelText(showLabel);
    return;
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
        int month = 0;
        int firstMonth = 0;

        date = GetDateAt(index);
        var pCalendar = GetCalendar();
        pCalendar.SetDateTime(date);
        month = pCalendar.Month;
        firstMonth = pCalendar.FirstMonthInThisYear;
        pIsFirstItemInScope = month == firstMonth;
    }

}

private void GetUnit(out int pValue)
{
    return GetCalendar().get_Month(pValue);
}

private void SetUnit( int value)
{
    return GetCalendar().Month = value;
}

private void AddUnits( int value)
{
    return GetCalendar().AddMonths(value);
}

private void AddScopes( int value)
{
    GetCalendar().AddYears(value);
    return;
}

private void GetFirstUnitInThisScope(out int pValue)
{
    return GetCalendar().get_FirstMonthInThisYear(pValue);
}
private void GetLastUnitInThisScope(out int pValue)
{
    return GetCalendar().get_LastMonthInThisYear(pValue);
}

private void OnScopeChanged()
{
    return GetOwner().FormatYearName(m_maxDateOfCurrentScope, m_pHeaderText.ReleaseAn());
}

private void GetPossibleItemStrings(out   std.CalculatorList<string>** ppStrings)
{
    ppStrings = &m_possibleItemStrings;

    if (m_possibleItemStrings.empty())
    {
        // for all known calendar identifiers so far (10 different calendar identifiers), we can find the longest year in no more than 3 years
        // if we start from min date of this calendar.

        // below are the longest year and the lowest index of that year we found for each calendar identifier. 
        // we hope that any new calendar in the future don't break this rule.

        // PersianCalendar, maxLength = 12 @ index 0
        // GregorianCalendar, maxLength = 12 @ index 0
        // HebrewCalendar, maxLength = 13 @ index 2
        // HijriCalendar, maxLength = 12 @ index 0
        // JapaneseCalendar, maxLength = 12 @ index 0
        // JulianCalendar, maxLength = 12 @ index 0
        // KoreanCalendar, maxLength = 12 @ index 0
        // TaiwanCalendar, maxLength = 12 @ index 0
        // ThaiCalendar, maxLength = 12 @ index 1
        // UmAlQuraCalendar, maxLength = 12 @ index 0
        {
             int MaxNumberOfYearsToBeChecked = 3;
            DateTime longestYear;
            int lengthOfLongestYear = 0;
            int numberOfMonths = 0;
            int month = 0;

            var pCalendar = GetCalendar();

            pCalendar.SetToMin();
            for (int i = 0; i < MaxNumberOfYearsToBeChecked; i++)
            {
                numberOfMonths = pCalendar.NumberOfMonthsInThisYear;
                if (numberOfMonths > lengthOfLongestYear)
                {
                    lengthOfLongestYear = numberOfMonths;
                    longestYear = pCalendar.GetDateTime);
                }
                pCalendar.AddYears(1);
            }

            global::System.Diagnostics.Debug.Assert(lengthOfLongestYear == 13 || lengthOfLongestYear == 12);
            pCalendar.SetDateTime(longestYear);
            month = pCalendar.FirstMonthInThisYear;
            pCalendar.Month = month;

            m_possibleItemStrings.reserve(lengthOfLongestYear);

            for (int i = 0; i < lengthOfLongestYear; i++)
            {
                string string;
                
                IFC_RETURN(pCalendar.MonthAsString(
                    0, /idealLength, set to 0 to get the abbreviated string/
                    string()));
                m_possibleItemStrings.emplace_back(std.move(string));
                pCalendar.AddMonths(1);
            }
        }
    }

    return;
}

private void CompareDate( DateTime lhs,  DateTime rhs, out int pResult)
{
    return GetOwner().GetDateComparer().CompareMonth(lhs, rhs, pResult);
}

INT64 GetAverageTicksPerUnit()
{
    // this is being used to estimate the distance between two dates,
    // it doesn't need to be (and it can't be) the exact value
    return CalendarConstants.s_ticksPerDay * 365 / 12;
}