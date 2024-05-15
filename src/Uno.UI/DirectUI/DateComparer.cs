// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using Windows.Globalization;
using DateTime = Windows.Foundation.WindowsFoundationDateTime;

namespace DirectUI
{
	internal class DateComparer
	{
		private Calendar m_spCalendar;

		public Func<DateTimeOffset, DateTimeOffset, bool> LessThanComparer
		{
			get
			{
				return (DateTimeOffset lhs, DateTimeOffset rhs) =>
				{
					return LessThan(lhs, rhs);
				};
			}
		}

		public Func<DateTimeOffset, DateTimeOffset, bool> AreEquivalentComparer
		{
			get
			{
				return (DateTimeOffset lhs, DateTimeOffset rhs) =>
				{
					return AreEquivalent(lhs, rhs);
				};
			}
		}

		// check if the whole date parts of two DateTime values are same.
		internal bool AreEquivalent(DateTime lhs, DateTime rhs)
		{
			return CompareDay(lhs, rhs) == 0;
		}

		// check if the date part of lhs is less than the date part of rhs.
		internal bool LessThan(DateTime lhs, DateTime rhs)
		{
			return CompareDay(lhs, rhs) == -1;
		}


		public void SetCalendarForComparison(Calendar pCalendar)
		{
			m_spCalendar = pCalendar.Clone();
		}

		//public int CompareDay(DateTime lhs, DateTime rhs)
		//{
		//	int result = 0;

		//	result = CompareDay(lhs, rhs);

		//	Cleanup:
		//	if (/*FAILED */ (hr))
		//	{
		//		THROW_HR(hr);
		//	}
		//	return result;
		//}

		internal int CompareDay(
			DateTime lhs,
			DateTime rhs)
		{
			return CompareDate(lhs, rhs, CalendarConstants.s_maxTicksPerDay, c => c.Day);
		}

		internal int CompareMonth(
			DateTime lhs,
			DateTime rhs)
		{
			return CompareDate(lhs, rhs, CalendarConstants.s_maxTicksPerMonth, c => c.Month);
		}

		internal int CompareYear(
			DateTime lhs,
			DateTime rhs)
		{
			return CompareDate(lhs, rhs, CalendarConstants.s_maxTicksPerYear, c => c.Year);
		}


		// compare two datetime values, when the difference between two UTC values is greater 
		// than given threshold they can be compared directly by the UTC values.
		// otherwise we need to have globalization calendar to help us to determine, basically 
		// we need the corresponding function from calendar (get_Day, get_Month or get_Year).

		private int CompareDate(
			DateTime lhs,
			DateTime rhs,
			long threshold,
			Func<Calendar, int> getUnit)
		{
			int sign = 1;
			var pResult = 1;

			global::System.Diagnostics.Debug.Assert(m_spCalendar is { });

			long delta = lhs.UniversalTime - rhs.UniversalTime;
			if (delta < 0)
			{
				delta = -delta;
				sign = -1;
			}

			// comparing the date parts of two datetime is expensive. only compare them when they could in a same day/month/year.
			if (delta < threshold)
			{
				int left = 0;
				int right = 0;

				m_spCalendar.SetDateTime(lhs);
				left = getUnit(m_spCalendar);

				m_spCalendar.SetDateTime(rhs);
				right = getUnit(m_spCalendar);

				if (left == right)
				{
					pResult = 0;
				}
			}

			pResult *= sign;

			return pResult;
		}
	}
}
