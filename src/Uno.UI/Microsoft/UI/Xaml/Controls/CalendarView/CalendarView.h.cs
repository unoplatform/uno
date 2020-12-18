// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Generic;
using Windows.Globalization;
using Windows.UI.Text;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using DirectUI;
using DateTime = System.DateTimeOffset;

#pragma once

namespace Windows.UI.Xaml.Controls
{
	partial class CalendarView
	{
		// Public properties

		internal Brush m_pFocusBorderBrush;
		internal Brush m_pSelectedHoverBorderBrush;
		internal Brush m_pSelectedPressedBorderBrush;
		internal Brush m_pSelectedBorderBrush;
		internal Brush m_pHoverBorderBrush;
		internal Brush m_pPressedBorderBrush;
		internal Brush m_pCalendarItemBorderBrush;

		internal Brush m_pOutOfScopeBackground;
		internal Brush m_pCalendarItemBackground;

		internal Brush m_pPressedForeground;
		internal Brush m_pTodayForeground;
		internal Brush m_pBlackoutForeground;
		internal Brush m_pSelectedForeground;
		internal Brush m_pOutOfScopeForeground;
		internal Brush m_pCalendarItemForeground;

		internal FontFamily m_pDayItemFontFamily;
		internal float m_dayItemFontSize;
		internal FontStyle m_dayItemFontStyle;
		internal FontWeight m_dayItemFontWeight;
		internal FontWeight m_todayFontWeight;

		internal FontFamily m_pFirstOfMonthLabelFontFamily;
		internal float m_firstOfMonthLabelFontSize;
		internal FontStyle m_firstOfMonthLabelFontStyle;
		internal FontWeight m_firstOfMonthLabelFontWeight;

		internal FontFamily m_pMonthYearItemFontFamily;
		internal float m_monthYearItemFontSize;
		internal FontStyle m_monthYearItemFontStyle;
		internal FontWeight m_monthYearItemFontWeight;

		internal FontFamily m_pFirstOfYearDecadeLabelFontFamily;
		internal float m_firstOfYearDecadeLabelFontSize;
		internal FontStyle m_firstOfYearDecadeLabelFontStyle;
		internal FontWeight m_firstOfYearDecadeLabelFontWeight;


		internal HorizontalAlignment m_horizontalDayItemAlignment;
		internal VerticalAlignment m_verticalDayItemAlignment;

		internal HorizontalAlignment m_horizontalFirstOfMonthLabelAlignment;
		internal VerticalAlignment m_verticalFirstOfMonthLabelAlignment;

		internal Thickness m_calendarItemBorderThickness;

		// Below brushes are hardcoded and internal, we can make them public when needed.
		internal Brush m_pDisabledForeground;
		internal Brush m_pTodaySelectedInnerBorderBrush;
		internal Brush m_pTodayHoverBorderBrush;
		internal Brush m_pTodayPressedBorderBrush;
		internal Brush m_pTodayBackground;
		internal Brush m_pTodayBlackoutBackground;


		private IDictionary<uint, DependencyObject> m_colorToBrushMap;

		// hold the resource extension to keep the bindings for the internal properties.
		//private ThemeResourceExtension[] m_internalThemeResourceExtensions = new ThemeResourceExtension[6];


		internal DateTime Today { get { return m_today; } }

		internal Calendar Calendar { get { return m_tpCalendar; } }

		internal DateComparer DateComparer { get { return m_dateComparer; } }

		internal void SetKeyDownEventArgsFromCalendarItem(KeyRoutedEventArgs pArgs)
		{
			m_wrKeyDownEventArgsFromCalendarItem.SetTarget(pArgs);
		}

		internal bool IsMultipleEraCalendar {get{ return m_isMultipleEraCalendar; }}
	}
}
