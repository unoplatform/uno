// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#include "precomp.h"
#include "CalendarViewGeneratorDecadeViewHost.h"
#include "CalendarView.g.h"
#include "CalendarViewItem.g.h"
#include "DateComparer.h"

using namespace DirectUI;
using namespace DirectUISynonyms;

// Work around disruptive max/min macros
#undef max
#undef min


_Check_return_ HRESULT GetContainer(
     IInspectable* pItem,
     xaml.IDependencyObject* pRecycledContainer,
    _Outptr_ CalendarViewBaseItem** ppContainer)
{
    HRESULT hr = S_OK;

    ctl.ComPtr<CalendarViewItem> spItem;

    IFC(ctl.new CalendarViewItem(&spItem));

    IFC(spItem.CopyTo(ppContainer));

Cleanup:
    return hr;
}

_Check_return_ IFACEMETHODIMP PrepareItemContainer(
     xaml.IDependencyObject* pContainer,
     IInspectable* pItem)
{
    HRESULT hr = S_OK;
    wf.DateTime date;
    ctl.ComPtr<CalendarViewItem> spContainer;

    IFC(ctl.do_get_value(date, pItem));
    spContainer = (CalendarViewItem*)(pContainer);
    IFC(spContainer.put_Date(date));
    IFC(GetCalendar().SetDateTime(date));

    // main text
    {
        wrl_wrappers.HString mainText;

        IFC(GetCalendar().YearAsString(mainText.GetAddressOf()));


        IFC(spContainer.UpdateMainText(mainText.Get()));
    }

    // today state will be updated in CalendarViewGeneratorHost.PrepareItemContainer

    // DecadeView doesn't have label, selection state

    // Make a grid effect on DecadeView.
    // For MonthView, we put a margin on CalendarViewDayItem in the template to achieve the grid effect.
    // For YearView and DecadeView, we can't do the same because there is no template for MonthItem and YearItem
    {
        xaml.Thickness margin{ 1.0, 1.0, 1.0, 1.0 };
        IFC(spContainer.put_Margin(margin));
    }

    //This code enables the focus visuals on the CalendarViewItems in the Decade Pane in the correct position.
    {
        const xaml.Thickness focusMargin{ -2.0, -2.0, -2.0, -2.0 };
        IFC(spContainer.put_FocusVisualMargin(focusMargin));

        IFC(spContainer.put_UseSystemFocusVisuals(TRUE));
    }

    IFC(CalendarViewGeneratorHost.PrepareItemContainer(pContainer, pItem));

Cleanup:
    return hr;
}

_Check_return_ HRESULT GetIsFirstItemInScope( int index, out bool* pIsFirstItemInScope)
{
    HRESULT hr = S_OK;

    *pIsFirstItemInScope = false;
    if (index == 0)
    {
        *pIsFirstItemInScope = true;
    }
    else
    {
        wf.DateTime date = {};
        int year = 0;

        IFC(GetDateAt(index, &date));
        var pCalendar = GetCalendar();
        IFC(pCalendar.SetDateTime(date));
        IFC(pCalendar.get_Year(&year));
        
        *pIsFirstItemInScope = year % s_decade == 0;

        // "Decade" is a virtual scope which should be less than Era and greater than Year. 
        // So a decade scope should not cross Eras.
        // When this year is the first year of this Era, we still look it 
        // as the first item in the scope.
        if (!*pIsFirstItemInScope)
        {
            int firstYearInThisEra = 0;
            IFC(pCalendar.get_FirstYearInThisEra(&firstYearInThisEra));
            *pIsFirstItemInScope = year == firstYearInThisEra;
        }
    }

Cleanup:
    return hr;
}

_Check_return_ HRESULT GetUnit(out int* pValue)
{
    return GetCalendar().get_Year(pValue);
}

_Check_return_ HRESULT SetUnit( int value)
{
    return GetCalendar().put_Year(value);
}

_Check_return_ HRESULT AddUnits( int value)
{
    return GetCalendar().AddYears(value);
}

_Check_return_ HRESULT AddScopes( int value)
{
    if (value != 0)
    {
        var pCalendar = GetCalendar();
        // for the calendars that don't have multiple Eras, we don't need to validate if add scope
        // will cause Era changes.
        if (!GetOwner().IsMultipleEraCalendar())
        {
            // TODO: boundary check
            IFC_RETURN(pCalendar.AddYears(value * s_decade));
        }
        else
        {            
            // In Windows.Globalization.Calendar, if the result crosses an era when we add a year or a month,
            // the result will be adjust to the first day of the new era (last day if it is a substraction).
            // Now we introduce a virtual scope "Decade" which doesn't exist in Windows.Globalization.Calendar, 
            // so when we add/substract a decade, we should follow the same rule as year/month.

            bool goForward = value > 0;

            if (!goForward)
            {
                value = -value;
            }

            while (value-- > 0)
            {
                int oldEra = 0;
                int oldYear = 0;
                int lastYearInOldEra = 0;
                int newEra = 0;
                int newYear = 0;
                int newMonth = 0;
                int newDay = 0;

                IFC_RETURN(pCalendar.get_Era(&oldEra));
                IFC_RETURN(pCalendar.get_Year(&oldYear));
                IFC_RETURN(pCalendar.get_LastYearInThisEra(&lastYearInOldEra));

                //when going back from year 10-19, we want to show 1-9, instead of 0-9.
                //year 0 will take us to previous era which we don't want to.
                if (oldYear == 10 && !goForward)
                {
                    IFC_RETURN(pCalendar.AddYears(-s_decade+1));
                }
                else
                {
                    IFC_RETURN(pCalendar.AddYears(goForward ? s_decade : -s_decade));
                }

                IFC_RETURN(pCalendar.get_Era(&newEra));

                if (oldEra != newEra)
                {
                    if (goForward)
                    {
                        // Adjust to the first day of this era if the new range starts in a different era.
                        // e.g Showa era ends at 64. For range 50-59, if we go forward a decade, the result should be 60-64.
                        // If we go forward another decade from Showa 60-64, the result should be Heisei 1-10.
                        if (oldYear + 10 > lastYearInOldEra)
                        {
                            IFC_RETURN(pCalendar.get_FirstYearInThisEra(&newYear));
                            IFC_RETURN(pCalendar.put_Year(newYear));
                        }
                    }
                    // Adjust to the last day of this era if the new range starts in a different era.
                    // Similiar as the example above. If we go back a decade from Heisei 1-10, the result should be Showa 60-64.
                    // Go back another decade from Showa 60-64, the result should be Showa 50-59.
                    else if(oldYear < 10)
                    {
                        IFC_RETURN(pCalendar.get_LastYearInThisEra(&newYear));;
                        IFC_RETURN(pCalendar.put_Year(newYear));
                    }

                    IFC_RETURN(pCalendar.get_FirstMonthInThisYear(&newMonth));
                    IFC_RETURN(pCalendar.put_Month(newMonth));
                    IFC_RETURN(pCalendar.get_FirstDayInThisMonth(&newDay));
                    IFC_RETURN(pCalendar.put_Day(newDay));
                }
            }
        }
    }

    return S_OK;
}

// the virtual "Decade" scope doesn't exist in globalization calendar, when we adjust to the first/last
// unit in the decade scope, we need to make sure we don't cross the boundaries (Era).
_Check_return_ HRESULT GetFirstUnitInThisScope(out int* pValue)
{
    int year = 0;
    int firstYearInThisEra = 0;
    *pValue = 0;

    IFC_RETURN(GetCalendar().get_Year(&year));    
    *pValue = year - year % s_decade;

    IFC_RETURN(GetCalendar().get_FirstYearInThisEra(&firstYearInThisEra));
    if (*pValue < firstYearInThisEra)
    {
        *pValue = firstYearInThisEra;
    }
    
    return S_OK;
}

// the virtual "Decade" scope doesn't exist in globalization calendar, when we adjust to the first/last
// unit in the decade scope, we need to make sure we don't cross the boundaries (Era).
_Check_return_ HRESULT GetLastUnitInThisScope(out int* pValue)
{
    int year = 0;
    int lastYearInThisEra = 0;
    *pValue = 0;

    IFC_RETURN(GetCalendar().get_Year(&year));
    *pValue = year - year % s_decade + s_decade - 1;

    IFC_RETURN(GetCalendar().get_LastYearInThisEra(&lastYearInThisEra));
    if (*pValue > lastYearInThisEra)
    {
        *pValue = lastYearInThisEra;
    }

    return S_OK;
}

_Check_return_ HRESULT OnScopeChanged()
{
    wrl_wrappers.HString minYearString;
    wrl_wrappers.HString maxYearString;
    wrl_wrappers.Hconst string seperator = " - ";
    wrl_wrappers.HString tempResult;

    IFC_RETURN(GetCalendar().SetDateTime(m_minDateOfCurrentScope));
    IFC_RETURN(GetCalendar().YearAsString(minYearString.GetAddressOf()));

    IFC_RETURN(GetCalendar().SetDateTime(m_maxDateOfCurrentScope));
    IFC_RETURN(GetCalendar().YearAsString(maxYearString.GetAddressOf()));

    IFC_RETURN(minYearString.Concat(seperator, tempResult));
    IFC_RETURN(tempResult.Concat(maxYearString, m_pHeaderText));  // "YYYY - YYYY"

    return S_OK;
}

_Check_return_ HRESULT GetPossibleItemStrings(_Outptr_ const std.vector<wrl_wrappers.HString>** ppStrings)
{
    *ppStrings = &m_possibleItemStrings;

    // for DecadeView, we couldn't get a list of possible strings because each year has different string.
    // the best we could do is just measure only one item (the string of current year) and assume this is 
    // the biggest item.
    if (m_possibleItemStrings.empty())
    {
        var pCalendar = GetCalendar();

        IFC_RETURN(pCalendar.SetToNow());

        m_possibleItemStrings.reserve(1);

        wrl_wrappers.HString string;
        IFC_RETURN(pCalendar.YearAsString(string.GetAddressOf()));
        m_possibleItemStrings.emplace_back(std.move(string));
    }

    return S_OK;
}

_Check_return_ HRESULT CompareDate( wf.DateTime lhs,  wf.DateTime rhs, out int* pResult)
{
    return GetOwner().GetDateComparer().CompareYear(lhs, rhs, pResult);
}

INT64 GetAverageTicksPerUnit()
{
    // this is being used to estimate the distance between two dates,
    // it doesn't need to be (and it can't be) the exact value
    return CalendarConstants.s_ticksPerDay * 365;
}
