using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MUXControlsTestApp.Utilities;
using Uno.UI.RuntimeTests.Helpers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

[TestClass]
[RunsOnUIThread]
public class Given_CalendarDatePicker
{
#if !WINAPPSDK && !__MACOS__ // test is failling in macOS for some reason.
	[TestMethod]
	public async Task TestCalendarPanelSize()
	{
		var SUT = new CalendarDatePicker();
		WindowHelper.WindowContent = SUT;
		await WindowHelper.WaitForIdle();

		var root = (Grid)SUT.FindName("Root");
		var flyout = (Flyout)FlyoutBase.GetAttachedFlyout(root);
		flyout.XamlRoot = SUT.XamlRoot;
		flyout.Open();

		await WindowHelper.WaitForIdle();
		var calendarView = (CalendarView)flyout.Content;

		Assert.IsTrue(calendarView.ActualHeight > 300);

		flyout.Close();
	}
#endif

#if !WINAPPSDK && !__MACOS__
	[TestMethod]
	public async Task When_Theme_Changes()
	{
		CalendarDatePicker datePicker = new CalendarDatePicker();
		WindowHelper.WindowContent = datePicker;

		await WindowHelper.WaitForLoaded(datePicker);

		//Open flyout
		var root = (Grid)datePicker.FindName("Root");
		var flyout = (Flyout)FlyoutBase.GetAttachedFlyout(root);

		try
		{
			bool opened = false;
			flyout.Opened += (_, _) =>
			{
				opened = true;
			};

			datePicker.IsCalendarOpen = true;

			await WindowHelper.WaitFor(() => opened);

			var items = VisualTreeUtils.FindVisualChildrenByType<CalendarViewDayItem>(flyout.Content).ToArray();
			Assert.IsTrue(items.Length > 0);
			foreach (var item in items)
			{
				var foreground = ((SolidColorBrush)item.Foreground).Color;
				Assert.IsTrue(foreground.Luminance < 0.5);

				if (item.GetItemBackgroundBrush() is not { } backgroundBrush)
				{
					continue;
				}
				var background = (backgroundBrush as SolidColorBrush)?.Color ?? ((RevealBackgroundBrush)backgroundBrush).Color;

				// Skip colored dates (selected), or those with opacity of zero.
				if (background.R == background.G && background.G == background.B && background.A != 0)
				{
					Assert.IsTrue(background.Luminance > 0.5);
				}
			}

			//Close flyout
			datePicker.IsCalendarOpen = false;

			await WindowHelper.WaitForIdle();

			//Change to dark theme
			using var _ = ThemeHelper.UseDarkTheme();

			opened = false;
			//Open flyout
			datePicker.IsCalendarOpen = true;

			await WindowHelper.WaitFor(() => opened);

			items = VisualTreeUtils.FindVisualChildrenByType<CalendarViewDayItem>(flyout.Content).ToArray();
			Assert.IsTrue(items.Length > 0);
			foreach (var item in items)
			{
				var foreground = ((SolidColorBrush)item.Foreground).Color;
				Assert.IsTrue(foreground.Luminance > 0.5);

				if (item.GetItemBackgroundBrush() is not { } backgroundBrush)
				{
					continue;
				}
				var background = (backgroundBrush as SolidColorBrush)?.Color ?? ((RevealBackgroundBrush)backgroundBrush).Color;

				// Skip colored dates (selected), or those with opacity of zero.
				if (background.R == background.G && background.G == background.B && background.A != 0)
				{
					Assert.IsTrue(background.Luminance < 0.5);
				}
			}
		}
		finally
		{
			if (datePicker is not null)
			{
				datePicker.IsCalendarOpen = false;
			}
		}
	}
#endif
}
