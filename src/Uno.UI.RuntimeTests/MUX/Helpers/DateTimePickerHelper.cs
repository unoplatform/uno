using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using MUXControlsTestApp.Utilities;
using Private.Infrastructure;
using Microsoft.UI.Xaml;
using Windows.Foundation;
using static Private.Infrastructure.TestServices;
using System.Globalization;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Tests.Common;
using Microsoft.UI.Xaml.Tests.Enterprise;
using Microsoft.UI.Xaml.Media;
using System.Collections.Generic;
using System;
using Windows.Globalization;

#if HAS_UNO
using Uno.UI.Xaml.Controls.TestHooks;
#endif

namespace Uno.UI.RuntimeTests.MUX.Helpers;

internal static class DateTimePickerHelper
{
	// Performs the following steps:
	//  1. Trys to open the Date/TimePicker using the specified key sequence
	//  2. Verifies that the Date/TimePickerFlyout opened by checking that there is exactly 1 open popup
	//     (we do not have a way to get a reference to the Date/TimePickerFlyout itself)
	//  3. Changes the date/time by panning one of the LoopingSelectors.
	//  4. Closes the Date/TimePickerFlyout using the specified key sequence.
	//  5. Verifies that the Date/TimeChanged event fires
	//  6. Verifies that the Date/TimePickerFlyout is closed by checking that there are no open popups.
	// Assumes that the DatePicker/TimePicker is already focused when this method is called.
	internal static async Task OpenAndCloseDateTimePickerUsingKeyboard(
		string keySequenceToOpenDateTimePicker,
		string keySequenceToCloseDateTimePicker,
		Event dateTimeChangedEvent)
	{
		await TestServices.KeyboardHelper.PressKeySequence(keySequenceToOpenDateTimePicker);
		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>
		{
			//There should be exactly one open popup:
			var popups = VisualTreeHelper.GetOpenPopupsForXamlRoot(TestServices.WindowHelper.WindowContent.XamlRoot);
			VERIFY_IS_TRUE(popups.Count == 1);
		});

		dateTimeChangedEvent.Reset();
		await LoopingSelectorHelper.PanSingleDateTimeLoopingSelector();

		await TestServices.KeyboardHelper.PressKeySequence(keySequenceToCloseDateTimePicker);
		await TestServices.WindowHelper.WaitForIdle();

		LOG_OUTPUT("Waiting for DateChanged/TimeChanged event to fire.");
		await dateTimeChangedEvent.WaitForDefault();
		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>
		{
			//There should be no open popups:
			var popups = VisualTreeHelper.GetOpenPopupsForXamlRoot(TestServices.WindowHelper.WindowContent.XamlRoot);
			VERIFY_IS_TRUE(popups.Count == 0);
		});
		await TestServices.WindowHelper.WaitForIdle();
	}

#if HAS_UNO
	internal static async Task ValidateDateTimePickerFlyoutPositioningAndSizing<T>()
		where T : FrameworkElement, IDateTimePickerTestHooks, new()
	{
		WindowHelper.SetWindowSizeOverride(new Size(400, 600));

		T dateTimePicker = null;

		await RunOnUIThread(() =>
		{
			var rootPanel = new Grid();

			dateTimePicker = new();
			dateTimePicker.VerticalAlignment = VerticalAlignment.Center;
			dateTimePicker.HorizontalAlignment = HorizontalAlignment.Center;
			dateTimePicker.Header = "Header";

			rootPanel.Children.Add(dateTimePicker);
			WindowHelper.WindowContent = rootPanel;
		});
		await WindowHelper.WaitForIdle();

		await OpenDateTimePicker(dateTimePicker);

		await RunOnUIThread(async () =>
		{
			// The flyout should be the same width as the datepicker.
			var flyoutPresenter = FlyoutHelper.GetOpenFlyoutPresenter();
			VERIFY_ARE_EQUAL(flyoutPresenter.ActualWidth, dateTimePicker.ActualWidth);

			// We expect the HighlightRect to be centered vertically and horizontally over the button.
			var highlightRect = TreeHelper.GetVisualChildByName(flyoutPresenter, "HighlightRect");
			var highlightRectCenter = await ControlHelper.GetCenterOfElement(highlightRect);

			var button = TreeHelper.GetVisualChildByName(dateTimePicker, "FlyoutButton");
			var buttonCenter = await ControlHelper.GetCenterOfElement(button);

			VERIFY_ARE_EQUAL(highlightRectCenter.X, buttonCenter.X);
			VERIFY_ARE_EQUAL(highlightRectCenter.Y, buttonCenter.Y);
		});

		await ControlHelper.ClickFlyoutCloseButton(dateTimePicker, true /* isAccept */);
	}
#endif

	internal static async Task OpenDateTimePicker(FrameworkElement dateTimePicker)
	{
		Button button = default;

		await RunOnUIThread(() =>
		{
			button = TreeHelper.GetVisualChildByName(dateTimePicker, "FlyoutButton") as Button;
		});

		await ControlHelper.DoClickUsingAP(button);
		await TestServices.WindowHelper.WaitForIdle();
		await WindowHelper.WaitForIdle();
	}

	private static async Task<FlyoutPresenterType> GetOpenFlyoutPresenter<FlyoutPresenterType>()
		where FlyoutPresenterType : class
	{
		FlyoutPresenterType flyoutPresenter = null;
		await RunOnUIThread(() =>
		{
			var popups = VisualTreeHelper.GetOpenPopupsForXamlRoot(TestServices.WindowHelper.WindowContent.XamlRoot);
			VERIFY_IS_TRUE(popups.Count == 1);
			var popup = popups[0];
			flyoutPresenter = popup.Child as FlyoutPresenterType;
		});

		return flyoutPresenter;
	}

	internal static async Task<DatePickerFlyoutPresenter> GetOpenDatePickerFlyoutPresenter()
	{
		return await GetOpenFlyoutPresenter<DatePickerFlyoutPresenter>();
	}

	internal static async Task<TimePickerFlyoutPresenter> GetOpenTimePickerFlyoutPresenter()
	{
		return await GetOpenFlyoutPresenter<TimePickerFlyoutPresenter>();
	}

	internal static async Task<(LoopingSelector dayLoopingSelector, LoopingSelector monthLoopingSelector, LoopingSelector yearLoopingSelector)> GetDayMonthYearLoopingSelectorsFromOpenFlyout()
	{
		LoopingSelector dayLoopingResult = null;
		LoopingSelector monthLoopingResult = null;
		LoopingSelector yearLoopingResult = null;
		await RunOnUIThread(async () =>
		{
			var datePickerFlyoutPresenter = await GetOpenDatePickerFlyoutPresenter();
			THROW_IF_NULL(datePickerFlyoutPresenter);

			dayLoopingResult = TreeHelper.GetVisualChildByName(datePickerFlyoutPresenter, "DayLoopingSelector") as LoopingSelector;
			monthLoopingResult = TreeHelper.GetVisualChildByName(datePickerFlyoutPresenter, "MonthLoopingSelector") as LoopingSelector;
			yearLoopingResult = TreeHelper.GetVisualChildByName(datePickerFlyoutPresenter, "YearLoopingSelector") as LoopingSelector;
			THROW_IF_NULL(dayLoopingResult);
			THROW_IF_NULL(monthLoopingResult);
			THROW_IF_NULL(yearLoopingResult);
		});

		return (dayLoopingResult, monthLoopingResult, yearLoopingResult);
	}

	internal static async Task SelectDateInOpenDatePickerFlyout(Windows.Globalization.Calendar dateToSelect, int minYear, LoopingSelectorHelper.SelectionMode selectionMode)
	{
		(var dayLoopingSelector, var monthLoopingSelector, var yearLoopingSelector) = await GetDayMonthYearLoopingSelectorsFromOpenFlyout();

		int dayToSelectIndex = dateToSelect.Day - 1; // index is zero based.
		int monthToSelectIndex = dateToSelect.Month - 1; // index is zero based
		int yearToSelectIndex = dateToSelect.Year - minYear; // index is based on value of min year

		await LoopingSelectorHelper.SelectItemByIndex(dayLoopingSelector, dayToSelectIndex, selectionMode);
		await TestServices.WindowHelper.WaitForIdle();
		await LoopingSelectorHelper.SelectItemByIndex(monthLoopingSelector, monthToSelectIndex, selectionMode);
		await TestServices.WindowHelper.WaitForIdle();
		await LoopingSelectorHelper.SelectItemByIndex(yearLoopingSelector, yearToSelectIndex, selectionMode);
		await TestServices.WindowHelper.WaitForIdle();

		await ControlHelper.ClickFlyoutCloseButton(dayLoopingSelector, true /* isAccept */);
		await TestServices.WindowHelper.WaitForIdle();
	}

	internal static async Task<(LoopingSelector hourLoopingSelector, LoopingSelector minuteLoopingSelector, LoopingSelector periodLoopingSelector)> GetHourMinutePeriodLoopingSelectorsFromOpenFlyout()
	{
		LoopingSelector hourLoopingResult = null;
		LoopingSelector minuteLoopingResult = null;
		LoopingSelector periodLoopingResult = null;
		await RunOnUIThread(async () =>
		{
			var timePickerFlyoutPresenter = await GetOpenTimePickerFlyoutPresenter();
			THROW_IF_NULL(timePickerFlyoutPresenter);

			// TimePicker does not name its LoopingSelectors, so we cannot find them by name.
			// The best we can do is find them by type, and rely on their order in the tree to distinguish them.
			List<LoopingSelector> loopingSelectors = new();
			TreeHelper.GetVisualChildrenByType(timePickerFlyoutPresenter, ref loopingSelectors);
			Assert.HasCount(3, loopingSelectors, "Expected to find 3 LoopingSelectors");

			// Uno Specific: we use GetOrder as the order may vary
#if HAS_UNO
			timePickerFlyoutPresenter.GetOrder(out var hourOrder, out var minuteOrder, out var periodOrder, out _);
#else
			var (hourOrder, minuteOrder, periodOrder) = (0, 1, 2);
#endif
			hourLoopingResult = loopingSelectors[hourOrder];
			minuteLoopingResult = loopingSelectors[minuteOrder];
			periodLoopingResult = loopingSelectors[periodOrder];
		});

		return (hourLoopingResult, minuteLoopingResult, periodLoopingResult);
	}

	internal static async Task SelectTimeInOpenTimePickerFlyout(Windows.Globalization.Calendar timeToSelect, LoopingSelectorHelper.SelectionMode selectionMode)
	{
		(var hourLoopingSelector, var minuteLoopingSelector, var periodLoopingSelector) = await GetHourMinutePeriodLoopingSelectorsFromOpenFlyout();

		int hourIndexToSelect = timeToSelect.Hour;
		int minuteIndexToSelect = timeToSelect.Minute;
		int periodIndeexToSelect = timeToSelect.Period - 1; // AM = 1, PM = 2

		await LoopingSelectorHelper.SelectItemByIndex(hourLoopingSelector, hourIndexToSelect, selectionMode);
		await TestServices.WindowHelper.WaitForIdle();
		await LoopingSelectorHelper.SelectItemByIndex(minuteLoopingSelector, minuteIndexToSelect, selectionMode);
		await TestServices.WindowHelper.WaitForIdle();
		await LoopingSelectorHelper.SelectItemByIndex(periodLoopingSelector, periodIndeexToSelect, selectionMode);
		await TestServices.WindowHelper.WaitForIdle();

		await ControlHelper.ClickFlyoutCloseButton(hourLoopingSelector, true /* isAccept */);
		await TestServices.WindowHelper.WaitForIdle();
	}
}
