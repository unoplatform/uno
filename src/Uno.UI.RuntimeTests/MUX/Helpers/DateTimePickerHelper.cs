using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using MUXControlsTestApp.Utilities;
using Private.Infrastructure;
using Windows.UI.Xaml;
using Windows.Foundation;
using Uno.UI.UI.Xaml.Controls.TestHooks;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.MUX.Helpers;

internal static class DateTimePickerHelper
{
	internal static async Task ValidateDateTimePickerFlyoutPositioningAndSizing<T>()
		where T : FrameworkElement, IDateTimePickerTestHooks, new()
	{
		TestServices.WindowHelper.SetWindowSizeOverride(new Size(400, 600));

		T dateTimePicker = null;

		await TestServices.RunOnUIThread(() =>
		{
			var rootPanel = new Grid();

			dateTimePicker = new();
			dateTimePicker.VerticalAlignment = VerticalAlignment.Center;
			dateTimePicker.HorizontalAlignment = HorizontalAlignment.Center;
			dateTimePicker.Header = "Header";

			rootPanel.Children.Add(dateTimePicker);
			TestServices.WindowHelper.WindowContent = rootPanel;
		});
		await TestServices.WindowHelper.WaitForIdle();

		await OpenDateTimePicker(dateTimePicker);

		await TestServices.RunOnUIThread(() =>
		{
			// The flyout should be the same width as the datepicker.
			var flyoutPresenter = FlyoutHelper.GetOpenFlyoutPresenter(TestServices.WindowHelper.XamlRoot);
			VERIFY_ARE_EQUAL(flyoutPresenter.ActualWidth, dateTimePicker.ActualWidth);

			// We expect the HighlightRect to be centered vertically and horizontally over the button.
			var highlightRect = TreeHelper.GetVisualChildByName(flyoutPresenter, "HighlightRect");
			var highlightRectCenter = ControlHelper.GetCenterOfElement(highlightRect);

			var button = TreeHelper.GetVisualChildByName(dateTimePicker, "FlyoutButton");
			var buttonCenter = ControlHelper.GetCenterOfElement(button);

			VERIFY_ARE_EQUAL(highlightRectCenter.X, buttonCenter.X);
			VERIFY_ARE_EQUAL(highlightRectCenter.Y, buttonCenter.Y);
		});

		await ControlHelper.ClickFlyoutCloseButton(dateTimePicker, true /* isAccept */);
	}

	internal static async Task OpenDateTimePicker(FrameworkElement dateTimePicker)
	{
		Button button = default;

		await RunOnUIThread.ExecuteAsync(() =>
		{
			button = TreeHelper.GetVisualChildByName(dateTimePicker, "FlyoutButton") as Button;
		});

		await ControlHelper.DoClickUsingTap(button);
		await TestServices.WindowHelper.WaitForIdle();
	}
}
