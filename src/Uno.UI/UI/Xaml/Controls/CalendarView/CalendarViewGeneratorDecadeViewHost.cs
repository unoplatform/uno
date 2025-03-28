// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using DirectUI;
using Uno.Extensions;
using DateTime = Windows.Foundation.WindowsFoundationDateTime;

namespace Windows.UI.Xaml.Controls
{
	partial class CalendarViewGeneratorDecadeViewHost
	{
		protected override CalendarViewBaseItem GetContainer(
			object pItem,
			DependencyObject pRecycledContainer)
		{
			CalendarViewItem spItem;

			spItem = new CalendarViewItem();

			return spItem;
		}

		internal override void PrepareItemContainer(
			DependencyObject pContainer,
			object pItem)
		{
			DateTime date;
			CalendarViewItem spContainer;

			date = (DateTime)pItem;
			spContainer = (CalendarViewItem)(pContainer);
			spContainer.DateBase = date;
			GetCalendar().SetDateTime(date);

			// main text
			{
				string mainText;

				mainText = GetCalendar().YearAsString();

				spContainer.UpdateMainText(mainText);
			}

			// today state will be updated in CalendarViewGeneratorHost.PrepareItemContainer

			// DecadeView doesn't have label, selection state

			// Make a grid effect on DecadeView.
			// For MonthView, we put a margin on CalendarViewDayItem in the template to achieve the grid effect.
			// For YearView and DecadeView, we can't do the same because there is no template for MonthItem and YearItem
			{
				Thickness margin = new Thickness(
					1.0, 1.0, 1.0, 1.0
				);
				spContainer.Margin = margin;
			}

			//This code enables the focus visuals on the CalendarViewItems in the Decade Pane in the correct position.
			{
				Thickness focusMargin = new Thickness(
					-2.0, -2.0, -2.0, -2.0
				);
				spContainer.FocusVisualMargin = focusMargin;

				spContainer.UseSystemFocusVisuals = true;
			}

			base.PrepareItemContainer(pContainer, pItem);
		}

		internal override bool GetIsFirstItemInScope(int index)
		{
			var pIsFirstItemInScope = false;
			if (index == 0)
			{
				pIsFirstItemInScope = true;
			}
			else
			{
				DateTime date = default;
				int year = 0;

				date = GetDateAt((uint)index);
				var pCalendar = GetCalendar();
				pCalendar.SetDateTime(date);
				year = pCalendar.Year;

				pIsFirstItemInScope = year % s_decade == 0;

				// "Decade" is a virtual scope which should be less than Era and greater than Year. 
				// So a decade scope should not cross Eras.
				// When this year is the first year of this Era, we still look it 
				// as the first item in the scope.
				if (!pIsFirstItemInScope)
				{
					int firstYearInThisEra = 0;
					firstYearInThisEra = pCalendar.FirstYearInThisEra;
					pIsFirstItemInScope = year == firstYearInThisEra;
				}
			}

			return pIsFirstItemInScope;
		}

		protected override int GetUnit()
		{
			var pValue = GetCalendar().Year;
			return pValue;
		}

		protected override void SetUnit(int value)
		{
			GetCalendar().Year = value;
		}

		protected override void AddUnits(int value)
		{
			GetCalendar().AddYears(value);
		}

		protected override void AddScopes(int value)
		{
			if (value != 0)
			{
				var pCalendar = GetCalendar();
				// for the calendars that don't have multiple Eras, we don't need to validate if add scope
				// will cause Era changes.
				if (!Owner.IsMultipleEraCalendar)
				{
					// TODO: boundary check
					pCalendar.AddYears(value * s_decade);
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

						oldEra = pCalendar.Era;
						oldYear = pCalendar.Year;
						lastYearInOldEra = pCalendar.LastYearInThisEra;

						//when going back from year 10-19, we want to show 1-9, instead of 0-9.
						//year 0 will take us to previous era which we don't want to.
						if (oldYear == 10 && !goForward)
						{
							pCalendar.AddYears(-s_decade + 1);
						}
						else
						{
							pCalendar.AddYears(goForward ? s_decade : -s_decade);
						}

						newEra = pCalendar.Era;

						if (oldEra != newEra)
						{
							if (goForward)
							{
								// Adjust to the first day of this era if the new range starts in a different era.
								// e.g Showa era ends at 64. For range 50-59, if we go forward a decade, the result should be 60-64.
								// If we go forward another decade from Showa 60-64, the result should be Heisei 1-10.
								if (oldYear + 10 > lastYearInOldEra)
								{
									newYear = pCalendar.FirstYearInThisEra;
									pCalendar.Year = newYear;
								}
							}
							// Adjust to the last day of this era if the new range starts in a different era.
							// Similiar as the example above. If we go back a decade from Heisei 1-10, the result should be Showa 60-64.
							// Go back another decade from Showa 60-64, the result should be Showa 50-59.
							else if (oldYear < 10)
							{
								newYear = pCalendar.LastYearInThisEra;
								;
								pCalendar.Year = newYear;
							}

							newMonth = pCalendar.FirstMonthInThisYear;
							pCalendar.Month = newMonth;
							newDay = pCalendar.FirstDayInThisMonth;
							pCalendar.Day = newDay;
						}
					}
				}
			}

			return;
		}

		// the virtual "Decade" scope doesn't exist in globalization calendar, when we adjust to the first/last
		// unit in the decade scope, we need to make sure we don't cross the boundaries (Era).
		protected override int GetFirstUnitInThisScope()
		{
			int year = 0;
			int firstYearInThisEra = 0;
			var pValue = 0;

			year = GetCalendar().Year;
			pValue = year - year % s_decade;

			firstYearInThisEra = GetCalendar().FirstYearInThisEra;
			if (pValue < firstYearInThisEra)
			{
				pValue = firstYearInThisEra;
			}

			return pValue;
		}

		// the virtual "Decade" scope doesn't exist in globalization calendar, when we adjust to the first/last
		// unit in the decade scope, we need to make sure we don't cross the boundaries (Era).
		protected override int GetLastUnitInThisScope()
		{
			int year = 0;
			int lastYearInThisEra = 0;
			var pValue = 0;

			year = GetCalendar().Year;
			pValue = year - year % s_decade + s_decade - 1;

			lastYearInThisEra = GetCalendar().LastYearInThisEra;
			if (pValue > lastYearInThisEra)
			{
				pValue = lastYearInThisEra;
			}

			return pValue;
		}

		protected override void OnScopeChanged()
		{
			string minYearString;
			string maxYearString;
			string seperator = " - ";
			string tempResult;

			GetCalendar().SetDateTime(m_minDateOfCurrentScope);
			minYearString = GetCalendar().YearAsString();

			GetCalendar().SetDateTime(m_maxDateOfCurrentScope);
			maxYearString = GetCalendar().YearAsString();

			tempResult = string.Concat(minYearString, seperator);
			m_pHeaderText = string.Concat(tempResult, maxYearString); // "YYYY - YYYY"
		}

		internal override List<string> GetPossibleItemStrings()
		{
			var ppStrings = m_possibleItemStrings;

			// for DecadeView, we couldn't get a list of possible strings because each year has different string.
			// the best we could do is just measure only one item (the string of current year) and assume this is 
			// the biggest item.
			if (m_possibleItemStrings.Empty())
			{
				var pCalendar = GetCalendar();

				pCalendar.SetToNow();

				//m_possibleItemStrings.reserve(1);

				var str = pCalendar.YearAsString();
				m_possibleItemStrings.Add(str);
			}

			return ppStrings;
		}

		internal override int CompareDate(DateTime lhs, DateTime rhs)
		{
			return Owner.DateComparer.CompareYear(lhs, rhs);
		}

		protected override long GetAverageTicksPerUnit()
		{
			// this is being used to estimate the distance between two dates,
			// it doesn't need to be (and it can't be) the exact value
			return CalendarConstants.s_ticksPerDay * 365;
		}
	}
}
