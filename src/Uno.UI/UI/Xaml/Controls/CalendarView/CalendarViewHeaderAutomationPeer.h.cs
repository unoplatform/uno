// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Windows.UI.Xaml.Automation.Peers;
using DateTime = Windows.Foundation.WindowsFoundationDateTime;

namespace Windows.UI.Xaml.Controls
{
	partial class CalendarViewHeaderAutomationPeer : AutomationPeer
	{
		/// <inheritdoc />
		protected override string GetNameCore()
			=> m_name;

		internal void Initialize(
			 string name,
			 int month,
			 int year,
			 CalendarViewDisplayMode mode)
		{
			m_name = name;
			m_month = month;
			m_year = year;
			m_mode = mode;
		}

		internal int GetMonth() { return m_month; }
		internal int GetYear() { return m_year; }
		internal CalendarViewDisplayMode GetMode() { return m_mode; }

		private string m_name;
		private int m_month = -1;
		private int m_year = -1;
		private CalendarViewDisplayMode m_mode = CalendarViewDisplayMode.Month;
	};
}
