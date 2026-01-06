// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#pragma warning disable CS0168 // Disable TestCleanupWrapper warnings
#pragma warning disable 168 // for cleanup imported member

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Tests.Common;
using AwesomeAssertions.Execution;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using Uno.Disposables;
using Uno.UI.RuntimeTests.Helpers;
using Uno.UI.RuntimeTests.MUX.Helpers;
using CalendarView = Microsoft.UI.Xaml.Controls.CalendarView;

using static Private.Infrastructure.TestServices;
using static Private.Infrastructure.CalendarHelper;
using Uno.UI.RuntimeTests;

namespace Microsoft.UI.Xaml.Tests.Enterprise
{
	[TestClass]
	public partial class CalendarViewIntegrationTests : BaseDxamlTestClass
	{

		private static string GetCurrentTimeZoneId()
		{
			return TimeZoneInfo.Local.Id;
		}

		[ClassInitialize]
		public static void ClassSetup()
		{
			CommonTestSetupHelper.CommonTestClassSetup();
		}

		[ClassCleanup]
		public static void TestCleanup()
		{
			TestServices.WindowHelper.VerifyTestCleanup();
		}

		//
		// Test Cases
		//
		[TestMethod]
		public async Task VerifyMultipleEraCalendar()
		{
			TestCleanupWrapper cleanup;

			Grid rootPanel = null;

			var helper = new CalendarHelper.CalendarViewHelper();

			CalendarView cv = await helper.GetCalendarView();

			rootPanel = await CalendarHelper.CreateTestResources();

			// load into visual tree
			await RunOnUIThread(() =>
			{
				/*
				JapaneseCalendar      Era 1: 01/01/1868 ~ 29/07/1912
				JapaneseCalendar      Era 2: 30/07/1912 ~ 24/12/1926
				JapaneseCalendar      Era 3: 25/12/1926 ~ 07/01/1989
				JapaneseCalendar      Era 4: 08/01/1989 ~ 31/12/9999
				*/

				// below date range will cross Era 3 and Era 4.
				cv.CalendarIdentifier = "JapaneseCalendar";
				cv.MinDate = ConvertToDateTime(1, 1988, 1, 1);
				cv.MaxDate = ConvertToDateTime(1, 2000, 1, 1);
				cv.DisplayMode = CalendarViewDisplayMode.Decade;
				rootPanel.Children.Add(cv);
			});

			// no crash!
			await WindowHelper.WaitForIdle();
		}

		[TestMethod]
		public async Task ValidateDefaultPropertyValues()
		{
			await RunOnUIThread(() =>
			{
				using var _ = new AssertionScope();

				var cv = new CalendarView();
				VERIFY_ARE_EQUAL(cv.NumberOfWeeksInView, 6);
				VERIFY_ARE_EQUAL(cv.FirstDayOfWeek, global::Windows.Globalization.DayOfWeek.Sunday);
				VERIFY_ARE_EQUAL(cv.SelectionMode, CalendarViewSelectionMode.Single);

				VERIFY_ARE_EQUAL(cv.DisplayMode, CalendarViewDisplayMode.Month);
				VERIFY_ARE_EQUAL(cv.IsTodayHighlighted, true);
				VERIFY_ARE_EQUAL(cv.IsOutOfScopeEnabled, true);
				VERIFY_ARE_EQUAL(cv.IsGroupLabelVisible, false);
				VERIFY_ARE_EQUAL(cv.DayItemFontStyle, global::Windows.UI.Text.FontStyle.Normal);
				VERIFY_ARE_EQUAL(cv.DayItemFontWeight.Weight, Microsoft.UI.Text.FontWeights.Normal.Weight);
				VERIFY_ARE_EQUAL(cv.TodayFontWeight.Weight, Microsoft.UI.Text.FontWeights.SemiBold.Weight);
				VERIFY_ARE_EQUAL(cv.FirstOfMonthLabelFontSize, 12.0);
				VERIFY_ARE_EQUAL(cv.FirstOfMonthLabelFontStyle, global::Windows.UI.Text.FontStyle.Normal);
				VERIFY_ARE_EQUAL(cv.FirstOfMonthLabelFontWeight.Weight, Microsoft.UI.Text.FontWeights.Normal.Weight);
				VERIFY_ARE_EQUAL(cv.MonthYearItemFontSize, 20.0);
				VERIFY_ARE_EQUAL(cv.MonthYearItemFontStyle, global::Windows.UI.Text.FontStyle.Normal);
				VERIFY_ARE_EQUAL(cv.MonthYearItemFontWeight.Weight, Microsoft.UI.Text.FontWeights.Normal.Weight);
				VERIFY_ARE_EQUAL(cv.FirstOfYearDecadeLabelFontSize, 12.0);
				VERIFY_ARE_EQUAL(cv.FirstOfYearDecadeLabelFontStyle, global::Windows.UI.Text.FontStyle.Normal);
				VERIFY_ARE_EQUAL(cv.FirstOfYearDecadeLabelFontWeight.Weight, Microsoft.UI.Text.FontWeights.Normal.Weight);

				global::Windows.Globalization.Calendar calendar = new global::Windows.Globalization.Calendar();
				calendar.SetToNow();

				calendar.AddYears(-100);

				calendar.Month = calendar.FirstMonthInThisYear;
				calendar.Day = calendar.FirstDayInThisMonth;

				var minDate = calendar.GetDateTime();

				calendar.AddYears(200);

				calendar.Month = calendar.LastMonthInThisYear;
				calendar.Day = calendar.LastDayInThisMonth;

				var maxDate = calendar.GetDateTime();

				CompareDate comparer = CalendarHelper.CompareDate;
				VERIFY_IS_TRUE(comparer(cv.MinDate, minDate));
				VERIFY_IS_TRUE(comparer(cv.MaxDate, maxDate));
			});
		}


		[TestMethod]
		public async Task TestSelection()
		{
			TestCleanupWrapper cleanup;

			Grid rootPanel = null;

			DateTimeOffset date = ConvertToDateTime(1, 2000, 1, 1, 1, 8, 30); // 1/1/2000 8:30 AM

			var helper = new CalendarHelper.CalendarViewHelper();

			Xaml.Controls.CalendarView cv = await helper.GetCalendarView();

			rootPanel = await CalendarHelper.CreateTestResources();

			// set mindate and maxdate
			await RunOnUIThread(() =>
			{
				cv.MinDate = ConvertToDateTime(1, 1999, 1, 1);

				cv.MaxDate = ConvertToDateTime(1, 2020, 12, 31);

			});

			// load into visual tree
			await RunOnUIThread(() =>
			{
				rootPanel.Children.Add(cv);
			});

			await WindowHelper.WaitForIdle();

			// select a single valid date
			{

				await helper.PrepareSelectedDatesChangedEvent();
				helper.ExpectAddedDate(date);

				await RunOnUIThread(() =>
				{
					LOG_OUTPUT("CalendarViewIntegrationTests: select a date.");
					cv.SelectedDates.Add(date);
				});

				await helper.WaitForSelectedDatesChanged();
			}

			// remove the date
			{
				await helper.PrepareSelectedDatesChangedEvent();
				helper.ExpectRemovedDate(date);

				await RunOnUIThread(() =>
				{
					cv.SelectedDates.RemoveAt(0);
				});

				await helper.WaitForSelectedDatesChanged();

			}

			// multiple selection
			{
				await WindowHelper.WaitForIdle();

				DateTimeOffset date1 = ConvertToDateTime(1, 2000, 1, 1);
				DateTimeOffset date2 = ConvertToDateTime(1, 2000, 1, 2);
				DateTimeOffset date3 = ConvertToDateTime(1, 2000, 1, 3);


				await RunOnUIThread(() =>
				{
					cv.SelectionMode = CalendarViewSelectionMode.Multiple;
					cv.SelectedDates.Clear();

					cv.SelectedDates.Add(date1);
					cv.SelectedDates.Add(date2);
					cv.SelectedDates.Add(date3);
				});

				await RunOnUIThread(() =>
				{
					VERIFY_ARE_EQUAL(cv.SelectedDates.Count, 3);
				});


				// switch selection mode to single, only the first selected date is kept.

				await helper.PrepareSelectedDatesChangedEvent();
				helper.ExpectRemovedDate(date2);
				helper.ExpectRemovedDate(date3);


				await RunOnUIThread(() =>
				{
					cv.SelectionMode = CalendarViewSelectionMode.Single;
				});

				await helper.WaitForSelectedDatesChanged();

				await RunOnUIThread(() =>
				{
					VERIFY_ARE_EQUAL(cv.SelectedDates.Count, 1);
				});


				// switch selection mode to none, the first selected date will be clear

				await helper.PrepareSelectedDatesChangedEvent();
				helper.ExpectRemovedDate(date1);

				await RunOnUIThread(() =>
				{
					cv.SelectionMode = CalendarViewSelectionMode.None;
				});

				await helper.WaitForSelectedDatesChanged();

				await RunOnUIThread(() =>
				{
					VERIFY_ARE_EQUAL(cv.SelectedDates.Count, 0);
				});
			}

			await WindowHelper.WaitForIdle();
		}

		[TestMethod]
		[Ignore("UNO-TODO: Test fails Debug.Assert()")]
		public async Task CanSelectOutOfRangeDate()
		{
			TestCleanupWrapper cleanup;

			Grid rootPanel = null;

			DateTimeOffset date = ConvertToDateTime(1, 2000, 1, 1, 1, 8, 30); // 1/1/2000 8:30 AM

			var helper = new CalendarHelper.CalendarViewHelper();

			CalendarView cv = await helper.GetCalendarView();

			rootPanel = await CalendarHelper.CreateTestResources();

			// load into visual tree
			await RunOnUIThread(() =>
			{
				rootPanel.Children.Add(cv);
				// note: above "date" is not in this range.
				cv.MinDate = ConvertToDateTime(1, 2010, 1, 1);
				cv.MaxDate = ConvertToDateTime(1, 2010, 1, 1);
			});

			await WindowHelper.WaitForIdle();

			// select an invalid date (out of min/max range)
			{
				await helper.PrepareSelectedDatesChangedEvent();
				helper.ExpectAddedDate(date);

				await RunOnUIThread(() =>
				{
					LOG_OUTPUT("select a date.");
					cv.SelectedDates.Add(date);
				});

				await helper.WaitForSelectedDatesChanged();
			}

			// remove the date
			{
				await helper.PrepareSelectedDatesChangedEvent();
				helper.ExpectRemovedDate(date);

				await RunOnUIThread(() =>
				{
					cv.SelectedDates.RemoveAt(0);
				});

				await helper.WaitForSelectedDatesChanged();
			}

			// change min/max to make this selected date valid
			{
				await helper.PrepareSelectedDatesChangedEvent();
				helper.ExpectAddedDate(date);
				await RunOnUIThread(() =>
				{
					cv.SelectedDates.Add(date);
				});
				await helper.WaitForSelectedDatesChanged();

				// make it in range will not triggle additional SelectedDatesChangedEvent
				await helper.PrepareSelectedDatesChangedEvent();
				await RunOnUIThread(() =>
				{
					cv.MinDate = date;
					cv.MaxDate = date;
					cv.UpdateLayout();
				});
				helper.VerifyNoSelectedDatesChanged();
			}

			// remove the date
			{
				await helper.PrepareSelectedDatesChangedEvent();
				helper.ExpectRemovedDate(date);

				await RunOnUIThread(() =>
				{
					cv.SelectedDates.RemoveAt(0);
				});

				await helper.WaitForSelectedDatesChanged();
			}
		}

		[TestMethod]
		[Ignore("UNO TODO")]
		public async Task CanSelectDuplicatedDates()
		{
			//WUCRenderingScopeGuard guard(DCompRendering.WUCCompleteSynchronousCompTree, false /* resizeWindow */);
			//TestServices.WindowHelper.SetWindowSizeOverride(wf.Count(400, 400));

			Grid rootPanel = null;

			DateTimeOffset date = ConvertToDateTime(1, 2010, 1, 1, 1, 8, 30); // 1/1/2000 8:30 AM

			var helper = new CalendarHelper.CalendarViewHelper();

			CalendarView cv = await helper.GetCalendarView();

			rootPanel = await CalendarHelper.CreateTestResources();



			// load into visual tree
			await RunOnUIThread(() =>
			{
				rootPanel.Children.Add(cv);
				cv.SelectionMode = CalendarViewSelectionMode.Multiple;
				CalendarHelper.ReplaceAccentColorForTesting(cv);
				cv.MinDate = date;
				cv.MaxDate = date;
			});

			await WindowHelper.WaitForIdle();

			var count = 2;

			// select the date for multiple times and remove the date programatically one by one (developer's behavior).
			// here are the result we expected:
			// 1. only the first time we add this date into SelectedDates, we get the SelectedDatesChanged event
			// 2. after the first time, the item is always marked as selected.
			// 3. when remove the date from SelectedDates, the item stays selected until all are removed.
			for (var i = 0; i < count; i++)
			{
				await helper.PrepareSelectedDatesChangedEvent();
				if (i == 0)
				{
					// only the first time we'll get selectedDatesChanged event
					helper.ExpectAddedDate(date);
				}

				await RunOnUIThread(() =>
				{
					LOG_OUTPUT("select a date (%d of %d).", i + 1, count);
					cv.SelectedDates.Add(date);
				});

				await WindowHelper.WaitForIdle();

				if (i == 0)
				{
					// only the first time we'll get selectedDatesChanged event
					await helper.WaitForSelectedDatesChanged();
				}
				else
				{
					helper.VerifyNoSelectedDatesChanged();
				}

				TestServices.Utilities.VerifyMockDCompOutput(MockDComp.SurfaceComparison.NoComparison, "S"); //Selected
			}

			// remove the date one by one
			for (var i = 0; i < count; i++)
			{
				await helper.PrepareSelectedDatesChangedEvent();
				if (i == count - 1)
				{
					helper.ExpectRemovedDate(date);
				}

				await RunOnUIThread(() =>
				{
					LOG_OUTPUT("remove a date (%d of %d).", i + 1, count);
					cv.SelectedDates.RemoveAt(0);
				});

				await WindowHelper.WaitForIdle();

				if (i == count - 1)
				{
					// we get the SelectedDatesChanged event in the last time.
					await helper.WaitForSelectedDatesChanged();
				}
				else
				{
					// otherwise, no selected dates chagned event.
					helper.VerifyNoSelectedDatesChanged();
				}

				// the item stays selected until the last time we remove the date from SelectedDates.
				if (i < count - 1)
				{
					TestServices.Utilities.VerifyMockDCompOutput(MockDComp.SurfaceComparison.NoComparison,
						"S"); // selected
				}
				else
				{
					TestServices.Utilities.VerifyMockDCompOutput(MockDComp.SurfaceComparison.NoComparison,
						"NS"); // not selected
				}
			}

			// select the date for multiple times and remove the date by tapping on the item (user's behavior).
			// in this scenario we'll expect all dates are removed together when user taps to deselect.

			await RunOnUIThread(() =>
			{
				for (var i = 0; i < count; i++)
				{

					LOG_OUTPUT("select a date (%d of %d).", i + 1, count);
					cv.SelectedDates.Add(date);
				}
			});

			await WindowHelper.WaitForIdle();
			await helper.PrepareSelectedDatesChangedEvent();
			helper.ExpectRemovedDate(date);

			CalendarViewDayItem firstItem = null;
			await RunOnUIThread(() =>
			{
				var calendarPanel = helper.GetTemplateChild("MonthViewPanel") as CalendarPanel;
				firstItem = calendarPanel.Children.GetAt(0) as CalendarViewDayItem;
				VERIFY_IS_TRUE(cv.SelectedDates.Count == count);
			});

			LOG_OUTPUT("Tap to deselect this item, all dates from SelectedDates will be cleared.");
			LOG_OUTPUT("Press center.");
			TestServices.InputHelper.DynamicPressCenter(firstItem, 0, 0, PointerFinger.Finger1);
			await WindowHelper.WaitForIdle();
			LOG_OUTPUT("Press successful.");

			LOG_OUTPUT("Release.");
			TestServices.InputHelper.DynamicRelease(PointerFinger.Finger1);

			// on desktop, move the mouse away to avoid hover state.
			LOG_OUTPUT("Moving mouse to prevent hover state on phone.");
			TestServices.InputHelper.MoveMouse(new global::Windows.Foundation.Point(0, 0));

			await WindowHelper.WaitForIdle();
			LOG_OUTPUT("Release successful.");

			LOG_OUTPUT("Waiting for SelectedDatesChangedEvent.");
			await helper.WaitForSelectedDatesChanged();
			LOG_OUTPUT("SelectedDatesChangedEvent successful.");

			await RunOnUIThread(() =>
			{
				VERIFY_IS_TRUE(cv.SelectedDates.Count == 0u);
			});
			await WindowHelper.WaitForIdle();
			TestServices.Utilities.VerifyMockDCompOutput(MockDComp.SurfaceComparison.NoComparison,
				"NS2"); // not selected.

			// test we can avoid change a date to the save value
			await RunOnUIThread(() =>
			{
				cv.SelectedDates.Add(date);
			});
			await helper.PrepareSelectedDatesChangedEvent();
			await RunOnUIThread(() =>
			{
				cv.SelectedDates.SetAt(0, date); // no crash, no selected dates changed event.
			});
			helper.VerifyNoSelectedDatesChanged();
		}

		[TestMethod]
		[Ignore("TestServices.InputHelper.DynamicPressCenter() / TestServices.InputHelper.DynamicRelease() not implemented yet")]
		public async Task CanNotSelectBlackoutDate()
		{
			TestCleanupWrapper cleanup;

			Grid rootPanel = null;

			DateTimeOffset blackoutDate = ConvertToDateTime(1, 2010, 1, 1, 1, 8, 30); // 1/1/2010 8:30 AM
			DateTimeOffset normalDate = ConvertToDateTime(1, 2010, 1, 2, 1, 8, 30); // 1/2/2010 8:30 AM

			var helper = new CalendarHelper.CalendarViewHelper();

			CalendarView cv = await helper.GetCalendarView();

			rootPanel = await CalendarHelper.CreateTestResources();



			// load into visual tree
			await RunOnUIThread(() =>
			{
				rootPanel.Children.Add(cv);
				cv.MinDate = blackoutDate;
				cv.MaxDate = normalDate;
			});

			await WindowHelper.WaitForIdle();

			CalendarViewDayItem firstItem = null;

			await RunOnUIThread(() =>
			{
				var calendarPanel = helper.GetTemplateChild("MonthViewPanel") as CalendarPanel;
				firstItem = calendarPanel.Children.GetAt(0) as CalendarViewDayItem;
				firstItem.IsBlackout = true;
			});

			await WindowHelper.WaitForIdle();

			// tap will not select it (user's behavior).
			await helper.PrepareSelectedDatesChangedEvent();
			LOG_OUTPUT("Tap the blackout item will not select it");
			LOG_OUTPUT("Press center.");
			TestServices.InputHelper.DynamicPressCenter(firstItem, 0, 0, PointerFinger.Finger1);
			await WindowHelper.WaitForIdle();
			LOG_OUTPUT("Release.");
			TestServices.InputHelper.DynamicRelease(PointerFinger.Finger1);
			await WindowHelper.WaitForIdle();
			helper.VerifyNoSelectedDatesChanged();
			await RunOnUIThread(() =>
			{
				VERIFY_IS_TRUE(cv.SelectedDates.Count == 0u);
			});

			// mark the blackout date as selected will fail (developer's behavior)

			await RunOnUIThread(() =>
			{
				VERIFY_THROWS_WINRT<Exception>(() => cv.SelectedDates.Add(blackoutDate),
					"should not be able to select a blackout date");

				// SelectedDates stay unchanged.
				VERIFY_IS_TRUE(cv.SelectedDates.Count == 0u);
			});

			await RunOnUIThread(() =>
			{
				cv.SelectedDates.Add(normalDate);
				VERIFY_IS_TRUE(cv.SelectedDates.Count == 1u);

				// change a normal date from SelectedDates to a blackout date will fail (developer's behavior)
				VERIFY_THROWS_WINRT<Exception>(() => cv.SelectedDates.SetAt(0, blackoutDate),
					"should not be able to replace a selected date by a blackout date");

				// SelectedDates stay unchanged.
				VERIFY_IS_TRUE(cv.SelectedDates.Count == 1u);
			});

		}

		[TestMethod]
		[Ignore("Test deactivate in Uno")]
		public async Task CanNotSelectMoreDates()
		{
			TestCleanupWrapper cleanup;

			Grid rootPanel = null;

			DateTimeOffset date1 = ConvertToDateTime(1, 2005, 1, 1, 1, 8, 30); // 1/1/2005 8:30 AM
			DateTimeOffset date2 = ConvertToDateTime(1, 2005, 1, 2, 1, 8, 30); // 1/2/2005 8:30 AM
			DateTimeOffset minDate = ConvertToDateTime(1, 2000, 1, 1, 1, 8, 30); // 1/1/2000 8:30 AM
			DateTimeOffset maxDate = ConvertToDateTime(1, 2010, 1, 1, 1, 8, 30); // 1/1/2010 8:30 AM

			var helper = new CalendarHelper.CalendarViewHelper();

			CalendarView cv = await helper.GetCalendarView();

			rootPanel = await CalendarHelper.CreateTestResources();



			// load into visual tree
			await RunOnUIThread(() =>
			{
				rootPanel.Children.Add(cv);
				cv.MinDate = minDate;
				cv.MaxDate = maxDate;
			});

			await WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				cv.SelectionMode = CalendarViewSelectionMode.None;
				VERIFY_THROWS_WINRT<Exception>(() => cv.SelectedDates.Add(date1),
					"should not be able to select any date when selection mode is None");

				// selectedDates stay unchanged
				VERIFY_IS_TRUE(cv.SelectedDates.Count == 0u);

				cv.SelectionMode = CalendarViewSelectionMode.Single;

				cv.SelectedDates.Add(date1);

				VERIFY_IS_TRUE(cv.SelectedDates.Count == 1u);

				VERIFY_THROWS_WINRT<Exception>(() => cv.SelectedDates.Add(date2),
					"should not be able to select more than one date when selection mode is Single");

				// selectedDates stay unchanged
				VERIFY_IS_TRUE(cv.SelectedDates.Count == 1u);
			});
		}

		[TestMethod]
		[Ignore("UNO TODO")]
		public async Task CanChangeSelectedDatesInsideSelectedDatesChangedEvent()
		{
			TestCleanupWrapper cleanup;

			Grid rootPanel = null;

			DateTimeOffset minDate = ConvertToDateTime(1, 2010, 1, 1); // 1/1/2010
			DateTimeOffset maxDate = ConvertToDateTime(1, 2010, 1, 31); // 1/31/2010

			var helper = new CalendarHelper.CalendarViewHelper();

			CalendarView cv = await helper.GetCalendarView();

			rootPanel = await CalendarHelper.CreateTestResources();

			// load into visual tree
			await RunOnUIThread(() =>
			{
				rootPanel.Children.Add(cv);
				cv.MinDate = minDate;
				cv.MaxDate = maxDate;
				cv.SelectionMode = CalendarViewSelectionMode.Multiple;
			});

			await WindowHelper.WaitForIdle();
			int hitCounts = 0;
			;
			var firstSelectedDatesChanged = new Event();
			var secondSelectedDatesChanged = new Event();
			var selectedDatesChangedRegistration =
				CreateSafeEventRegistration<CalendarView, TypedEventHandler<CalendarView, CalendarViewSelectedDatesChangedEventArgs>>("SelectedDatesChanged");
			using var _ = selectedDatesChangedRegistration.Attach(
				cv,
				(sender, e) =>
				{
					int number = ++hitCounts;
					LOG_OUTPUT("Entering CalendarViewSelectedDatesChanged event# %d", number);

					if (number == 1)
					{
						VERIFY_ARE_EQUAL(cv.SelectedDates.Count, 1);
						VERIFY_ARE_EQUAL(e.AddedDates.Count, 1);
						VERIFY_DATES_ARE_EQUAL(e.AddedDates.GetAt(0).UniversalTime(), minDate.UniversalTime());

						LOG_OUTPUT("Try to select the 2nd date %ld inside the SelectedDatesChanged event",
							maxDate.UniversalTime());
						cv.SelectedDates.Add(maxDate);

						firstSelectedDatesChanged.Set();
					}
					else if (number == 2)
					{
						VERIFY_ARE_EQUAL(cv.SelectedDates.Count, 2);
						VERIFY_ARE_EQUAL(e.AddedDates.Count, 1);
						VERIFY_DATES_ARE_EQUAL(e.AddedDates.GetAt(0).UniversalTime(), maxDate.UniversalTime());
						secondSelectedDatesChanged.Set();
					}

					LOG_OUTPUT("Leaving CalendarViewSelectedDatesChanged event# %d", number);
				});

			CalendarViewDayItem firstDayItem = null;
			await RunOnUIThread(() =>
			{
				var monthPanel = helper.GetTemplateChild("MonthViewPanel") as CalendarPanel;
				firstDayItem = monthPanel.Children.GetAt(0) as CalendarViewDayItem;
			});
			TestServices.VERIFY_IS_NOT_NULL(firstDayItem);
			LOG_OUTPUT("select the 1st date %ld by tap", minDate.UniversalTime());
			TestServices.InputHelper.Tap(firstDayItem);

			await secondSelectedDatesChanged.WaitForDefault();
			VERIFY_IS_TRUE(secondSelectedDatesChanged.HasFired(), "secondSelectedDatesChanged not fired");

			await firstSelectedDatesChanged.WaitForDefault();
			VERIFY_IS_TRUE(firstSelectedDatesChanged.HasFired(), "firstSelectedDatesChanged not fired");
		}

		[TestMethod]
		public async Task TestViewMode()
		{
			TestCleanupWrapper cleanup;

			Grid rootPanel = null;
			Button headerButton = null;

			var helper = new CalendarHelper.CalendarViewHelper();

			CalendarView cv = await helper.GetCalendarView();

			rootPanel = await CalendarHelper.CreateTestResources();

			// load into visual tree
			await RunOnUIThread(() =>
			{
				rootPanel.Children.Add(cv);
			});


			await WindowHelper.WaitForIdle();

			// find the header button and tap it
			{
				await RunOnUIThread(() =>
				{
					headerButton = helper.GetTemplateChild("HeaderButton") as Button;
					VERIFY_ARE_EQUAL(cv.DisplayMode, CalendarViewDisplayMode.Month);
				});

				LOG_OUTPUT("CalendarViewIntegrationTests: changing viewmode to Year by using Tap.");

				await ControlHelper.DoClickUsingAP(headerButton);

				await WindowHelper.WaitForIdle();

				await RunOnUIThread(() =>
				{
					VERIFY_ARE_EQUAL(cv.DisplayMode, CalendarViewDisplayMode.Year);
				});

				LOG_OUTPUT("CalendarViewIntegrationTests: changing viewmode to Decade by using Tap.");

				await ControlHelper.DoClickUsingAP(headerButton);

				await WindowHelper.WaitForIdle();

				await RunOnUIThread(() =>
				{
					VERIFY_ARE_EQUAL(cv.DisplayMode, CalendarViewDisplayMode.Decade);
				});
			}
		}

		[TestMethod]
		[Ignore("Not Implemented")]
		public async Task TestCICEvents()
		{
			TestCleanupWrapper cleanup;

			Grid rootPanel = null;
			//Button headerButton = null;

			var helper = new CalendarHelper.CalendarViewHelper();
			await helper.PrepareLoadedEvent();
			CalendarView cv = await helper.GetCalendarView();

			//WUCRenderingScopeGuard guard(DCompRendering.WUCCompleteSynchronousCompTree, false /* resizeWindow */);
			//TestServices.WindowHelper.SetWindowSizeOverride(wf.Count(400, 400));

			rootPanel = await CalendarHelper.CreateTestResources();

			await helper.PrepareCICEvent();

			var date = ConvertToDateTime(1, 2000, 2, 1, 1, 12, 0, 0, 0); // 2/1/2000 12:00:00 AM

			// set selections
			await RunOnUIThread(() =>
			{
				// CIC event will be raised on all realized dayitem
				// to easily track the CIC event we let the calendarview have only one item
				cv.MinDate = date;
				cv.MaxDate = date;

				// select the only day
				cv.SelectedDates.Add(date);
			});


			// in CIC Event, we are going to blackout the selected date
			// so we expect the selectedDatesChanging event.
			await helper.PrepareSelectedDatesChangedEvent();
			helper.ExpectRemovedDate(date);

			// load into visual tree
			await RunOnUIThread(() =>
			{
				rootPanel.Children.Add(cv);
			});

			await helper.WaitForLoaded();

			await helper.WaitForCICEvent();

			await helper.WaitForSelectedDatesChanged();

			await RunOnUIThread(() =>
			{
				VERIFY_ARE_EQUAL(cv.SelectedDates.Count, 0);
			});

			await WindowHelper.WaitForIdle();

			// verify density bars
			TestServices.Utilities.VerifyMockDCompOutput(MockDComp.SurfaceComparison.NoComparison);
		}


		[TestMethod]
		[Ignore("UNO TODO")]
		public async Task VerifyBlackoutProperty()
		{
			TestCleanupWrapper cleanup;

			Grid rootPanel = null;
			//Button headerButton = null;

			var helper = new CalendarHelper.CalendarViewHelper();

			CalendarView cv = await helper.GetCalendarView();

			CalendarViewDayItem firstContainer = null;

			rootPanel = await CalendarHelper.CreateTestResources();

			var minDate = ConvertToDateTime(1, 2015, 1, 1);
			var maxDate = ConvertToDateTime(1, 2020, 1, 1);

			// set selections
			await RunOnUIThread(() =>
			{
				cv.MinDate = minDate;
				cv.MaxDate = maxDate;
				// display the first day, in CIC event we'll try to find the first container and set it as blackout
				cv.SetDisplayDate(minDate);
				rootPanel.Children.Add(cv);
			});

			await WindowHelper.WaitForIdle();

			var cicEvent = new Event();
			var cicRegistration = CreateSafeEventRegistration<CalendarView, TypedEventHandler<CalendarView, CalendarViewDayItemChangingEventArgs>>("CalendarViewDayItemChanging");
			cicRegistration.Attach(
				cv,
			(sender, e) =>
			{
				if (e.Item.Date.UniversalTime() == minDate.UniversalTime())
				{
					// the first container is being set up the first time, remember it and mark it as blackout
					e.Item.IsBlackout = true;
					firstContainer = e.Item;
				}
				else if (firstContainer != null && e.Item == firstContainer)
				{
					// next time the firstContainer is being reused, the blackout flag should be gone
					// i.e. we don't keep the blackout flag on the container, developer should check
					// the date property and decide to set blackout flag or not.

					VERIFY_IS_FALSE(firstContainer.IsBlackout);
					cicEvent.Set();
				}
			});


			await RunOnUIThread(() =>
			{
				// scroll to maxDate, the first container will be recycled and reused.
				cv.SetDisplayDate(maxDate);
			});

			await cicEvent.WaitForDefault();
			VERIFY_IS_TRUE(cicEvent.HasFired(), "Event not fired");
		}


		[TestMethod]
		[Ignore("Asserts ERROR_CALENDAR_NUMBER_OF_WEEKS_OUTOFRANGE")]
		public async Task VerifyCalendarItemCount()
		{
			TestCleanupWrapper cleanup;

			Grid rootPanel = null;

			//Windows.Globalization.Calendar calendar = null;

			var helper = new CalendarHelper.CalendarViewHelper();

			CalendarView cv = await helper.GetCalendarView();

			rootPanel = await CalendarHelper.CreateTestResources();

			// load into visual tree
			await RunOnUIThread(() =>
			{
				// note, don't make the range too big.
				// to make sure this test works correctly, all items should be realized.
				// by default the monthview panel will realize about 120 day items.
				cv.MinDate = ConvertToDateTime(1, 2000, 2, 1, 1, 12, 0, 0, 0); // 2/1/2000 12:00:00 AM
				cv.MaxDate = ConvertToDateTime(1, 2000, 3, 1, 2, 11, 59, 59, 0); // 3/1/2000 11:59:59 PM
				rootPanel.Children.Add(cv);
			});


			await WindowHelper.WaitForIdle();

			// find the Panels and verify the item numbers.
			{
				await RunOnUIThread(() =>
				{
					CalendarPanel monthPanel = helper.GetTemplateChild("MonthViewPanel") as CalendarPanel;
					VERIFY_ARE_EQUAL(monthPanel.Children.Count, 30); // 29 days in Feb + 1 day in Mar

					cv.DisplayMode = CalendarViewDisplayMode.Year;
				});

				await WindowHelper.WaitForIdle();

				await RunOnUIThread(() =>
				{
					CalendarPanel yearPanel = helper.GetTemplateChild("YearViewPanel") as CalendarPanel;
					VERIFY_ARE_EQUAL(yearPanel.Children.Count, 2); // Feb + Mar

					cv.DisplayMode = CalendarViewDisplayMode.Decade;
				});

				await WindowHelper.WaitForIdle();

				await RunOnUIThread(() =>
				{
					CalendarPanel decadePanel = helper.GetTemplateChild("DecadeViewPanel") as CalendarPanel;
					VERIFY_ARE_EQUAL(decadePanel.Children.Count, 1); // year 2000
				});
			}
		}

		[TestMethod]
		[Ignore("ERROR_CALENDAR_NUMBER_OF_WEEKS_OUTOFRANGE")]
		public async Task ValidateNumberOfWeeksInViewRange()
		{
			TestCleanupWrapper cleanup;

			CalendarView calendarView = null;

			var loadedEvent = new Event();
			var loadedRegistration = CreateSafeEventRegistration<StackPanel, RoutedEventHandler>("Loaded");

			await RunOnUIThread(() =>
			{
				StackPanel rootPanel = XamlReader.Load(
					@"<StackPanel Width=""400"" Height=""400"" xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"" xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
						HorizontalAlignment=""Center"" VerticalAlignment=""Center"">
							<CalendarView x:Name=""calendarview"" HorizontalAlignment=""Stretch"" VerticalAlignment=""Stretch"" Margin=""50"" NumberOfWeeksInView=""2"" />
						</StackPanel>") as StackPanel;

				VERIFY_THROWS_WINRT<Exception>(() => XamlReader.Load(
						@"<CalendarView xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"" xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"" NumberOfWeeksInView=""1"" />"),
					"Should not be able to set NumberOfWeeksInView to a value smaller than 2.");

				VERIFY_THROWS_WINRT<Exception>(() => XamlReader.Load(
					@"(<CalendarView xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"" xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"" NumberOfWeeksInView=""9""/>"),
					"Should not be able to set NumberOfWeeksInView to a value greater than 8.");

				calendarView = rootPanel.FindName("calendarview") as CalendarView;
				TestServices.VERIFY_IS_NOT_NULL(calendarView);

				using var _ = loadedRegistration.Attach(rootPanel, (snd, evt) =>
				{
					LOG_OUTPUT("StackPanel.Loaded event raised.");
					loadedEvent.Set();
				});

				TestServices.WindowHelper.WindowContent = rootPanel;
			});

			LOG_OUTPUT("Waiting for StackPanel.Loaded event...");
			await loadedEvent.WaitForDefault();
			await WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				LOG_OUTPUT("Verifying NumberOfWeeksInView value set in markup.");
				VERIFY_ARE_EQUAL(calendarView.NumberOfWeeksInView, 2);

				LOG_OUTPUT("Setting NumberOfWeeksInView to valid value.");
				calendarView.NumberOfWeeksInView = 8;
			});

			await WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				LOG_OUTPUT("Verifying valid NumberOfWeeksInView value.");
				VERIFY_ARE_EQUAL(calendarView.NumberOfWeeksInView, 8);

				LOG_OUTPUT("Verifying too small NumberOfWeeksInView value.");
				VERIFY_THROWS_WINRT<Exception>(() => calendarView.NumberOfWeeksInView = 1,
					"Should not be able to set NumberOfWeeksInView to a value smaller than 2.");

				LOG_OUTPUT("Verifying too large NumberOfWeeksInView value.");
				VERIFY_THROWS_WINRT<Exception>(() => calendarView.NumberOfWeeksInView = 9,
					"Should not be able to set NumberOfWeeksInView to a value greater than 8.");
			});
		}

		[TestMethod]
#if __WASM__
		[Ignore("UNO TODO - This test is failing on WASM")]
#endif
#if __SKIA__
		[RequiresFullWindow]
		[Ignore("This test sometimes breaks the state of the app for unknown reasons. This doesn't happen in isolation, but only when run a part of a larger test run.")]
#endif
		public async Task VerifyButtonState()
		{
			TestCleanupWrapper cleanup;

			Grid rootPanel = null;

			Button headerButton = null;
			Button previousButton = null;
			Button nextButton = null;
			DateTimeOffset begin = ConvertToDateTime(1, 1800, 1, 1);
			DateTimeOffset medium = ConvertToDateTime(1, 2200, 1, 1);
			DateTimeOffset end = ConvertToDateTime(1, 2400, 1, 1);

			var helper = new CalendarHelper.CalendarViewHelper();

			LOG_OUTPUT("init");

			CalendarView cv = await helper.GetCalendarView();

			rootPanel = await CalendarHelper.CreateTestResources();

			// load into visual tree
			await RunOnUIThread(() =>
			{
				cv.MinDate = begin;
				cv.MaxDate = end;
				rootPanel.Children.Add(cv);
			});

			await WindowHelper.WaitForIdle();

			// find the buttons
			await RunOnUIThread(() =>
			{
				headerButton = helper.GetTemplateChild("HeaderButton") as Button;
				previousButton = helper.GetTemplateChild("PreviousButton") as Button;
				nextButton = helper.GetTemplateChild("NextButton") as Button;
			});

			// monthview mode:
			{
				LOG_OUTPUT("month view mode");
				// 1. go to the very beginning
				await RunOnUIThread(() =>
				{
					cv.SetDisplayDate(begin);
				});

				await WindowHelper.WaitForIdle();

				await RunOnUIThread(() =>
				{
					VERIFY_IS_TRUE(headerButton.IsEnabled); // can switch to yearview
					VERIFY_IS_FALSE(previousButton.IsEnabled); // can't go backwards
					VERIFY_IS_TRUE(nextButton.IsEnabled); // can go forward
				});

				// 2. go to the medium part
				await RunOnUIThread(() =>
				{
					cv.SetDisplayDate(medium);
				});

				await WindowHelper.WaitForIdle();

				await RunOnUIThread(() =>
				{
					VERIFY_IS_TRUE(headerButton.IsEnabled); // can switch to yearview
					VERIFY_IS_TRUE(previousButton.IsEnabled); // can go backward
					VERIFY_IS_TRUE(nextButton.IsEnabled); // can go forward
				});

				// 3. go to the end
				await RunOnUIThread(() =>
				{
					cv.SetDisplayDate(end);
				});

				await WindowHelper.WaitForIdle();

				await RunOnUIThread(() =>
				{
					VERIFY_IS_TRUE(headerButton.IsEnabled); // can switch to yearview
					VERIFY_IS_TRUE(previousButton.IsEnabled); // can go backward
					VERIFY_IS_FALSE(nextButton.IsEnabled); // can't go forward
				});
			}

			// yearview mode:
			{
				LOG_OUTPUT("year view mode");
				// 1. go to the very beginning
				await RunOnUIThread(() =>
				{
					cv.DisplayMode = CalendarViewDisplayMode.Year;
					cv.SetDisplayDate(begin);
				});

				await WindowHelper.WaitForIdle();

				await RunOnUIThread(() =>
				{
					VERIFY_IS_TRUE(headerButton.IsEnabled); // can switch to decadeview
					VERIFY_IS_FALSE(previousButton.IsEnabled); // can't go backwards
					VERIFY_IS_TRUE(nextButton.IsEnabled); // can go forward
				});

				// 2. go to the medium part
				await RunOnUIThread(() =>
				{
					cv.SetDisplayDate(medium);
				});

				await WindowHelper.WaitForIdle();

				await RunOnUIThread(() =>
				{
					VERIFY_IS_TRUE(headerButton.IsEnabled); // can switch to decadeview
					VERIFY_IS_TRUE(previousButton.IsEnabled); // can go backward
					VERIFY_IS_TRUE(nextButton.IsEnabled); // can go forward
				});

				// 3. go to the end
				await RunOnUIThread(() =>
				{
					cv.SetDisplayDate(end);
				});

				await WindowHelper.WaitForIdle();

				await RunOnUIThread(() =>
				{
					VERIFY_IS_TRUE(headerButton.IsEnabled); // can switch to decadeview
					VERIFY_IS_TRUE(previousButton.IsEnabled); // can go backward
					VERIFY_IS_FALSE(nextButton.IsEnabled); // can't go forward
				});
			}

			// decadeview mode:
			{
				LOG_OUTPUT("decade view mode");
				// 1. go to the very beginning
				await RunOnUIThread(() =>
				{
					cv.DisplayMode = CalendarViewDisplayMode.Decade;
					cv.SetDisplayDate(begin);
				});

				await WindowHelper.WaitForIdle();

				await RunOnUIThread(() =>
				{
					VERIFY_IS_FALSE(headerButton.IsEnabled); // can't switch viewmode by tapping header button
					VERIFY_IS_FALSE(previousButton.IsEnabled); // can't go backwards
					VERIFY_IS_TRUE(nextButton.IsEnabled); // can go forward
				});

				// 2. go to the medium part
				await RunOnUIThread(() =>
				{
					cv.SetDisplayDate(medium);
				});

				await WindowHelper.WaitForIdle();

				await RunOnUIThread(() =>
				{
					VERIFY_IS_FALSE(headerButton.IsEnabled); // can't switch viewmode by tapping header button
					VERIFY_IS_TRUE(previousButton.IsEnabled); // can go backward
					VERIFY_IS_TRUE(nextButton.IsEnabled); // can go forward
				});

				// 3. go to the end
				await RunOnUIThread(() =>
				{
					cv.SetDisplayDate(end);
				});

				await WindowHelper.WaitForIdle();

				await RunOnUIThread(() =>
				{
					VERIFY_IS_FALSE(headerButton.IsEnabled); // can't switch viewmode by tapping header button
					VERIFY_IS_TRUE(previousButton.IsEnabled); // can go backward
					VERIFY_IS_FALSE(nextButton.IsEnabled); // can't go forward
				});
			}
		}

		[TestMethod]
		[Ignore("UNO TODO")]
		public async Task VerifyNavigationButtonsBehavior()
		{
			TestCleanupWrapper cleanup;

			Grid rootPanel = null;

			Button headerButton = null;
			Button nextButton = null;
			ScrollViewer scrollViewer = null;

			var minDate = ConvertToDateTime(1, 2000, 1, 1);
			var maxDate = ConvertToDateTime(1, 2100, 12, 31);

			var helper = new CalendarHelper.CalendarViewHelper();

			CalendarView cv = await helper.GetCalendarView();

			rootPanel = await CalendarHelper.CreateTestResources();

			// load into visual tree
			await RunOnUIThread(() =>
			{
				cv.MinDate = minDate;
				cv.MaxDate = maxDate;
				cv.SetDisplayDate(minDate);
				cv.NumberOfWeeksInView =
					2; // putting 2 rows in the view instead of the default 6, so a button tap will move by two rows instead of one scope.
				rootPanel.Children.Add(cv);
			});

			await WindowHelper.WaitForIdle();

			// find the template parts
			await RunOnUIThread(() =>
			{
				headerButton = helper.GetTemplateChild("HeaderButton") as Button;
				nextButton = helper.GetTemplateChild("NextButton") as Button;
				scrollViewer = helper.GetTemplateChild("MonthViewScrollViewer") as ScrollViewer;
			});

			var viewChangedEvent = new Event();
			var viewChangedRegistration = CreateSafeEventRegistration<ScrollViewer, EventHandler<ScrollViewerViewChangedEventArgs>>("ViewChanged");

			using var _ = viewChangedRegistration.Attach(scrollViewer,
				(sender, e) =>
				{
					if (!e.IsIntermediate)
					{
						viewChangedEvent.Set();
					}
				});

			// verify that when tapping the next button 3 times, we are still on the first month.
			for (int i = 0; i < 3; i++)
			{
				await RunOnUIThread(() =>
				{
					var headText = headerButton.Content as string;
					LOG_OUTPUT("Current month %s", headText);
					//VERIFY_ARE_EQUAL(headText, "\u200eJanuary\u200e \u200e2000");
					VERIFY_ARE_EQUAL(headText, "January 2000");
				});

				TestServices.InputHelper.Tap(nextButton);
				await viewChangedEvent.WaitForDefault();
				VERIFY_IS_TRUE(viewChangedEvent.HasFired(), "Event not fired");
				viewChangedEvent.Reset();
			}
		}

		[TestMethod]
#if __ANDROID__
		[Ignore("UNO TODO - This this is crashing the app on Android")]
#endif
		public async Task VerifySelfAdaptivePanel()
		{
			TestCleanupWrapper cleanup;

			Grid rootPanel = null;

			var helper = new CalendarHelper.CalendarViewHelper();

			CalendarView cv = await helper.GetCalendarView();

			//WUCRenderingScopeGuard guard(DCompRendering.WUCCompleteSynchronousCompTree, false /* resizeWindow */);
			//TestServices.WindowHelper.SetWindowSizeOverride(wf.Count(400, 400));

			var minDate = ConvertToDateTime(1, 2000, 1, 1);
			var maxDate = ConvertToDateTime(1, 2100, 12, 31);

			rootPanel = await CalendarHelper.CreateTestResources();

			// load into visual tree
			await RunOnUIThread(() =>
			{
				cv.MinDate = minDate;
				cv.MaxDate = maxDate;
				cv.SetDisplayDate(minDate);
				cv.Language = "sms"; // long month name in this language
				cv.DisplayMode = CalendarViewDisplayMode.Year;
				rootPanel.Children.Add(cv);
			});

			await WindowHelper.WaitForIdle();

			LOG_OUTPUT("Validate Gregorian Calendar with sms language in Year mode (should be 1 x 4)");
			TestServices.Utilities.VerifyMockDCompOutput(MockDComp.SurfaceComparison.NoComparison, "1");

			await RunOnUIThread(() =>
			{
				cv.Language = "en-us";
				cv.MonthYearItemFontSize = 36;
			});

			await WindowHelper.WaitForIdle();

			LOG_OUTPUT("Validate Gregorian Calendar with big font size in Year mode (should be 3 x 4)");
			TestServices.Utilities.VerifyMockDCompOutput(MockDComp.SurfaceComparison.NoComparison, "2");

			await RunOnUIThread(() =>
			{
				cv.SetYearDecadeDisplayDimensions(4,
					4); // explicitly set the dimension, the panel will no longer adjust the dimension.
			});

			await WindowHelper.WaitForIdle();
			LOG_OUTPUT(
				"Validate Gregorian Calendar with big font size but have dimension set explicitly in Year mode (should be 4 x 4, i.e. some items get clipped)");
			TestServices.Utilities.VerifyMockDCompOutput(MockDComp.SurfaceComparison.NoComparison, "3");
		}

		[TestMethod]
		public async Task CanEnterAndLeaveLiveTree()
		{
			// Note: CalendarView can't use below commented line to test "CanEnterAndLeaveLiveTree"
			// the problem is in below helper, we did these:
			// 1. create CalendarView
			// 2. added into visual tree
			// 3. test loaded and unloaded event
			// 4. remove calendarview from visual tree
			// 5. destroy CalendarView
			// .....

			// because we destroy CalendarView after we remove it from visual tree, so if there are any left work in build tree services, they can't be cleaned up correctly.
			// this should happens on ListView and GridView, however for default ListView and GridView (especially in below helper method) are empty and there is no buildtree work.
			// But for default CalendarView, we have! because default Calendarview will show the dates in 3 years.

			//Generic.FrameworkElementTests<CalendarView>.CanEnterAndLeaveLiveTree();

			TestCleanupWrapper cleanup;

			Grid rootPanel = null;

			var helper = new CalendarHelper.CalendarViewHelper();

			CalendarView cv = await helper.GetCalendarView();

			rootPanel = await CalendarHelper.CreateTestResources();

			// load into visual tree
			await RunOnUIThread(() =>
			{
				rootPanel.Children.Add(cv);
			});

			await WindowHelper.WaitForIdle();

			// remove from visual tree
			await RunOnUIThread(() =>
			{
				rootPanel.Children.Clear();
			});

			await WindowHelper.WaitForIdle();
		}

		private void VerifyItemPositionInPanel(
				 UIElement item, CalendarPanel panel, int col, int row)
		{
			Point origin = new Point(0, 0);
			var itemPos = item.TransformToVisual(panel).TransformPoint(origin);
			var itemWidth = (item as FrameworkElement).ActualWidth;
			var itemHeight = (item as FrameworkElement).ActualHeight;
			var margin = (item as FrameworkElement).Margin;

			itemWidth += margin.Left + margin.Right;
			itemHeight += margin.Top + margin.Bottom;

			itemPos.X -= (float)(margin.Left);
			itemPos.Y -= (float)(margin.Top);

			VERIFY_IS_TRUE(CalendarHelper.AreClose(itemPos.X, itemWidth * col, 1.0 /* rounding issue, to be fixed */));
			VERIFY_IS_TRUE(CalendarHelper.AreClose(itemPos.Y, itemHeight * row, 1.0 /* rounding issue, to be fixed */));
		}

		void VerifyItemCountInViewport(CalendarPanel panel, ScrollViewer scrollViewer, int col, int row)
		{
			var item = panel.Children.GetAt(0); // any item, we just want the size.
			var itemWidth = (item as FrameworkElement).ActualWidth;
			var itemHeight = (item as FrameworkElement).ActualHeight;
			var viewportWidth = scrollViewer.ViewportWidth;
			var viewportHeight = scrollViewer.ViewportHeight;

			var margin = FrameworkElement(item).Margin;

			itemWidth += margin.Left + margin.Right;
			itemHeight += margin.Top + margin.Bottom;

			VERIFY_ARE_VERY_CLOSE(viewportWidth / col, itemWidth, 1.0 /* rounding issue, to be fixed */, "itemWidth");
			VERIFY_ARE_VERY_CLOSE(viewportHeight / row, itemHeight, 1.0 /* rounding issue, to be fixed */, "itemHeight");
		}

		[TestMethod]
		[Ignore("UNO TODO")]
		public async Task CalendarPanelLayoutTestFirstItemPositonTest()
		{
			TestCleanupWrapper cleanup;

			Grid rootPanel = null;
			CalendarPanel monthPanel = null;
			UIElement firstDayItem = null;
			var helper = new CalendarHelper.CalendarViewHelper();

			CalendarView cv = await helper.GetCalendarView();

			rootPanel = await CalendarHelper.CreateTestResources();

			var minDate = ConvertToDateTime(1, 2014, 8, 30); // Saturday
			var maxDate = ConvertToDateTime(1, 2036, 1, 1);


			// load into visual tree
			await RunOnUIThread(() =>
			{
				cv.MinDate = minDate;
				cv.MaxDate = maxDate;
				cv.SetDisplayDate(minDate);
				rootPanel.Children.Add(cv);
			});

			await WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				monthPanel = helper.GetTemplateChild("MonthViewPanel") as CalendarPanel;
				firstDayItem = monthPanel.Children.GetAt(0);
			});

			await WindowHelper.WaitForIdle();

			// the first day item is on Saturday, when we change FirstDayOfWeek from Sunday (0) to Saturday (6), the col of first item will be from 6 to 0
			for (int i = 0; i < 7; i++)
			{
				global::Windows.Globalization.DayOfWeek dayofWeek =
					(global::Windows.Globalization.DayOfWeek)((int)(global::Windows.Globalization.DayOfWeek.Sunday) + i);

				await RunOnUIThread(() =>
				{
					cv.FirstDayOfWeek = dayofWeek;
				});
				await WindowHelper.WaitForIdle();
				await RunOnUIThread(() =>
				{
					VerifyItemPositionInPanel(firstDayItem, monthPanel, 6 - i /* col */,
						0 /* the first item is always stay in row 0. */);
				});
			}

			await WindowHelper.WaitForIdle();

			// now change to year view, the item start at 0 always
			await RunOnUIThread(() =>
			{

				cv.DisplayMode = CalendarViewDisplayMode.Year;
			});

			await WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				var yearPanel = CalendarPanel(helper.GetTemplateChild("YearViewPanel"));
				var firstYearItem = yearPanel.Children.GetAt(0);
				VerifyItemPositionInPanel(firstYearItem, yearPanel, 0 /* col */, 0 /* row */);
			});

			// now change to decade view, the item start at 0 always
			await RunOnUIThread(() =>
			{

				cv.DisplayMode = CalendarViewDisplayMode.Decade;
			});

			await WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				var decadePanel = CalendarPanel(helper.GetTemplateChild("YearViewPanel"));
				var firstDecadeItem = decadePanel.Children.GetAt(0);
				VerifyItemPositionInPanel(firstDecadeItem, decadePanel, 0 /* col */, 0 /* row */);
			});

		}

		[TestMethod]
		[Ignore("UNO TODO")]
		public async Task CalendarPanelLayoutTestRowsAndColsTest()
		{
			TestCleanupWrapper cleanup;

			Grid rootPanel = null;
			CalendarPanel calendarPanel = null;
			ScrollViewer scrollViewer = null;
			//UIElement calendarItem = null;
			var helper = new CalendarHelper.CalendarViewHelper();

			CalendarView cv = await helper.GetCalendarView();

			rootPanel = await CalendarHelper.CreateTestResources();

			// load into visual tree
			await RunOnUIThread(() =>
			{
				rootPanel.Children.Add(cv);
			});

			await WindowHelper.WaitForIdle();

			// month view, we change the NumberOfWeeksInView from 2 to 8 and then go back to 2
			int[] numberofWeeksInView = {
				2, 3, 4, 5, 6, 7, 8, 6, 4, 2
			}
			;
			for (int i = 0; i < ARRAYSIZE(numberofWeeksInView); i++)
			{
				int numberOfWeeksInView = numberofWeeksInView[i];
				await RunOnUIThread(() =>
				{
					cv.NumberOfWeeksInView = numberOfWeeksInView;
					calendarPanel = CalendarPanel(helper.GetTemplateChild("MonthViewPanel"));
					scrollViewer = ScrollViewer(helper.GetTemplateChild("MonthViewScrollViewer"));
				});

				await WindowHelper.WaitForIdle();

				await RunOnUIThread(() =>
				{
					VerifyItemCountInViewport(calendarPanel, scrollViewer, 7, numberOfWeeksInView);
				});
			}

			// now test year view and decacde view

			CalendarViewDisplayMode[] modes = { CalendarViewDisplayMode.Year, CalendarViewDisplayMode.Decade };

			string[] panelNames = { "YearViewPanel", "DecadeViewPanel" };

			string[] scrollViewerNames = { "YearViewScrollViewer", "DecadeViewScrollViewer" };

			for (int i = 0; i < 2; i++)
			{
				// first let's change to the corresponding view.
				await RunOnUIThread(() =>
				{
					cv.DisplayMode = modes[i];
				});

				await WindowHelper.WaitForIdle();

				await RunOnUIThread(() =>
				{
					calendarPanel = CalendarPanel(helper.GetTemplateChild(panelNames[i]));
					scrollViewer = ScrollViewer(helper.GetTemplateChild(scrollViewerNames[i]));
				});

				// change the dimensions and verify size.
				for (int row = 1; row < 8; row += 2)
				{
					for (int col = 1; col < 8; col += 2)
					{
						await RunOnUIThread(() =>
						{
							cv.SetYearDecadeDisplayDimensions(col, row);
						});

						await WindowHelper.WaitForIdle();

						await RunOnUIThread(() =>
						{
							VerifyItemCountInViewport(calendarPanel, scrollViewer, col, row);
						});
					}
				}

			}
		}

		[TestMethod]
#if __APPLE_UIKIT__ || __ANDROID__ || __SKIA__
		[Ignore("UNO TODO - This test is failing on iOS/macOS/Android/Skia")]
#endif
		public async Task CalendarPanelLayoutTestStretchTest()
		{
			TestCleanupWrapper cleanup;

			Grid rootPanel = null;
			CalendarPanel calendarPanel = null;
			ScrollViewer scrollViewer = null;
			//UIElement calendarItem = null;
			var helper = new CalendarHelper.CalendarViewHelper();

			CalendarView cv = await helper.GetCalendarView();

			rootPanel = await CalendarHelper.CreateTestResources();

			// load into visual tree
			await RunOnUIThread(() =>
			{
				rootPanel.Children.Add(cv);
			});

			await WindowHelper.WaitForIdle();

			// test when changing alignment from stretch to non-stretch mode, for both Horizontal and Vertical
			(HorizontalAlignment ha, HorizontalAlignment hca, VerticalAlignment va, VerticalAlignment vca)[] alignments = {
				(HorizontalAlignment.Center, HorizontalAlignment.Center, VerticalAlignment.Center, VerticalAlignment.Center),
				(HorizontalAlignment.Stretch, HorizontalAlignment.Stretch, VerticalAlignment.Center, VerticalAlignment.Center),
				(HorizontalAlignment.Stretch, HorizontalAlignment.Stretch, VerticalAlignment.Stretch, VerticalAlignment.Stretch),
				(HorizontalAlignment.Stretch, HorizontalAlignment.Center, VerticalAlignment.Stretch, VerticalAlignment.Center),
				(HorizontalAlignment.Center, HorizontalAlignment.Stretch, VerticalAlignment.Center, VerticalAlignment.Stretch),
				(HorizontalAlignment.Center, HorizontalAlignment.Center, VerticalAlignment.Center, VerticalAlignment.Center),
			};

			(CalendarViewDisplayMode mode, string panelName, string scrollViewerName, int col, int row)[] modes = {
				( CalendarViewDisplayMode.Month, "MonthViewPanel", "MonthViewScrollViewer", 7, 6 ),
				( CalendarViewDisplayMode.Year, "YearViewPanel", "YearViewScrollViewer", 4, 4 ),
				( CalendarViewDisplayMode.Decade, "DecadeViewPanel", "DecadeViewScrollViewer", 4, 4 ),
			};

			for (int j = 0; j < ARRAYSIZE(alignments); j++)
			{
				await RunOnUIThread(() =>
				{
					cv.HorizontalAlignment = alignments[j].ha;
					cv.HorizontalContentAlignment = alignments[j].hca;
					cv.VerticalAlignment = alignments[j].va;
					cv.VerticalContentAlignment = alignments[j].vca;
				});

				await WindowHelper.WaitForIdle();

				for (int i = 0; i < ARRAYSIZE(modes); i++)
				{
					await RunOnUIThread(() =>
					{
						cv.DisplayMode = modes[i].mode;
					});

					await WindowHelper.WaitForIdle();

					await RunOnUIThread(() =>
					{
						calendarPanel = CalendarPanel(helper.GetTemplateChild(modes[i].panelName));
						scrollViewer = ScrollViewer(helper.GetTemplateChild(modes[i].scrollViewerName));
					});

					await WindowHelper.WaitForIdle();

					await RunOnUIThread(() =>
					{
						VerifyItemCountInViewport(calendarPanel, scrollViewer, modes[i].col, modes[i].row);
					});
				}
			}
		}

		[TestMethod]
		[Ignore("UNO TODO - TestServices.KeyboardHelper not implemented yet.")]
		public async Task CanSwitchDisplayModeByCtrlUpAfterLoaded()
		{
			TestCleanupWrapper cleanup;

			Grid rootPanel = null;
			CalendarPanel calendarPanel = null;
			var helper = new CalendarHelper.CalendarViewHelper();

			CalendarView cv = await helper.GetCalendarView();

			rootPanel = await CalendarHelper.CreateTestResources();

			// load into visual tree
			await RunOnUIThread(() =>
			{
				rootPanel.Children.Add(cv);
			});

			await WindowHelper.WaitForIdle();
			await RunOnUIThread(() =>
			{
				calendarPanel = CalendarPanel(helper.GetTemplateChild("MonthViewPanel"));
				var firstItem = CalendarViewDayItem(calendarPanel.Children.GetAt(0));
				firstItem.Focus(FocusState.Programmatic);
			});

			await TestServices.KeyboardHelper.PressKeySequence("$d$_ctrl#$d$_up#$u$_up#$u$_ctrl");
			await WindowHelper.WaitForIdle();
			await RunOnUIThread(() =>
			{
				VERIFY_IS_TRUE(cv.DisplayMode == CalendarViewDisplayMode.Year);
			});
		}

		[TestMethod]
		[Ignore("UNO TODO - TestServices.KeyboardHelper not implemented yet.")]
		public async Task KeyboardNavigationTestNavigationKeyTest()
		{
			TestCleanupWrapper cleanup;

			Grid rootPanel = null;
			CalendarPanel calendarPanel = null;
			var helper = new CalendarHelper.CalendarViewHelper();

			CalendarView cv = await helper.GetCalendarView();
			CompareDate comparer = CalendarHelper.CompareDate;

			rootPanel = await CalendarHelper.CreateTestResources();


			var minDate = ConvertToDateTime(1, 2014, 9, 3); // Wednesday
			var maxDate = ConvertToDateTime(1, 2015, 9, 3);

			// load into visual tree
			await RunOnUIThread(() =>
			{
				cv.MinDate = minDate;
				cv.MaxDate = maxDate;
				cv.SetDisplayDate(minDate);
				rootPanel.Children.Add(cv);
			});

			await WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				calendarPanel = CalendarPanel(helper.GetTemplateChild("MonthViewPanel"));
				var firstItem = CalendarViewDayItem(calendarPanel.Children.GetAt(0));
				firstItem.Focus(FocusState.Programmatic);
			});

			await WindowHelper.WaitForIdle();
			await RunOnUIThread(() =>
			{
				var focusedElement = CalendarViewDayItem(Input.FocusManager.GetFocusedElement(WindowHelper.XamlRoot));
				CalendarHelper.DumpDate(focusedElement.Date, "Focus starts from ");
				VERIFY_IS_TRUE(comparer(focusedElement.Date, minDate));
			});

			await TestServices.KeyboardHelper.Down();
			await TestServices.KeyboardHelper.Down();
			await WindowHelper.WaitForIdle();
			await RunOnUIThread(() =>
			{
				var focusedElement = CalendarViewDayItem(Input.FocusManager.GetFocusedElement(WindowHelper.XamlRoot));
				CalendarHelper.DumpDate(focusedElement.Date, "Pressed down twice, now focus is on ");
				VERIFY_IS_TRUE(comparer(focusedElement.Date, ConvertToDateTime(1, 2014, 9, 17)));
			});

			await TestServices.KeyboardHelper.PressKeySequence(
				"$d$_left#$u$_left#$d$_left#$u$_left#$d$_left#$u$_left#$d$_left#$u$_left");
			await WindowHelper.WaitForIdle();
			await RunOnUIThread(() =>
			{
				var focusedElement = CalendarViewDayItem(Input.FocusManager.GetFocusedElement(WindowHelper.XamlRoot));
				CalendarHelper.DumpDate(focusedElement.Date, "Pressed left 4 times, now focus is on ");
				VERIFY_IS_TRUE(comparer(focusedElement.Date, ConvertToDateTime(1, 2014, 9, 13)));
			});

			await TestServices.KeyboardHelper.PressKeySequence("$d$_right#$u$_right");
			await WindowHelper.WaitForIdle();
			await RunOnUIThread(() =>
			{
				var focusedElement = CalendarViewDayItem(Input.FocusManager.GetFocusedElement(WindowHelper.XamlRoot));
				CalendarHelper.DumpDate(focusedElement.Date, "Pressed right once, now focus is on ");
				VERIFY_IS_TRUE(comparer(focusedElement.Date, ConvertToDateTime(1, 2014, 9, 14)));
			});

			await TestServices.KeyboardHelper.Up();
			await WindowHelper.WaitForIdle();
			await RunOnUIThread(() =>
			{
				var focusedElement = CalendarViewDayItem(Input.FocusManager.GetFocusedElement(WindowHelper.XamlRoot));
				CalendarHelper.DumpDate(focusedElement.Date, "Pressed up once, now focus is on ");
				VERIFY_IS_TRUE(comparer(focusedElement.Date, ConvertToDateTime(1, 2014, 9, 7)));
			});

			await TestServices.KeyboardHelper.PressKeySequence("$d$_home#$u$_home");
			await WindowHelper.WaitForIdle();
			await RunOnUIThread(() =>
			{
				var focusedElement = CalendarViewDayItem(Input.FocusManager.GetFocusedElement(WindowHelper.XamlRoot));
				CalendarHelper.DumpDate(focusedElement.Date, "Pressed home once, now focus is on ");
				// note we should be at 9/1 but the first day is 9/3.
				VERIFY_IS_TRUE(comparer(focusedElement.Date, ConvertToDateTime(1, 2014, 9, 3)));
			});

			await TestServices.KeyboardHelper.PressKeySequence("$d$_home#$u$_home");
			await WindowHelper.WaitForIdle();
			await RunOnUIThread(() =>
			{
				var focusedElement = CalendarViewDayItem(Input.FocusManager.GetFocusedElement(WindowHelper.XamlRoot));
				CalendarHelper.DumpDate(focusedElement.Date, "Pressed home again, now focus is on ");
				VERIFY_IS_TRUE(comparer(focusedElement.Date, ConvertToDateTime(1, 2014, 9, 3)));
			});

			await TestServices.KeyboardHelper.PressKeySequence("$d$_end#$u$_end");
			await WindowHelper.WaitForIdle();
			await RunOnUIThread(() =>
			{
				var focusedElement = CalendarViewDayItem(Input.FocusManager.GetFocusedElement(WindowHelper.XamlRoot));
				CalendarHelper.DumpDate(focusedElement.Date, "Pressed end once, now focus is on ");
				VERIFY_IS_TRUE(comparer(focusedElement.Date, ConvertToDateTime(1, 2014, 9, 30)));
			});

			await TestServices.KeyboardHelper.PressKeySequence("$d$_end#$u$_end");
			await WindowHelper.WaitForIdle();
			await RunOnUIThread(() =>
			{
				var focusedElement = CalendarViewDayItem(Input.FocusManager.GetFocusedElement(WindowHelper.XamlRoot));
				CalendarHelper.DumpDate(focusedElement.Date, "Pressed end again, now focus is on ");
				VERIFY_IS_TRUE(comparer(focusedElement.Date, ConvertToDateTime(1, 2014, 9, 30)));
			});

			await TestServices.KeyboardHelper.PressKeySequence("$d$_pagedown#$u$_pagedown");
			// sometime the following right key press event is not handled by App, can't repro this issue locally with or without stress.
			// try an additional UpdateLayout to see if it helps.
			await RunOnUIThread(() =>
			{
				cv.UpdateLayout();
			});
			await WindowHelper.WaitForIdle();
			await TestServices.KeyboardHelper.PressKeySequence("$d$_right#$u$_right");
			await WindowHelper.WaitForIdle();
			await RunOnUIThread(() =>
			{
				var focusedElement = CalendarViewDayItem(Input.FocusManager.GetFocusedElement(WindowHelper.XamlRoot));
				CalendarHelper.DumpDate(focusedElement.Date,
					"Pressed pagedown once followed by right once, now focus is on ");
				VERIFY_IS_TRUE(comparer(focusedElement.Date, ConvertToDateTime(1, 2014, 10, 31)));
			});

			await TestServices.KeyboardHelper.PressKeySequence("$d$_pageup#$u$_pageup");
			await WindowHelper.WaitForIdle();
			await RunOnUIThread(() =>
			{
				var focusedElement = CalendarViewDayItem(Input.FocusManager.GetFocusedElement(WindowHelper.XamlRoot));
				CalendarHelper.DumpDate(focusedElement.Date,
					"Pressed pagedown once followed by right once, now focus is on ");
				VERIFY_IS_TRUE(comparer(focusedElement.Date, ConvertToDateTime(1, 2014, 9, 30)));
			});

			await TestServices.KeyboardHelper.PressKeySequence("$d$_pagedown#$u$_pagedown");
			await WindowHelper.WaitForIdle();
			await RunOnUIThread(() =>
			{
				var focusedElement = CalendarViewDayItem(Input.FocusManager.GetFocusedElement(WindowHelper.XamlRoot));
				CalendarHelper.DumpDate(focusedElement.Date,
					"Pressed pagedown once followed by right once, now focus is on ");
				VERIFY_IS_TRUE(comparer(focusedElement.Date, ConvertToDateTime(1, 2014, 10, 30)));
			});
		}

		// UNO-TODO: support for gamepad by integration tests
		//[TestMethod]
		//public async Task ValidateNavigation()
		//{
		//	TestCleanupWrapper cleanup;
		//	InputDevice device = InputDevice.Gamepad;

		//	Grid rootPanel = null;
		//	CalendarPanel calendarPanel = null;
		//	var helper = new CalendarHelper.CalendarViewHelper();

		//	CalendarView cv = await helper.GetCalendarView();
		//	CompareDate comparer;

		//	rootPanel = await CalendarHelper.CreateTestResources();

		//	var minDate = ConvertToDateTime(1, 2014, 9, 3); // Wednesday
		//	var maxDate = ConvertToDateTime(1, 2015, 9, 3);

		//	// load into visual tree
		//	await RunOnUIThread(() =>
		//	{
		//		cv.MinDate = minDate;
		//		cv.MaxDate = maxDate;
		//		cv.SetDisplayDate(minDate);
		//		rootPanel.Children.Add(cv);
		//	});

		//	await WindowHelper.WaitForIdle();

		//	await RunOnUIThread(() =>
		//	{
		//		calendarPanel = CalendarPanel(helper.GetTemplateChild("MonthViewPanel"));
		//		var firstItem = CalendarViewDayItem(calendarPanel.Children.GetAt(0));
		//		firstItem.Focus(FocusState.Programmatic);
		//	});

		//	await WindowHelper.WaitForIdle();
		//	await RunOnUIThread(() =>
		//	{
		//		var focusedElement = CalendarViewDayItem(Input.FocusManager.GetFocusedElement(WindowHelper.XamlRoot));
		//		CalendarHelper.DumpDate(focusedElement.Date, "Focus starts from ");
		//		VERIFY_IS_TRUE(comparer(focusedElement.Date, minDate));
		//	});

		//	CommonInputHelper.Down(device);
		//	CommonInputHelper.Down(device);
		//	await WindowHelper.WaitForIdle();
		//	await RunOnUIThread(() =>
		//	{
		//		var focusedElement = CalendarViewDayItem(Input.FocusManager.GetFocusedElement(WindowHelper.XamlRoot));
		//		CalendarHelper.DumpDate(focusedElement.Date, "Pressed down twice, now focus is on ");
		//		VERIFY_IS_TRUE(comparer(focusedElement.Date, ConvertToDateTime(1, 2014, 9, 17)));
		//	});

		//	CommonInputHelper.Left(device);
		//	CommonInputHelper.Left(device);
		//	CommonInputHelper.Left(device);
		//	await WindowHelper.WaitForIdle();
		//	await RunOnUIThread(() =>
		//	{
		//		var focusedElement = CalendarViewDayItem(Input.FocusManager.GetFocusedElement(WindowHelper.XamlRoot));
		//		CalendarHelper.DumpDate(focusedElement.Date, "Pressed left 3 times, now focus is on ");
		//		VERIFY_IS_TRUE(comparer(focusedElement.Date, ConvertToDateTime(1, 2014, 9, 14)));
		//	});

		//	CommonInputHelper.Left(device);
		//	await WindowHelper.WaitForIdle();
		//	await RunOnUIThread(() =>
		//	{
		//		var focusedElement = CalendarViewDayItem(Input.FocusManager.GetFocusedElement(WindowHelper.XamlRoot));
		//		CalendarHelper.DumpDate(focusedElement.Date, "Pressed left again, now focus is on ");
		//		VERIFY_IS_TRUE(comparer(focusedElement.Date, ConvertToDateTime(1, 2014, 9, 14)));
		//	});

		//	CommonInputHelper.Right(device);
		//	CommonInputHelper.Right(device);
		//	CommonInputHelper.Right(device);
		//	CommonInputHelper.Right(device);
		//	CommonInputHelper.Right(device);
		//	CommonInputHelper.Right(device);
		//	await WindowHelper.WaitForIdle();
		//	await RunOnUIThread(() =>
		//	{
		//		var focusedElement = CalendarViewDayItem(Input.FocusManager.GetFocusedElement(WindowHelper.XamlRoot));
		//		CalendarHelper.DumpDate(focusedElement.Date, "Pressed right 6 times, now focus is on ");
		//		VERIFY_IS_TRUE(comparer(focusedElement.Date, ConvertToDateTime(1, 2014, 9, 20)));
		//	});

		//	CommonInputHelper.Right(device);
		//	await WindowHelper.WaitForIdle();
		//	await RunOnUIThread(() =>
		//	{
		//		var focusedElement = CalendarViewDayItem(Input.FocusManager.GetFocusedElement(WindowHelper.XamlRoot));
		//		CalendarHelper.DumpDate(focusedElement.Date, "Pressed right again, now focus is on ");
		//		VERIFY_IS_TRUE(comparer(focusedElement.Date, ConvertToDateTime(1, 2014, 9, 20)));
		//	});

		//	CommonInputHelper.Up(device);
		//	await WindowHelper.WaitForIdle();
		//	await RunOnUIThread(() =>
		//	{
		//		var focusedElement = CalendarViewDayItem(Input.FocusManager.GetFocusedElement(WindowHelper.XamlRoot));
		//		CalendarHelper.DumpDate(focusedElement.Date, "Pressed up once, now focus is on ");
		//		VERIFY_IS_TRUE(comparer(focusedElement.Date, ConvertToDateTime(1, 2014, 9, 13)));
		//	});
		//}

		[TestMethod]
		[Ignore("await TestServices.KeyboardHelper.PressKeySequence() not supported yet")]
		public async Task KeyboardNavigationTestCanTryToNavigateOutOfBoundary()
		{
			TestCleanupWrapper cleanup;

			Grid rootPanel = null;
			CalendarPanel calendarPanel = null;
			var helper = new CalendarHelper.CalendarViewHelper();

			CalendarView cv = await helper.GetCalendarView();
			CompareDate comparer = CalendarHelper.CompareDate;
			rootPanel = await CalendarHelper.CreateTestResources();

			var minDate = ConvertToDateTime(1, 2014, 9, 3); // Wednesday
			var maxDate = minDate;

			// load into visual tree
			await RunOnUIThread(() =>
			{
				cv.MinDate = minDate;
				cv.MaxDate = maxDate;
				cv.SetDisplayDate(minDate);
				rootPanel.Children.Add(cv);
			});

			await WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				calendarPanel = CalendarPanel(helper.GetTemplateChild("MonthViewPanel"));
				var firstItem = CalendarViewDayItem(calendarPanel.Children.GetAt(0));
				firstItem.Focus(FocusState.Programmatic);
			});


			await WindowHelper.WaitForIdle();
			await RunOnUIThread(() =>
			{
				CalendarHelper.CheckFocusedItem();
				var focusedElement = CalendarViewDayItem(Input.FocusManager.GetFocusedElement(WindowHelper.XamlRoot));
				CalendarHelper.DumpDate(focusedElement.Date, "Focus starts from ");
				VERIFY_IS_TRUE(comparer(focusedElement.Date, minDate));
			});

			await TestServices.KeyboardHelper.Down();
			await WindowHelper.WaitForIdle();
			await RunOnUIThread(() =>
			{
				// Cinch fails here - the focusedElement is not a CalendarVIewDayItem, however the focus should be still on the calendarViewDayItem and it is true locally.
				// add a check to see where is the focus now.
				CalendarHelper.CheckFocusedItem();
				var focusedElement = CalendarViewDayItem(Input.FocusManager.GetFocusedElement(WindowHelper.XamlRoot));
				CalendarHelper.DumpDate(focusedElement.Date, "Pressed down, now focus is on ");
				VERIFY_IS_TRUE(comparer(focusedElement.Date, minDate));
			});

			await TestServices.KeyboardHelper.Up();
			await WindowHelper.WaitForIdle();
			await RunOnUIThread(() =>
			{
				CalendarHelper.CheckFocusedItem();
				var focusedElement = CalendarViewDayItem(Input.FocusManager.GetFocusedElement(WindowHelper.XamlRoot));
				CalendarHelper.DumpDate(focusedElement.Date, "Pressed up, now focus is on ");
				VERIFY_IS_TRUE(comparer(focusedElement.Date, minDate));
			});


			await TestServices.KeyboardHelper.PressKeySequence("$d$_right#$u$_right");
			await WindowHelper.WaitForIdle();
			await RunOnUIThread(() =>
			{
				CalendarHelper.CheckFocusedItem();
				var focusedElement = CalendarViewDayItem(Input.FocusManager.GetFocusedElement(WindowHelper.XamlRoot));
				CalendarHelper.DumpDate(focusedElement.Date, "Pressed right, now focus is on ");
				VERIFY_IS_TRUE(comparer(focusedElement.Date, minDate));
			});

			await TestServices.KeyboardHelper.PressKeySequence("$d$_left#$u$_left");
			await WindowHelper.WaitForIdle();
			await RunOnUIThread(() =>
			{
				CalendarHelper.CheckFocusedItem();
				var focusedElement = CalendarViewDayItem(Input.FocusManager.GetFocusedElement(WindowHelper.XamlRoot));
				CalendarHelper.DumpDate(focusedElement.Date, "Pressed left, now focus is on ");
				VERIFY_IS_TRUE(comparer(focusedElement.Date, minDate));
			});


		}

		static bool IsCalendarItem(object @object)
		{
			if (@object is CalendarViewDayItem)
			{
				return true;
			}
			else
			{
				// only CalendarViewDayItem is public, so for the item in YearView and DecadeView (they are CalendarViewItems, which are not public)
				// we have to check:
				// 1. It is a control
				// 2. It's parent is CalendarPanel

				// we still can't make sure that this item is a CalendarItem but above check is good enough in this test.
				if (@object as Control != null)
				{
					var parent = VisualTreeHelper.GetParent(@object as DependencyObject);
					return (parent as CalendarPanel) != null;
				}

			}

			return false;
		}

		[TestMethod]
		[Ignore("UNO TODO - TestServices.KeyboardHelper not implemented yet.")]
		public async Task KeyboardNavigationTestTabTest()
		{
			TestCleanupWrapper cleanup;

			Grid rootPanel = null;
			CalendarPanel calendarPanel = null;
			var helper = new CalendarHelper.CalendarViewHelper();

			CalendarView cv = await helper.GetCalendarView();

			rootPanel = await CalendarHelper.CreateTestResources();

			// load into visual tree
			await RunOnUIThread(() =>
			{
				// make sure in decadeview, we have enough items so at least one navigation button is enabled
				// or we can't tab/shift-tab out from a focused item.
				cv.MinDate = ConvertToDateTime(1, 1900, 1, 1);
				cv.MaxDate = ConvertToDateTime(1, 2300, 12, 31);

				Button btn = new Button();
				btn.Content = "Button to accept focus since the navigation buttons are invisible by default now.";

				rootPanel.Children.Add(cv);
				rootPanel.Children.Add(btn);
			});

			await WindowHelper.WaitForIdle();

			(string panelName, CalendarViewDisplayMode displayMode)[] modes =
			{
				("MonthViewPanel", CalendarViewDisplayMode.Month),
				("YearViewPanel", CalendarViewDisplayMode.Year),
				("DecadeViewPanel", CalendarViewDisplayMode.Decade),
			};

			foreach (var mode in modes)
			{
				await RunOnUIThread(() =>
				{
					cv.DisplayMode = mode.displayMode;
				});

				await WindowHelper.WaitForIdle();

				await RunOnUIThread(() =>
				{
					calendarPanel = CalendarPanel(helper.GetTemplateChild(mode.panelName));
					// let's start from not the first or last item.
					var item = Control(calendarPanel.Children.GetAt(1));
					item.Focus(FocusState.Programmatic);
				});

				await WindowHelper.WaitForIdle();

				// tab out
				await TestServices.KeyboardHelper.Tab();
				await WindowHelper.WaitForIdle();
				await RunOnUIThread(() =>
				{
					var focusedElement = Input.FocusManager.GetFocusedElement(WindowHelper.XamlRoot);
					VERIFY_IS_TRUE(!IsCalendarItem(focusedElement));
				});

				// shift tab back
				await TestServices.KeyboardHelper.ShiftTab();
				await WindowHelper.WaitForIdle();
				await RunOnUIThread(() =>
				{
					var focusedElement = Input.FocusManager.GetFocusedElement(WindowHelper.XamlRoot);
					VERIFY_IS_TRUE(IsCalendarItem(focusedElement));
				});

				// shift tab out
				await TestServices.KeyboardHelper.ShiftTab();
				await WindowHelper.WaitForIdle();
				await RunOnUIThread(() =>
				{
					var focusedElement = Input.FocusManager.GetFocusedElement(WindowHelper.XamlRoot);
					VERIFY_IS_TRUE(!IsCalendarItem(focusedElement));
				});

				// tab back
				await TestServices.KeyboardHelper.Tab();
				await WindowHelper.WaitForIdle();
				await RunOnUIThread(() =>
				{
					var focusedElement = Input.FocusManager.GetFocusedElement(WindowHelper.XamlRoot);
					VERIFY_IS_TRUE(IsCalendarItem(focusedElement));
				});
			}
		}

		[TestMethod]
		[Ignore("await TestServices.KeyboardHelper.PressKeySequence() not supported yet")]
		public async Task KeyboardNavigationTestSpaceEnterTest()
		{
			TestCleanupWrapper cleanup;

			Grid rootPanel = null;
			CalendarPanel calendarPanel = null;
			var helper = new CalendarHelper.CalendarViewHelper();

			CalendarView cv = await helper.GetCalendarView();
			CompareDate comparer = CalendarHelper.CompareDate;

			rootPanel = await CalendarHelper.CreateTestResources();

			// in this test we'll focus on 2014/1/31, then switch to year 2015, month 2, we should see the day is adjusted to 28 (2/28/2015).
			var minDate = ConvertToDateTime(1, 2014, 1, 31);
			var maxDate = ConvertToDateTime(1, 2015, 9, 3);

			// load into visual tree
			await RunOnUIThread(() =>
			{
				cv.MinDate = minDate;
				cv.MaxDate = maxDate;
				cv.SetDisplayDate(minDate);
				rootPanel.Children.Add(cv);
			});

			await WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				calendarPanel = CalendarPanel(helper.GetTemplateChild("MonthViewPanel"));
				var firstItem = CalendarViewDayItem(calendarPanel.Children.GetAt(0));
				firstItem.Focus(FocusState.Programmatic);
			});

			await WindowHelper.WaitForIdle();
			await RunOnUIThread(() =>
			{
				CalendarHelper.CheckFocusedItem();
				var focusedElement = CalendarViewDayItem(Input.FocusManager.GetFocusedElement(WindowHelper.XamlRoot));
				CalendarHelper.DumpDate(focusedElement.Date, "Focus starts from ");
				VERIFY_IS_TRUE(comparer(focusedElement.Date, minDate));
			});

			// ctrl + up
			await TestServices.KeyboardHelper.PressKeySequence("$d$_ctrl#$d$_up#$u$_up#$u$_ctrl");
			await WindowHelper.WaitForIdle();
			await RunOnUIThread(() =>
			{
				CalendarHelper.CheckFocusedItem();
				// the focused element is calendar monthitem, which is not public, we can not verify the date of that item
				VERIFY_IS_TRUE(IsCalendarItem(Input.FocusManager.GetFocusedElement(WindowHelper.XamlRoot)));
				VERIFY_IS_TRUE(cv.DisplayMode == CalendarViewDisplayMode.Year);
			});

			// ctrl + up
			await TestServices.KeyboardHelper.PressKeySequence("$d$_ctrl#$d$_up#$u$_up#$u$_ctrl");
			await WindowHelper.WaitForIdle();
			await RunOnUIThread(() =>
			{
				CalendarHelper.CheckFocusedItem();
				// the focused element is calendar yearitem, which is not public, we can not verify the date of that item
				VERIFY_IS_TRUE(IsCalendarItem(Input.FocusManager.GetFocusedElement(WindowHelper.XamlRoot)));
				VERIFY_IS_TRUE(cv.DisplayMode == CalendarViewDisplayMode.Decade);
			});

			// ctrl + up
			await TestServices.KeyboardHelper.PressKeySequence("$d$_ctrl#$d$_up#$u$_up#$u$_ctrl");
			await WindowHelper.WaitForIdle();
			await RunOnUIThread(() =>
			{
				CalendarHelper.CheckFocusedItem();
				// nothing is changed this time.
				VERIFY_IS_TRUE(IsCalendarItem(Input.FocusManager.GetFocusedElement(WindowHelper.XamlRoot)));
				VERIFY_IS_TRUE(cv.DisplayMode == CalendarViewDisplayMode.Decade);
			});

			// right, ctrl + down - > to select year 2015 and switch back to Year mode
			await TestServices.KeyboardHelper.PressKeySequence("$d$_right#$u$_right#$d$_ctrl#$d$_down#$u$_down#$u$_ctrl");
			await WindowHelper.WaitForIdle();
			await RunOnUIThread(() =>
			{
				CalendarHelper.CheckFocusedItem();
				VERIFY_IS_TRUE(IsCalendarItem(Input.FocusManager.GetFocusedElement(WindowHelper.XamlRoot)));
				VERIFY_IS_TRUE(cv.DisplayMode == CalendarViewDisplayMode.Year);
			});

			// right, ctrl + down - > to select month Feburary and switch back to month mode
			// we should on 2/28/2015
			await TestServices.KeyboardHelper.PressKeySequence("$d$_right#$u$_right#$d$_ctrl#$d$_down#$u$_down#$u$_ctrl");
			await WindowHelper.WaitForIdle();
			await RunOnUIThread(() =>
			{
				CalendarHelper.CheckFocusedItem();
				var focusedElement = CalendarViewDayItem(Input.FocusManager.GetFocusedElement(WindowHelper.XamlRoot));
				CalendarHelper.DumpDate(focusedElement.Date, "Pressed right, ctrl + down, now focus is on ");
				VERIFY_IS_TRUE(comparer(focusedElement.Date, ConvertToDateTime(1, 2015, 2, 28)));
				VERIFY_IS_TRUE(cv.DisplayMode == CalendarViewDisplayMode.Month);
			});

			// ctrl + down . nothing should happen
			await TestServices.KeyboardHelper.PressKeySequence("$d$_ctrl#$d$_down#$u$_down#$u$_ctrl");
			await WindowHelper.WaitForIdle();
			await RunOnUIThread(() =>
			{
				CalendarHelper.CheckFocusedItem();
				var focusedElement = CalendarViewDayItem(Input.FocusManager.GetFocusedElement(WindowHelper.XamlRoot));
				CalendarHelper.DumpDate(focusedElement.Date, "Pressed ctrl + down, now focus is on ");
				VERIFY_IS_TRUE(comparer(focusedElement.Date, ConvertToDateTime(1, 2015, 2, 28)));
				VERIFY_IS_TRUE(cv.DisplayMode == CalendarViewDisplayMode.Month);
			});

		}

		[TestMethod]
		[Ignore("UNO TODO - InputHelper.ScrollMouseWheel() not implemented yet.")]
		public async Task SnapPointTest()
		{
			// Note: InputHelper.Flick does not like a flick, it is more like a pan. by default CalendarView
			// has optional snap point so we can't test the snap point with a "pan".

			// This test is designed to validate that CalendarView
			// 1. has a regular snap point on each row if the current viewport doesn't have enough space to show a full scope.
			// 2. has an irregular snap point on the first item of each scope if the current viewport has enough space to show a full scope.

			// So here we'll "hack" the snap point type to MandatorySingle so we can use InputHelper.ScrollMouseWheel to test this behavior.


			//struct ModeHelper
			//{
			//	string panelName;
			//	string scrollViewerName;
			//	CalendarViewDisplayMode displayMode;
			//	struct TestDataHelper
			//	{
			//		int row;
			//		int col;
			//		array < int, 3 > snapPoints; // each mode we test the first 3 snap points
			//	}
			//	;
			//	// in each mode we have two group of data to test: regular and irregular.
			//	array < TestDataHelper, 2 > testData;
			//}
			//;

			(string panelName,
				string scrollViewerName,
				CalendarViewDisplayMode displayMode,
				(
				int row,
				int col,
				int[] snapPoints // each mode we test the first 3 snap points
				)[] testData)[] modes =
				{

					(
						"MonthViewPanel", "MonthViewScrollViewer", CalendarViewDisplayMode.Month,
						new [] {
								// dimension 6 x 7, irregular snap point (per scope).
								(
									6, 7, new []
										{
											5, 9, 13
										}

								),
								// dimension 4 x 7, regular snap point (per row).
								(
									4, 7, new []
										{
											1, 2, 3
										}

								)
						}
					),

					(
						"YearViewPanel", "YearViewScrollViewer", CalendarViewDisplayMode.Year,
						new []
							{
								// dimension 4 x 4, irregular snap point (per scope).
								(
									4, 4, new []
										{
											3, 6, 9
										}
								),
								// dimension 2 x 4, regular snap point (per row).
								(
									2, 4, new []
										{
											1, 2, 3
										}
								)

						}
					),

					(
						"DecadeViewPanel", "DecadeViewScrollViewer", CalendarViewDisplayMode.Decade,
						new []
							{
								// dimension 4 x 4, irregular snap point (per scope).
								(
									4, 4, new []
										{
											2, 5, 7
										}
								),
								// dimension 2 x 4, regular snap point (per row).
								(
									2, 4, new []
										{
											1, 2, 3
										}
								)
							}
					)
			};

			foreach (var mode in modes)
			{
				foreach (var testData in mode.testData)
				{
					TestCleanupWrapper cleanup;

					Grid rootPanel = null;
					CalendarPanel calendarPanel = null;
					ScrollViewer scrollViewer = null;
					var helper = new CalendarHelper.CalendarViewHelper();

					CalendarView cv = await helper.GetCalendarView();

					rootPanel = await CalendarHelper.CreateTestResources();

					// load into visual tree
					await RunOnUIThread(() =>
					{
						cv.MinDate = ConvertToDateTime(1, 2000, 1, 1);
						cv.MaxDate = ConvertToDateTime(1, 2300, 12, 31);
						cv.SetDisplayDate(cv.MinDate);
						rootPanel.Children.Add(cv);
					});

					await WindowHelper.WaitForIdle();

					await RunOnUIThread(() =>
					{
						cv.DisplayMode = mode.displayMode;
						cv.UpdateLayout();
						scrollViewer = ScrollViewer(helper.GetTemplateChild(mode.scrollViewerName));
						calendarPanel = CalendarPanel(helper.GetTemplateChild(mode.panelName));
						// hack the snap point type.
						scrollViewer.VerticalSnapPointsType = SnapPointsType.MandatorySingle;

						// setup dimension
						if (mode.displayMode == CalendarViewDisplayMode.Month)
						{
							cv.NumberOfWeeksInView =
								testData
									.row; // for month mode, just simply ignore the col (because it is hardcoded to 7)
						}
						else
						{
							cv.SetYearDecadeDisplayDimensions(testData.col, testData.row);
						}
					});

					LOG_OUTPUT("Mode: %d (0 - Month, 1 - Year, 2 - Decade.), dimension %d (col) x %d (row).",
						mode.displayMode, testData.col, testData.row);

					await WindowHelper.WaitForIdle();

					var viewChangedEvent = new Event();
					var viewChangedRegistration = CreateSafeEventRegistration<ScrollViewer, EventHandler<ScrollViewerViewChangedEventArgs>>("ViewChanged");

					viewChangedRegistration.Attach(scrollViewer,
						(sender, e) =>
						{
							if (!e.IsIntermediate)
							{
								viewChangedEvent.Set();
							}
						});

					double itemHeight = 0; // we need the item height to determine which row the viewport snaps to.
					double itemTopMargin = 0;

					await RunOnUIThread(() =>
					{
						itemHeight = FrameworkElement(calendarPanel.Children.GetAt(0))
							.ActualHeight;
						var margin = FrameworkElement(calendarPanel.Children.GetAt(0)).Margin;
						itemHeight += margin.Top + margin.Bottom;
						itemTopMargin = margin.Top;
					});


					for (var i = 0; i < testData.snapPoints.Length; i++)
					{
						LOG_OUTPUT("scroll down to next snap point and wait for viewchanged event.");
						TestServices.InputHelper.ScrollMouseWheel(cv, -1);

						await viewChangedEvent.WaitForDefault();
						viewChangedEvent.Reset();

						// check the relative distance between Panel and ScrollViewer, divide this distance by the itemHeight to get the
						// row that we snapped to.
						await RunOnUIThread(() =>
						{
							var distance = scrollViewer.TransformToVisual(calendarPanel)
								.TransformPoint(new global::Windows.Foundation.Point(0, 0));
							distance.Y -= (float)(itemTopMargin);
							LOG_OUTPUT("actual position %f, expected %f", distance.Y,
								itemHeight * testData.snapPoints[i]);
							VERIFY_IS_TRUE(
								CalendarHelper.AreClose(distance.Y / testData.snapPoints[i], itemHeight, 1.0));
						});
					}
				}
			}
		}

		[TestMethod]
#if __SKIA__
		[Ignore("Currently flaky on Skia, part of #9080 epic")]
#endif
		public async Task CanChangeDisplayModeBeforeLoaded()
		{
			TestCleanupWrapper cleanup;

			Grid rootPanel = null;
			var helper = new CalendarHelper.CalendarViewHelper();

			CalendarView cv = await helper.GetCalendarView();

			rootPanel = await CalendarHelper.CreateTestResources();

			// load into visual tree
			await RunOnUIThread(() =>
			{
				// change display mode before loaded.
				cv.DisplayMode = CalendarViewDisplayMode.Year;
				rootPanel.Children.Add(cv);
			});

			await WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				Grid monthViewGrid = Grid(helper.GetTemplateChild("MonthView"));
				ScrollViewer yearScrollViewer = ScrollViewer(helper.GetTemplateChild("YearViewScrollViewer"));

				TestServices.VERIFY_IS_NOT_NULL(monthViewGrid);
				VERIFY_ARE_EQUAL(monthViewGrid.Opacity, 0);

				TestServices.VERIFY_IS_NOT_NULL(yearScrollViewer);
				VERIFY_ARE_EQUAL(yearScrollViewer.Visibility, Visibility.Visible);
				VERIFY_ARE_EQUAL(yearScrollViewer.Opacity, 1.0);
			});
		}

		[TestMethod]
#if __ANDROID__
		[Ignore("UNO TODO - This test is failing on Android")]
#endif
		public async Task VerifyTransitionAnimation()
		{
			TestCleanupWrapper cleanup;

			Grid rootPanel = null;
			var helper = new CalendarHelper.CalendarViewHelper();

			CalendarView cv = await helper.GetCalendarView();

			rootPanel = await CalendarHelper.CreateTestResources();

			// load into visual tree
			await RunOnUIThread(() =>
			{
				rootPanel.Children.Add(cv);
			});

			await WindowHelper.WaitForIdle();


			Storyboard monthToYearTransitionStoryboard = null;
			await RunOnUIThread(() =>
			{
				// find the root border and find the Month to Year transition.
				var rootBorder = Border(VisualTreeHelper.GetChild(cv, 0));
				TestServices.VERIFY_IS_NOT_NULL(rootBorder);
				var vsgs = VisualStateManager.GetVisualStateGroups(rootBorder);

				VisualTransition monthToYearTransition = null;

				foreach (var vsg in vsgs)
				{
					if (vsg.Name == "DisplayModeStates")
					{
						foreach (var transition in vsg.Transitions)
						{
							if (transition.From == "Month" && transition.To == "Year")
							{
								monthToYearTransition = transition;
								break;
							}
						}
						if (monthToYearTransition != null)
						{
							break;
						}
					}
				}

				TestServices.VERIFY_IS_NOT_NULL(monthToYearTransition);
				monthToYearTransitionStoryboard = monthToYearTransition.Storyboard;
				TestServices.VERIFY_IS_NOT_NULL(monthToYearTransitionStoryboard);
			});

			var storyboardCompletedEvent = new Event();
			var storyboardCompletedRegistration = CreateSafeEventRegistration<Storyboard, EventHandler<object>>("Completed");

			using var _ = storyboardCompletedRegistration.Attach(monthToYearTransitionStoryboard,
				(snd, evt) =>
				{
					storyboardCompletedEvent.Set();
				});

			await RunOnUIThread(() =>
			{
				cv.DisplayMode = CalendarViewDisplayMode.Year;
			});

			await storyboardCompletedEvent.WaitForDefault();
			VERIFY_IS_TRUE(storyboardCompletedEvent.HasFired());

			await RunOnUIThread(() =>
			{
				Grid monthViewGrid = Grid(helper.GetTemplateChild("MonthView"));
				ScrollViewer yearScrollViewer = ScrollViewer(helper.GetTemplateChild("YearViewScrollViewer"));

				TestServices.VERIFY_IS_NOT_NULL(monthViewGrid);
				VERIFY_ARE_EQUAL(monthViewGrid.Opacity, 0);

				TestServices.VERIFY_IS_NOT_NULL(yearScrollViewer);
				VERIFY_ARE_EQUAL(yearScrollViewer.Visibility, Visibility.Visible);
				VERIFY_ARE_EQUAL(yearScrollViewer.Opacity, 1.0);

			});
		}

		[TestMethod]
		public async Task VerifyHeaderTransitionAnimation()
		{
			TestCleanupWrapper cleanup;

			Grid rootPanel = null;
			var helper = new CalendarHelper.CalendarViewHelper();

			CalendarView cv = await helper.GetCalendarView();

			rootPanel = await CalendarHelper.CreateTestResources();

			// load into visual tree
			await RunOnUIThread(() =>
			{
				rootPanel.Children.Add(cv);
			});

			await WindowHelper.WaitForIdle();

			Storyboard headerTransitionStoryboard = null;
			await RunOnUIThread(() =>
			{
				// find the root border and find the Month to Year transition.
				var rootBorder = Border(VisualTreeHelper.GetChild(cv, 0));
				TestServices.VERIFY_IS_NOT_NULL(rootBorder);
				var vsgs = VisualStateManager.GetVisualStateGroups(rootBorder);

				VisualState changingState = null;

				foreach (var vsg in vsgs)
				{
					if (vsg.Name == "HeaderButtonStates")
					{
						foreach (var state in vsg.States)
						{
							if (state.Name == "ViewChanging")
							{
								changingState = state;
								break;
							}
						}
						if (changingState != null)
						{
							break;
						}
					}
				}

				TestServices.VERIFY_IS_NOT_NULL(changingState);
				headerTransitionStoryboard = changingState.Storyboard;
				TestServices.VERIFY_IS_NOT_NULL(headerTransitionStoryboard);
			});

			var storyboardCompletedEvent = new Event();
			var storyboardCompletedRegistration =
				CreateSafeEventRegistration<Storyboard, EventHandler<object>>("Completed");

			using var _ = storyboardCompletedRegistration.Attach(headerTransitionStoryboard,
				(s, e) =>
				{
					storyboardCompletedEvent.Set();
				});

			await RunOnUIThread(() =>
			{
				// switching view will trigger transition animation on Header.
				cv.DisplayMode = CalendarViewDisplayMode.Year;
			});
			await storyboardCompletedEvent.WaitForDefault();
			VERIFY_IS_TRUE(storyboardCompletedEvent.HasFired());
		}

		[TestMethod]
		[Ignore("UNO TODO")]
		public async Task CanChangeLanguage()
		{
			TestCleanupWrapper cleanup;

			Grid rootPanel = null;

			var helper = new CalendarHelper.CalendarViewHelper();

			CalendarView cv = await helper.GetCalendarView();

			rootPanel = await CalendarHelper.CreateTestResources();

			// load into visual tree
			await RunOnUIThread(() =>
			{
				rootPanel.Children.Add(cv);
			});

			await WindowHelper.WaitForIdle();


			await RunOnUIThread(() =>
			{
				// We should check the item text after we changed the language.
				// However the item text is chrome and it doesn't exist in visual tree
				// so we can't validate item text. Instead we can validate the weekday names,
				// which is also effected by languages.

				Grid weekDayNamesGrid = Grid(helper.GetTemplateChild("WeekDayNames"));
				TextBlock firstWeekDay = TextBlock(weekDayNamesGrid.Children.GetAt(0));
				//VERIFY_ARE_EQUAL(firstWeekDay.Text, "Su");
				VERIFY_ARE_EQUAL(firstWeekDay.Text, "Sun");

				cv.Language = "zh-CN";
				LOG_OUTPUT("Change languages to Chinese.");
				cv.UpdateLayout();
				VERIFY_ARE_EQUAL(firstWeekDay.Text, "日");
			});

		}

		[TestMethod]
		[Ignore("UNO TODO - InputHelper.MoveMouse() not implemented")]
		public async Task ValidateDCompTreeWithPointerOverNavigationButton()
		{
			//WUCRenderingScopeGuard guard(DCompRendering.WUCCompleteSynchronousCompTree, false /* resizeWindow */);

			TestCleanupWrapper cleanup;

			StackPanel rootPanel = null;
			Button nextButton = null;
			CalendarView calendarView = null;
			Button button = null;

			var pointerEnteredEvent = new Event();
			var pointerEnteredRegistration = CreateSafeEventRegistration<Button, PointerEventHandler>("PointerEntered");

			await RunOnUIThread(() =>
			{
				rootPanel = StackPanel(XamlReader.Load(
					@"<StackPanel Width=""400"" Height=""400"" xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"" xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
								HorizontalAlignment=""Center"" VerticalAlignment=""Center"">
								<Button x:Name=""button"" Content=""Discard Button"" Margin=""20,40,20,0"" />
								<CalendarView x:Name=""calendarview"" />
							</StackPanel>"));

				calendarView = CalendarView(rootPanel.FindName("calendarview"));

				button = Button(rootPanel.FindName("button"));
				TestServices.VERIFY_IS_NOT_NULL(button);

				TestServices.WindowHelper.WindowContent = rootPanel;
			});

			await WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				nextButton = Button(TreeHelper.GetVisualChildByName(calendarView, "NextButton"));
				TestServices.VERIFY_IS_NOT_NULL(nextButton);
			});
			pointerEnteredRegistration.Attach(nextButton, (s, e) =>
			{
				pointerEnteredEvent.Set();
			});

			await WindowHelper.WaitForIdle();
			LOG_OUTPUT("Make sure mouse pointer is away from CalendarView...");
			TestServices.InputHelper.MoveMouse(button);
			await WindowHelper.WaitForIdle();

			LOG_OUTPUT("Move mouse to nextButton.");
			TestServices.InputHelper.MoveMouse(nextButton);
			await pointerEnteredEvent.WaitForDefault();
			await WindowHelper.WaitForIdle();

			LOG_OUTPUT("Validate the dark theme of the overlay.");
			await RunOnUIThread(() =>
			{
				rootPanel.RequestedTheme = ElementTheme.Dark;
			});
			await WindowHelper.WaitForIdle();
			TestServices.Utilities.VerifyMockDCompOutput(MockDComp.SurfaceComparison.NoComparison, "Dark");

			LOG_OUTPUT("Validate the light theme of the overlay.");
			await RunOnUIThread(() =>
			{
				rootPanel.RequestedTheme = ElementTheme.Light;
			});
			await WindowHelper.WaitForIdle();
			TestServices.Utilities.VerifyMockDCompOutput(MockDComp.SurfaceComparison.NoComparison, "Light");

			LOG_OUTPUT("Validate the high-contrast theme of the overlay.");
			await RunOnUIThread(() =>
			{
				//TestServices.ThemingHelper.HighContrastTheme = HighContrastTheme.Test;
			});
			await WindowHelper.WaitForIdle();
			TestServices.Utilities.VerifyMockDCompOutput(MockDComp.SurfaceComparison.NoComparison, "HC");
		}

		[TestMethod]
#if __ANDROID__
		[Ignore("UNO TODO - This test is failing on Android")]
#endif
		public async Task ValidateDCompTree()
		{
			//WUCRenderingScopeGuard guard(DCompRendering.WUCCompleteSynchronousCompTree, false /* resizeWindow */);
			//TestServices.WindowHelper.SetWindowSizeOverride(wf.Count(400, 400));

			var helper = new CalendarHelper.CalendarViewHelper();

			Grid rootPanel = null;
			CalendarView cv = await helper.GetCalendarView();

			rootPanel = await CalendarHelper.CreateTestResources();

			// load into visual tree
			await RunOnUIThread(() =>
			{
				cv.MinDate = ConvertToDateTime(1, 2014, 1, 1);
				cv.MaxDate = ConvertToDateTime(1, 2014, 1, 1);
				cv.SelectedDates.Add(ConvertToDateTime(1, 2014, 1, 1));
				CalendarHelper.ReplaceAccentColorForTesting(cv);
				rootPanel.Children.Add(cv);
			});

			await WindowHelper.WaitForIdle();

			LOG_OUTPUT("Validate dark theme");
			await WindowHelper.WaitForIdle();
			TestServices.Utilities.VerifyMockDCompOutput(MockDComp.SurfaceComparison.NoComparison, "dark");

			await RunOnUIThread(() =>
			{
				cv.DisplayMode = CalendarViewDisplayMode.Year;
			});

			LOG_OUTPUT("Validate Year mode");
			await WindowHelper.WaitForIdle();
			TestServices.Utilities.VerifyMockDCompOutput(MockDComp.SurfaceComparison.NoComparison, "year");

			await RunOnUIThread(() =>
			{
				cv.DisplayMode = CalendarViewDisplayMode.Decade;
			});

			LOG_OUTPUT("Validate decade mode");
			await WindowHelper.WaitForIdle();
			TestServices.Utilities.VerifyMockDCompOutput(MockDComp.SurfaceComparison.NoComparison, "decade");

			var calendarIdentifiers = new[]
				{
					"PersianCalendar",
					//"GregorianCalendar", // skip this. this is the default calendar and we've verified already.
					"HebrewCalendar",
					"HijriCalendar",
					"JapaneseCalendar",
					"JulianCalendar",
					"KoreanCalendar",
					"TaiwanCalendar",
					"ThaiCalendar",
					"UmAlQuraCalendar"
				};

			await RunOnUIThread(() =>
			{
				cv.DisplayMode = CalendarViewDisplayMode.Month;
			});

			foreach (var cid in calendarIdentifiers)
			{
				await RunOnUIThread(() =>
				{
					cv.CalendarIdentifier = cid;
				});
				await WindowHelper.WaitForIdle();
				var masterFileName = new char[3];
				masterFileName[0] = cid[0];
				masterFileName[1] = cid[1];
				masterFileName[2] = '\0';
				LOG_OUTPUT("Validate %s", cid);

				await WindowHelper.WaitForIdle();
				TestServices.Utilities.VerifyMockDCompOutput(MockDComp.SurfaceComparison.NoComparison,
					new string(masterFileName));
			}

			await RunOnUIThread(() =>
			{
				cv.DisplayMode = CalendarViewDisplayMode.Month;
				cv.IsEnabled = false;
			});

			LOG_OUTPUT("Validate disabled + month mode");
			await WindowHelper.WaitForIdle();
			TestServices.Utilities.VerifyMockDCompOutput(MockDComp.SurfaceComparison.NoComparison, "disabled");

			/* Light Themed testing blocked by TFS #1063901
			//Change theme, validate again.
			await RunOnUIThread(() =>
			{
			  cv.RequestedTheme = xaml.ElementTheme.Light;
			});

			LOG_OUTPUT("Validate light theme");
			await WindowHelper.WaitForIdle();
			TestServices.Utilities.VerifyMockDCompOutput(MockDComp.SurfaceComparison.NoComparison, "light");
			*/
		}

		[TestMethod]
		public async Task ValidateDCompTreeWithCompositionBrush()
		{
			//WUCRenderingScopeGuard guard(DCompRendering.WUCCompleteSynchronousCompTree, false /* resizeWindow */);
			//TestServices.WindowHelper.SetWindowSizeOverride(wf.Count(400, 400));

			var helper = new CalendarHelper.CalendarViewHelper();
			Grid rootPanel = null;
			CalendarView cv = await helper.GetCalendarView();
			rootPanel = await CalendarHelper.CreateTestResources();

			// load into visual tree
			await RunOnUIThread(() =>
			{
				cv.MinDate = ConvertToDateTime(1, 2014, 1, 1);
				cv.MaxDate = ConvertToDateTime(1, 2014, 1, 1);
				cv.SelectedDates.Add(ConvertToDateTime(1, 2014, 1, 1));

				cv.CalendarItemBorderBrush = new SolidColorBrush { Color = Colors.Purple };
				cv.CalendarItemBackground = new SolidColorBrush { Color = Colors.Purple };
				rootPanel.Children.Add(cv);
			});

			await WindowHelper.WaitForIdle();
			TestServices.Utilities.VerifyMockDCompOutput(MockDComp.SurfaceComparison.NoComparison);
		}

		[TestMethod]
		public async Task ValidateDCompTreeWithLinearGradientBrush()
		{
			//WUCRenderingScopeGuard guard(DCompRendering.WUCCompleteSynchronousCompTree, false /* resizeWindow */);
			//TestServices.WindowHelper.SetWindowSizeOverride(wf.Count(400, 400));

			var helper = new CalendarHelper.CalendarViewHelper();
			Grid rootPanel = null;
			CalendarView cv = await helper.GetCalendarView();
			rootPanel = await CalendarHelper.CreateTestResources();

			// load into visual tree
			await RunOnUIThread(() =>
			{
				cv.MinDate = ConvertToDateTime(1, 2014, 1, 1);
				cv.MaxDate = ConvertToDateTime(1, 2014, 1, 1);
				cv.SelectedDates.Add(ConvertToDateTime(1, 2014, 1, 1));

				cv.CalendarItemBorderBrush = MakeLinearGradientBrush(0.0f, 1.0f);
				cv.CalendarItemBackground = MakeLinearGradientBrush(0.0f, 1.0f);
				rootPanel.Children.Add(cv);
			});

			await WindowHelper.WaitForIdle();
			TestServices.Utilities.VerifyMockDCompOutput(MockDComp.SurfaceComparison.NoComparison);
		}

		[TestMethod]
		[Ignore("ControlHelper.ValidateUIElementTree() not supported yet.")]
		public void ValidateUIElementTree()
		{
			ControlHelper.ValidateUIElementTree(
					new Size(400, 600),
					1,
				// Test setup.
				async () =>
			{
				var helper = new CalendarHelper.CalendarViewHelper();
				Xaml.Controls.CalendarView cv = await helper.GetCalendarView();

				Grid root = null;
				root = await CalendarHelper.CreateTestResources();

				// load into visual tree
				await RunOnUIThread(() =>
				{
					cv.MinDate = ConvertToDateTime(1, 2010, 1, 1);
					cv.MaxDate = ConvertToDateTime(1, 2014, 1, 1);
					root.Children.Add(cv);
				});
				await WindowHelper.WaitForIdle();

				await RunOnUIThread(() =>
				{
					// In test environment sometime the CalendarView doesn't have focus.
					// To get a consistent result, we move the focus to it forcedly.
					cv.Focus(FocusState.Pointer);
				});
				await WindowHelper.WaitForIdle();

				return root;
			});
		}

		[TestMethod]
		[Ignore("TODO UNO - For some reason this test is producing a StackOverflow exception.")]
		public async Task ChangeStylePropsDynamically()
		{
			TestCleanupWrapper cleanup;

			Grid rootPanel = null;

			var helper = new CalendarHelper.CalendarViewHelper();

			CalendarView cv = await helper.GetCalendarView();


			//WUCRenderingScopeGuard guard(DCompRendering.WUCCompleteSynchronousCompTree, false /* resizeWindow */);
			//TestServices.WindowHelper.SetWindowSizeOverride(wf.Count(400, 400));

			rootPanel = await CalendarHelper.CreateTestResources();

			// load into visual tree
			await RunOnUIThread(() =>
			{
				// make sure no virutalization, or DComp comparison will fail.
				cv.MinDate = ConvertToDateTime(1, 2014, 1, 1);
				cv.MaxDate = ConvertToDateTime(1, 2014, 1, 1);
				cv.SelectedDates.Add(ConvertToDateTime(1, 2014, 1, 1));
				CalendarHelper.ReplaceAccentColorForTesting(cv);
				rootPanel.Children.Add(cv);
			});

			await WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				cv.SelectedForeground = new SolidColorBrush(Microsoft.UI.Colors.Red);
				cv.BorderThickness = new Thickness(3);
				cv.HorizontalDayItemAlignment = HorizontalAlignment.Left;
				cv.HorizontalFirstOfMonthLabelAlignment = HorizontalAlignment.Right;
				cv.CalendarItemBorderBrush = new SolidColorBrush(Microsoft.UI.Colors.Yellow);
				cv.CalendarViewDayItemStyle = (Style)(XamlReader.Load(
					"<Style xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' TargetType='CalendarViewDayItem'>" +
					" <Setter Property='MinWidth' Value='50' />" +
					" <Setter Property='MinHeight' Value='50' />" +
					// Bug 1174125:CalendarView: change dayitem's template will cause day text disappear
					// now changing the template will not remove the chrom textblock from visual tree.
					" <Setter Property='Template'>" +
					"  <Setter.Value>" +
					"   <ControlTemplate TargetType='CalendarViewDayItem'/>" +
					"  </Setter.Value>" +
					" </Setter>" +
					"</Style>"));
			});
			await WindowHelper.WaitForIdle();
			TestServices.Utilities.VerifyMockDCompOutput(MockDComp.SurfaceComparison.NoComparison);
		}

		[TestMethod]
		public async Task ValidateScopeChange()
		{
			TestCleanupWrapper cleanup;

			Grid rootPanel = null;

			var helper = new CalendarHelper.CalendarViewHelper();

			CalendarView cv = await helper.GetCalendarView();

			//WUCRenderingScopeGuard guard(DCompRendering.WUCCompleteSynchronousCompTree, false /* resizeWindow */);
			//TestServices.WindowHelper.SetWindowSizeOverride(wf.Count(400, 400));

			rootPanel = await CalendarHelper.CreateTestResources();

			// load into visual tree
			await RunOnUIThread(() =>
			{
				// make sure no virtualization, or DComp comparison will fail.
				cv.MinDate = ConvertToDateTime(1, 2014, 9, 27);
				cv.MaxDate = ConvertToDateTime(1, 2014, 10, 20);
				CalendarHelper.ReplaceAccentColorForTesting(cv);
				rootPanel.Children.Add(cv);
			});


			await WindowHelper.WaitForIdle();
			TestServices.Utilities.VerifyMockDCompOutput(MockDComp.SurfaceComparison.NoComparison);
		}

		[TestMethod]
		[Ignore("UNO-TODO: Test fails Debug.Assert()")]
		public async Task CanSetDayItemStyle()
		{
			TestCleanupWrapper cleanup;

			Grid rootPanel = null;

			var helper = new CalendarHelper.CalendarViewHelper();

			CalendarView cv = await helper.GetCalendarView();


			//WUCRenderingScopeGuard guard(DCompRendering.WUCCompleteSynchronousCompTree, false /* resizeWindow */);
			//TestServices.WindowHelper.SetWindowSizeOverride(wf.Count(400, 400));

			rootPanel = await CalendarHelper.CreateTestResources();

			// load into visual tree
			await RunOnUIThread(() =>
			{
				// make sure no virutalization, or DComp comparison will fail.
				cv.MinDate = ConvertToDateTime(1, 2014, 1, 1);
				cv.MaxDate = ConvertToDateTime(1, 2014, 1, 1);
				cv.SelectedDates.Add(ConvertToDateTime(1, 2014, 1, 1));
				CalendarHelper.ReplaceAccentColorForTesting(cv);

				cv.CalendarViewDayItemStyle = (Style)XamlReader.Load(
					"<Style xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' TargetType='CalendarViewDayItem'>" +
					" <Setter Property='MinWidth' Value='50' />" +
					" <Setter Property='MinHeight' Value='50' />" +
					"</Style>");

				rootPanel.Children.Add(cv);
			});


			await WindowHelper.WaitForIdle();
			TestServices.Utilities.VerifyMockDCompOutput(MockDComp.SurfaceComparison.NoComparison);
		}

		private async Task VerifyChangingCalendarIdentifier(CalendarViewDisplayMode displayMode)
		{
			TestCleanupWrapper cleanup;
			Grid rootPanel = null;
			var helper = new CalendarHelper.CalendarViewHelper();
			CalendarView cv = await helper.GetCalendarView();
			rootPanel = await CalendarHelper.CreateTestResources();

			//TestServices.WindowHelper.SetWindowSizeOverride(wf.Count(400, 400));

			var cids = GetAllSupportedCalendarIdentifiers();

			// load into visual tree
			await RunOnUIThread(() =>
			{
				rootPanel.Children.Add(cv);
			});

			LOG_OUTPUT(" Change DisplayMode to %s", displayMode.ToString());

			await RunOnUIThread(() =>
			{
				cv.DisplayMode = displayMode;
			});

			await WindowHelper.WaitForIdle();

			foreach (var cid in cids)
			{
				LOG_OUTPUT("Begin Testing CalendarIdentifier to %s", cid);
				// change calendar identifier
				await RunOnUIThread(() =>
				{
					cv.CalendarIdentifier = cid;
				});

				await WindowHelper.WaitForIdle();

				LOG_OUTPUT("End Testing CalendarIdentifier to %s", cid);
			}

		}

		[TestMethod]
#if __ANDROID__
		[Ignore("UNO TODO - This this is crashing the app on Android")]
#endif
		public async Task CanChangeMonthCalendarIdentifier()
		{
			await VerifyChangingCalendarIdentifier(CalendarViewDisplayMode.Month);
		}

		[TestMethod]
		[Ignore("UNO TODO - Dynamic change of DisplayMode raising exception")]
		public async Task CanChangeYearCalendarIdentifier()
		{
			await VerifyChangingCalendarIdentifier(CalendarViewDisplayMode.Year);
		}

		[TestMethod]
		[Ignore("await TestServices.KeyboardHelper.PressKeySequence() not supported yet")]
		public async Task CanChangeDecadeCalendarIdentifier()
		{
			await VerifyChangingCalendarIdentifier(CalendarViewDisplayMode.Decade);
		}

		async Task VerifyBoundaries(string cid, string panelName, CalendarViewDisplayMode
			displayMode)
		{
			TestCleanupWrapper cleanup;
			Grid rootPanel = null;
			var helper = new CalendarHelper.CalendarViewHelper();
			CalendarView cv = await helper.GetCalendarView();
			rootPanel = await CalendarHelper.CreateTestResources();
			var defaultCalendar = new global::Windows.Globalization.Calendar();
			var today = defaultCalendar.GetDateTime();

			//TestServices.WindowHelper.SetWindowSizeOverride(wf.Count(400, 400));

			LOG_OUTPUT("Begin Testing CalendarIdentifier to %s", cid);

			// load into visual tree
			await RunOnUIThread(() =>
			{
				rootPanel.Children.Add(cv);
				// set MinDate/MaxDate to the maximium range that calendar can support.
				cv.CalendarIdentifier = cid;
				var cal = new global::Windows.Globalization.Calendar(defaultCalendar.Languages, cid,
					defaultCalendar.GetClock());
				cal.SetToMin();
				cv.MinDate = cal.GetDateTime();
				cal.SetToMax();
				cv.MaxDate = cal.GetDateTime();
			});

			await WindowHelper.WaitForIdle();

			LOG_OUTPUT(" Change DisplayMode to %s", displayMode.ToString());
			await RunOnUIThread(() =>
			{
				cv.DisplayMode = displayMode;
			});

			await WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				// move focus to the items so we can test keyboard navigations.
				var calendarPanel = CalendarPanel(helper.GetTemplateChild(panelName));
				var firstItem = Control(calendarPanel.Children.GetAt(0));
				firstItem.Focus(FocusState.Programmatic);

				cv.SetDisplayDate(cv.MinDate);
			});

			LOG_OUTPUT("  Jump to the beginning");
			await WindowHelper.WaitForIdle();

			// at the beginning, we try to cross the boundary.
			// press Down once, Up twice,
			// press Right once, Left twice,
			// press PgDn once, PgUp twice

			await TestServices.KeyboardHelper.Down();
			await TestServices.KeyboardHelper.Up();
			await TestServices.KeyboardHelper.Up();
			await WindowHelper.WaitForIdle();
			await TestServices.KeyboardHelper.PressKeySequence("$d$_right#$u$_right#$d$_left#$u$_left#$d$_left#$u$_left");
			await WindowHelper.WaitForIdle();
			await TestServices.KeyboardHelper.PressKeySequence("$d$_pagedown#$u$_pagedown#$d$_pageup#$u$_pageup#$d$_pageup#$u$_pageup");
			await WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				cv.SetDisplayDate(today);
			});

			LOG_OUTPUT("  Jump to today");
			await WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				cv.SetDisplayDate(cv.MaxDate);
			});

			LOG_OUTPUT("  Jump to the end");
			await WindowHelper.WaitForIdle();

			// at the end, we try to cross the boundary.
			// press Up once, Down twice,
			// press Left once, Right twice,
			// press PgUp once, PgDown twice

			await TestServices.KeyboardHelper.Up();
			await TestServices.KeyboardHelper.Down();
			await TestServices.KeyboardHelper.Down();
			await WindowHelper.WaitForIdle();
			await TestServices.KeyboardHelper.PressKeySequence("$d$_left#$u$_left#$d$_right#$u$_right#$d$_right#$u$_right");
			await WindowHelper.WaitForIdle();
			await TestServices.KeyboardHelper.PressKeySequence(
				"$d$_pageup#$u$_pageup#$d$_pagedown#$u$_pagedown#$d$_pagedown#$u$_pagedown");
			await WindowHelper.WaitForIdle();

			LOG_OUTPUT("End Testing CalendarIdentifier to %s", cid);
		}

		[TestMethod]
		[Ignore("UNO TODO - TestServices.KeyboardHelper not implemented yet.")]
		public async Task VerifyPersianCalendarMonthBoundaries()
		{
			await VerifyBoundaries("PersianCalendar", "MonthViewPanel", CalendarViewDisplayMode.Month);
		}

		[TestMethod]
		[Ignore("UNO TODO - TestServices.KeyboardHelper not implemented yet.")]
		public async Task VerifyPersianCalendarYearBoundaries()
		{
			await VerifyBoundaries("PersianCalendar", "YearViewPanel", CalendarViewDisplayMode.Year);
		}

		[TestMethod]
		[Ignore("UNO TODO - TestServices.KeyboardHelper not implemented yet.")]
		public async Task VerifyPersianCalendarDecadeBoundaries()
		{
			await VerifyBoundaries("PersianCalendar", "DecadeViewPanel", CalendarViewDisplayMode.Decade);
		}

		[TestMethod]
		[Ignore("UNO TODO - TestServices.KeyboardHelper not implemented yet.")]
		public async Task VerifyGregorianCalendarMonthBoundaries()
		{
			await VerifyBoundaries("GregorianCalendar", "MonthViewPanel", CalendarViewDisplayMode.Month);
		}

		[TestMethod]
		[Ignore("UNO TODO - TestServices.KeyboardHelper not implemented yet.")]
		public async Task VerifyGregorianCalendarYearBoundaries()
		{
			await VerifyBoundaries("GregorianCalendar", "YearViewPanel", CalendarViewDisplayMode.Year);
		}

		[TestMethod]
		[Ignore("UNO TODO - TestServices.KeyboardHelper not implemented yet.")]
		public async Task VerifyGregorianCalendarDecadeBoundaries()
		{
			await VerifyBoundaries("GregorianCalendar", "DecadeViewPanel", CalendarViewDisplayMode.Decade);
		}

		[TestMethod]
		[Ignore("UNO-TODO: Test fails Debug.Assert()")]
		public async Task VerifyHebrewCalendarMonthBoundaries()
		{
			await VerifyBoundaries("HebrewCalendar", "MonthViewPanel", CalendarViewDisplayMode.Month);
		}

		[TestMethod]
		[Ignore("UNO-TODO: Test fails Debug.Assert()")]
		public async Task VerifyHebrewCalendarYearBoundaries()
		{
			await VerifyBoundaries("HebrewCalendar", "YearViewPanel", CalendarViewDisplayMode.Year);
		}

		[TestMethod]
		[Ignore("UNO-TODO: Test fails Debug.Assert()")]
		public async Task VerifyHebrewCalendarDecadeBoundaries()
		{
			await VerifyBoundaries("HebrewCalendar", "DecadeViewPanel", CalendarViewDisplayMode.Decade);
		}

		[TestMethod]
		[Ignore("UNO-TODO: Test fails Debug.Assert()")]
		public async Task VerifyHijriCalendarMonthBoundaries()
		{
			await VerifyBoundaries("HijriCalendar", "MonthViewPanel", CalendarViewDisplayMode.Month);
		}

		[TestMethod]
		[Ignore("UNO-TODO: Test fails Debug.Assert()")]
		public async Task VerifyHijriCalendarYearBoundaries()
		{
			await VerifyBoundaries("HijriCalendar", "YearViewPanel", CalendarViewDisplayMode.Year);
		}

		[TestMethod]
		[Ignore("UNO-TODO: Test fails Debug.Assert()")]
		public async Task VerifyHijriCalendarDecadeBoundaries()
		{
			await VerifyBoundaries("HijriCalendar", "DecadeViewPanel", CalendarViewDisplayMode.Decade);
		}

		[TestMethod]
		[Ignore("UNO TODO - TestServices.KeyboardHelper not implemented yet.")]
		public async Task VerifyJapaneseCalendarMonthBoundaries()
		{
			await VerifyBoundaries("JapaneseCalendar", "MonthViewPanel", CalendarViewDisplayMode.Month);
		}

		[TestMethod]
		[Ignore("UNO TODO - TestServices.KeyboardHelper not implemented yet.")]
		public async Task VerifyJapaneseCalendarYearBoundaries()
		{
			await VerifyBoundaries("JapaneseCalendar", "YearViewPanel", CalendarViewDisplayMode.Year);
		}

		[TestMethod]
		[Ignore("UNO TODO - TestServices.KeyboardHelper not implemented yet.")]
		public async Task VerifyJapaneseCalendarDecadeBoundaries()
		{
			await VerifyBoundaries("JapaneseCalendar", "DecadeViewPanel", CalendarViewDisplayMode.Decade);
		}

		[TestMethod]
		[Ignore("UNO-TODO: Test fails Debug.Assert()")]
		public async Task VerifyJulianCalendarMonthBoundaries()
		{
			await VerifyBoundaries("JulianCalendar", "MonthViewPanel", CalendarViewDisplayMode.Month);
		}

		[TestMethod]
		[Ignore("UNO-TODO: Test fails Debug.Assert()")]
		public async Task VerifyJulianCalendarYearBoundaries()
		{
			await VerifyBoundaries("JulianCalendar", "YearViewPanel", CalendarViewDisplayMode.Year);
		}

		[TestMethod]
		[Ignore("UNO-TODO: Test fails Debug.Assert()")]
		public async Task VerifyJulianCalendarDecadeBoundaries()
		{
			await VerifyBoundaries("JulianCalendar", "DecadeViewPanel", CalendarViewDisplayMode.Decade);
		}

		[TestMethod]
		[Ignore("UNO-TODO: Test fails Debug.Assert()")]
		public async Task VerifyKoreanCalendarMonthBoundaries()
		{
			await VerifyBoundaries("KoreanCalendar", "MonthViewPanel", CalendarViewDisplayMode.Month);
		}

		[TestMethod]
		[Ignore("UNO TODO - TestServices.KeyboardHelper not implemented yet.")]
		public async Task VerifyKoreanCalendarYearBoundaries()
		{
			await VerifyBoundaries("KoreanCalendar", "YearViewPanel", CalendarViewDisplayMode.Year);
		}

		[TestMethod]
		[Ignore("UNO TODO - TestServices.KeyboardHelper not implemented yet.")]
		public async Task VerifyKoreanCalendarDecadeBoundaries()
		{
			await VerifyBoundaries("KoreanCalendar", "DecadeViewPanel", CalendarViewDisplayMode.Decade);
		}

		[TestMethod]
		[Ignore("UNO TODO - TestServices.KeyboardHelper not implemented yet.")]
		public async Task VerifyTaiwanCalendarMonthBoundaries()
		{
			await VerifyBoundaries("TaiwanCalendar", "MonthViewPanel", CalendarViewDisplayMode.Month);
		}

		[TestMethod]
		[Ignore("UNO TODO - TestServices.KeyboardHelper not implemented yet.")]
		public async Task VerifyTaiwanCalendarYearBoundaries()
		{
			await VerifyBoundaries("TaiwanCalendar", "YearViewPanel", CalendarViewDisplayMode.Year);
		}

		[TestMethod]
		[Ignore("UNO TODO - TestServices.KeyboardHelper not implemented yet.")]
		public async Task VerifyTaiwanCalendarDecadeBoundaries()
		{
			await VerifyBoundaries("TaiwanCalendar", "DecadeViewPanel", CalendarViewDisplayMode.Decade);
		}

		[TestMethod]
		[Ignore("UNO TODO - TestServices.KeyboardHelper not implemented yet.")]
		public async Task VerifyThaiCalendarMonthBoundaries()
		{
			await VerifyBoundaries("ThaiCalendar", "MonthViewPanel", CalendarViewDisplayMode.Month);
		}

		[TestMethod]
		[Ignore("UNO TODO - TestServices.KeyboardHelper not implemented yet.")]
		public async Task VerifyThaiCalendarYearBoundaries()
		{
			await VerifyBoundaries("ThaiCalendar", "YearViewPanel", CalendarViewDisplayMode.Year);
		}

		[TestMethod]
		[Ignore("UNO TODO - TestServices.KeyboardHelper not implemented yet.")]
		public async Task VerifyThaiCalendarDecadeBoundaries()
		{
			await VerifyBoundaries("ThaiCalendar", "DecadeViewPanel", CalendarViewDisplayMode.Decade);
		}

		[TestMethod]
		[Ignore("UNO-TODO: Test fails Debug.Assert()")]
		public async Task VerifyUmAlQuraCalendarMonthBoundaries()
		{
			await VerifyBoundaries("UmAlQuraCalendar", "MonthViewPanel", CalendarViewDisplayMode.Month);
		}

		[TestMethod]
		[Ignore("UNO-TODO: Test fails Debug.Assert()")]
		public async Task VerifyUmAlQuraCalendarYearBoundaries()
		{
			await VerifyBoundaries("UmAlQuraCalendar", "YearViewPanel", CalendarViewDisplayMode.Year);
		}

		[TestMethod]
		[Ignore("UNO-TODO: Test fails Debug.Assert()")]
		public async Task VerifyUmAlQuraCalendarDecadeBoundaries()
		{
			await VerifyBoundaries("UmAlQuraCalendar", "DecadeViewPanel", CalendarViewDisplayMode.Decade);
		}

		[TestMethod]
		public async Task ValidateScopeStateAfterLoaded()
		{
			TestCleanupWrapper cleanup;

			Grid rootPanel = null;

			var helper = new CalendarHelper.CalendarViewHelper();

			CalendarView cv = await helper.GetCalendarView();

			//WUCRenderingScopeGuard guard(DCompRendering.WUCCompleteSynchronousCompTree, false /* resizeWindow */);
			//TestServices.WindowHelper.SetWindowSizeOverride(wf.Count(400, 400));

			rootPanel = await CalendarHelper.CreateTestResources();

			// load into visual tree
			await RunOnUIThread(() =>
			{
				cv.MinDate = ConvertToDateTime(1, 2013, 5, 18);
				cv.MaxDate = ConvertToDateTime(1, 2013, 6, 23);
				CalendarHelper.ReplaceAccentColorForTesting(cv);
				rootPanel.Children.Add(cv);
			});

			await WindowHelper.WaitForIdle();
			TestServices.Utilities.VerifyMockDCompOutput(MockDComp.SurfaceComparison.NoComparison);
		}

		[TestMethod]
		[Ignore("UNO TODO - InputHelper.DynamicPressCenter() not implemented")]
		public async Task ValidateScopeInManipulation()
		{
			TestCleanupWrapper cleanup;

			Grid rootPanel = null;

			var helper = new CalendarHelper.CalendarViewHelper();

			CalendarView cv = await helper.GetCalendarView();

			//WUCRenderingScopeGuard guard(DCompRendering.WUCCompleteSynchronousCompTree, false /* resizeWindow */);
			//TestServices.WindowHelper.SetWindowSizeOverride(wf.Count(400, 400));

			rootPanel = await CalendarHelper.CreateTestResources();

			// load into visual tree
			await RunOnUIThread(() =>
			{
				cv.MinDate = ConvertToDateTime(1, 2013, 1, 1);
				cv.MaxDate = ConvertToDateTime(1, 2013, 12, 31);
				CalendarHelper.ReplaceAccentColorForTesting(cv);
				cv.SetDisplayDate(cv.MinDate);
				rootPanel.Children.Add(cv);
			});

			await WindowHelper.WaitForIdle();

			CalendarViewDayItem firstItem = null;

			await RunOnUIThread(() =>
			{
				var calendarPanel =
					CalendarPanel(helper.GetTemplateChild("MonthViewPanel"));
				firstItem = CalendarViewDayItem(calendarPanel.Children.GetAt(0));
			});

			TestServices.InputHelper.DynamicPressCenter(firstItem, 0, 0, PointerFinger.Finger1);

			// Finger down, we don't change OutOfScope state
			TestServices.Utilities.VerifyMockDCompOutput(MockDComp.SurfaceComparison.NoComparison);

			// it is not easy to validate OutOfScope state during manipulation because OutOfScope state must be validated via DComp Comparison,
			// however during manipulation DComp comparison is not predictable.
			TestServices.InputHelper.DynamicRelease(PointerFinger.Finger1);
		}


		[TestMethod]
		public async Task ValidateScopeWithDST()
		{
			TestCleanupWrapper cleanup;

			Grid rootPanel = null;

			var helper = new CalendarHelper.CalendarViewHelper();

			CalendarView cv = await helper.GetCalendarView();

			//WUCRenderingScopeGuard guard(DCompRendering.WUCCompleteSynchronousCompTree, false /* resizeWindow */);
			//TestServices.WindowHelper.SetWindowSizeOverride(wf.Count(400, 400));

			rootPanel = await CalendarHelper.CreateTestResources();

			// load into visual tree
			await RunOnUIThread(() =>
			{
				cv.MinDate = ConvertToDateTime(1, 2014, 3, 8);
				cv.MaxDate = ConvertToDateTime(1, 2014, 4, 1);
				CalendarHelper.ReplaceAccentColorForTesting(cv);
				rootPanel.Children.Add(cv);
			});

			await WindowHelper.WaitForIdle();

			// before the fix, 3/31/2014 is in OutOfScope state (gray by default).
			TestServices.Utilities.VerifyMockDCompOutput(MockDComp.SurfaceComparison.NoComparison);
		}


		[TestMethod]
		[Ignore("UNO TODO")]
		public async Task CanDisplayDateOnCorrectPositionWhenCallSetDisplayDate()
		{
			TestCleanupWrapper cleanup;
			CalendarPanel calendarPanel = null;
			ScrollViewer scrollViewer = null;
			CalendarViewDayItem firstItem = null;
			Grid rootPanel = null;
			var helper = new CalendarHelper.CalendarViewHelper();

			CalendarView cv = await helper.GetCalendarView();

			rootPanel = await CalendarHelper.CreateTestResources();

			var minDate = ConvertToDateTime(1, 2000, 1, 1);
			var maxDate = ConvertToDateTime(1, 2010, 1, 1);

			// load into visual tree
			await RunOnUIThread(() =>
			{
				cv.MinDate = minDate;
				cv.MaxDate = maxDate;
				rootPanel.Children.Add(cv);
			});

			await WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				calendarPanel = CalendarPanel(helper.GetTemplateChild("MonthViewPanel"));
				scrollViewer = ScrollViewer(helper.GetTemplateChild("MonthViewScrollViewer"));
				firstItem = CalendarViewDayItem(calendarPanel.Children.GetAt(0));
			});

			// First we have 6 rows in month view so call displaydate will bring 1/1/2000 to the first row
			// hence the panel's offset is 0
			await RunOnUIThread(() =>
			{
				cv.SetDisplayDate(ConvertToDateTime(1, 2000, 1, 20));
			});
			await WindowHelper.WaitForIdle();
			await RunOnUIThread(() =>
			{
				var distance = scrollViewer.TransformToVisual(calendarPanel)
					.TransformPoint(new global::Windows.Foundation.Point(0, 0));
				VERIFY_ARE_VERY_CLOSE(distance.Y, 0, 1.0, "distance.Y 1");
			});

			// Second we set numberof weeks to 4, call displaydate will bring 1/20/2000 to the first row
			// hence the panel's offset is (-3) * itemHeight. 1/20/2000 is on the third row.
			await RunOnUIThread(() =>
			{
				cv.NumberOfWeeksInView = 4;
				cv.SetDisplayDate(ConvertToDateTime(1, 2000, 1, 20));
			});
			await WindowHelper.WaitForIdle();
			await RunOnUIThread(() =>
			{
				var distance = scrollViewer.TransformToVisual(calendarPanel)
					.TransformPoint(new global::Windows.Foundation.Point(0, 0));
				var itemHeight = firstItem.ActualHeight;
				var itemMargin = firstItem.Margin;
				itemHeight += itemMargin.Top + itemMargin.Bottom;
				VERIFY_ARE_VERY_CLOSE(distance.Y, itemHeight * 3, 1.0, "distance.Y 2");
			});
		}

		[TestMethod]
		[Ignore("UNO TODO")]
		public async Task CanDisplayDateOnCorrectPositionWhenSwitchView()
		{
			TestCleanupWrapper cleanup;
			CalendarPanel monthPanel = null;
			ScrollViewer monthScrollViewer = null;
			CalendarViewDayItem firstItem = null;
			CalendarViewDayItem itemFeb20 = null;
			CalendarPanel yearPanel = null;
			Control firstYearItem = null;
			Grid rootPanel = null;
			var helper = new CalendarHelper.CalendarViewHelper();

			CalendarView cv = await helper.GetCalendarView();

			rootPanel = await CalendarHelper.CreateTestResources();

			var minDate = ConvertToDateTime(1, 2000, 1, 1);
			var maxDate = ConvertToDateTime(1, 2010, 1, 1);

			// load into visual tree
			await RunOnUIThread(() =>
			{
				cv.MinDate = minDate;
				cv.MaxDate = maxDate;
				cv.SetDisplayDate(minDate);
				rootPanel.Children.Add(cv);
			});


			await WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				monthPanel = CalendarPanel(helper.GetTemplateChild("MonthViewPanel"));
				monthScrollViewer = ScrollViewer(helper.GetTemplateChild("MonthViewScrollViewer"));
				firstItem = CalendarViewDayItem(monthPanel.Children.GetAt(0));
				itemFeb20 = CalendarViewDayItem(monthPanel.Children.GetAt(50)); // 20/2/2000
			});

			// In below test we'll do:
			// 1. Display and Focus Feb20
			// 2. Switch to YearView
			// 3. Tap January 2000
			// After above steps, we'll come back to month view and Jan20 will be focused and brought into view
			(int numberOfWeeks, int distanceOfRows)[] testData =
			{
				(6, 0), // When MonthView has 6 weeks, the first row ( 1/1/2000) will be visible row after above steps.
				(4, 3) // When MonthView has 4 weeks, the third row (1/20/2000) will be visible row after above steps.
			};

			foreach (var data in testData)
			{
				await RunOnUIThread(() =>
				{
					LOG_OUTPUT("Setting NumberOfWeeksInView to %d.", data.numberOfWeeks);
					cv.DisplayMode = CalendarViewDisplayMode.Month;
					cv.NumberOfWeeksInView = data.numberOfWeeks;
					cv.SetDisplayDate(itemFeb20.Date);
					itemFeb20.Focus(FocusState.Programmatic);
				});
				await WindowHelper.WaitForIdle();
				await RunOnUIThread(() =>
				{
					LOG_OUTPUT("Dayitem 2/20/2000 should be focused.");
					CalendarHelper.CheckFocusedItem();
					cv.DisplayMode = CalendarViewDisplayMode.Year;
				});
				await WindowHelper.WaitForIdle();
				await RunOnUIThread(() =>
				{
					LOG_OUTPUT("Switched to Year mode, yearitem Feb/2000 should be focused.");
					CalendarHelper.CheckFocusedItem();
					yearPanel = CalendarPanel(helper.GetTemplateChild("YearViewPanel"));
					firstYearItem = Control(yearPanel.Children.GetAt(0));
				});
				TestServices.InputHelper.Tap(firstYearItem);
				await WindowHelper.WaitForIdle();
				await RunOnUIThread(() =>
				{
					LOG_OUTPUT(
						"Tapped on the first yearitem Jan/2000, we should go back to month mode and dayitem 1/20/2000 should be focused.");
					CalendarHelper.CheckFocusedItem();
					var distance = monthScrollViewer.TransformToVisual(monthPanel)
						.TransformPoint(new global::Windows.Foundation.Point(0, 0));
					var itemHeight = firstItem.ActualHeight;
					var itemMargin = firstItem.Margin;
					itemHeight += itemMargin.Top + itemMargin.Bottom;

					VERIFY_ARE_VERY_CLOSE(distance.Y, itemHeight * data.distanceOfRows, 1.0);
				});
			}
		}

		[TestMethod]
		[Ignore("UNO TODO")]
		public async Task CanDayItemFontPropertiesAffectMeasure()
		{
			TestCleanupWrapper cleanup;
			Grid rootPanel = null;
			var helper = new CalendarHelper.CalendarViewHelper();

			CalendarView cv = await helper.GetCalendarView();
			CalendarPanel monthPanel = null;
			CalendarViewDayItem dayItem = null;

			//TestServices.WindowHelper.SetWindowSizeOverride(wf.Count(400, 400));

			rootPanel = await CalendarHelper.CreateTestResources();

			var minDate = ConvertToDateTime(1, 2000, 1, 28);
			var maxDate = ConvertToDateTime(1, 2000, 1, 28);

			// load into visual tree
			await RunOnUIThread(() =>
			{
				cv.MinDate = minDate;
				cv.MaxDate = maxDate;
				cv.SetDisplayDate(minDate);
				rootPanel.Children.Add(cv);
			});

			await WindowHelper.WaitForIdle();

			// in below test we'll change the dayitem font properties and expect a change on the dayitem's actual size.
			await RunOnUIThread(() =>
			{
				monthPanel = CalendarPanel(helper.GetTemplateChild("MonthViewPanel"));
				dayItem = CalendarViewDayItem(monthPanel.Children.GetAt(0));

				double actualHeight = dayItem.ActualHeight;
				double actualWidth = dayItem.ActualWidth;
				double expectedHeight = 40.0;
				double expectedWidth = 40.0;
				VERIFY_ARE_VERY_CLOSE(actualHeight, expectedHeight);
				VERIFY_ARE_VERY_CLOSE(actualWidth, expectedWidth);

				cv.DayItemFontFamily = new Media.FontFamily("Segoe UI Symbol");
				cv.DayItemFontSize = 50;
				cv.DayItemFontStyle = global::Windows.UI.Text.FontStyle.Italic;
				cv.DayItemFontWeight = Microsoft.UI.Text.FontWeights.ExtraBold;
				cv.UpdateLayout();

				actualHeight = dayItem.ActualHeight;
				actualWidth = dayItem.ActualWidth;
				expectedHeight = 75.0;
				expectedWidth = 60.0;
				VERIFY_ARE_VERY_CLOSE(actualHeight, expectedHeight);
				VERIFY_ARE_VERY_CLOSE(actualWidth, expectedWidth);
			});
		}

		[TestMethod]
		[Ignore("UNO TODO")]
		public async Task VerifyHeaderTextChangesWhenCalendarIdentifierChanged()
		{
			TestCleanupWrapper cleanup;
			Grid rootPanel = null;

			var helper = new CalendarHelper.CalendarViewHelper();

			CalendarView cv = await helper.GetCalendarView();

			rootPanel = await CalendarHelper.CreateTestResources();

			var minDate = ConvertToDateTime(1, 2000, 1, 28);
			var maxDate = ConvertToDateTime(1, 2000, 1, 28);

			// load into visual tree
			await RunOnUIThread(() =>
			{
				cv.MinDate = minDate;
				cv.MaxDate = maxDate;
				rootPanel.Children.Add(cv);
			});


			await WindowHelper.WaitForIdle();


			await RunOnUIThread(() =>
			{
				var headerButton = Button(helper.GetTemplateChild("HeaderButton"));
				var headText = String(headerButton.Content);

				LOG_OUTPUT("header text in Gregorian calendar: %s", headText);
				//VERIFY_ARE_EQUAL(headText, "\u200eJanuary\u200e \u200e2000");
				VERIFY_ARE_EQUAL(headText, "January 2000");

				cv.CalendarIdentifier = "HebrewCalendar";
				cv.UpdateLayout();

				var headText2 = String(headerButton.Content);
				LOG_OUTPUT("header text in Hebrew calendar: %s", headText2);

				VERIFY_ARE_EQUAL(headText2, "‏שבט‏ ‏תש״ס");
			});

		}

		[TestMethod]
		[Ignore("UNO TODO")]
		public async Task VerifySetDisplayDateBeforeLoaded()
		{
			TestCleanupWrapper cleanup;
			//TestServices.WindowHelper.SetWindowSizeOverride(wf.Count(400, 400));

			Grid rootPanel = null;

			var helper = new CalendarHelper.CalendarViewHelper();

			CalendarView cv = await helper.GetCalendarView();
			CalendarViewDayItem firstVisibleItem = null;

			rootPanel = await CalendarHelper.CreateTestResources();

			var minDate = ConvertToDateTime(1, 2000, 1, 1);
			var maxDate = ConvertToDateTime(1, 2020, 1, 2);
			var displayDate = ConvertToDateTime(1, 2014, 6, 1); // by default this date will be on first column.

			// in below test we are going to find the first visible item and to verify if it is displayDate.
			// however CalendarPanel doesn't expose firstVisibleIndex API, so here we'll try to workaround it.
			// we use CalendarViewDayItemChanging event to find the item with displayDate, then check if this item is
			// at (0, 0) in the visible window.
			CompareDate dateComparer = CalendarHelper.CompareDate;
			var cicEvent = new Event();
			var cicRegistration = CreateSafeEventRegistration<CalendarView, TypedEventHandler<CalendarView, CalendarViewDayItemChangingEventArgs>>("CalendarViewDayItemChanging");
			cicRegistration.Attach(
				cv,
				(sender, e) =>
				{
					if (dateComparer(e.Item.Date, displayDate))
					{
						firstVisibleItem = e.Item;
						cicEvent.Set();
					}

				});

			// load into visual tree
			await RunOnUIThread(() =>
			{
				cv.MinDate = minDate;
				cv.MaxDate = maxDate;
				cv.SetDisplayDate(displayDate);
				rootPanel.Children.Add(cv);
			});


			await WindowHelper.WaitForIdle();
			await cicEvent.WaitForDefault();
			VERIFY_IS_TRUE(cicEvent.HasFired());
			TestServices.VERIFY_IS_NOT_NULL(firstVisibleItem);

			await RunOnUIThread(() =>
			{
				var scrollViewer = ScrollViewer(helper.GetTemplateChild("MonthViewScrollViewer"));
				global::Windows.Foundation.Point origin = new Point(0, 0);
				var itemPos = firstVisibleItem.TransformToVisual(scrollViewer).TransformPoint(origin);
				LOG_OUTPUT("actual position of display date is (%f, %f)., expected (1, 1)", itemPos.X, itemPos.Y);

				//CalendarViewDayItem's margin is {1, 1, 1, 1}
				VERIFY_ARE_EQUAL(itemPos.X, 1d, "itemPos.X");
				VERIFY_ARE_EQUAL(itemPos.Y, 1d, "itemPos.Y");
			});

		}

		[TestMethod]
		[Ignore("UNO-TODO: Test fails Debug.Assert()")]
		public async Task CanFocusOnCorrectItemWithTap()
		{
			TestCleanupWrapper cleanup;
			//TestServices.WindowHelper.SetWindowSizeOverride(wf.Count(400, 400));

			Grid rootPanel = null;
			CalendarPanel monthPanel = null;

			var helper = new CalendarHelper.CalendarViewHelper();

			CalendarView cv = await helper.GetCalendarView();

			rootPanel = await CalendarHelper.CreateTestResources();

			var minDate = ConvertToDateTime(1, 2000, 1, 1);
			var maxDate = ConvertToDateTime(1, 2000, 1, 2);

			// load into visual tree
			await RunOnUIThread(() =>
			{
				cv.MinDate = minDate;
				cv.MaxDate = maxDate;
				rootPanel.Children.Add(cv);
			});


			await WindowHelper.WaitForIdle();

			CalendarViewDayItem firstItem = null;
			CalendarViewDayItem secondItem = null;
			await RunOnUIThread(() =>
			{
				monthPanel = CalendarPanel(helper.GetTemplateChild("MonthViewPanel"));
				firstItem = CalendarViewDayItem(monthPanel.Children.GetAt(0));
				secondItem = CalendarViewDayItem(monthPanel.Children.GetAt(1));
				TestServices.VERIFY_IS_NOT_NULL(firstItem);
				TestServices.VERIFY_IS_NOT_NULL(secondItem);
			});

			Event gotFocusEvent = new Event();
			var gotFocusRegistration = CreateSafeEventRegistration<UIElement, RoutedEventHandler>("GotFocus");

			using var _ = gotFocusRegistration.Attach(secondItem,
				(o, args) =>
				{
					LOG_OUTPUT("CalendarViewDayItem got focus");
					gotFocusEvent.Set();
				});

			TestServices.InputHelper.Tap(secondItem);
			await gotFocusEvent.WaitForDefault();
			VERIFY_IS_TRUE(gotFocusEvent.HasFired());
			await RunOnUIThread(() =>
			{
				CalendarHelper.CheckFocusedItem();
				var focusedItem = CalendarViewDayItem(Input.FocusManager.GetFocusedElement(WindowHelper.XamlRoot));
				TestServices.VERIFY_IS_NOT_NULL(focusedItem);
				// before this fix, it will focus on first item.
				VERIFY_ARE_EQUAL(focusedItem, secondItem);
			});


			Event lostFocusEvent = new Event();
			var lostFocusRegistration = CreateSafeEventRegistration<UIElement, RoutedEventHandler>("LostFocus");

			lostFocusRegistration.Attach(secondItem, (o, args) =>
			{
				LOG_OUTPUT("CalendarViewDayItem lost focus");
				lostFocusEvent.Set();
			});

			// tap the center of Panel (no item at center), we don't focus on any item.
			TestServices.InputHelper.Tap(monthPanel);
			await lostFocusEvent.WaitForDefault();
			VERIFY_IS_TRUE(lostFocusEvent.HasFired());

			await RunOnUIThread(() =>
			{
				var focusedItem = (CalendarViewDayItem)(Input.FocusManager.GetFocusedElement(WindowHelper.XamlRoot));
				VERIFY_IS_NULL(focusedItem);
			});

		}

		[TestMethod]
		[Ignore("UNO-TODO: Test fails Debug.Assert()")]
		public async Task VerifyRenderLayers()
		{
			TestCleanupWrapper cleanup;

			Grid rootPanel = null;

			var helper = new CalendarHelper.CalendarViewHelper();

			CalendarView cv = await helper.GetCalendarView();

			//WUCRenderingScopeGuard guard(DCompRendering.WUCCompleteSynchronousCompTree, false /* resizeWindow */);
			//TestServices.WindowHelper.SetWindowSizeOverride(wf.Count(400, 400));

			rootPanel = await CalendarHelper.CreateTestResources();

			// load into visual tree
			await RunOnUIThread(() =>
			{
				cv.MinDate = ConvertToDateTime(1, 1999, 12, 30);
				cv.MaxDate = ConvertToDateTime(1, 2000, 1, 5);
				cv.SelectedDates.Add(ConvertToDateTime(1, 2000, 1, 1));
				cv.HorizontalDayItemAlignment = HorizontalAlignment.Right;
				cv.VerticalDayItemAlignment = VerticalAlignment.Bottom;
				cv.HorizontalFirstOfMonthLabelAlignment = HorizontalAlignment.Left;
				cv.DayItemFontSize = 25;
				cv.FirstOfMonthLabelFontSize = 20;
				cv.IsGroupLabelVisible = true;
				cv.FirstDayOfWeek = global::Windows.Globalization.DayOfWeek.Thursday;
				cv.OutOfScopeBackground = new SolidColorBrush(Microsoft.UI.Colors.Red);
				cv.CalendarItemBackground = new SolidColorBrush(Microsoft.UI.Colors.Blue);
				cv.SelectedBorderBrush = new SolidColorBrush(Microsoft.UI.Colors.White);
				cv.CalendarViewDayItemStyle = (Style)(XamlReader.Load(
					"<Style xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' TargetType='CalendarViewDayItem'>" +
					" <Setter Property='MinWidth' Value='50' />" +
					" <Setter Property='MinHeight' Value='50' />" +
					" <Setter Property='Template'>" +
					"  <Setter.Value>" +
					"   <ControlTemplate TargetType='CalendarViewDayItem'>" +
					// template child will be rendered before Number and Label textblocks, regardless ZIndex
					"    <Border Background='Blue' Width='30' Height='30' Canvas.ZIndex='1000'/>" +
					"   </ControlTemplate>" +
					"  </Setter.Value>" +
					" </Setter>" +
					"</Style>"));
				rootPanel.Children.Add(cv);
			});

			await WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				var monthPanel = CalendarPanel(helper.GetTemplateChild("MonthViewPanel"));
				var dayDec31 = CalendarViewDayItem(monthPanel.Children.GetAt(1));
				var dayJan1 = CalendarViewDayItem(monthPanel.Children.GetAt(2));

				// half transparent green background will be drawn on below Red background
				dayDec31.Background =
					new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(0x80, 0, 0xFF, 0));
				dayJan1.Background =
					new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(0x80, 0, 0xFF, 0));

				ColorCollection colors = new ColorCollection();
				for (int i = 0; i < 8; i++)
				{
					colors.Add(Microsoft.UI.Colors.Yellow);
				}

				dayDec31.SetDensityColors(colors);
				dayJan1.SetDensityColors(colors);
			});

			await WindowHelper.WaitForIdle();
			TestServices.Utilities.VerifyMockDCompOutput(MockDComp.SurfaceComparison.NoComparison);

			await RunOnUIThread(() =>
			{
				// remove the template child, density bar should be still there (and only drawn once).
				cv.CalendarViewDayItemStyle = (Style)(XamlReader.Load(
					"<Style xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' TargetType='CalendarViewDayItem'>" +
					" <Setter Property='MinWidth' Value='50' />" +
					" <Setter Property='MinHeight' Value='50' />" +
					" <Setter Property='Template'>" +
					"  <Setter.Value>" +
					// remove the template child, the density bar should be still there.
					"   <ControlTemplate TargetType='CalendarViewDayItem'/>" +
					"  </Setter.Value>" +
					" </Setter>" +
					"</Style>"
				));

			});

			await WindowHelper.WaitForIdle();
			TestServices.Utilities.VerifyMockDCompOutput(MockDComp.SurfaceComparison.NoComparison, "2");
		}

		[TestMethod]
		[Ignore("UNO TODO")]
		public async Task VerifyDisplayDate()
		{
			TestCleanupWrapper cleanup;
			Grid rootPanel = null;

			var helper = new CalendarHelper.CalendarViewHelper();

			CalendarView cv = await helper.GetCalendarView();

			//TestServices.WindowHelper.SetWindowSizeOverride(wf.Count(400, 400));

			rootPanel = await CalendarHelper.CreateTestResources();

			// load into visual tree
			await RunOnUIThread(() =>
			{
				cv.MinDate = ConvertToDateTime(1, 2000, 1, 1);
				cv.MaxDate = ConvertToDateTime(1, 2100, 1, 1);
				cv.SetDisplayDate(ConvertToDateTime(1, 2050, 1, 1));
				rootPanel.Children.Add(cv);
			});

			await WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				// switch to Year mode directly, we'll show year 2050 and panel offset will be at -10875
				cv.DisplayMode = CalendarViewDisplayMode.Year;
				cv.UpdateLayout();

				var yearPanel = CalendarPanel(helper.GetTemplateChild("YearViewPanel"));
				var yearScrollViewer = ScrollViewer(helper.GetTemplateChild("YearViewScrollViewer"));
				var headerButton = Button(helper.GetTemplateChild("HeaderButton"));
				var offset = yearPanel.TransformToVisual(yearScrollViewer)
					.TransformPoint(new global::Windows.Foundation.Point(0, 0));
				var headText = String(headerButton.Content);
				LOG_OUTPUT("yearpanel offset is %f, header text is %s", offset.Y, headText);
				//VERIFY_ARE_EQUAL(headText, "\u200e2050");
				VERIFY_ARE_EQUAL(headText, "2050");
				VERIFY_ARE_EQUAL(offset.Y, -10875);

				// switch back to MonthView and call display the minDate (1/1/2000)
				// then switch back to yearView, we'll see year 2000 and panel offset will be at 0.
				cv.DisplayMode = CalendarViewDisplayMode.Month;
				cv.SetDisplayDate(cv.MinDate);
				cv.UpdateLayout();
				cv.DisplayMode = CalendarViewDisplayMode.Year;
				cv.UpdateLayout();
				offset = yearPanel.TransformToVisual(yearScrollViewer).TransformPoint(new global::Windows.Foundation.Point(0, 0));
				headText = String(headerButton.Content);
				LOG_OUTPUT("yearpanel offset is %f, header text is %s", offset.Y, headText);
				//VERIFY_ARE_EQUAL(headText, "\u200e2000");
				VERIFY_ARE_EQUAL(headText, "2000");
				VERIFY_ARE_EQUAL(offset.Y, 0);
			});
		}

		[TestMethod]
		public async Task CanChangeStyle()
		{
			TestCleanupWrapper cleanup;
			Grid rootPanel = null;

			var helper = new CalendarHelper.CalendarViewHelper();

			CalendarView cv = await helper.GetCalendarView();

			//TestServices.WindowHelper.SetWindowSizeOverride(wf.Count(400, 400));

			rootPanel = await CalendarHelper.CreateTestResources();

			// load into visual tree
			await RunOnUIThread(() =>
			{
				rootPanel.Children.Add(cv);
			});

			await WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				// changing the template will not crash the app
				cv.Style = (Style)(XamlReader.Load(
					"<Style xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' TargetType='CalendarView'>" +
					" <Setter Property='Template'>" +
					"  <Setter.Value>" +
					"   <ControlTemplate TargetType='CalendarView'>" +
					"    <ScrollViewer x:Name='MonthViewScrollViewer'>" +
					"     <CalendarPanel x:Name='MonthViewPanel'/>" +
					"    </ScrollViewer>" +
					"   </ControlTemplate>" +
					"  </Setter.Value>" +
					" </Setter>" +
					"</Style>"));
			});

			await WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				// even an empty template will not crash the app.
				cv.Style = (Style)(XamlReader.Load(
					"<Style xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' TargetType='CalendarView'>" +
					" <Setter Property='Template'>" +
					"  <Setter.Value>" +
					"   <ControlTemplate TargetType='CalendarView'/>" +
					"  </Setter.Value>" +
					" </Setter>" +
					"</Style>"));
			});

			await WindowHelper.WaitForIdle();
		}

		[TestMethod]
		[Ignore("UNO TODO")]
		public async Task NavigationButtonShouldBeDisabledWhenThereIsNoMoreDates()
		{
			TestCleanupWrapper cleanup;
			Grid rootPanel = null;

			Button previousButton = null;
			Button nextButton = null;
			DateTimeOffset minDate = ConvertToDateTime(1, 2000, 1, 1);
			DateTimeOffset maxDate = ConvertToDateTime(1, 2000, 2, 6);

			var helper = new CalendarHelper.CalendarViewHelper();

			CalendarView cv = await helper.GetCalendarView();

			rootPanel = await CalendarHelper.CreateTestResources();

			// load into visual tree
			await RunOnUIThread(() =>
			{
				cv.MinDate = minDate;
				cv.MaxDate = maxDate;
				cv.SetDisplayDate(minDate);
				rootPanel.Children.Add(cv);
			});


			await WindowHelper.WaitForIdle();

			// find the buttons
			await RunOnUIThread(() =>
			{
				previousButton = Button(helper.GetTemplateChild("PreviousButton"));
				nextButton = Button(helper.GetTemplateChild("NextButton"));

				LOG_OUTPUT("previous button's enable state is %d, next button's enable state is %d.",
					previousButton.IsEnabled, nextButton.IsEnabled);
				VERIFY_IS_FALSE(previousButton.IsEnabled, "can't go backwards");
				VERIFY_IS_TRUE(nextButton.IsEnabled, "can go forward");
			});

			var viewChangedRegistration = CreateSafeEventRegistration<ScrollViewer, EventHandler<ScrollViewerViewChangedEventArgs>>("ViewChanged");
			var viewChangedEvent = new Event();
			await RunOnUIThread(() =>
			{
				var scrollViewer = ScrollViewer(helper.GetTemplateChild("MonthViewScrollViewer"));
				viewChangedRegistration.Attach(scrollViewer,
					(sender, args) =>
					{
						if (args.IsIntermediate == false)
						{
							viewChangedEvent.Set();
						}
					});
			});

			TestServices.InputHelper.DynamicPressCenter(nextButton, 0, 0, PointerFinger.Finger1);
			await WindowHelper.WaitForIdle();
			TestServices.InputHelper.DynamicRelease(PointerFinger.Finger1);
			await WindowHelper.WaitForIdle();

			await viewChangedEvent.WaitForDefault();
			VERIFY_IS_TRUE(viewChangedEvent.HasFired());

			await RunOnUIThread(() =>
			{
				LOG_OUTPUT("previous button's enable state is %d, next button's enable state is %d.",
					previousButton.IsEnabled, nextButton.IsEnabled);
				VERIFY_IS_TRUE(previousButton.IsEnabled, "can go backwards");
				VERIFY_IS_FALSE(nextButton.IsEnabled, "can't go forward");
			});
		}

		[TestMethod]
		public async Task ValidateChromeFocus()
		{
			TestCleanupWrapper cleanup;

			Grid rootPanel = null;

			var helper = new CalendarHelper.CalendarViewHelper();

			CalendarView cv = await helper.GetCalendarView();

			//WUCRenderingScopeGuard guard(DCompRendering.WUCCompleteSynchronousCompTree, false /* resizeWindow */);
			//TestServices.WindowHelper.SetWindowSizeOverride(wf.Count(400, 400));

			rootPanel = await CalendarHelper.CreateTestResources();

			// load into visual tree
			await RunOnUIThread(() =>
			{
				cv.MinDate = ConvertToDateTime(1, 2014, 1, 1);
				cv.MaxDate = ConvertToDateTime(1, 2014, 1, 1);
				rootPanel.Children.Add(cv);
			});

			await WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				var monthPanel = CalendarPanel(helper.GetTemplateChild("MonthViewPanel"));
				var firstItem = CalendarViewDayItem(monthPanel.Children.GetAt(0));
				firstItem.Focus(FocusState.Keyboard);
			});

			await WindowHelper.WaitForIdle();
			TestServices.Utilities.VerifyMockDCompOutput(MockDComp.SurfaceComparison.NoComparison);
		}

		[TestMethod]
		public async Task ValidateChromeFocusYear()
		{
			TestCleanupWrapper cleanup;

			Grid rootPanel = null;

			var helper = new CalendarHelper.CalendarViewHelper();
			//Button headerButton = null;

			CalendarView cv = await helper.GetCalendarView();

			//WUCRenderingScopeGuard guard(DCompRendering.WUCCompleteSynchronousCompTree, false /* resizeWindow */);
			//TestServices.WindowHelper.SetWindowSizeOverride(wf.Count(400, 400));

			rootPanel = await CalendarHelper.CreateTestResources();

			// load into visual tree
			await RunOnUIThread(() =>
			{
				cv.MinDate = ConvertToDateTime(1, 2014, 1, 1);
				cv.MaxDate = ConvertToDateTime(1, 2014, 1, 1);
				cv.DisplayMode = CalendarViewDisplayMode.Year;
				rootPanel.Children.Add(cv);
			});

			await WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				var yearPanel = CalendarPanel(helper.GetTemplateChild("YearViewPanel"));
				var firstItem = Control(yearPanel.Children.GetAt(0));
				firstItem.Focus(FocusState.Keyboard);
			});

			await WindowHelper.WaitForIdle();
			TestServices.Utilities.VerifyMockDCompOutput(MockDComp.SurfaceComparison.NoComparison);
		}

		[TestMethod]
		public async Task ValidateChromeFocusDecade()
		{
			TestCleanupWrapper cleanup;

			Grid rootPanel = null;

			var helper = new CalendarHelper.CalendarViewHelper();
			//Button headerButton = null;

			CalendarView cv = await helper.GetCalendarView();

			//WUCRenderingScopeGuard guard(DCompRendering.WUCCompleteSynchronousCompTree, false /* resizeWindow */);
			//TestServices.WindowHelper.SetWindowSizeOverride(wf.Count(400, 400));

			rootPanel = await CalendarHelper.CreateTestResources();

			// load into visual tree
			await RunOnUIThread(() =>
			{
				cv.MinDate = ConvertToDateTime(1, 2014, 1, 1);
				cv.MaxDate = ConvertToDateTime(1, 2014, 1, 1);
				cv.DisplayMode = CalendarViewDisplayMode.Decade;
				rootPanel.Children.Add(cv);
			});

			await WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				var decadePanel = CalendarPanel(helper.GetTemplateChild("DecadeViewPanel"));
				var firstItem = Control(decadePanel.Children.GetAt(0));
				firstItem.Focus(FocusState.Keyboard);
			});

			await WindowHelper.WaitForIdle();
			TestServices.Utilities.VerifyMockDCompOutput(MockDComp.SurfaceComparison.NoComparison);
		}

		[TestMethod]
		[Ignore("UNO TODO - TestServices.KeyboardHelper not implemented yet.")]
		public async Task IgnoreBringIntoViewOnFocusChange()
		{
			TestCleanupWrapper cleanup;

			Grid rootPanel = null;

			var helper = new CalendarHelper.CalendarViewHelper();

			CalendarView cv = await helper.GetCalendarView();

			rootPanel = await CalendarHelper.CreateTestResources();

			//TestServices.WindowHelper.SetWindowSizeOverride(wf.Count(400, 400));

			// load into visual tree
			await RunOnUIThread(() =>
			{
				cv.MinDate = ConvertToDateTime(1, 2000, 1, 1);
				cv.MaxDate = ConvertToDateTime(1, 2000, 2, 6);
				rootPanel.Children.Add(cv);
			});


			await WindowHelper.WaitForIdle();

			// We'll focus on the item (Jan 23) on last second row, then we press Down arrow key to move focus to the last row (Jan 30)
			// because Jan30 is on the last visible row, without this fix, ScrollViewer will bring this item into view and try to reserver 20 pixels
			// on the bottom, which cause bad visual effect. (by default viewport should land on item's edge, not half of item).

			await RunOnUIThread(() =>
			{
				var monthPanel = CalendarPanel(helper.GetTemplateChild("MonthViewPanel"));
				var itemJan23 = CalendarViewDayItem(monthPanel.Children.GetAt(22));
				itemJan23.Focus(FocusState.Keyboard);
			});

			await WindowHelper.WaitForIdle();

			await TestServices.KeyboardHelper.Down();

			await WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				CheckFocusedItem();
				var focusedElement = CalendarViewDayItem(Input.FocusManager.GetFocusedElement(WindowHelper.XamlRoot));
				var monthPanel = CalendarPanel(helper.GetTemplateChild("MonthViewPanel"));
				var itemJan30 = CalendarViewDayItem(monthPanel.Children.GetAt(29));
				VERIFY_ARE_EQUAL(focusedElement, itemJan30);

				var scrollViewer = ScrollViewer(helper.GetTemplateChild("MonthViewScrollViewer"));
				global::Windows.Foundation.Point origin = new Point(0, 0);
				var itemPos = focusedElement.TransformToVisual(scrollViewer).TransformPoint(origin);

				var itemHeight = FrameworkElement(focusedElement).ActualHeight;

				var margin = FrameworkElement(focusedElement).Margin;

				itemHeight += margin.Top + margin.Bottom;

				global::Windows.Foundation.Point expectedPos = new Point((float)(margin.Left), (float)(itemHeight * 5 + margin.Top));
				LOG_OUTPUT("actual position of Jan30 is (%f, %f)., expected (%f, %f)", itemPos.X, itemPos.Y,
					expectedPos.X, expectedPos.Y);

				VERIFY_ARE_EQUAL(itemPos.X, expectedPos.X);
				VERIFY_ARE_EQUAL(itemPos.Y, expectedPos.Y);
			});
		}


		[TestMethod]
		[Ignore("UNO TODO")]
		public async Task SuspendBuildTreeWhileCollapsed()
		{
			TestCleanupWrapper cleanup;

			Grid rootPanel = null;

			var helper = new CalendarHelper.CalendarViewHelper();

			CalendarView cv = await helper.GetCalendarView();

			rootPanel = await CalendarHelper.CreateTestResources();

			var cicEvent = new Event();
			var cicRegistration = CreateSafeEventRegistration<CalendarView, TypedEventHandler<CalendarView, CalendarViewDayItemChangingEventArgs>>("CalendarViewDayItemChanging");
			cicRegistration.Attach(
				cv,
				(sender, e) =>
				{
					if (!e.InRecycleQueue)
					{
						e.RegisterUpdateCallback(
							(sender, e) =>
						{
							VERIFY_IS_TRUE(e.Phase == 1);
							cicEvent.Set();
						});
					}
				});

			// load into visual tree
			await RunOnUIThread(() =>
			{
				rootPanel.Visibility = Visibility.Collapsed;
				cv.MinDate = ConvertToDateTime(1, 2000, 1, 1);
				cv.MaxDate = ConvertToDateTime(1, 2000, 1, 1);
				rootPanel.Children.Add(cv);
			});

			await WindowHelper.WaitForIdle();
			VERIFY_IS_FALSE(cicEvent.HasFired());

			await RunOnUIThread(() =>
			{
				LOG_OUTPUT("measure to force it to register buildtree work.");
				cv.Measure(new Size(500, 500));
			});

			await WindowHelper.WaitForIdle();
			VERIFY_IS_FALSE(cicEvent.HasFired());

			await RunOnUIThread(() =>
			{
				LOG_OUTPUT("make parent visible");
				rootPanel.Visibility = Visibility.Visible;
			});

			await cicEvent.WaitForDefault();
			VERIFY_IS_TRUE(cicEvent.HasFired());
			await WindowHelper.WaitForIdle();
		}

		// read all the timezone ids, return the one slice
		string[] ReadTimeZoneIds(int part, int total)
		{
			var zones = TimeZoneInfo.GetSystemTimeZones();
			var chunkSize = (zones.Count / total) + 1;

			return zones
				.Skip(chunkSize * part)
				.Take(chunkSize)
				.Select(tzi => tzi.Id)
				.ToArray();
		}

		private async Task VerifyTimeZones(int part, int total)
		{
			string currentTimeZoneId = GetCurrentTimeZoneId();
			var tzids = ReadTimeZoneIds(part, total);
			using var _ = Disposable.Create(
				() =>
				{
					// restore timezone information
					TestServices.Utilities.SetTimeZone(String(currentTimeZoneId));

					// restore the privilege
					TestServices.Utilities.EnableChangingTimeZone(false);

					TestServices.WindowHelper.ResetWindowContentAndWaitForIdle();
				});

			// Enable the required privilege (to allow our broker process to adjust time zone information)
			TestServices.Utilities.EnableChangingTimeZone(true);

			Grid rootPanel = null;
			rootPanel = await CalendarHelper.CreateTestResources();

			// Adjust the time zone information
			foreach (var tzid in tzids)
			{
				if (tzid == "Samoa Standard Time")
				{
					// TODO: enable this time zone once below bug is fixed
					// Bug 4667807:calling AddDays(N) followed by AddDays(-N) doesn't go back to same day in Samoa TimeZone when the range contains [12/30/2008, 1/2/2012]
					continue;
				}

				LOG_OUTPUT("\r\nSet TimeZone to \"%s\"", tzid);
				TestServices.Utilities.SetTimeZone(tzid);

				var helper = new CalendarHelper.CalendarViewHelper();
				CalendarView cv = await helper.GetCalendarView();
				CalendarPanel monthPanel = null;
				await RunOnUIThread(() =>
				{
					rootPanel.Children.Clear();
					rootPanel.Children.Add(cv);
					cv.MinDate = ConvertToDateTime(1, 1910, 1, 2, 1, 8, 0, 0, 0); // Monday
					cv.MaxDate = ConvertToDateTime(1, 2101, 1, 1, 1, 8, 0, 0, 0); // Sunday

				});

				await WindowHelper.WaitForIdle();

				// below tests will verify the first day and last day are in the correct positions.

				await RunOnUIThread(() =>
				{
					monthPanel = CalendarPanel(helper.GetTemplateChild("MonthViewPanel"));
					cv.SetDisplayDate(cv.MinDate);
				});

				await RunOnUIThread(() =>
				{
					var firstItem = CalendarViewDayItem(monthPanel.Children.GetAt(0));
					CalendarHelper.VerifyDateTimesAreEqual(firstItem.Date, cv.MinDate);
					VerifyItemPositionInPanel(firstItem, monthPanel, 0, 0); //Monday

					cv.SetDisplayDate(cv.MaxDate);

				});

				await WindowHelper.WaitForIdle();

				await RunOnUIThread(() =>
				{
					CalendarViewDayItem lastItem;
					global::Windows.Foundation.Rect layoutSlot;
					int lastIndex = monthPanel.Children.Count;

					// find the last realized item. The realized item's layout slot will
					// not have negative offset.
					do
					{
						lastIndex--;
						lastItem = CalendarViewDayItem(monthPanel.Children.GetAt(lastIndex));
						layoutSlot = LayoutInformation.GetLayoutSlot(lastItem);
					} while (lastIndex > 0 && (layoutSlot.X < 0 || layoutSlot.Y < 0));

					CalendarHelper.VerifyDateTimesAreEqual(lastItem.Date, cv.MaxDate);

					VerifyItemPositionInPanel(lastItem, monthPanel, 6, 9965); // Sunday
				});
			}
		}

		[TestMethod]
		[Ignore("UNO TODO - TestServices.Utilities.SetTimeZone() not implemented")]
		public async Task VerifySkippedDaysInSamoa()
		{
			string currentTimeZoneId = GetCurrentTimeZoneId();
			using var cleanup = Disposable.Create(
				() =>
				{
					// restore timezone information
					TestServices.Utilities.SetTimeZone(String(currentTimeZoneId));

					// restore the privilege
					TestServices.Utilities.EnableChangingTimeZone(false);

					TestServices.WindowHelper.ResetWindowContentAndWaitForIdle();
				});

			// Enable the required privilege (to allow our broker process to adjust time zone information)
			TestServices.Utilities.EnableChangingTimeZone(true);

			Grid rootPanel = null;
			rootPanel = await CalendarHelper.CreateTestResources();

			// Adjust the time zone information
			string tzid = "Samoa Standard Time";
			LOG_OUTPUT("\r\nSet TimeZone to \"%s\"", tzid);
			TestServices.Utilities.SetTimeZone(tzid);

			var helper = new CalendarHelper.CalendarViewHelper();
			CalendarView cv = await helper.GetCalendarView();
			await RunOnUIThread(() =>
			{
				rootPanel.Children.Add(cv);
				cv.MinDate = ConvertToDateTime(1, 2011, 12, 31); // Saturday
				cv.MaxDate = ConvertToDateTime(1, 2012, 1, 2); // Monday
				cv.FirstDayOfWeek = global::Windows.Globalization.DayOfWeek.Saturday;
			});

			await WindowHelper.WaitForIdle();

			// below test will verify that we have a hole between 12/31/2011 and 1/2/2012
			// and also verify that days after 1/2/2012 are on the correct dayofweek.
			await RunOnUIThread(() =>
			{
				var monthPanel = CalendarPanel(helper.GetTemplateChild("MonthViewPanel"));
				VERIFY_ARE_EQUAL(monthPanel.Children.Count, 2); // there are only two days in this view
				VerifyItemPositionInPanel(monthPanel.Children.GetAt(0), monthPanel, 0,
					0); // the first date (12/21/2011) is at (0, 0)
				VerifyItemPositionInPanel(monthPanel.Children.GetAt(1), monthPanel, 2,
					0); // the second date (1/2/2012) is at (0, 2)
			});

		}

		[TestMethod]
		[Ignore("UNO TODO - InputHelper.MoveMouse() not implemented")]
		public async Task TodayVisualTest()
		{
			//WUCRenderingScopeGuard guard(DCompRendering.WUCCompleteSynchronousCompTree, false /* resizeWindow */);
			//TestServices.WindowHelper.SetWindowSizeOverride(wf.Count(400, 400));

			var Jan12015 = ConvertToDateTime(1, 2015, 1, 1);

			Grid rootPanel = null;
			rootPanel = await CalendarHelper.CreateTestResources();

			CalendarView cv = null;
			CalendarViewDayItem todayItem = null;

			{
				await RunOnUIThread(() =>
				{
					cv = new CalendarView();

					rootPanel.Children.Add(cv);
					cv.MinDate = Jan12015;
					cv.MaxDate = Jan12015;
				});

				await WindowHelper.WaitForIdle();
			}

			TestServices.Utilities.VerifyMockDCompOutput(MockDComp.SurfaceComparison.NoComparison, "1"); //Today

			await RunOnUIThread(() =>
			{
				cv.IsTodayHighlighted = false;
			});

			await WindowHelper.WaitForIdle();
			TestServices.Utilities.VerifyMockDCompOutput(MockDComp.SurfaceComparison.NoComparison,
				"2"); //Today+without highlighted

			await RunOnUIThread(() =>
			{
				cv.IsTodayHighlighted = true;
				cv.SelectedDates.Add(Jan12015);
			});

			await WindowHelper.WaitForIdle();
			TestServices.Utilities.VerifyMockDCompOutput(MockDComp.SurfaceComparison.NoComparison,
				"3"); //Today+Selected

			await RunOnUIThread(() =>
			{
				var monthPanel = CalendarPanel(CalendarHelper.GetTemplateChild(cv,
					"MonthViewPanel"));
				todayItem = CalendarViewDayItem(monthPanel.Children.GetAt(0));
				todayItem.IsBlackout = true;
			});

			await WindowHelper.WaitForIdle();
			TestServices.Utilities.VerifyMockDCompOutput(MockDComp.SurfaceComparison.NoComparison,
				"4"); //Today+Blackout

			await RunOnUIThread(() =>
			{
				todayItem.IsBlackout = false;
			});

			TestServices.InputHelper.MoveMouse(todayItem);

			await WindowHelper.WaitForIdle();
			TestServices.Utilities.VerifyMockDCompOutput(MockDComp.SurfaceComparison.NoComparison, "5"); //Today+Hover
																										 // move mouse away to avoid any unexpected hover state.
			TestServices.InputHelper.MoveMouse(new global::Windows.Foundation.Point(0, 0));

			TestServices.InputHelper.DynamicPressCenter(todayItem, 0, 0, PointerFinger.Finger1);

			await WindowHelper.WaitForIdle();
			await TestServices.WindowHelper.SynchronouslyTickUIThread(2);

			TestServices.Utilities.VerifyMockDCompOutput(MockDComp.SurfaceComparison.NoComparison, "6"); //Today+Pressed

			TestServices.InputHelper.DynamicRelease(PointerFinger.Finger1);
		}

		[TestMethod]
		[Ignore("UNO TODO")]
		public async Task ChangingViewHeaderTest()
		{
			TestCleanupWrapper cleanup;

			Grid rootPanel = null;
			Button headerButton = null;

			var helper = new CalendarHelper.CalendarViewHelper();
			CalendarView cv = await helper.GetCalendarView();
			rootPanel = await CalendarHelper.CreateTestResources();

			// load into visual tree
			await RunOnUIThread(() =>
			{
				rootPanel.Children.Add(cv);
			});

			await WindowHelper.WaitForIdle();

			// find the header button and tap it
			{
				await RunOnUIThread(() =>
				{
					headerButton = Button(helper.GetTemplateChild("HeaderButton"));
					var headText = String(headerButton.Content);
					LOG_OUTPUT("Current month %s", headText);
					//VERIFY_ARE_EQUAL(headText, "\u200eDecember\u200e \u200e2015");
					VERIFY_ARE_EQUAL(headText, "December 2015");
				});

				LOG_OUTPUT("CalendarViewIntegrationTests: changing viewmode to Year by using Tap.");

				await ControlHelper.DoClickUsingAP(headerButton);

				await WindowHelper.WaitForIdle();

				await RunOnUIThread(() =>
				{
					headerButton = Button(helper.GetTemplateChild("HeaderButton"));
					var headText = String(headerButton.Content);
					LOG_OUTPUT("Current year %s", headText);
					//VERIFY_ARE_EQUAL(headText, "\u200e2015");
					VERIFY_ARE_EQUAL(headText, "2015");

				});

				LOG_OUTPUT("CalendarViewIntegrationTests: changing viewmode to Decade by using Tap.");

				await ControlHelper.DoClickUsingAP(headerButton);

				await WindowHelper.WaitForIdle();

				await RunOnUIThread(() =>
				{
					headerButton = Button(helper.GetTemplateChild("HeaderButton"));
					var headText = String(headerButton.Content);
					LOG_OUTPUT("Current decade %s", headText);
					VERIFY_IS_TRUE(headText == "2010 - 2019");
				});
			}

		}

		[TestMethod]
		[Ignore("UNO TODO")]
		public async Task ChangingViewHeaderWithCustomDimensionsTest()
		{
			TestCleanupWrapper cleanup;

			Grid rootPanel = null;
			Button headerButton = null;

			var helper = new CalendarHelper.CalendarViewHelper();
			CalendarView cv = await helper.GetCalendarView();
			rootPanel = await CalendarHelper.CreateTestResources();

			// load into visual tree
			await RunOnUIThread(() =>
			{
				rootPanel.Children.Add(cv);
			});

			await WindowHelper.WaitForIdle();

			// find the header button and tap it
			{
				await RunOnUIThread(() =>
				{
					headerButton = Button(helper.GetTemplateChild("HeaderButton"));
					var headText = String(headerButton.Content);
					LOG_OUTPUT("Current month %s", headText);
					//VERIFY_ARE_EQUAL(headText, "\u200eDecember\u200e \u200e2015");
					VERIFY_ARE_EQUAL(headText, "December 2015");
				});

				await WindowHelper.WaitForIdle();
				await RunOnUIThread(() =>
				{
					cv.SetYearDecadeDisplayDimensions(4, 1);
				});
				await WindowHelper.WaitForIdle();

				LOG_OUTPUT("CalendarViewIntegrationTests: changing viewmode to Year by using Tap.");

				await ControlHelper.DoClickUsingAP(headerButton);

				await WindowHelper.WaitForIdle();

				await RunOnUIThread(() =>
				{
					headerButton = Button(helper.GetTemplateChild("HeaderButton"));
					var headText = String(headerButton.Content);
					LOG_OUTPUT("Current year %s", headText);
					//VERIFY_ARE_EQUAL(headText, "\u200e2015");
					VERIFY_ARE_EQUAL(headText, "2015");
				});

				LOG_OUTPUT("CalendarViewIntegrationTests: changing viewmode to Decade by using Tap.");

				await ControlHelper.DoClickUsingAP(headerButton);

				await WindowHelper.WaitForIdle();

				await RunOnUIThread(() =>
				{
					headerButton = Button(helper.GetTemplateChild("HeaderButton"));
					var headText = String(headerButton.Content);
					LOG_OUTPUT("Current decade %s", headText);
					VERIFY_ARE_EQUAL(headText, "2010 - 2019");
				});
			}

		}

		[TestMethod]
		[Ignore("UNO TODO")]
		public async Task VerifyChangingHeightDoesNotScrollAwayFromItems()
		{
			TestCleanupWrapper cleanup;


			Grid rootPanel = null;
			//Button headerButton = null;
			int changeCount = 0;

			var helper = new CalendarHelper.CalendarViewHelper();

			CalendarView cv = await helper.GetCalendarView();

			rootPanel = await CalendarHelper.CreateTestResources();

			// load into visual tree
			await RunOnUIThread(() =>
			{
				cv.Height = 300;
				rootPanel.Children.Add(cv);
			});

			await WindowHelper.WaitForIdle();

			var cicEvent = new Event();
			var cicRegistration = CreateSafeEventRegistration<CalendarView, TypedEventHandler<CalendarView, CalendarViewDayItemChangingEventArgs>>("CalendarViewDayItemChanging");
			cicRegistration.Attach(
				cv,
				(sender, e) =>
				{
					changeCount = changeCount + 1;
				});

			await RunOnUIThread(() =>
			{
				VERIFY_ARE_EQUAL(changeCount, 0, "changeCount - 1");

				cv.Height = 350;
			});

			await WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				//Initially we had expected this to return as 0, as in this scenario the items should stretch. However
				//after a little research we discovered that ModernCollectionBasePanel.ArrangeOverride adds small
				//deltas to the visible window, which causes this behavior when the CalendarView's height increases.
				VERIFY_ARE_EQUAL(changeCount, 7);

				changeCount = 0;

				cv.Height = 300;
			});

			await WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				VERIFY_ARE_EQUAL(changeCount, 0, "changeCount - 2");
			});
		}

		[TestMethod]
		[Ignore("UNO TODO")]
		public async Task VerifyForegroundColorChangesPropagateWhenInPopup()
		{
			TestCleanupWrapper cleanup;
			var helper = new CalendarHelper.CalendarViewHelper();
			global::Windows.UI.Color newColor = Microsoft.UI.Colors.Red;

			Button flyoutButton = null;
			Flyout flyout = null;
			CalendarView calendarView = await helper.GetCalendarView();

			var loadedEvent = new Event();
			var loadedRegistration = CreateSafeEventRegistration<StackPanel, RoutedEventHandler>("Loaded");

			await RunOnUIThread(() =>
			{
				var rootPanel = new StackPanel();

				flyoutButton = new Button();
				flyoutButton.Content = "Click for CalendarView flyout";

				flyout = new Flyout();
				flyout.Content = calendarView;

				flyoutButton.Flyout = flyout;
				rootPanel.Children.Add(flyoutButton);

				loadedRegistration.Attach(rootPanel, (s, e) =>
				{
					loadedEvent.Set();
				});
				global::Private.Infrastructure.TestServices.WindowHelper.WindowContent = rootPanel;
			});

			await loadedEvent.WaitForDefault();
			await WindowHelper.WaitForIdle();

			LOG_OUTPUT("Open the flyout to initially realize all of the CalendarView items.");
			FlyoutHelper.OpenFlyout<Flyout>(flyout, flyoutButton, FlyoutOpenMethod.Programmatic_ShowAt);
			FlyoutHelper.HideFlyout<Flyout>(flyout);

			await RunOnUIThread(() =>
			{
				LOG_OUTPUT(
					"Now set CalendarItemForeground to change the intended foreground color of CalendarView items.");
				calendarView.CalendarItemForeground = new SolidColorBrush(newColor);
			});

			LOG_OUTPUT("Open the flyout again. The items should have changed to the new foreground color.");
			FlyoutHelper.OpenFlyout<Flyout>(flyout, flyoutButton, FlyoutOpenMethod.Programmatic_ShowAt);

			await RunOnUIThread(() =>
			{
				var calendarPanel = CalendarPanel(helper.GetTemplateChild("MonthViewPanel"));
				var item = Control(calendarPanel.Children.GetAt(0));
				var textBlock = TreeHelper.GetVisualChildByType<TextBlock>(item);
				var textBlockColor = SolidColorBrush(textBlock.Foreground).Color;

				VERIFY_ARE_EQUAL(newColor, textBlockColor);
			});

			FlyoutHelper.HideFlyout<Flyout>(flyout);
		}

		// we have 128 timezones so far, split them into small parts so the test won't take too long.
		//const int VerifyAllTimeZonesPartImpl(i, = j) ;
		[TestMethod]
		[Ignore("UNO TODO - TestServices.Utilities.SetTimeZone() not implemented")]
		public async Task VerifyAllTimeZonesPart()
		{
			await VerifyTimeZones(0, 20);
			await VerifyTimeZones(1, 20);
			await VerifyTimeZones(2, 20);
			await VerifyTimeZones(3, 20);
			await VerifyTimeZones(4, 20);
			await VerifyTimeZones(5, 20);
			await VerifyTimeZones(6, 20);
			await VerifyTimeZones(7, 20);
			await VerifyTimeZones(8, 20);
			await VerifyTimeZones(9, 20);
			await VerifyTimeZones(10, 20);
			await VerifyTimeZones(11, 20);
			await VerifyTimeZones(12, 20);
			await VerifyTimeZones(13, 20);
			await VerifyTimeZones(14, 20);
			await VerifyTimeZones(15, 20);
			await VerifyTimeZones(16, 20);
			await VerifyTimeZones(17, 20);
			await VerifyTimeZones(18, 20);
			await VerifyTimeZones(19, 20);
		}



		[TestMethod]
		[Ignore("UNO TODO - Asserts ERROR_CALENDAR_NUMBER_OF_WEEKS_OUTOFRANGE")]
		public async Task VerifyCalendarBoundariesByTappingByTappingNavigationButton(string cid)
		{
			TestCleanupWrapper cleanup;

			Grid rootPanel = null;
			var defaultCalendar = new global::Windows.Globalization.Calendar();

			(CalendarViewDisplayMode displayMode, string panelName, string scrollViewerName)[] modes = {
				(
					CalendarViewDisplayMode.Month, "MonthViewPanel", "MonthViewScrollViewer"
				),
				(
					CalendarViewDisplayMode.Year, "YearViewPanel", "YearViewScrollViewer"
				),
				(
					CalendarViewDisplayMode.Decade, "DecadeViewPanel", "DecadeViewScrollViewer"
				)
			};

			(int numberOfWeeks, int rowInYearDecadeView, int colInYearDecadeView)[] dimensions =
			{
				(
					2, 2, 4
				), // to test the code that Panel can not show a full scope
				(
					6, 4, 4
				) // to test the code that Panel can show a full scope
			};

			rootPanel = await CalendarHelper.CreateTestResources();

			LOG_OUTPUT("Begin Testing CalendarIdentifier: %s", cid);

			foreach (var dimension in dimensions)
			{
				//CalendarPanel calendarPanel = null;
				var helper = new CalendarHelper.CalendarViewHelper();

				CalendarView cv = await helper.GetCalendarView();
				ScrollViewer scrollViewer = null;
				CalendarPanel panel = null;

				Button previousButton = null;
				Button nextButton = null;

				await RunOnUIThread(() =>
				{
					rootPanel.Children.Clear();

					// set the limits from current calendaridentifier on CalendarView.
					cv.CalendarIdentifier = cid;
					var cal = new global::Windows.Globalization.Calendar(defaultCalendar.Languages, cid,
						defaultCalendar.GetClock());

					cal.SetToMin();
					cv.MinDate = cal.GetDateTime();

					cal.SetToMax();
					cv.MaxDate = cal.GetDateTime();

					// load into visual tree
					rootPanel.Children.Add(cv);

					cv.UpdateLayout();

					cv.NumberOfWeeksInView = dimension.numberOfWeeks;
					cv.SetYearDecadeDisplayDimensions(dimension.colInYearDecadeView, dimension.rowInYearDecadeView);
				});

				await WindowHelper.WaitForIdle();

				await RunOnUIThread(() =>
				{
					previousButton = Button(helper.GetTemplateChild("PreviousButton"));
					nextButton = Button(helper.GetTemplateChild("NextButton"));
				});

				foreach (var mode in modes)
				{
					LOG_OUTPUT(" Change DisplayMode to %s", mode.displayMode.ToString());

					double itemHeight = 0;

					await RunOnUIThread(() =>
					{
						cv.DisplayMode = mode.displayMode;
						cv.UpdateLayout();

						scrollViewer = ScrollViewer(helper.GetTemplateChild(mode.scrollViewerName));
						panel = CalendarPanel(helper.GetTemplateChild(mode.panelName));
						var firstItem = Control(panel.Children.GetAt(0));
						itemHeight = firstItem.ActualHeight;
					});

					var viewChangedEvent = new Event();
					var viewChangedRegistration = CreateSafeEventRegistration<ScrollViewer, EventHandler<ScrollViewerViewChangedEventArgs>>("ViewChanged");

					viewChangedRegistration.Attach(scrollViewer,
						(sender, e) =>
						{
							if (!e.IsIntermediate)
							{
								LOG_OUTPUT("ViewChanged. vertical offset: %lf",
									((ScrollViewer)sender).VerticalOffset);
								viewChangedEvent.Set();
							}
						});

					await RunOnUIThread(() =>
					{
						// jump to row#1.5, previous button is enabled
						scrollViewer.ChangeView(null /* horizontalOffset */, itemHeight * 1.5, 1.0f, true /* disableAnimation */);
					});

					await viewChangedEvent.WaitForDefault();
					VERIFY_IS_TRUE(viewChangedEvent.HasFired());
					viewChangedEvent.Reset();

					await RunOnUIThread(() =>
					{
						VERIFY_IS_TRUE(previousButton.IsEnabled);
					});

					// tap the previous button, we should go to the very beginning without crash.
					TestServices.InputHelper.Tap(previousButton);

					await viewChangedEvent.WaitForDefault();
					VERIFY_IS_TRUE(viewChangedEvent.HasFired());
					viewChangedEvent.Reset();

					await RunOnUIThread(() =>
					{
						VERIFY_ARE_EQUAL(scrollViewer.VerticalOffset, 0);

						// jump to the last row#1.5, next button is enabled
						scrollViewer.ChangeView(null /* horizontalOffset */,
							scrollViewer.ScrollableHeight - itemHeight * 1.5, 1.0f, true /* disableAnimation */);
					});

					await viewChangedEvent.WaitFor(TimeSpan.FromMilliseconds(10000));
					VERIFY_IS_TRUE(viewChangedEvent.HasFired());
					viewChangedEvent.Reset();

					await RunOnUIThread(() =>
					{
						VERIFY_IS_TRUE(nextButton.IsEnabled); // can go forward
					});

					// tap the next button, we should go to the very end without crash
					TestServices.InputHelper.Tap(nextButton);

					await viewChangedEvent.WaitForDefault();
					VERIFY_IS_TRUE(viewChangedEvent.HasFired());
					viewChangedEvent.Reset();

					await RunOnUIThread(() =>
					{
						VERIFY_ARE_EQUAL(scrollViewer.VerticalOffset, scrollViewer.ScrollableHeight);
					});
				}
				LOG_OUTPUT("End Testing CalendarIdentifier: %s", cid);
			}
		}

		[TestMethod]
		[Ignore("UNO-TODO: Test fails Debug.Assert()")]
		public Task VerifyPersianCalendarBoundariesByTapping()
		{
			return VerifyCalendarBoundariesByTappingByTappingNavigationButton("PersianCalendar");
		}

		[TestMethod]
		[Ignore("UNO-TODO: Test fails Debug.Assert()")]
		public Task VerifyGregorianCalendarBoundariesByTapping()
		{
			return VerifyCalendarBoundariesByTappingByTappingNavigationButton("GregorianCalendar");
		}

		[TestMethod]
		[Ignore("UNO-TODO: Test fails Debug.Assert()")]
		public Task VerifyHebrewCalendarBoundariesByTapping()
		{
			return VerifyCalendarBoundariesByTappingByTappingNavigationButton("HebrewCalendar");
		}

		[TestMethod]
		[Ignore("UNO-TODO: Test fails Debug.Assert()")]
		public Task VerifyHijriCalendarBoundariesByTapping()
		{
			return VerifyCalendarBoundariesByTappingByTappingNavigationButton("HijriCalendar");
		}

		[TestMethod]
		[Ignore("UNO-TODO: Test fails Debug.Assert()")]
		public Task VerifyJapaneseCalendarBoundariesByTapping()
		{
			return VerifyCalendarBoundariesByTappingByTappingNavigationButton("JapaneseCalendar");
		}

		[TestMethod]
		[Ignore("UNO-TODO: Test fails Debug.Assert()")]
		public Task VerifyJulianCalendarBoundariesByTapping()
		{
			return VerifyCalendarBoundariesByTappingByTappingNavigationButton("JulianCalendar");
		}

		[TestMethod]
		[Ignore("UNO-TODO: Test fails Debug.Assert()")]
		public Task VerifyKoreanCalendarBoundariesByTapping()
		{
			return VerifyCalendarBoundariesByTappingByTappingNavigationButton("KoreanCalendar");
		}

		[TestMethod]
		[Ignore("UNO-TODO: Test fails Debug.Assert()")]
		public Task VerifyTaiwanCalendarBoundariesByTapping()
		{
			return VerifyCalendarBoundariesByTappingByTappingNavigationButton("TaiwanCalendar");
		}

		[TestMethod]
		[Ignore("UNO-TODO: Test fails Debug.Assert()")]
		public Task VerifyThaiCalendarBoundariesByTapping()
		{
			return VerifyCalendarBoundariesByTappingByTappingNavigationButton("ThaiCalendar");
		}

		[TestMethod]
		[Ignore("UNO TODO - Test fails Debug.Assert()")]
		public Task VerifyUmAlQuraCalendarBoundariesByTapping()
		{
			return VerifyCalendarBoundariesByTappingByTappingNavigationButton("UmAlQuraCalendar");
		}

		[TestMethod]
		[Ignore("UNO TODO - RTL (Right To Left) not supported yet on Uno")]
		public async Task VerifyFlowDirectionForCalendar()
		{
			TestCleanupWrapper cleanup;

			var helper = new CalendarHelper.CalendarViewHelper();
			CalendarView cv = await helper.GetCalendarView();
			Grid rootPanel;
			rootPanel = await CalendarHelper.CreateTestResources();

			await RunOnUIThread(() =>
			{
				rootPanel.Children.Add(cv);
			});
			await WindowHelper.WaitForIdle();

			// The expected FlowDirection for each calendar id:
			(string first, FlowDirection second)[] calendarFlowDirections =
			{
				(
					"PersianCalendar", FlowDirection.RightToLeft
				),
				(
					"GregorianCalendar", FlowDirection.LeftToRight
				),
				(
					"HebrewCalendar", FlowDirection.RightToLeft
				),
				(
					"HijriCalendar", FlowDirection.RightToLeft
				),
				(
					"JapaneseCalendar", FlowDirection.LeftToRight
				),
				(
					"JulianCalendar", FlowDirection.LeftToRight
				),
				(
					"KoreanCalendar", FlowDirection.LeftToRight
				),
				(
					"TaiwanCalendar", FlowDirection.LeftToRight
				),
				(
					"ThaiCalendar", FlowDirection.LeftToRight
				),
				(
					"UmAlQuraCalendar", FlowDirection.RightToLeft
				)
			};

			foreach (var calendarFlowDirection in calendarFlowDirections)
			{
				var cid = calendarFlowDirection.first;
				var expectedFlowDirection = calendarFlowDirection.second;

				LOG_OUTPUT("Verifying FlowDirection for Calendar '%s' is '%s'", cid, expectedFlowDirection.ToString());

				await RunOnUIThread(() =>
				{
					cv.CalendarIdentifier = cid;
				});
				await WindowHelper.WaitForIdle();

				await RunOnUIThread(() =>
				{
					var viewsGrid = Grid(helper.GetTemplateChild("Views"));
					var actualFlowDirection = viewsGrid.FlowDirection;
					VERIFY_ARE_EQUAL(expectedFlowDirection, actualFlowDirection);
				});
			}
		}

		[TestMethod]
#if !__SKIA__
		[Ignore("Test is failing on non-Skia targets https://github.com/unoplatform/uno/issues/17984")]
#endif
		public async Task CanScrollAcrossJapaneseEraOnMonthView()
		{
			TestCleanupWrapper cleanup;

			var helper = new CalendarHelper.CalendarViewHelper();
			CalendarView cv = await helper.GetCalendarView();

			Grid rootPanel = null;
			rootPanel = await CalendarHelper.CreateTestResources();

			await RunOnUIThread(() =>
			{
				cv.CalendarIdentifier = "JapaneseCalendar";
				cv.DisplayMode = CalendarViewDisplayMode.Month;
				// by default, CalendarView uses today +- 100years for min/max date
				// we need enough wiggle room for previous-Decade/Year/Month button to work correctly
				cv.MinDate = ConvertToDateTime(1, 1926, 12 - 1 * 2, 25);
				rootPanel.Children.Add(cv);
			});

			await WindowHelper.WaitForIdle();

			LOG_OUTPUT("Verify Taisho - Showa boundary in year view");
			var firstDayOfShowa = ConvertToDateTime(1, 1926, 12, 25);
			await VerifyJapaneseEraBoundary(helper, CalendarViewDisplayMode.Month, firstDayOfShowa);

			await WindowHelper.WaitForIdle();

			LOG_OUTPUT("Verify Showa - Heisei boundary in year view");
			var firstDayOfHeisei = ConvertToDateTime(1, 1989, 1, 8);
			await VerifyJapaneseEraBoundary(helper, CalendarViewDisplayMode.Month, firstDayOfHeisei);

			// we don't know the name of the next era yet, using "??" as place holder
			LOG_OUTPUT("Verify Heisei - ?? boundary in year view");
			var firstDayOfNewEra = ConvertToDateTime(1, 2019, 5, 1);
			await VerifyJapaneseEraBoundary(helper, CalendarViewDisplayMode.Month, firstDayOfNewEra);
		}

		[TestMethod]
		[Ignore("Japanese calendars are not supported yet")]
		public async Task CanScrollAcrossJapaneseEraOnYearView()
		{
			TestCleanupWrapper cleanup;

			var helper = new CalendarHelper.CalendarViewHelper();
			CalendarView cv = await helper.GetCalendarView();

			Grid rootPanel = null;
			rootPanel = await CalendarHelper.CreateTestResources();

			await RunOnUIThread(() =>
			{
				cv.CalendarIdentifier = "JapaneseCalendar";
				cv.DisplayMode = CalendarViewDisplayMode.Year;
				// by default, CalendarView uses today +- 100years for min/max date
				// we need enough wiggle room for previous-Decade/Year/Month button to work correctly
				cv.MinDate = ConvertToDateTime(1, 1926 - 1 * 2, 12, 25);
				rootPanel.Children.Add(cv);
			});

			await WindowHelper.WaitForIdle();

			LOG_OUTPUT("Verify Taisho - Showa boundary in year view");
			var firstDayOfShowa = ConvertToDateTime(1, 1926, 12, 25);
			await VerifyJapaneseEraBoundary(helper, CalendarViewDisplayMode.Year, firstDayOfShowa);

			await WindowHelper.WaitForIdle();

			LOG_OUTPUT("Verify Showa - Heisei boundary in year view");
			var firstDayOfHeisei = ConvertToDateTime(1, 1989, 1, 8);
			await VerifyJapaneseEraBoundary(helper, CalendarViewDisplayMode.Year, firstDayOfHeisei);

			// we don't know the name of the next era yet, using "??" as place holder
			LOG_OUTPUT("Verify Heisei - ?? boundary in year view");
			var firstDayOfNewEra = ConvertToDateTime(1, 2019, 5, 1);
			await VerifyJapaneseEraBoundary(helper, CalendarViewDisplayMode.Year, firstDayOfNewEra);
		}

		[TestMethod]
		[Ignore("UNO TODO")]
		public async Task CanScrollAcrossJapaneseEraOnDecadeView()
		{
			TestCleanupWrapper cleanup;

			var helper = new CalendarHelper.CalendarViewHelper();
			CalendarView cv = await helper.GetCalendarView();

			Grid rootPanel = null;
			rootPanel = await CalendarHelper.CreateTestResources();

			await RunOnUIThread(() =>
			{
				cv.CalendarIdentifier = "JapaneseCalendar";
				cv.DisplayMode = CalendarViewDisplayMode.Decade;
				// by default, CalendarView uses today +- 100years for min/max date
				// we need enough wiggle room for previous-Decade/Year/Month button to work correctly
				cv.MinDate = ConvertToDateTime(1, 1926 - 10 * 2, 12, 25);
				rootPanel.Children.Add(cv);
			});

			await WindowHelper.WaitForIdle();

			LOG_OUTPUT("Verify Taisho - Showa boundary in decade view");
			var firstDayOfShowa = ConvertToDateTime(1, 1926, 12, 25);
			await VerifyJapaneseEraBoundary(helper, CalendarViewDisplayMode.Decade, firstDayOfShowa, "10 - 15", "1 - 9");

			await WindowHelper.WaitForIdle();

			LOG_OUTPUT("Verify Showa - Heisei boundary in decade view");
			var firstDayOfHeisei = ConvertToDateTime(1, 1989, 1, 8);
			await VerifyJapaneseEraBoundary(helper, CalendarViewDisplayMode.Decade, firstDayOfHeisei, "60 - 64", "1 - 9");

			// we don't know the name of the next era yet, using "??" as place holder
			LOG_OUTPUT("Verify Heisei - ?? boundary in decade view");
			var firstDayOfNewEra = ConvertToDateTime(1, 2019, 5, 1);
			await VerifyJapaneseEraBoundary(helper, CalendarViewDisplayMode.Decade, firstDayOfNewEra);
		}

		private async Task VerifyJapaneseEraBoundary(
			CalendarHelper.CalendarViewHelper helper, CalendarViewDisplayMode displayMode,
			DateTimeOffset date, string oldEraHeaderText = null, string newEraHeaderText = null)
		{
			CalendarView cv = await helper.GetCalendarView();

			Button headerButton = null;
			Button previousButton = null;
			Button nextButton = null;
			ScrollViewer scrollViewer = null;
			await RunOnUIThread(() =>
			{
				headerButton = Button(helper.GetTemplateChild("HeaderButton"));
				previousButton = Button(helper.GetTemplateChild("PreviousButton"));
				nextButton = Button(helper.GetTemplateChild("NextButton"));
				switch (displayMode)
				{
					case CalendarViewDisplayMode.Decade:
						scrollViewer = ScrollViewer(helper.GetTemplateChild("DecadeViewScrollViewer"));
						break;
					case CalendarViewDisplayMode.Year:
						scrollViewer = ScrollViewer(helper.GetTemplateChild("YearViewScrollViewer"));
						break;
					case CalendarViewDisplayMode.Month:
						scrollViewer = ScrollViewer(helper.GetTemplateChild("MonthViewScrollViewer"));
						break;
				}

				cv.SetDisplayDate(date);
			});

			var viewChangedEvent = new Event();
			var viewChangedRegistration = CreateSafeEventRegistration<ScrollViewer, EventHandler<ScrollViewerViewChangedEventArgs>>("ViewChanged");
			viewChangedRegistration.Attach(scrollViewer,
				(sender, e) =>
				{
					if (!e.IsIntermediate)
					{
						viewChangedEvent.Set();
					}
				});

			await WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				var headText = String(headerButton.Content);
				if (newEraHeaderText is { })
				{
					VERIFY_ARE_EQUAL(headText, newEraHeaderText, "newEraHeaderText 1");
				}
			});

			TestServices.InputHelper.Tap(previousButton);

			await viewChangedEvent.WaitForDefault();
			viewChangedEvent.Reset();
			await WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				var headText = String(headerButton.Content);
				if (oldEraHeaderText is { })
				{
					VERIFY_ARE_EQUAL(headText, oldEraHeaderText, "oldEraHeaderText");
				}
			});

			TestServices.InputHelper.Tap(nextButton);

			await viewChangedEvent.WaitForDefault();
			viewChangedEvent.Reset();
			await WindowHelper.WaitForIdle();

			await RunOnUIThread(() =>
			{
				var headText = String(headerButton.Content);
				if (newEraHeaderText is { })
				{
					VERIFY_ARE_EQUAL(headText, newEraHeaderText, "newEraHeaderText 2");
				}
			});
		}

		[TestMethod]
		public async Task CanSwitchDisplayMode()
		{
			TestCleanupWrapper cleanup;

			CalendarViewDisplayMode[] modes =
			{
				CalendarViewDisplayMode.Month,
				CalendarViewDisplayMode.Year,
				CalendarViewDisplayMode.Decade,
				CalendarViewDisplayMode.Year,
				CalendarViewDisplayMode.Month
			};

			string[] panelNames = {
				"MonthViewPanel", "YearViewPanel", "DecadeViewPanel", "YearViewPanel", "MonthViewPanel"
			}
			;

			var helper = new CalendarHelper.CalendarViewHelper();
			CalendarView cv = await helper.GetCalendarView();

			Grid rootPanel = null;
			rootPanel = await CalendarHelper.CreateTestResources();

			await RunOnUIThread(() =>
			{
				rootPanel.Children.Add(cv);
			});

			int count = modes.Length;
			for (int i = 0; i < count; i++)
			{
				await RunOnUIThread(() =>
				{
					cv.DisplayMode = modes[i];
				});
				await WindowHelper.WaitForIdle();

				await RunOnUIThread(() =>
				{
					CalendarPanel panel = CalendarPanel(helper
						.GetTemplateChild(panelNames[i]));
					TestServices.VERIFY_IS_NOT_NULL(panel);
				});
			}

		}

		static LinearGradientBrush MakeLinearGradientBrush(float startX, float endX)
		{
			GradientStop stop1 = new GradientStop();
			stop1.Color = Microsoft.UI.Colors.Green;
			stop1.Offset = 0.2;

			GradientStop stop2 = new GradientStop();
			stop2.Color = Microsoft.UI.Colors.Red;
			stop2.Offset = 0.8;

			GradientStopCollection stopCollection = new GradientStopCollection();
			stopCollection.Add(stop1);
			stopCollection.Add(stop2);

			LinearGradientBrush brush = new LinearGradientBrush();
			brush.StartPoint = new global::Windows.Foundation.Point(startX, 0);
			brush.EndPoint = new global::Windows.Foundation.Point(endX, 0);
			brush.GradientStops = stopCollection;
			return brush;
		}
	}
}
