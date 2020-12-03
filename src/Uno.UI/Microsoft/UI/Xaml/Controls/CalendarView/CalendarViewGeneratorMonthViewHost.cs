// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#include "precomp.h"
#include "CalendarViewGeneratorMonthViewHost.h"
#include "CalendarView.g.h"
#include "CalendarViewItem.g.h"
#include "CalendarViewDayItem_Partial.h"
#include "CalendarViewDayItemChangingEventArgs.g.h"
#include "DateComparer.h"
#include "BuildTreeService.g.h"
#include "BudgetManager.g.h"

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

_Check_return_ HRESULT GetContainer(
     IInspectable* pItem,
     xaml.IDependencyObject* pRecycledContainer,
    _Outptr_ CalendarViewBaseItem** ppContainer)
{
    HRESULT hr = S_OK;
    ctl.ComPtr<CalendarViewDayItem> spContainer;

    IFC(ctl.new CalendarViewDayItem(&spContainer));

    IFC(spContainer.CopyTo(ppContainer));

Cleanup:
    return hr;
}

_Check_return_ IFACEMETHODIMP PrepareItemContainer(
     xaml.IDependencyObject* pContainer,
     IInspectable* pItem)
{
    HRESULT hr = S_OK;
    wf.DateTime date;
    ctl.ComPtr<CalendarViewDayItem> spContainer;

    spContainer = (CalendarViewDayItem*)(pContainer);

    // first let's check if this container is already in the tobecleared queue
    // the container might be already marked as to be cleared but not cleared yet, 
    // if we pick up this container we don't need to clear it up.
    IFC(RemoveToBeClearedContainer(spContainer.Get()));

    // now prepare the container    

    IFC(ctl.do_get_value(date, pItem));
    IFC(GetCalendar().SetDateTime(date));

    IFC(spContainer.put_Date(date));

    // main text
    {
        wrl_wrappers.HString mainText;

        IFC(GetCalendar().DayAsString(mainText.GetAddressOf()));
        IFC(spContainer.UpdateMainText(mainText.Get()));
    }

    // label text
    {
        BOOLEAN isLabelVisible = FALSE;

        IFC(GetOwner().get_IsGroupLabelVisible(&isLabelVisible));

        IFC(UpdateLabel(spContainer.Get(), !!isLabelVisible));
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
        IFC(spContainer.put_IsBlackout(FALSE));

        // set selection state.
        bool isSelected = false;
        IFC(GetOwner().IsSelected(date, &isSelected));
        IFC(spContainer.SetIsSelected(isSelected));
    }

    // clear the density bar as well.
    IFC(spContainer.SetDensityColors(null));

    // apply style to CalendarViewDayItem if any.
    {
        ctl.ComPtr<IStyle> spStyle;

        IFC(GetOwner().get_CalendarViewDayItemStyle(&spStyle));
        CalendarView.SetDayItemStyle(spContainer.Get(), spStyle.Get());
    }

    IFC(CalendarViewGeneratorHost.PrepareItemContainer(pContainer, pItem));

Cleanup:
    return hr;
}

_Check_return_ HRESULT UpdateLabel( CalendarViewBaseItem* pItem,  bool isLabelVisible)
{
    bool showLabel = false;
    if (isLabelVisible)
    {
        wf.DateTime date;
        var pCalendar = GetCalendar();
        int day = 0;
        int firstDayInThisMonth = 0;

        // TODO: consider caching the firstday flag because we also need this information when determining snap points 
        // (however Decadeview doesn't need this for Label).
        IFC_RETURN(pItem.GetDate(&date));
        IFC_RETURN(pCalendar.SetDateTime(date));
        IFC_RETURN(pCalendar.get_FirstDayInThisMonth(&firstDayInThisMonth));
        IFC_RETURN(pCalendar.get_Day(&day));
        showLabel = firstDayInThisMonth == day;

        if (showLabel)
        {
            wrl_wrappers.HString labelText;
            IFC_RETURN(pCalendar.MonthAsString(
                0, /*idealLength, set to 0 to get the abbreviated string*/
                labelText.GetAddressOf()));
            IFC_RETURN(pItem.UpdateLabelText(labelText.Get()));
        }
    }
    IFC_RETURN(pItem.ShowLabelText(showLabel));
    return S_OK;
}

// reset CIC event if the container is being cleared.
_Check_return_ IFACEMETHODIMP ClearContainerForItem(
     xaml.IDependencyObject* pContainer,
     IInspectable* pItem)
{
    HRESULT hr = S_OK;

    ctl.ComPtr<CalendarViewDayItem> spContainer = (CalendarViewDayItem*)(pContainer);

    // There is much perf involved with doing a clear, and usually it is going to be
    // a waste of time since we are going to immediately follow up with a prepare. 
    // Perf traces have found this to be about 8 to 12% during a full virtualization pass (!!)
    // Although with other optimizations we would expect that to go down, it is unlikely to go 
    // down to 0. Therefore we are deferring the impl work here to later.
    // We have decided to do this only for the new panels.

    // also, do not defer items that are uielement. They need to be cleared straight away so that
    // they can be messed with again.
    IFC(m_toBeClearedContainers.Append(spContainer.Get()));

    // note that if we are being cleared, we are not going to be in the 
    // visible index, or the caches. And thus we will never be called in the 
    // prepare queuing part.

    if (!m_isRegisteredForCallbacks)
    {
        ctl.ComPtr<BuildTreeService> spBuildTree;
        IFC(DXamlCore.GetCurrent().GetBuildTreeService(spBuildTree));
        IFC(spBuildTree.RegisterWork(this));
    }
    ASSERT(m_isRegisteredForCallbacks);


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
        int day = 0;
        int firstDay = 0;

        IFC(GetDateAt(index, &date));
        var pCalendar = GetCalendar();
        IFC(pCalendar.SetDateTime(date));
        IFC(pCalendar.get_Day(&day));
        IFC(pCalendar.get_FirstDayInThisMonth(&firstDay));
        *pIsFirstItemInScope = day == firstDay;
    }

Cleanup:
    return hr;
}

_Check_return_ HRESULT GetUnit(out int* pValue)
{
    return GetCalendar().get_Day(pValue);
}

_Check_return_ HRESULT SetUnit( int value)
{
    return GetCalendar().put_Day(value);
}

_Check_return_ HRESULT AddUnits( int value)
{
    return GetCalendar().AddDays(value);
}

_Check_return_ HRESULT AddScopes( int value)
{
    return GetCalendar().AddMonths(value);
}

_Check_return_ HRESULT GetFirstUnitInThisScope(out int* pValue)
{
    return GetCalendar().get_FirstDayInThisMonth(pValue);
}
_Check_return_ HRESULT GetLastUnitInThisScope(out int* pValue)
{
    return GetCalendar().get_LastDayInThisMonth(pValue);
}

_Check_return_ HRESULT OnScopeChanged()
{
    return GetOwner().FormatMonthYearName(m_minDateOfCurrentScope, m_pHeaderText.ReleaseAndGetAddressOf());
}

_Check_return_ HRESULT GetPossibleItemStrings(_Outptr_ const std.vector<wrl_wrappers.HString>** ppStrings)
{
    *ppStrings = &m_possibleItemStrings;

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
            const int MaxNumberOfMonthsToBeChecked = 3;
            wf.DateTime longestMonth;
            int lengthOfLongestMonth = 0;
            int numberOfDays = 0;
            int day = 0;

            var pCalendar = GetCalendar();

            IFC_RETURN(pCalendar.SetToMin());
            for (int i = 0; i < MaxNumberOfMonthsToBeChecked; i++)
            {
                IFC_RETURN(pCalendar.get_NumberOfDaysInThisMonth(&numberOfDays));
                if (numberOfDays > lengthOfLongestMonth)
                {
                    lengthOfLongestMonth = numberOfDays;
                    IFC_RETURN(pCalendar.GetDateTime(&longestMonth));
                }
                IFC_RETURN(pCalendar.AddMonths(1));
            }

            ASSERT(lengthOfLongestMonth == 30 || lengthOfLongestMonth == 31);
            IFC_RETURN(pCalendar.SetDateTime(longestMonth));
            IFC_RETURN(pCalendar.get_FirstDayInThisMonth(&day));
            IFC_RETURN(pCalendar.put_Day(day));

            m_possibleItemStrings.reserve(lengthOfLongestMonth);

            for (int i = 0; i < lengthOfLongestMonth; i++)
            {
                wrl_wrappers.HString string;
                IFC_RETURN(pCalendar.DayAsString(string.GetAddressOf()));
                m_possibleItemStrings.emplace_back(std.move(string));
                IFC_RETURN(pCalendar.AddDays(1));
            }
        }
    }

    return S_OK;
}

_Check_return_ HRESULT CompareDate( wf.DateTime lhs,  wf.DateTime rhs, out int* pResult)
{
    return GetOwner().GetDateComparer().CompareDay(lhs, rhs, pResult);
}

INT64 GetAverageTicksPerUnit()
{
    // this is being used to estimate the distance between two dates,
    // it doesn't need to be (and it can't be) the exact value
    return CalendarConstants.s_ticksPerDay;
}
