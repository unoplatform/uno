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
using Private.Infrastructure;
using Uno.UI.RuntimeTests.MUX.Helpers;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

#if !WINAPPSDK
[TestClass]
[RunsOnUIThread]
public class Given_CalendarDatePicker
{
#if __MACOS__
	[Ignore("test is failling in macOS for some reason.")]
#endif
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

#if __MACOS__
	[Ignore]
#endif
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
				var background = (backgroundBrush as SolidColorBrush)?.Color ?? ((Microsoft.UI.Xaml.Media.RevealBackgroundBrush)backgroundBrush).Color;

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
				var background = (backgroundBrush as SolidColorBrush)?.Color ?? ((Microsoft.UI.Xaml.Media.RevealBackgroundBrush)backgroundBrush).Color;

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

#if HAS_UNO
	[TestMethod]
	public async Task When_Default_Flyout_Date()
	{
		var now = DateTimeOffset.UtcNow;
		var datePicker = new Microsoft.UI.Xaml.Controls.CalendarDatePicker();

		TestServices.WindowHelper.WindowContent = datePicker;

		await TestServices.WindowHelper.WaitForLoaded(datePicker);

		datePicker.IsCalendarOpen = true;

		await WindowHelper.WaitFor(() => VisualTreeHelper.GetOpenPopupsForXamlRoot(datePicker.XamlRoot).Count > 0);
		var popup = VisualTreeHelper.GetOpenPopupsForXamlRoot(datePicker.XamlRoot).First();
		var child = (FlyoutPresenter)popup.Child;
		var calendarView = (CalendarView)child.Content;
		Assert.AreEqual(now.Day, calendarView.m_lastDisplayedDate.Day);
		Assert.AreEqual(now.Month, calendarView.m_lastDisplayedDate.Month);
		Assert.AreEqual(now.Year, calendarView.m_lastDisplayedDate.Year);
	}

	[TestMethod]
	public async Task When_Default_Date_Displayed()
	{
		var now = DateTimeOffset.UtcNow;
		var datePicker = new Microsoft.UI.Xaml.Controls.CalendarDatePicker();

		TestServices.WindowHelper.WindowContent = datePicker;

		await TestServices.WindowHelper.WaitForLoaded(datePicker);

		datePicker.IsCalendarOpen = true;

		await WindowHelper.WaitFor(() => VisualTreeHelper.GetOpenPopupsForXamlRoot(datePicker.XamlRoot).Count > 0);
		var popup = VisualTreeHelper.GetOpenPopupsForXamlRoot(datePicker.XamlRoot).First();
		var child = (FlyoutPresenter)popup.Child;
		var calendarView = (CalendarView)child.Content;

		await TestServices.WindowHelper.WaitFor(() => calendarView.ActualHeight > 0);

		Assert.IsTrue(calendarView.TemplateSettings.HeaderText.EndsWith(now.Year.ToString(), StringComparison.Ordinal));
	}
#endif
}
#endif
