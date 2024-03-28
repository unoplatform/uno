// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Globalization;
using DateTime = Windows.Foundation.WindowsFoundationDateTime;

namespace Windows.UI.Xaml.Controls
{
	partial class CalendarView
	{
		const string c_samoaTimeZone = "Samoa Standard Time";

		private const string TIME_ZONE_ID_INVALID = null; //((DWORD)0xFFFFFFFF)

		private void InitializeIndexCorrectionTableIfNeeded()
		{
			var pMonthPanel = m_tpMonthViewItemHost.Panel;
			if (pMonthPanel is { })
			{
				TimeZoneInfo dtzi = default;
				var ret = dtzi = TimeZoneInfo.Local; //GetDynamicTimeZoneInformation();

				ILayoutStrategy spCalendarLayoutStrategy;

				uint indexOfSkippedDay = 0;
				uint correctionForSkippedDay = 0;

				if (ret?.Id != TIME_ZONE_ID_INVALID && StringComparer.InvariantCultureIgnoreCase.Compare(dtzi.StandardName, c_samoaTimeZone) == 0)
				{
					//OS: Bug 4074103 : CalendarView : 1 / 1 / 2012 is skipped in Samoa timezone due to the TimeZone changed from UTC - 11 : 00 to UTC + 13 : 00 at the end of 12/31/2011
					// below code will find the index of the skipped day in Samoe time, and shift all the days after this index (inclusive) by one cell to make sure 
					// the days after this index have correct dayofweek.
					// we use layout strategy to push the days to the right, see details from 
					// CalendarLayoutStrategyImpl.h

					var SUCCEEDED = false;
					try
					{
						m_tpCalendar.Year = 2012; // in some calendar systems, this date doesn't exist, we should not fail on these calendar systems.
						m_tpCalendar.Month = 1;
						m_tpCalendar.Day = 2;
						SUCCEEDED = true;
					}
					catch { }

					if (SUCCEEDED)
					{
						DateTime date;
						int index = -1;

						date = m_tpCalendar.GetDateTime();
						index = m_tpMonthViewItemHost.CalculateOffsetFromMinDate(date);
						if (index > 0)
						{
							indexOfSkippedDay = (uint)(index);
							correctionForSkippedDay = 1;
						}
					}
				}

				spCalendarLayoutStrategy = pMonthPanel.LayoutStrategy;

				var table = (spCalendarLayoutStrategy as CalendarLayoutStrategy).GetIndexCorrectionTable();

				table.SetCorrectionEntryForSkippedDay((int)indexOfSkippedDay, (int)correctionForSkippedDay);

			}

			return;
		}
	}
}
