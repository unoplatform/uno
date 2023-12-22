using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Private.Infrastructure;
using Microsoft.UI.Xaml.Controls;
using MUXControlsTestApp.Utilities;
using Microsoft.UI.Xaml.Media;
using Uno.UI.RuntimeTests.MUX.Helpers;
using Windows.UI;
using Microsoft.UI.Xaml.Input;
using System.Reflection;

#if !HAS_UNO_WINUI
using Microsoft/* UWP don't rename */.UI.Xaml.Controls;
#endif

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

#if !WINAPPSDK
[TestClass]
[RunsOnUIThread]
public class Given_CalendarView
{
#if __ANDROID__ || __IOS__ || __MACOS__
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
}
#endif
