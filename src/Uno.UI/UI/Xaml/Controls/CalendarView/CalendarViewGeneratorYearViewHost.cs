// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using Windows.UI.Xaml.Automation;
using DirectUI;
using Uno.Extensions;
using DateTime = Windows.Foundation.WindowsFoundationDateTime;

namespace Windows.UI.Xaml.Controls
{
	partial class CalendarViewGeneratorYearViewHost
	{
		protected override CalendarViewBaseItem GetContainer(
			object pItem,
			DependencyObject pRecycledContainer)
		{
			CalendarViewItem spContainer;

			spContainer = new CalendarViewItem();

			return spContainer;
		}

		internal override void PrepareItemContainer(
			DependencyObject pContainer,
			object pItem)
		{
			DateTime date;
			CalendarViewItem spContainer;

			spContainer = (CalendarViewItem)(pContainer);

			date = (DateTime)pItem;
			GetCalendar().SetDateTime(date);
			spContainer.DateBase = date;

			// maintext
			{
				string mainText;
				string automationName;

				automationName = GetCalendar().MonthAsFullString();

				AutomationProperties.SetName(spContainer as FrameworkElement, automationName);

				mainText = GetCalendar().MonthAsString(
					0  /* idealLength, set to 0 to get the abbreviated string */
					);

				spContainer.UpdateMainText(mainText);
			}

			// label text
			{
				bool isLabelVisible = false;

				isLabelVisible = Owner.IsGroupLabelVisible;

				UpdateLabel(spContainer, isLabelVisible);
			}

			// today state will be updated in CalendarViewGeneratorHost.PrepareItemContainer

			// YearView doesn't have selection state

			// Make a grid effect on YearView.
			// For MonthView, we put a margin on CalendarViewDayItem in the template to achieve the grid effect.
			// For YearView and DecadeView, we can't do the same because there is no template for MonthItem and YearItem
			{
				Thickness margin = new Thickness(
					1.0, 1.0, 1.0, 1.0
				);
				spContainer.Margin = margin;
			}

			//This code enables the focus visuals on the CalendarViewItems in the Year Pane in the correct position.
			{
				Thickness focusMargin = new Thickness(
					-2.0, -2.0, -2.0, -2.0
				);
				spContainer.FocusVisualMargin = focusMargin;

				spContainer.UseSystemFocusVisuals = true;
			}

			base.PrepareItemContainer(pContainer, pItem);
		}

		internal override void UpdateLabel(CalendarViewBaseItem pItem, bool isLabelVisible)
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
				date = pItem.DateBase;
				pCalendar.SetDateTime(date);
				firstMonthOfThisYear = pCalendar.FirstMonthInThisYear;
				month = pCalendar.Month;

				showLabel = firstMonthOfThisYear == month;

				if (showLabel)
				{
					string labelText;
					labelText = pCalendar.YearAsString();
					pItem.UpdateLabelText(labelText);
				}
			}

			pItem.ShowLabelText(showLabel);
			return;
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
				int month = 0;
				int firstMonth = 0;

				date = GetDateAt((uint)index);
				var pCalendar = GetCalendar();
				pCalendar.SetDateTime(date);
				month = pCalendar.Month;
				firstMonth = pCalendar.FirstMonthInThisYear;
				pIsFirstItemInScope = month == firstMonth;
			}

			return pIsFirstItemInScope;
		}

		protected override int GetUnit()
		{
			return GetCalendar().Month;
		}

		protected override void SetUnit(int value)
		{
			GetCalendar().Month = value;
		}
		protected override void AddUnits(int value)
		{
			GetCalendar().AddMonths(value);
		}

		protected override void AddScopes(int value)
		{
			GetCalendar().AddYears(value);
			return;
		}

		protected override int GetFirstUnitInThisScope()
		{
			return GetCalendar().FirstMonthInThisYear;
		}

		protected override int GetLastUnitInThisScope()
		{
			return GetCalendar().LastMonthInThisYear;
		}

		protected override void OnScopeChanged()
		{
			m_pHeaderText = Owner.FormatYearName(m_maxDateOfCurrentScope);
		}

		internal override List<string> GetPossibleItemStrings()
		{
			var ppStrings = m_possibleItemStrings;

			if (m_possibleItemStrings.Empty())
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
					DateTime longestYear = default;
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
							longestYear = pCalendar.GetDateTime();
						}

						pCalendar.AddYears(1);
					}

					global::System.Diagnostics.Debug.Assert(lengthOfLongestYear == 13 || lengthOfLongestYear == 12);
					pCalendar.SetDateTime(longestYear);
					month = pCalendar.FirstMonthInThisYear;
					pCalendar.Month = month;

					//m_possibleItemStrings.reserve(lengthOfLongestYear);

					for (int i = 0; i < lengthOfLongestYear; i++)
					{
						string @string;

						@string = pCalendar.MonthAsString(
							0  /* idealLength, set to 0 to get the abbreviated string */
							);
						m_possibleItemStrings.Add(@string);
						pCalendar.AddMonths(1);
					}
				}
			}

			return m_possibleItemStrings;
		}

		internal override int CompareDate(DateTime lhs, DateTime rhs)
		{
			return Owner.DateComparer.CompareMonth(lhs, rhs);
		}

		protected override long GetAverageTicksPerUnit()
		{
			// this is being used to estimate the distance between two dates,
			// it doesn't need to be (and it can't be) the exact value
			return CalendarConstants.s_ticksPerDay * 365 / 12;
		}
	}
}
