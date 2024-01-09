using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Tests.Common;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;
using Uno.UI.RuntimeTests.MUX.Helpers;
using Windows.Foundation;
using Windows.Globalization;
using static Private.Infrastructure.TestServices;

// TODO Uno: Missing ValidateUIElementTree, CanOpenAndCloseUsingKeyboard tests

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

[TestClass]
public class TimePickerIntegrationTests
{
	private static Calendar CreateTime(int hours, int minutes, int period = 1)
	{
		var time = new Calendar();
		time.Hour = hours;
		time.Minute = minutes;
		time.Period = period;
		time.Nanosecond = 0;
		time.Month = 1;
		time.Day = 1;
		time.Year = 2018;
		return time;
	}

	private static TimeSpan CreateTimeSpan(int hours, int minutes, int period = 1)
	{
		return CreateTimeSpan(hours, minutes, 0, period);
	}

	private static TimeSpan CalendarToTimeSpan(Calendar calendar)
	{
		return CreateTimeSpan(calendar.Hour, calendar.Minute, calendar.Second, calendar.Period);
	}

	private static bool AreClose(long x, long y, long tolerance)
	{
		var diff = x - y;
		return -tolerance < diff && diff < tolerance;
	}

	[TestMethod]
	[RunsOnUIThread]
	public void CanInstantiate()
	{
		var _ = new TimePicker();
	}

	[TestMethod]
	public async Task VerifyDefaultProperties()
	{
		TimePicker timePicker = null;

		await TestServices.RunOnUIThread(() =>
		{
			timePicker = new TimePicker();
			Assert.IsNotNull(timePicker);

			Assert.AreEqual(-1, timePicker.Time.Ticks);

			timePicker.Header = "TimePickerTest P0";

			TestServices.WindowHelper.WindowContent = timePicker;
		});

		await TestServices.WindowHelper.WaitForIdle();

		await TestServices.RunOnUIThread(() =>
		{
			Assert.AreEqual(ClockIdentifiers.TwelveHour, timePicker.ClockIdentifier);
			Assert.AreEqual(1, timePicker.MinuteIncrement);
		});

		timePicker = await SetupTimePickerTestAsync();
		await DateTimePickerHelper.OpenDateTimePicker(timePicker);
		await TestServices.WindowHelper.WaitForIdle();

#if !__ANDROID__ && !__IOS__
		await TestServices.RunOnUIThread(() =>
		{
			var timePickerFlyoutPresenter = TreeHelper.GetVisualChildByTypeFromOpenPopups<TimePickerFlyoutPresenter>(timePicker);
			Assert.IsNotNull(timePickerFlyoutPresenter);
			Assert.AreEqual(true, timePickerFlyoutPresenter.IsDefaultShadowEnabled);
		});
#endif
	}

	[TestMethod]
	public async Task CanFireTimeChangedEvent()
	{
		TimePicker timePicker = null;

		TimeSpan timeSpanOriginal = TimeSpan.Zero;
		TimeSpan timeSpanNew = TimeSpan.Zero;

		bool selectedTimeChangedEventFired = false;
		bool timeChangedEventFired = false;

		timePicker = await SetupTimePickerTestAsync();

		await RunOnUIThread(() =>
		{
			var selectedTimeChangedRegistration = CreateSafeEventRegistration<TimePicker, TypedEventHandler<TimePicker, TimePickerSelectedValueChangedEventArgs>>("SelectedTimeChanged");
			selectedTimeChangedRegistration.Attach(timePicker, (sender, args) =>
			{
				LOG_OUTPUT("CanFireTimeChangedEvent: TimePickerSelectedValueChanged event fired.");

				VERIFY_IS_NULL(args.OldTime);
				VERIFY_ARE_EQUAL(args.NewTime.Value.TotalMinutes, timeSpanNew.TotalMinutes);

				selectedTimeChangedEventFired = true;
			});

			LOG_OUTPUT("CanFireTimeChangedEvent: Execute time change from null to 2:15.");
			timeSpanOriginal = timeSpanNew = CreateTimeSpan(2, 15);
			timePicker.SelectedTime = new TimeSpan?(timeSpanOriginal);
			selectedTimeChangedRegistration.Detach();

			var timeChangedRegistration = CreateSafeEventRegistration<TimePicker, EventHandler<TimePickerValueChangedEventArgs>>("TimeChanged");
			timeChangedRegistration.Attach(timePicker, (sender, args) =>
			{
				LOG_OUTPUT("CanFireTimeChangedEvent: TimePickerValueChanged event fired.");

				VERIFY_ARE_EQUAL(args.OldTime.Value.TotalMinutes, timeSpanOriginal.TotalMinutes);
				VERIFY_ARE_EQUAL(args.NewTime.Value.TotalMinutes, timeSpanNew.TotalMinutes);

				timeChangedEventFired = true;
			});

			// Set the time to 4:30AM.
			timeSpanNew = CreateTimeSpan(4, 30);

			LOG_OUTPUT("CanFireTimeChangedEvent: Execute time change from 2:15 to 4:30.");
			timePicker.SelectedTime = new TimeSpan?(timeSpanNew);
			timeChangedRegistration.Detach();
		});

		await WindowHelper.WaitFor(() => selectedTimeChangedEventFired);
		await WindowHelper.WaitFor(() => timeChangedEventFired);
	}

	private async Task<TimePicker> SetupTimePickerTestAsync()
	{
		TimePicker timePicker = null;

		await TestServices.RunOnUIThread(() =>
		{
			var rootGrid = new Grid();
			TestServices.WindowHelper.WindowContent = rootGrid;

			timePicker = new TimePicker();
			timePicker.Header = "TimePickerTest";

			rootGrid.Children.Add(timePicker);
		});

		await TestServices.WindowHelper.WaitForLoaded(timePicker);

		await TestServices.WindowHelper.WaitForIdle();

		return timePicker;
	}

	[TestMethod]
	public async Task ValidateFootprint()
	{
		IDisposable fluentStylesDisposable = null;
		await RunOnUIThread(() => fluentStylesDisposable = StyleHelper.UseFluentStyles());
		try
		{

			TestServices.WindowHelper.SetWindowSizeOverride(new Size(500, 600));

			const double expectedTimePickerWidth = 242;
			const double expectedTimePickerWidth_WithWideHeader = 350;

			const double expectedTimePickerHeight = 30;
			const double expectedTimePickerHeight_WithHeader = 19 + 4 + expectedTimePickerHeight;

			TimePicker timePicker = null;
			TimePicker timePickerWithHeader = null;
			TimePicker timePickerWithWideHeader = null;
			TimePicker timePickerStretched = null;
			TimePicker timePicker24Hour = null;

			await RunOnUIThread(() =>
			{
				var rootPanel = (StackPanel)XamlReader.Load(
					@"<StackPanel xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"" xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"" >
                        <TimePicker x:Name=""timePicker"" />
                        <TimePicker x:Name=""timePickerWithHeader"" Header=""H"" />
                        <TimePicker x:Name=""timePickerWithWideHeader"" >
                            <TimePicker.Header>
                                <Rectangle Height=""19"" Width=""350"" Fill=""Red"" />
                            </TimePicker.Header>
                        </TimePicker>
                        <TimePicker x:Name=""timePickerStretched"" HorizontalAlignment=""Stretch"" />
                        <TimePicker x:Name=""timePicker24Hour"" ClockIdentifier=""24HourClock"" />
                    </StackPanel>");

				timePicker = (TimePicker)rootPanel.FindName("timePicker");
				timePickerWithHeader = (TimePicker)rootPanel.FindName("timePickerWithHeader");
				timePickerWithWideHeader = (TimePicker)rootPanel.FindName("timePickerWithWideHeader");
				timePickerStretched = (TimePicker)rootPanel.FindName("timePickerStretched");
				timePicker24Hour = (TimePicker)rootPanel.FindName("timePicker24Hour");

				TestServices.WindowHelper.WindowContent = rootPanel;
			});
			await TestServices.WindowHelper.WaitForIdle();
			await RunOnUIThread(() =>
			{
				// Verify Footprint of TimePicker:
				VERIFY_ARE_EQUAL(expectedTimePickerWidth, timePicker.ActualWidth);
				VERIFY_ARE_EQUAL(expectedTimePickerHeight, timePicker.ActualHeight);

				// Verify Footprint of TimePicker with Header:
				VERIFY_ARE_EQUAL(expectedTimePickerWidth, timePickerWithHeader.ActualWidth);
				VERIFY_ARE_EQUAL(expectedTimePickerHeight_WithHeader, timePickerWithHeader.ActualHeight);

				// Verify Footprint of TimePicker with wide Header:
				VERIFY_ARE_EQUAL(expectedTimePickerWidth_WithWideHeader, timePickerWithWideHeader.ActualWidth);
				VERIFY_ARE_EQUAL(expectedTimePickerHeight_WithHeader, timePickerWithWideHeader.ActualHeight);

				// Verify Footprint of TimePicker with 24Hour Clock:
				VERIFY_ARE_EQUAL(expectedTimePickerWidth, timePicker24Hour.ActualWidth);
				VERIFY_ARE_EQUAL(expectedTimePickerHeight, timePicker24Hour.ActualHeight);
			});
		}
		finally
		{
			if (fluentStylesDisposable is not null)
			{
				await RunOnUIThread(() => fluentStylesDisposable.Dispose());
			}
		}
	}

#if HAS_UNO
	[TestMethod]
	[RequiresFullWindow]
#if __ANDROID__ || __IOS__
	[Ignore("This is only relevant for managed implementation")]
#endif
	public async Task ValidateFlyoutPositioningAndSizing()
	{
		await DateTimePickerHelper.ValidateDateTimePickerFlyoutPositioningAndSizing<TimePicker>();
	}
#endif

	[TestMethod]
	public async Task HasPlaceholderTextByDefault()
	{
		var timePicker = await SetupTimePickerTestAsync();

		await RunOnUIThread(() =>
		{
			VERIFY_IS_NULL(timePicker.SelectedTime);
		});

		await VerifyHasPlaceholder(timePicker);
	}

	[TestMethod]
#if __ANDROID__ || __IOS__
	[Ignore("This is only relevant for managed implementation")]
#endif
	public async Task SelectingTimeSetsSelectedTime()
	{
		var timePicker = await SetupTimePickerTestAsync();
		var targetTime = CreateTime(4, 30, 2);
		targetTime.Second = 0;

		LOG_OUTPUT("Selecting 4:30 PM.");
		await DateTimePickerHelper.OpenDateTimePicker(timePicker);
		await TestServices.WindowHelper.WaitForIdle();

		await DateTimePickerHelper.SelectTimeInOpenTimePickerFlyout(targetTime, LoopingSelectorHelper.SelectionMode.Keyboard);
		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>
		{
			var targetTimeSpan = CalendarToTimeSpan(targetTime);

			LOG_OUTPUT("Time and SelectedTime should now refer to the same date.");
			VERIFY_ARE_EQUAL(targetTimeSpan, timePicker.Time);
			VERIFY_IS_NOT_NULL(timePicker.SelectedTime);
			VERIFY_ARE_EQUAL(targetTimeSpan, timePicker.SelectedTime.Value);
		});
	}

	[TestMethod]
	public async Task ValidateSelectedTimePropagatesToTime()
	{
		var timePicker = await SetupTimePickerTestAsync();
		var time = CreateTimeSpan(4, 30, 2);
		var time2 = CreateTimeSpan(5, 45, 1);

		await RunOnUIThread(() =>
		{
			LOG_OUTPUT("Setting SelectedTime to null. Time should be the null sentinel value.");
			timePicker.SelectedTime = null;

			VERIFY_ARE_EQUAL(-1, timePicker.Time.Ticks);

			LOG_OUTPUT("Setting SelectedTime to 5:45 AM. Time should change to this value.");
			timePicker.SelectedTime = time2;

			VERIFY_ARE_EQUAL(time2, timePicker.Time);
			VERIFY_IS_NOT_NULL(timePicker.SelectedTime);
			VERIFY_ARE_EQUAL(time2, timePicker.SelectedTime.Value);

			LOG_OUTPUT("Setting Time to February 4:30 PM. SelectedTime should change to this value.");
			timePicker.Time = time;

			VERIFY_ARE_EQUAL(time, timePicker.Time);
			VERIFY_IS_NOT_NULL(timePicker.SelectedTime);
			VERIFY_ARE_EQUAL(time, timePicker.SelectedTime.Value);

			LOG_OUTPUT("Setting Time to the null sentinel value. SelectedTime should revert to null.");
			TimeSpan nullTime = TimeSpan.FromTicks(-1);
			timePicker.Time = nullTime;

			VERIFY_IS_NULL(timePicker.SelectedTime);
		});
	}

	[TestMethod]
	public async Task CanProgrammaticallyClearSelectedTime()
	{
		var timePicker = await SetupTimePickerTestAsync();

		await VerifyHasPlaceholder(timePicker);

		await RunOnUIThread(() =>
		{
			timePicker.SelectedTime = CreateTimeSpan(4, 30, 2);
		});

		await VerifyDoesNotHavePlaceholder(timePicker);

		await RunOnUIThread(() =>
		{
			timePicker.SelectedTime = null;
		});

		await VerifyHasPlaceholder(timePicker);
	}

	[TestMethod]
#if __ANDROID__ || __IOS__
	[Ignore("This is only relevant for managed implementation")]
#endif
	public async Task ValidateMinuteIncrementProperty()
	{
		var timePicker = await SetupTimePickerTestAsync();

		var timeChangedEvent = false;
		var timeChangedRegistration = CreateSafeEventRegistration<TimePicker, EventHandler<TimePickerValueChangedEventArgs>>("TimeChanged");
		timeChangedRegistration.Attach(timePicker, (s, e) => timeChangedEvent = true);

		var initialTime = CreateTimeSpan(5, 12, 2); //5:12 PM
		var initialTimeRounded = CreateTimeSpan(5, 10, 2); //5:10 PM

		var timeToSet = CreateTimeSpan(7, 47, 1); //7:47 PM
		var timeToSetRounded = CreateTimeSpan(7, 45, 1); //7:45 PM

		await RunOnUIThread(() =>
		{
			timePicker.SelectedTime = initialTime;
		});
		await TestServices.WindowHelper.WaitForIdle();

		timeChangedEvent = false;
		await RunOnUIThread(() =>
		{
			timePicker.MinuteIncrement = 5;
			Assert.AreEqual(initialTimeRounded, timePicker.Time, "Setting TimePicker.MinuteIncrement should cause TimePicker.Time to get rounded");
		});

		// Setting MinuteIncrement should result in TimeChanged being raised:
		await WindowHelper.WaitFor(() => timeChangedEvent);

		// Setting Time should result in the value being rounded based on MinuteIncrement:
		await RunOnUIThread(() =>
		{
			timePicker.SelectedTime = timeToSet;
			Assert.AreEqual(timeToSetRounded, timePicker.Time, "TimePicker.Time should get rounded based on TimePicker.MinuteIncrement");
		});

		await DateTimePickerHelper.OpenDateTimePicker(timePicker);
		await TestServices.WindowHelper.WaitForIdle();

		// The TimePickerFlyout should generate Minute items based on MinuteIncrement:
		await RunOnUIThread(async () =>
		{
			(var hourLoopingSelector, var minuteLoopingSelector, var periodLoopingSelector) = await DateTimePickerHelper.GetHourMinutePeriodLoopingSelectorsFromOpenFlyout();

			var minuteItems = minuteLoopingSelector.Items;

			Assert.AreEqual(12, minuteItems.Count);
			for (int i = 0; i < minuteItems.Count; i++)
			{
				var dayItem = minuteItems[i] as DatePickerFlyoutItem;
				Assert.IsNotNull(dayItem);

				// The item's PrimaryText string should be the minute value as a 2 digit number (with a leading 0 as necessary).
				var minuteValue = i * 5;
				var expectedMinuteString = string.Format("{0:00}", minuteValue);
				Assert.AreEqual(expectedMinuteString, dayItem.PrimaryText);
			}
		});

		await ControlHelper.ClickFlyoutCloseButton(timePicker, true /* isAccept */);
		await TestServices.WindowHelper.WaitForIdle();
	}

	[TestMethod]
	public async Task ValidateClockIdentifierProperty()
	{
		var timePicker = await SetupTimePickerTestAsync();

		var initialTime = CreateTimeSpan(5, 15, 2); //5:15 PM

		await RunOnUIThread(() =>
		{
			timePicker.SelectedTime = new TimeSpan?(initialTime);
		});
		await TestServices.WindowHelper.WaitForIdle();

		TextBlock hourTextBlock = null;
		TextBlock minuteTextBlock = null;
		TextBlock periodTextBlock = null;
		await RunOnUIThread(() =>
		{
			hourTextBlock = (TextBlock)TreeHelper.GetVisualChildByName(timePicker, "HourTextBlock");
			minuteTextBlock = (TextBlock)TreeHelper.GetVisualChildByName(timePicker, "MinuteTextBlock");
			periodTextBlock = (TextBlock)TreeHelper.GetVisualChildByName(timePicker, "PeriodTextBlock");
			if (hourTextBlock == null || minuteTextBlock == null || periodTextBlock == null)
			{
				throw new Exception("Failed to find required TextBlock elements");
			}

			Assert.AreEqual("5", hourTextBlock.Text);
			Assert.AreEqual("15", minuteTextBlock.Text);
			Assert.AreEqual("PM", periodTextBlock.Text);

			timePicker.ClockIdentifier = ClockIdentifiers.TwentyFourHour;
		});
		await TestServices.WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>
		{
			// periodTextBlock should get removed from the tree when using a 24Hour clock.
			Assert.IsNull(periodTextBlock.Parent);

			Assert.AreEqual("17", hourTextBlock.Text);
			Assert.AreEqual("15", minuteTextBlock.Text);
		});
	}

	//private Button GetFlyoutButtonFromTimePicker(TimePicker timePicker)
	//{
	//	var templateRoot = VisualTreeHelper.GetChild(timePicker, 0) as FrameworkElement;
	//	var flyoutButton = templateRoot?.FindName("FlyoutButton") as Button;
	//	return flyoutButton;
	//}

	private static TimeSpan CreateTimeSpan(int hours, int minutes, int seconds, int period)
	{
		if (period != 1 && period != 2)
		{
			throw new InvalidOperationException("period must be 1 (AM) or 2 (PM)");
		}
		TimeSpan timeSpan = new TimeSpan();

		// Conver to 24 hours
		if (hours == 12)
		{
			if (period == 1)
			{
				hours = 0;
			}
			else
			{
				hours = 12;
			}
		}
		else if (period == 2)
		{
			hours += 12;
		}
		timeSpan = TimeSpan.FromSeconds((hours * 60 + minutes) * 60 + seconds);

		LOG_OUTPUT("CreateTimeSpan period=%d hours=%d minutes=%d seconds=%d timeDuration=%llu", period, hours, minutes, seconds, timeSpan.Ticks);

		return timeSpan;
	}

	private async Task VerifyHasPlaceholder(TimePicker timePicker)
	{
		await RunOnUIThread(() =>
		{
			var hourTextBlock = (TextBlock)(TreeHelper.GetVisualChildByName(timePicker, "HourTextBlock"));
			var minuteTextBlock = (TextBlock)(TreeHelper.GetVisualChildByName(timePicker, "MinuteTextBlock"));
			var periodTextBlock = (TextBlock)(TreeHelper.GetVisualChildByName(timePicker, "PeriodTextBlock"));

			void validatePlaceholder(TextBlock textBlock, string placeholder)
			{
				LOG_OUTPUT("Expected placeholder: \"%s\"", placeholder);
				LOG_OUTPUT("Actual text: \"%s\"", textBlock.Text);

				VERIFY_IS_TRUE(string.CompareOrdinal(placeholder, textBlock.Text) == 0);
			};

			validatePlaceholder(hourTextBlock, "hour");
			validatePlaceholder(minuteTextBlock, "minute");
			validatePlaceholder(periodTextBlock, "AM");
		});
	}

	private async Task VerifyDoesNotHavePlaceholder(TimePicker timePicker)
	{
		await RunOnUIThread(() =>
		{
			var hourTextBlock = TreeHelper.GetVisualChildByName(timePicker, "HourTextBlock") as TextBlock;
			var minuteTextBlock = TreeHelper.GetVisualChildByName(timePicker, "MinuteTextBlock") as TextBlock;
			var periodTextBlock = TreeHelper.GetVisualChildByName(timePicker, "PeriodTextBlock") as TextBlock;

			void validateValue(TextBlock textBlock, string placeholder)
			{
				LOG_OUTPUT("Placeholder: \"%s\"", placeholder);
				LOG_OUTPUT("Actual text: \"%s\"", textBlock.Text);

				VERIFY_IS_TRUE(string.CompareOrdinal(placeholder, textBlock.Text) != 0);
			};

			validateValue(hourTextBlock, "hour");
			validateValue(minuteTextBlock, "minute");
			validateValue(periodTextBlock, "AM");
		});
	}

	//	private void SetTime(TimePicker timePicker, TimeSpan time)
	//	{
	//#if WI_IS_FEATURE_PRESENTFeature_DateTimePickerNullVisualization
	//        timePicker.SelectedTime = new Platform.Box<wf.TimeSpan>(time);
	//#else
	//		timePicker.Time = time;
	//#endif
	//	}
}
