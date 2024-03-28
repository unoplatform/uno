// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Globalization;
using Windows.Globalization.DateTimeFormatting;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Markup;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Tests.Enterprise;
using Uno.UI.RuntimeTests.Helpers;

using static Private.Infrastructure.TestServices;

namespace Private.Infrastructure
{
	internal static class CalendarHelper
	{
		internal static (int era, int year, int month, int day) GetDateFromDateTime(DateTimeOffset datetime)
		{
			var calendar = new Calendar();
			calendar.SetDateTime(datetime);

			return (
				calendar.Era,
				calendar.Year,
				calendar.Month,
				calendar.Day);
		}

		internal static bool CompareDate(DateTimeOffset lhs, DateTimeOffset rhs)
		{
			var ldate = GetDateFromDateTime(lhs);
			var rdate = GetDateFromDateTime(rhs);

			return ldate == rdate;
		}

		internal static bool CompareColor(Color lhs, Color rhs)
		{
			return lhs.A == rhs.A
				   && lhs.R == rhs.R
				   && lhs.G == rhs.G
				   && lhs.B == rhs.B;
		}

		internal class DateCollection : List<DateTimeOffset>
		{
			internal int Size => Count;
		}

		internal class ColorCollection : List<Color>
		{
			internal int Size => Count;
		}

		internal static DependencyObject GetTemplateChild(DependencyObject root, string childName)
		{

			var count = VisualTreeHelper.GetChildrenCount(root);
			for (var i = 0; i < count; i++)
			{
				var child = VisualTreeHelper.GetChild(root, i);
				var childAsFE = child as FrameworkElement;
				if (childAsFE.Name == childName)
				{
					return child;
				}

				var result = GetTemplateChild(child, childName);
				if (result != null)
				{
					return result;
				}
			}

			return null;
		}

		internal static DateTimeOffset ConvertToDateTime(
			int era, int year, int month, int day, int period = 1,
			int hour = 12, int minute = 0, int second = 0, int nanosecond = 0)
		{
			var calendar = new Calendar
			{
				//Era = era,  -- REMOVE FOR https://github.com/unoplatform/uno/issues/6160
				Year = year,
				Month = month,
				Day = day,
				Period = period,
				Hour = hour,
				Minute = minute,
				Second = second,
				Nanosecond = nanosecond
			};
			return calendar.GetDateTime();
		}

		internal static async Task<Grid> CreateTestResources()
		{
			Grid rootPanel = default;
			await TestServices.RunOnUIThread(() =>
			{
				rootPanel = XamlReader.Load(
						"<Grid xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' " +
						"      Width='400' Height='400' VerticalAlignment='Top' HorizontalAlignment='Left' Background='Navy'/>")
					as Grid;

				TestServices.WindowHelper.WindowContent = rootPanel;
			});

			return rootPanel;
		}

		internal static bool CompareDateTime(DateTimeOffset lhs, DateTimeOffset rhs)
		{
			return lhs.UniversalTime() == rhs.UniversalTime();
		}

		internal class CalendarViewHelper
		{
			// public
			internal CalendarViewHelper()
			{
				m_loadedEvent = new Event();
				m_selectedDatesChangedEvent = new Event();
				m_cicEvent = new Event();
				m_addedDates = new DateCollection();
				m_removedDates = new DateCollection();
				m_loadedRegistration = CreateSafeEventRegistration<CalendarView, RoutedEventHandler>("Loaded");
				m_selectedDatesChangedRegistration = CreateSafeEventRegistration<CalendarView, TypedEventHandler<CalendarView, CalendarViewSelectedDatesChangedEventArgs>>("SelectedDatesChanged");
				m_cicRegistration = CreateSafeEventRegistration<CalendarView, TypedEventHandler<CalendarView, CalendarViewDayItemChangingEventArgs>>("CalendarViewDayItemChanging");
			}

			internal async Task<CalendarView> GetCalendarView()
			{
				if (m_cv == null)
				{
					await TestServices.RunOnUIThread(() =>
					{
						if (m_cv == null)
						{
							m_cv = new CalendarView();
							TestServices.VERIFY_IS_NOT_NULL(m_cv);
						}
					});
				}


				return m_cv;
			}

			internal DependencyObject GetTemplateChild(string childName)
			{
				return GetTemplateChild(m_cv, childName);
			}

			internal async Task PrepareLoadedEvent()
			{
				await TestServices.RunOnUIThread(() =>
				{
					m_loadedRegistration.Attach(
						m_cv,
						(object sender, RoutedEventArgs e) =>
						{
							OnLoaded(sender, e);
						});
				});
			}

			internal async Task PrepareSelectedDatesChangedEvent()
			{
				m_addedDates.Clear();
				m_removedDates.Clear();

				await TestServices.RunOnUIThread(() =>
				{
					m_selectedDatesChangedRegistration.Attach(
						m_cv,
						(sender, e) =>
						{
							OnSelectedDatesChanged(sender, e);
						});
				});
			}

			internal void ExpectAddedDate(DateTimeOffset date)
			{
				m_addedDates.Append(date);
			}

			internal void ExpectRemovedDate(DateTimeOffset date)
			{
				m_removedDates.Append(date);
			}

			// note: PrepareCICEvent must be called before item get realized
			// the best position is before CalendarView enters visual tree.
			internal async Task PrepareCICEvent()
			{
				await TestServices.RunOnUIThread(() =>
				{
					m_cicRegistration.Attach(
						m_cv,
							(sender, e) =>
					{
						OnCalendarViewDayItemChanging(sender, e);
					});
				});
			}

			internal async Task WaitForLoaded()
			{
				await m_loadedEvent.WaitForDefault();
				TestServices.VERIFY_IS_TRUE(m_loadedEvent.HasFired());
				m_loadedEvent.Reset();
				m_loadedRegistration.Detach();
			}

			internal async Task WaitForSelectedDatesChanged()
			{
				await m_selectedDatesChangedEvent.WaitForDefault();
				TestServices.VERIFY_IS_TRUE(m_selectedDatesChangedEvent.HasFired());
				m_selectedDatesChangedEvent.Reset();
				m_selectedDatesChangedRegistration.Detach();
			}

			internal void VerifyNoSelectedDatesChanged()
			{
				// we expect no event here, so below statement will timeout and throw WEX.Common.Exception.
				TestServices.VERIFY_THROWS_WINRT<Exception>(
					async () => await m_selectedDatesChangedEvent.WaitFor(TimeSpan.FromMilliseconds(5000), true /* enforceUnderDebugger */),
					"SelectedDatesChanged event should not raise!");
				TestServices.VERIFY_IS_FALSE(m_selectedDatesChangedEvent.HasFired());
				m_selectedDatesChangedRegistration.Detach();
			}

			internal async Task WaitForCICEvent()
			{
				await m_cicEvent.WaitForDefault();
				TestServices.VERIFY_IS_TRUE(m_cicEvent.HasFired());
				m_cicEvent.Reset();
				m_cicRegistration.Detach();
			}

			// private
			void OnLoaded(object sender, RoutedEventArgs e)
			{
				TestServices.LOG_OUTPUT("CalendarViewIntegrationTests: CalendarView Loaded.");
				m_loadedEvent.Set();
			}

			void OnSelectedDatesChanged(object sender,
				CalendarViewSelectedDatesChangedEventArgs e)
			{
				TestServices.VERIFY_ARE_EQUAL(e.AddedDates.Count, m_addedDates.Count);
				TestServices.VERIFY_ARE_EQUAL(e.RemovedDates.Count, m_removedDates.Count);

				for (var i = 0; i < e.AddedDates.Count; ++i)
				{
					TestServices.VERIFY_ARE_EQUAL(e.AddedDates.GetAt(i).UniversalTime(), m_addedDates.GetAt(i).UniversalTime());
				}

				for (var i = 0; i < e.RemovedDates.Count; ++i)
				{
					TestServices.VERIFY_ARE_EQUAL(e.RemovedDates.GetAt(i).UniversalTime(), m_removedDates.GetAt(i).UniversalTime());
				}

				m_selectedDatesChangedEvent.Set();
			}

			void OnCalendarViewDayItemChanging(object sender,
				CalendarViewDayItemChangingEventArgs e)
			{
				// phase 2: set density bar
				// phase 5: blackout
				// phase 7: end cic event

				if (e.Phase == 2)
				{
					ColorCollection colors = new ColorCollection();
					colors.Append(Colors.Red);
					colors.Append(Colors.Green);
					colors.Append(Colors.Blue);
					colors.Append(Colors.Yellow);
					e.Item.SetDensityColors(colors);
				}
				else if (e.Phase == 5)
				{
					e.Item.IsBlackout = true;
				}
				else if (e.Phase == 7)
				{
					m_cicEvent.Set();
				}

				// keep subscribing cic event until phase 7.
				if (e.Phase < 7)
				{
					e.RegisterUpdateCallback(
						(sender, e) =>
						{
							OnCalendarViewDayItemChanging(sender, e);
						});
				}
			}

			DependencyObject GetTemplateChild(DependencyObject root, string childName)
			{
				var count = VisualTreeHelper.GetChildrenCount(root);
				for (var i = 0; i < count; i++)
				{
					var child = VisualTreeHelper.GetChild(root, i);
					var childAsFE = child as FrameworkElement;
					if (childAsFE?.Name == childName)
					{
						return child;
					}

					var result = GetTemplateChild(child, childName);
					if (result != null)
					{
						return result;
					}
				}

				return null;
			}

			// private
			CalendarView m_cv;

			SafeEventRegistration<CalendarView, RoutedEventHandler> m_loadedRegistration;

			SafeEventRegistration<CalendarView, TypedEventHandler<CalendarView, CalendarViewSelectedDatesChangedEventArgs>> m_selectedDatesChangedRegistration;

			SafeEventRegistration<CalendarView, TypedEventHandler<CalendarView, CalendarViewDayItemChangingEventArgs>> m_cicRegistration;

			Event m_loadedEvent;
			Event m_selectedDatesChangedEvent;
			Event m_cicEvent;
			DateCollection m_addedDates;
			DateCollection m_removedDates;
		};


		internal static void DumpDate(DateTimeOffset date, string prefix)
		{
			var calendar = new Calendar();
			calendar.SetDateTime(date);
			TestServices.LOG_OUTPUT("%s: %d/%d/%d (dd/mm/yy)", prefix, calendar.Year, calendar.Month, calendar.Day);
		}

		internal static void VerifyDateTimesAreEqual(DateTimeOffset date1, DateTimeOffset date2)
		{
			if (date1.UniversalTime() != date2.UniversalTime())
			{
				var calendar = new Calendar();
				calendar.SetDateTime(date1);
				TestServices.LOG_OUTPUT("UTC %lld: %d/%d/%d %d:%d:%d %d", date1.UniversalTime(), calendar.Year, calendar.Month,
					calendar.Day, calendar.Hour, calendar.Minute, calendar.Second, calendar.Nanosecond);
				calendar.SetDateTime(date2);
				TestServices.LOG_OUTPUT("UTC %lld: %d/%d/%d %d:%d:%d %d", date2.UniversalTime(), calendar.Year, calendar.Month,
					calendar.Day, calendar.Hour, calendar.Minute, calendar.Second, calendar.Nanosecond);
				TestServices.VERIFY_ARE_EQUAL(date1.UniversalTime(), date2.UniversalTime());
			}
		}

		internal static void CheckFocusedItem()
		{
#if WINAPPSDK
			var item = FocusManager.GetFocusedElement();
#else
			var item = FocusManager.GetFocusedElement((TestServices.WindowHelper.WindowContent as UIElement)?.XamlRoot);
#endif
			TestServices.LOG_OUTPUT("Type of focused item is: %s", item.GetType().FullName);
			var itemAsFE = (FrameworkElement)(item);
			if (itemAsFE is { })
			{
				var point = itemAsFE.TransformToVisual(null).TransformPoint(new Point(0, 0));
				TestServices.LOG_OUTPUT("Focused item position. x %f, y %f, width %f, height %f", point.X, point.Y,
					itemAsFE.ActualWidth, itemAsFE.ActualHeight);
			}

			var itemAsDayItem =
				(CalendarViewDayItem)(item);
			if (itemAsDayItem is { })
			{
				TestServices.LOG_OUTPUT("Focused item is a day item, date is %s",
					DateTimeFormatter.ShortDate.Format(itemAsDayItem.Date)
						);
			}
		}

		internal static bool AreClose(double a, double b, double threshold = 0.1)
		{
			TestServices.LOG_OUTPUT("AreClose? %lf, %lf", a, b);
			return Math.Abs(a - b) <= threshold;
		}

		internal static void ReplaceAccentColorForTesting(CalendarView cv)
		{
			// replace the accent colors to some color ants to make sure test results are not affected by the accent color.
			cv.TodayForeground = new SolidColorBrush(Colors.Red);
			cv.SelectedBorderBrush = new SolidColorBrush(Colors.Red);
			cv.SelectedPressedBorderBrush = new SolidColorBrush(Colors.Green);
			cv.SelectedHoverBorderBrush = new SolidColorBrush(Colors.Blue);
		}


		internal static string[] GetAllSupportedCalendarIdentifiers()
		{
			return new[]
				{
					"PersianCalendar",
					"GregorianCalendar",
					"HebrewCalendar",
					"HijriCalendar",
					"JapaneseCalendar",
					"JulianCalendar",
					"KoreanCalendar",
					"TaiwanCalendar",
					"ThaiCalendar",
					"UmAlQuraCalendar"
				};
		}
	}
}
