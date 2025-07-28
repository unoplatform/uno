using System;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Input;
using MUXControlsTestApp.Utilities;
using Private.Infrastructure;
using Uno.UI.Extensions;
using Uno.UI.RuntimeTests.Helpers;
using System.Linq;
using SamplesApp.UITests;
using Windows.Foundation;
using static Private.Infrastructure.TestServices;

#if HAS_UNO && !HAS_UNO_WINUI
using Microsoft.UI.Xaml.Controls;
#endif

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

#if !WINAPPSDK
[TestClass]
[RunsOnUIThread]
public class Given_CalendarView
{
	const int DEFAULT_MIN_MAX_DATE_YEAR_OFFSET = 100;

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/16123")]
	[Ignore("Test is unstable on CI: https://github.com/unoplatform/uno/issues/16123")]
	public async Task When_ReMeasure_After_Changing_MaxDate()
	{
		var contentDialog = new ContentDialog();
		contentDialog.XamlRoot = TestServices.WindowHelper.XamlRoot;
		var calendarView = new CalendarView();
		contentDialog.Content = calendarView;

		// Set MaxDate, show dialog, screenshot it, and hide it.
		calendarView.MaxDate = DateTimeOffset.Now.AddDays(1);
		var task = contentDialog.ShowAsync();
		await TestServices.WindowHelper.WaitForIdle();
		var screenshot1 = await UITestHelper.ScreenShot(calendarView);
		task.Cancel();

		// Change MaxDate, show dialog, screenshot it, and hide it.
		calendarView.MaxDate = DateTimeOffset.Now.AddDays(2);
		task = contentDialog.ShowAsync();
		await TestServices.WindowHelper.WaitForIdle();
		var screenshot2 = await UITestHelper.ScreenShot(calendarView);
		task.Cancel();

		await ImageAssert.AreEqualAsync(screenshot1, screenshot2);
	}

	[TestMethod]
	public async Task When_MinDate_Has_Different_Offset()
	{
		var calendarView = new CalendarView();
		calendarView.MinDate = new DateTimeOffset(new DateTime(2010, 1, 1, 22, 0, 0), TimeSpan.Zero);
		calendarView.MaxDate = new DateTimeOffset(new DateTime(2010, 1, 31), TimeSpan.FromHours(2));

		await UITestHelper.Load(calendarView);
	}

	[TestMethod]
	public async Task When_Scroll_To_MaxDate()
	{
		var calendarView = new CalendarView()
		{
			DisplayMode = CalendarViewDisplayMode.Decade
		};

		await UITestHelper.Load(calendarView);

		Type calendarViewType = typeof(CalendarView);
		MethodInfo ChangeVisualStateInfo = calendarViewType.GetMethod("ChangeVisualState", BindingFlags.NonPublic | BindingFlags.Instance);

		// Scroll to max date
		calendarView.SetDisplayDate(calendarView.MaxDate);

		// Switch to Year view
		calendarView.DisplayMode = CalendarViewDisplayMode.Year;
		ChangeVisualStateInfo.Invoke(calendarView, new object[] { false });
		await TestServices.WindowHelper.WaitForIdle();

		// Switch back to Decade view
		calendarView.DisplayMode = CalendarViewDisplayMode.Decade;
		ChangeVisualStateInfo.Invoke(calendarView, new object[] { false });
		await TestServices.WindowHelper.WaitForIdle();

		// Decade viewport should be full of items (no missing row)
		calendarView.GetActiveGeneratorHost(out var pHost);
		var maxDecadeIndex = DEFAULT_MIN_MAX_DATE_YEAR_OFFSET * 2;
		var maxDisplayedItems = pHost.Panel.Rows * pHost.Panel.Cols;

		// The first visible index should be less than the max possible index minus the max items we can display
		// Worst case scenario is that the last row only has 1 item
		Assert.IsTrue(pHost.Panel.FirstVisibleIndex <= maxDecadeIndex - (maxDisplayedItems - pHost.Panel.Rows - 1));
	}

#if __WASM__
	[TestMethod]
	[Ignore("Fails on Fluent styles #17272")]
	public async Task When_ItemCornerRadius()
	{
		var calendarView = new CalendarView();
		calendarView.OutOfScopeBackground = new SolidColorBrush(Microsoft.UI.Colors.Red);
		calendarView.CalendarItemCornerRadius = new CornerRadius(40);
		calendarView.DayItemCornerRadius = new CornerRadius(20);

		await UITestHelper.Load(calendarView);
		await TestServices.WindowHelper.WaitForIdle();

		var hasOutOfScope = false;

		foreach (var dayItem in calendarView.EnumerateDescendants().OfType<CalendarViewDayItem>())
		{
			var color = Uno.Foundation.WebAssemblyRuntime.InvokeJS($"document.getElementById({dayItem.HtmlId}).style[\"background-color\"]");
			if (color == "rgb(255, 0, 0)")
			{
				hasOutOfScope = true;
				var result = Uno.Foundation.WebAssemblyRuntime.InvokeJS($"document.getElementById({dayItem.HtmlId}).style[\"border-radius\"]");
				Assert.AreEqual("21px", result);
			}
		}

		Assert.IsTrue(hasOutOfScope);
	}
#endif
#if __ANDROID__ || __APPLE_UIKIT__
	[Ignore("Test fails on these platforms")]
#endif
	[TestMethod]
	public async Task SelectedDatesBorder()
	{
		DateTimeOffset day1 = new DateTimeOffset(DateTime.Now.AddDays(-3));
		DateTimeOffset day2 = new DateTimeOffset(DateTime.Now.AddDays(4));
		Type type = typeof(CalendarViewBaseItem);
		MethodInfo GetItemBorderBrushInfo = type.GetMethod("GetItemBorderBrush", BindingFlags.NonPublic | BindingFlags.Instance);
		Type dayItemType = typeof(CalendarViewDayItem);
		MethodInfo OnTappedInfo = dayItemType.GetMethod("OnTapped", BindingFlags.NonPublic | BindingFlags.Instance);
		CalendarViewDayItem dayItem1, dayItem2;
		Brush brush1, brush2;
		//Single Mode
		//Init SelectedDates as day1. { } => { day1 }
		CalendarView calendar = new CalendarView
		{
			SelectedDates = { day1 },
			SelectionMode = CalendarViewSelectionMode.Single,
			MinDate = DateTimeOffset.Now.AddDays(-10),
			MaxDate = DateTimeOffset.Now.AddDays(10)
		};
		TestServices.WindowHelper.WindowContent = calendar;
		await TestServices.WindowHelper.WaitForIdle();
		Assert.AreEqual(1, calendar.SelectedDates.Count);
		Assert.AreEqual(day1, calendar.SelectedDates[0]);
		dayItem1 = MUXTestPage.FindVisualChildrenByType<CalendarViewDayItem>(calendar).Find(it => it.Date.Date == calendar.SelectedDates[0].Date);
		Assert.IsNotNull(dayItem1);
		brush1 = (Brush)GetItemBorderBrushInfo.Invoke(dayItem1, new object[] { false });
		Assert.AreEqual(calendar.SelectedBorderBrush, brush1);

		//Click day1. { day1 } => { }
		OnTappedInfo.Invoke(dayItem1, new object[] { new TappedRoutedEventArgs() });
		await TestServices.WindowHelper.WaitForIdle();
		brush1 = (Brush)GetItemBorderBrushInfo.Invoke(dayItem1, new object[] { false });
		Assert.AreEqual(0, calendar.SelectedDates.Count);
		Assert.AreEqual(calendar.CalendarItemBorderBrush, brush1);

		//Add day2 to SelectedDatesItem. { } => { day2 }
		calendar.SelectedDates.Add(day2);
		Assert.AreEqual(1, calendar.SelectedDates.Count);
		Assert.AreEqual(day2, calendar.SelectedDates[0]);
		await TestServices.WindowHelper.WaitForIdle();
		dayItem2 = MUXTestPage.FindVisualChildrenByType<CalendarViewDayItem>(calendar).Find(it => it.Date.Date == calendar.SelectedDates[0].Date);
		Assert.IsNotNull(dayItem2);
		brush2 = (Brush)GetItemBorderBrushInfo.Invoke(dayItem2, new object[] { false });
		Assert.AreEqual(calendar.SelectedBorderBrush, brush2);

		//Click day1. { day2 } => { day1 }
		OnTappedInfo.Invoke(dayItem1, new object[] { new TappedRoutedEventArgs() });
		await TestServices.WindowHelper.WaitForIdle();
		Assert.AreEqual(1, calendar.SelectedDates.Count);
		Assert.AreEqual(dayItem1.Date, calendar.SelectedDates[0]);
		brush1 = (Brush)GetItemBorderBrushInfo.Invoke(dayItem1, new object[] { false });
		Assert.AreEqual(calendar.SelectedBorderBrush, brush1);
		brush2 = (Brush)GetItemBorderBrushInfo.Invoke(dayItem2, new object[] { false });
		Assert.AreEqual(calendar.CalendarItemBorderBrush, brush2);


		//MultipleMode
		//Init SelectedDates with multiple dates. { } => { day1, day2 }
		calendar = new CalendarView
		{
			SelectionMode = CalendarViewSelectionMode.Multiple,
			SelectedDates = { day1, day2 },
			MinDate = DateTimeOffset.Now.AddDays(-10),
			MaxDate = DateTimeOffset.Now.AddDays(10)
		};
		Assert.AreEqual(2, calendar.SelectedDates.Count);
		Assert.AreEqual(day1, calendar.SelectedDates[0]);
		Assert.AreEqual(day2, calendar.SelectedDates[1]);
		TestServices.WindowHelper.WindowContent = calendar;
		await TestServices.WindowHelper.WaitForIdle();
		dayItem1 = MUXTestPage.FindVisualChildrenByType<CalendarViewDayItem>(calendar).Find(it => it.Date.Date == calendar.SelectedDates[0].Date);
		Assert.IsNotNull(dayItem1);
		dayItem2 = MUXTestPage.FindVisualChildrenByType<CalendarViewDayItem>(calendar).Find(it => it.Date.Date == calendar.SelectedDates[1].Date);
		Assert.IsNotNull(dayItem2);
		await TestServices.WindowHelper.WaitForIdle();
		brush1 = (Brush)GetItemBorderBrushInfo.Invoke(dayItem1, new object[] { false });
		Assert.AreEqual(calendar.SelectedBorderBrush, brush1);
		brush2 = (Brush)GetItemBorderBrushInfo.Invoke(dayItem2, new object[] { false });
		Assert.AreEqual(calendar.SelectedBorderBrush, brush2);

		//Click day1. { day1, day2 } => { day2 }
		OnTappedInfo.Invoke(dayItem1, new object[] { new TappedRoutedEventArgs() });
		await TestServices.WindowHelper.WaitForIdle();
		Assert.AreEqual(1, calendar.SelectedDates.Count);
		Assert.AreEqual(day2, calendar.SelectedDates[0]);
		brush1 = (Brush)GetItemBorderBrushInfo.Invoke(dayItem1, new object[] { false });
		Assert.AreEqual(calendar.CalendarItemBorderBrush, brush1);
		brush2 = (Brush)GetItemBorderBrushInfo.Invoke(dayItem2, new object[] { false });
		Assert.AreEqual(calendar.SelectedBorderBrush, brush2);

		//Click day2. { day2 } => { }
		OnTappedInfo.Invoke(dayItem2, new object[] { new TappedRoutedEventArgs() });
		await TestServices.WindowHelper.WaitForIdle();
		Assert.AreEqual(0, calendar.SelectedDates.Count);
		brush1 = (Brush)GetItemBorderBrushInfo.Invoke(dayItem1, new object[] { false });
		Assert.AreEqual(calendar.CalendarItemBorderBrush, brush1);
		brush2 = (Brush)GetItemBorderBrushInfo.Invoke(dayItem2, new object[] { false });
		Assert.AreEqual(calendar.CalendarItemBorderBrush, brush2);

		//Click day1. { } => { day1 }
		OnTappedInfo.Invoke(dayItem1, new object[] { new TappedRoutedEventArgs() });
		await TestServices.WindowHelper.WaitForIdle();
		Assert.AreEqual(1, calendar.SelectedDates.Count);
		Assert.AreEqual(dayItem1.Date, calendar.SelectedDates[0]);
		brush1 = (Brush)GetItemBorderBrushInfo.Invoke(dayItem1, new object[] { false });
		Assert.AreEqual(calendar.SelectedBorderBrush, brush1);
		brush2 = (Brush)GetItemBorderBrushInfo.Invoke(dayItem2, new object[] { false });
		Assert.AreEqual(calendar.CalendarItemBorderBrush, brush2);

		//Click day2. { day1 } => { day1, day2 }
		OnTappedInfo.Invoke(dayItem2, new object[] { new TappedRoutedEventArgs() });
		await TestServices.WindowHelper.WaitForIdle();
		Assert.AreEqual(2, calendar.SelectedDates.Count);
		Assert.AreEqual(dayItem1.Date, calendar.SelectedDates[0]);
		Assert.AreEqual(dayItem2.Date, calendar.SelectedDates[1]);
		brush1 = (Brush)GetItemBorderBrushInfo.Invoke(dayItem1, new object[] { false });
		Assert.AreEqual(calendar.SelectedBorderBrush, brush1);
		brush2 = (Brush)GetItemBorderBrushInfo.Invoke(dayItem2, new object[] { false });
		Assert.AreEqual(calendar.SelectedBorderBrush, brush2);
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeIOS)] // Test is flaky on iOS native because the calendar takes longer to fully #9080.
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/20575")]
	public async Task When_Year_Mode_Shown()
	{
		var now = DateTimeOffset.UtcNow;
		var calendarView = new Microsoft.UI.Xaml.Controls.CalendarView();

		TestServices.WindowHelper.WindowContent = calendarView;

		await TestServices.WindowHelper.WaitForLoaded(calendarView);

		calendarView.DisplayMode = CalendarViewDisplayMode.Year;

		await TestServices.WindowHelper.WaitForIdle();

		Assert.IsTrue(calendarView.TemplateSettings.HeaderText.EndsWith(now.Year.ToString(), StringComparison.Ordinal));
	}
}
#endif
