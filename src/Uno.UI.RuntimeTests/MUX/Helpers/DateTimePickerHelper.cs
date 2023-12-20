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
}
