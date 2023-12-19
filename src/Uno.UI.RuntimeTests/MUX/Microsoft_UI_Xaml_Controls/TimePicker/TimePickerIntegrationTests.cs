using System;
using System.Threading.Tasks;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.MUX.Helpers;
using Windows.Foundation;
using Windows.Globalization;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Markup;
using static Private.Infrastructure.TestServices;

// TODO Uno: Missing ValidateUIElementTree, CanOpenAndCloseUsingKeyboard tests

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

[TestClass]
public class TimePickerIntegrationTests
{
	[TestMethod]
	public async Task When_Default_Properties()
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

		await TestServices.RunOnUIThread(() =>
		{
			var timePickerFlyoutPresenter = TreeHelper.GetVisualChildByTypeFromOpenPopups<TimePickerFlyoutPresenter>(timePicker);
			Assert.IsNotNull(timePickerFlyoutPresenter);
			Assert.AreEqual(true, timePickerFlyoutPresenter.IsDefaultShadowEnabled);
		});
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

			var timeChangedRegistration = CreateSafeEventRegistration<TimePicker, TypedEventHandler<TimePicker, TimePickerValueChangedEventArgs>>("TimeChanged");
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

	[TestMethod]
	public async Task ValidateFlyoutPositioningAndSizing()
	{
		DateTimePickerHelper.ValidateDateTimePickerFlyoutPositioningAndSizing<TimePicker>();
	}

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
	public async Task SelectingTimeSetsSelectedTimeAsync()
	{
		var timePicker = await SetupTimePickerTestAsync();
		var targetTime = CreateTimeSpan(4, 30, 2);
		targetTime.Second = 0;

		LOG_OUTPUT("Selecting 4:30 PM.");
		await DateTimePickerHelper.OpenDateTimePicker(timePicker);
		await TestServices.WindowHelper.WaitForIdle();

		DateTimePickerHelper.SelectTimeInOpenTimePickerFlyout(targetTime, LoopingSelectorHelper.SelectionMode.Keyboard);
		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread(() =>
		{
			var targetTimeSpan = CalendarToTimeSpan(targetTime);

			LOG_OUTPUT("Time and SelectedTime should now refer to the same date.");
			VERIFY_ARE_EQUAL(targetTimeSpan.Duration, timePicker.Time.Duration);
			VERIFY_IS_NOT_NULL(timePicker.SelectedTime);
			VERIFY_ARE_EQUAL(targetTimeSpan.Duration, timePicker.SelectedTime.Value.Duration);
		});
	}

	private async Task VerifyHasPlaceholder(TimePicker timePicker)
	{
		await RunOnUIThread(() =>
		{
			var hourTextBlock = (TextBlock) (TreeHelper::GetVisualChildByName(timePicker, "HourTextBlock"));
			var minuteTextBlock = (TextBlock) (TreeHelper::GetVisualChildByName(timePicker, "MinuteTextBlock"));
			var periodTextBlock = (TextBlock) (TreeHelper::GetVisualChildByName(timePicker, "PeriodTextBlock"));

			var validatePlaceholder = [](TextBlock ^ textBlock, Platform::String ^ placeholder)

			{
				LOG_OUTPUT("Expected placeholder: \"%s\"", placeholder->Data());
				LOG_OUTPUT("Actual text: \"%s\"", textBlock->Text->Data());

				VERIFY_IS_TRUE(Platform::String::CompareOrdinal(placeholder, textBlock->Text) == 0);
			};

			validatePlaceholder(hourTextBlock, "hour");
			validatePlaceholder(minuteTextBlock, "minute");
			validatePlaceholder(periodTextBlock, "AM");
		});
	}

	private TimeSpan CreateTimeSpan(int hours, int minutes, int seconds = 0, int period = 1)
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
}
