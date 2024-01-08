using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using MUXControlsTestApp.Utilities;
using Private.Infrastructure;
using Microsoft.UI.Xaml;
using Windows.Foundation;
using Uno.UI.UI.Xaml.Controls.TestHooks;
using static Private.Infrastructure.TestServices;
using System.Globalization;

namespace Uno.UI.RuntimeTests.MUX.Helpers;

internal static class DateTimePickerHelper
{
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

	internal static async Task OpenDateTimePicker(FrameworkElement dateTimePicker)
	{
		Button button = default;

		await RunOnUIThread(() =>
		{
			button = TreeHelper.GetVisualChildByName(dateTimePicker, "FlyoutButton") as Button;
		});

		await ControlHelper.DoClickUsingTap(button);
		await WindowHelper.WaitForIdle();
	}
	
	internal static void SelectTimeInOpenTimePickerFlyout(Calendar timeToSelect, LoopingSelectorHelper.SelectionMode selectionMode)
	{
		xaml_primitives::LoopingSelector ^ hourLoopingSelector;
		xaml_primitives::LoopingSelector ^ minuteLoopingSelector;
		xaml_primitives::LoopingSelector ^ periodLoopingSelector;
		GetHourMinutePeriodLoopingSelectorsFromOpenFlyout(hourLoopingSelector, minuteLoopingSelector, periodLoopingSelector);

		int hourIndexToSelect = timeToSelect->Hour;
		int minuteIndexToSelect = timeToSelect->Minute;
		int periodIndeexToSelect = timeToSelect->Period - 1; // AM = 1, PM = 2

		LoopingSelectorHelper::SelectItemByIndex(hourLoopingSelector, hourIndexToSelect, selectionMode);
		LoopingSelectorHelper::SelectItemByIndex(minuteLoopingSelector, minuteIndexToSelect, selectionMode);
		LoopingSelectorHelper::SelectItemByIndex(periodLoopingSelector, periodIndeexToSelect, selectionMode);

		ControlHelper::ClickFlyoutCloseButton(hourLoopingSelector, true /* isAccept */);
		TestServices::WindowHelper->WaitForIdle();
	}
}
