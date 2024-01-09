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
			var flyoutPresenter = FlyoutHelper.GetOpenFlyoutPresenter(WindowHelper.XamlRoot);
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

	internal static async Task<TimePickerFlyoutPresenter> GetOpenTimePickerFlyoutPresenter()
	{
		return await GetOpenFlyoutPresenter<TimePickerFlyoutPresenter>();
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
			Assert.AreEqual(3, loopingSelectors.Count, "Expected to find 3 LoopingSelectors");
			hourLoopingResult = loopingSelectors[0];
			minuteLoopingResult = loopingSelectors[1];
			periodLoopingResult = loopingSelectors[2];
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
		await LoopingSelectorHelper.SelectItemByIndex(minuteLoopingSelector, minuteIndexToSelect, selectionMode);
		await LoopingSelectorHelper.SelectItemByIndex(periodLoopingSelector, periodIndeexToSelect, selectionMode);

		await ControlHelper.ClickFlyoutCloseButton(hourLoopingSelector, true /* isAccept */);
		await TestServices.WindowHelper.WaitForIdle();
	}
}
