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

		Assert.IsGreaterThan(300, calendarView.ActualHeight);

		flyout.Close();
	}

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
			Assert.IsGreaterThan(0, items.Length);
			foreach (var item in items)
			{
				var foreground = ((SolidColorBrush)item.Foreground).Color;
				Assert.IsLessThan(0.5, foreground.Luminance);

				if (item.GetItemBackgroundBrush() is not { } backgroundBrush)
				{
					continue;
				}
				var background = (backgroundBrush as SolidColorBrush)?.Color ?? ((Microsoft.UI.Xaml.Media.RevealBackgroundBrush)backgroundBrush).Color;

				// Skip colored dates (selected), or those with opacity of zero.
				if (background.R == background.G && background.G == background.B && background.A != 0)
				{
					Assert.IsGreaterThan(0.5, background.Luminance);
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
			Assert.IsGreaterThan(0, items.Length);
			foreach (var item in items)
			{
				var foreground = ((SolidColorBrush)item.Foreground).Color;
				Assert.IsGreaterThan(0.5, foreground.Luminance);

				if (item.GetItemBackgroundBrush() is not { } backgroundBrush)
				{
					continue;
				}
				var background = (backgroundBrush as SolidColorBrush)?.Color ?? ((Microsoft.UI.Xaml.Media.RevealBackgroundBrush)backgroundBrush).Color;

				// Skip colored dates (selected), or those with opacity of zero.
				if (background.R == background.G && background.G == background.B && background.A != 0)
				{
					Assert.IsLessThan(0.5, background.Luminance);
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
