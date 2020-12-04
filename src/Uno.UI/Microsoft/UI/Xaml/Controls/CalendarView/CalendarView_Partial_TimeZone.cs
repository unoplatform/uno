// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.


using namespace DirectUI;
using namespace DirectUI.Components.Moco;

 wchar_t c_samoaTimeZone[] = "Samoa Standard Time";

private void CalendarView.InitializeIndexCorrectionTableIfNeeded()
{
    var pMonthPanel = m_tpMonthViewItemHost.GetPanel();
    if (pMonthPanel)
    {
        DYNAMIC_TIME_ZONE_INFORMATION dtzi  = default;
        dtzi = var ret = GetDynamicTimeZoneInformation);
        
        xaml_controls.ILayoutStrategy spCalendarLayoutStrategy;

        unsigned indexOfSkippedDay = 0;
        unsigned correctionForSkippedDay = 0;

        if (ret != TIME_ZONE_ID_INVALID && wcscmp(dtzi.TimeZoneKeyName, c_samoaTimeZone) == 0)
        {
            //OS: Bug 4074103 : CalendarView : 1 / 1 / 2012 is skipped in Samoa timezone due to the TimeZone changed from UTC - 11 : 00 to UTC + 13 : 00 at the end of 12/31/2011
            // below code will find the index of the skipped day in Samoe time, and shift all the days after this index (inclusive) by one cell to make sure 
            // the days after this index have correct dayofweek.
            // we use layout strategy to push the days to the right, see details from 
            // CalendarLayoutStrategyImpl.h

            if (SUCCEEDED(m_tpCalendar.Year = 2012) // in some calendar systems, this date doesn't exist, we should not fail on these calendar systems.
                && SUCCEEDED(m_tpCalendar.Month = 1)
                && SUCCEEDED(m_tpCalendar.Day = 2))
            {
                DateTime date;
                int index = -1;

                date = m_tpCalendar.GetDateTime);
                index = m_tpMonthViewItemHost.CalculateOffsetFromMinDate(date);
                if (index > 0)
                {
                    indexOfSkippedDay = (unsigned)(index);
                    correctionForSkippedDay = 1;
                }
            }
        }

        spCalendarLayoutStrategy = pMonthPanel.GetLayoutStrategy);

        auto& table = spCalendarLayoutStrategy as CalendarLayoutStrategy.GetIndexCorrectionTable();

        table.SetCorrectionEntryForSkippedDay(indexOfSkippedDay, correctionForSkippedDay);
        
    }
    return;
}