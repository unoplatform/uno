using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using MUXControlsTestApp.Utilities;
using Private.Infrastructure;
using SamplesApp.UITests;
using Uno.UI.Extensions;
using Uno.UI.RuntimeTests.Helpers;
using Uno.UI.RuntimeTests.MUX.Helpers;

#if HAS_UNO && !HAS_UNO_WINUI
using Microsoft.UI.Xaml.Controls.Primitives;
#endif

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	[TestClass]
	[RunsOnUIThread]
	public class Given_TimePicker
	{
#if HAS_UNO
		[TestMethod]
		[RequiresFullWindow]
		public async Task When_TimePickerFlyout_Placed_Outside_Window()
		{
			var btn = new Button
			{
				HorizontalAlignment = HorizontalAlignment.Right,
				Content = "Open Flyout",
				Flyout = new TimePickerFlyout()
			};

			await UITestHelper.Load(btn);
			btn.ProgrammaticClick();
			await UITestHelper.WaitForIdle();

			var presenter = (TimePickerFlyoutPresenter)VisualTreeHelper.GetOpenPopupsForXamlRoot(TestServices.WindowHelper.XamlRoot)[0].Child;
			Assert.IsTrue(presenter.GetAbsoluteBoundsRect().IntersectWith(TestServices.WindowHelper.XamlRoot.VisualTree.VisibleBounds).Equals(presenter.GetAbsoluteBoundsRect()));
		}
#endif

		[TestMethod]
		public async Task When_MinuteIncrement_In_Range_Should_Be_Set_Properly()
		{
			var timePicker = new TimePicker();
			Assert.AreEqual(1, timePicker.MinuteIncrement);

			timePicker.MinuteIncrement = 59;
			Assert.AreEqual(59, timePicker.MinuteIncrement);
		}

		[TestMethod]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/22207")]
		public async Task When_12HourClock_Should_Display_12_Instead_Of_0()
		{
			var timePicker = new TimePicker();
#if HAS_UNO
			timePicker.UseNativeStyle = false;
#endif
			timePicker.ClockIdentifier = Windows.Globalization.ClockIdentifiers.TwelveHour;
			// Set time to midnight (0:00) or noon (12:00)
			timePicker.Time = new TimeSpan(0, 0, 0);
			TestServices.WindowHelper.WindowContent = timePicker;
			await TestServices.WindowHelper.WaitForLoaded(timePicker);

			await DateTimePickerHelper.OpenDateTimePicker(timePicker);
			await TestServices.WindowHelper.WaitForIdle();

			var hourTextBlockField = typeof(TimePicker).GetField("m_tpHourTextBlock",
				System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
			Assert.IsNotNull(hourTextBlockField, "m_tpHourTextBlock field should exist");

			var hourTextBlock = hourTextBlockField.GetValue(timePicker) as TextBlock;
			Assert.IsNotNull(hourTextBlock, "HourTextBlock should be created");

			Assert.AreEqual("12", hourTextBlock.Text, "Hour should display as 12 for midnight in 12-hour clock");

			var popup = VisualTreeHelper.GetOpenPopupsForXamlRoot(TestServices.WindowHelper.XamlRoot).FirstOrDefault();
			var timePickerFlyoutPresenter = popup?.Child as TimePickerFlyoutPresenter;
			Assert.IsNotNull(timePickerFlyoutPresenter);

			// Find the hour looping selector
			(var hourLoopingSelector, _, _) = await DateTimePickerHelper.GetHourMinutePeriodLoopingSelectorsFromOpenFlyout();

			Assert.IsNotNull(hourLoopingSelector, "HourLoopingSelector should be found");

			var items = hourLoopingSelector.Items;
			Assert.IsTrue(items.Count > 0, "Hour items should be populated");

			var firstItem = items[0] as DatePickerFlyoutItem;
			Assert.IsNotNull(firstItem, "First item should be a DatePickerFlyoutItem");

			// In 12-hour clock, the first hour should represent 12 (not 0), regardless of formatting or digit shapes
			Assert.IsTrue(int.TryParse(firstItem.PrimaryText, out var firstHourValue), "First hour text should be a valid integer");
			Assert.AreEqual(12, firstHourValue, "First hour in 12-hour clock should represent 12");
		}

		[TestMethod]
		public async Task When_MinuteIncrement_Not_In_Range_Should_Throw_And_Keep_OldValue()
		{
			var timePicker = new TimePicker();
			timePicker.MinuteIncrement = 17;
			Assert.AreEqual(17, timePicker.MinuteIncrement);
			Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => timePicker.MinuteIncrement = 60);
			Assert.AreEqual(17, timePicker.MinuteIncrement);
			Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => timePicker.MinuteIncrement = -1);
			Assert.AreEqual(17, timePicker.MinuteIncrement);
		}

		[TestMethod]
		public async Task When_SettingNullTime_ShouldNotCrash()
		{
			var timePicker = new TimePicker();
			timePicker.SetBinding(TimePicker.TimeProperty, new Binding { Path = new PropertyPath("StartTime") });

			var root = new Grid
			{
				DataContext = new MyContext()
			};

			root.Children.Add(timePicker);

			TestServices.WindowHelper.WindowContent = root;

			await TestServices.WindowHelper.WaitForIdle();
		}

#if HAS_UNO
		// Validates the workaround for missing support of MonochromaticOverlayPresenter in Uno
		[TestMethod]
		public async Task When_MonochromaticOverlayPresenter_Workaround()
		{
			var timePicker = new TimePicker();
			timePicker.UseNativeStyle = false;

			TestServices.WindowHelper.WindowContent = timePicker;
			await TestServices.WindowHelper.WaitForLoaded(timePicker);

			await DateTimePickerHelper.OpenDateTimePicker(timePicker);

			var popup = VisualTreeHelper.GetOpenPopupsForXamlRoot(TestServices.WindowHelper.XamlRoot).FirstOrDefault();
			var timePickerFlyoutPresenter = popup?.Child as TimePickerFlyoutPresenter;
			Assert.IsNotNull(timePickerFlyoutPresenter);

			var presenters = VisualTreeUtils.FindVisualChildrenByType<MonochromaticOverlayPresenter>(timePickerFlyoutPresenter);
			foreach (var presenter in presenters)
			{
				Assert.AreEqual(0, presenter.Opacity);
			}

			var highlightRect = VisualTreeUtils.FindVisualChildByName(timePickerFlyoutPresenter, "HighlightRect") as Grid;
			Assert.IsNotNull(highlightRect);
			Assert.AreEqual(0.5, highlightRect.Opacity);
		}
#endif

		[TestMethod]
		public async Task When_PM_Opened_And_Closed()
		{
			// This tests whether the looping selector does not unexpectedly
			// change the time when the flyout is opened and closed.
			var timePicker = new TimePicker();
#if HAS_UNO
			timePicker.UseNativeStyle = false;
#endif
			var expectedTime = new TimeSpan(15, 0, 0);
			timePicker.Time = expectedTime;
			timePicker.SelectedTime = expectedTime;
			TestServices.WindowHelper.WindowContent = timePicker;
			await TestServices.WindowHelper.WaitForLoaded(timePicker);
			await DateTimePickerHelper.OpenDateTimePicker(timePicker);
			await TestServices.WindowHelper.WaitForIdle();

			// Confirm the "same time"
			await ControlHelper.ClickFlyoutCloseButton(timePicker, true /* isAccept */);
			await TestServices.WindowHelper.WaitForIdle();

			Assert.AreEqual(expectedTime, timePicker.SelectedTime);
			Assert.AreEqual(expectedTime, timePicker.Time);
		}

		[TestMethod]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/15409")]
		public async Task When_Opened_From_Button_Flyout()
		{
			var button = new Button();
			var timePickerFlyout = new TimePickerFlyout();
			button.Flyout = timePickerFlyout;

			var root = new Grid();
			root.Children.Add(button);

			TestServices.WindowHelper.WindowContent = root;

			await TestServices.WindowHelper.WaitForLoaded(root);

			var buttonAutomationPeer = FrameworkElementAutomationPeer.CreatePeerForElement(button);
			var invokePattern = buttonAutomationPeer.GetPattern(PatternInterface.Invoke) as IInvokeProvider;
			invokePattern.Invoke();

			await TestServices.WindowHelper.WaitForIdle();

			var popup = VisualTreeHelper.GetOpenPopupsForXamlRoot(TestServices.WindowHelper.XamlRoot).FirstOrDefault();
			var timePickerFlyoutPresenter = popup?.Child as TimePickerFlyoutPresenter;

			try
			{
				Assert.IsNotNull(timePickerFlyoutPresenter);
				Assert.AreEqual(1, timePickerFlyoutPresenter.Opacity);
			}
			finally
			{
				if (popup is not null)
				{
					popup.IsOpen = false;
				}
			}
		}

		[TestMethod]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/15256")]
		public async Task When_Opened_And_Unloaded_Unloaded_Native() => await When_Opened_And_Unloaded(true);

		[TestMethod]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/15256")]
		public async Task When_Opened_And_Unloaded_Managed() => await When_Opened_And_Unloaded(false);

		private async Task When_Opened_And_Unloaded(bool useNative)
		{
			var timePicker = new Microsoft.UI.Xaml.Controls.TimePicker();
#if HAS_UNO
			timePicker.UseNativeStyle = useNative;
#endif

			TestServices.WindowHelper.WindowContent = timePicker;

			await TestServices.WindowHelper.WaitForLoaded(timePicker);

			await DateTimePickerHelper.OpenDateTimePicker(timePicker);

#if HAS_UNO // FlyoutBase.OpenFlyouts also includes native popups like NativeTimePickerFlyout
			var openFlyouts = FlyoutBase.OpenFlyouts;
			Assert.AreEqual(1, openFlyouts.Count);
			var associatedFlyout = openFlyouts[0];
			Assert.IsInstanceOfType(associatedFlyout, typeof(Microsoft.UI.Xaml.Controls.TimePickerFlyout));
#endif

			bool unloaded = false;
			timePicker.Unloaded += (s, e) => unloaded = true;

			TestServices.WindowHelper.WindowContent = null;

			await TestServices.WindowHelper.WaitFor(() => unloaded, message: "DatePicker did not unload");

			var openFlyoutsCount = VisualTreeHelper.GetOpenPopupsForXamlRoot(TestServices.WindowHelper.XamlRoot).Count;
			openFlyoutsCount.Should().Be(0, "There should be no open flyouts");

#if HAS_UNO // FlyoutBase.OpenFlyouts also includes native popups like NativeTimePickerFlyout
			openFlyoutsCount = FlyoutBase.OpenFlyouts.Count;
			openFlyoutsCount.Should().Be(0, "There should be no open flyouts");
#endif

#if __ANDROID__ || __APPLE_UIKIT__
			if (useNative)
			{
				var nativeTimePickerFlyout = (NativeTimePickerFlyout)associatedFlyout;
				Assert.IsFalse(nativeTimePickerFlyout.IsNativeDialogOpen);
			}
#endif
		}

#if HAS_UNO
		[TestMethod]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/15256")]
		public async Task When_Flyout_Closed_FlyoutBase_Closed_Native() => await When_Flyout_Closed_FlyoutBase_Closed_Invoked(true);

		[TestMethod]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/15256")]
		public async Task When_Flyout_Closed_FlyoutBase_Closed_Managed() => await When_Flyout_Closed_FlyoutBase_Closed_Invoked(false);

		private async Task When_Flyout_Closed_FlyoutBase_Closed_Invoked(bool useNative)
		{
			// Open flyout, close it via method or via native dismiss, check if event on flyoutbase was invoked
			var timePicker = new Microsoft.UI.Xaml.Controls.TimePicker();
			timePicker.UseNativeStyle = useNative;

			TestServices.WindowHelper.WindowContent = timePicker;

			await TestServices.WindowHelper.WaitForLoaded(timePicker);

			await DateTimePickerHelper.OpenDateTimePicker(timePicker);

			var openFlyouts = FlyoutBase.OpenFlyouts;
			Assert.AreEqual(1, openFlyouts.Count);
			var associatedFlyout = openFlyouts[0];

			Assert.IsInstanceOfType(associatedFlyout, typeof(Microsoft.UI.Xaml.Controls.TimePickerFlyout));
			var timePickerFlyout = (TimePickerFlyout)associatedFlyout;
			bool flyoutClosed = false;
			timePickerFlyout.Closed += (s, e) => flyoutClosed = true;
			timePickerFlyout.Close();

			await TestServices.WindowHelper.WaitFor(() => flyoutClosed, message: "Flyout did not close");

#if __ANDROID__ || __APPLE_UIKIT__
			if (useNative)
			{
				var nativeTimePickerFlyout = (NativeTimePickerFlyout)timePickerFlyout;
				Assert.IsFalse(nativeTimePickerFlyout.IsNativeDialogOpen);
			}
#endif
		}
#endif

#if __APPLE_UIKIT__
		[TestMethod]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/15263")]
		public async Task When_App_Theme_Dark_Native_Flyout_Theme()
		{
			using var _ = ThemeHelper.UseDarkTheme();
			await When_Native_Flyout_Theme(UIKit.UIUserInterfaceStyle.Dark);
		}

		[TestMethod]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/15263")]
		public async Task When_App_Theme_Light_Native_Flyout_Theme() => await When_Native_Flyout_Theme(UIKit.UIUserInterfaceStyle.Light);

		private async Task When_Native_Flyout_Theme(UIKit.UIUserInterfaceStyle expectedStyle)
		{
			var timePicker = new Microsoft.UI.Xaml.Controls.TimePicker();
			timePicker.UseNativeStyle = true;

			TestServices.WindowHelper.WindowContent = timePicker;

			await TestServices.WindowHelper.WaitForLoaded(timePicker);

			await DateTimePickerHelper.OpenDateTimePicker(timePicker);

			var openFlyouts = FlyoutBase.OpenFlyouts;
			Assert.AreEqual(1, openFlyouts.Count);
			var associatedFlyout = openFlyouts[0];
			Assert.IsInstanceOfType(associatedFlyout, typeof(Microsoft.UI.Xaml.Controls.TimePickerFlyout));
			var timePickerFlyout = (TimePickerFlyout)associatedFlyout;

			var nativeTimePickerFlyout = (NativeTimePickerFlyout)timePickerFlyout;

			var nativeTimePicker = nativeTimePickerFlyout._timeSelector;
			Assert.AreEqual(expectedStyle, nativeTimePicker.OverrideUserInterfaceStyle);
		}
#endif
#if __IOS__ || __ANDROID__
		[TestMethod]
		public async Task When_Time_Uninitialized_Should_Display_Current_Time()
		{
			var timePicker = new Microsoft.UI.Xaml.Controls.TimePicker();
			timePicker.Time = new TimeSpan(NativeTimePickerFlyout.DEFAULT_TIME_TICKS);

			var expectedCurrentTime = GetCurrentTime();

			TestServices.WindowHelper.WindowContent = timePicker;
			await TestServices.WindowHelper.WaitForLoaded(timePicker);

			await DateTimePickerHelper.OpenDateTimePicker(timePicker);

			var openFlyouts = FlyoutBase.OpenFlyouts;
			var associatedFlyout = openFlyouts[0];
			Assert.IsInstanceOfType(associatedFlyout, typeof(NativeTimePickerFlyout));

#if __ANDROID__
			var nativeFlyout = (NativeTimePickerFlyout)associatedFlyout;

			var dialog = nativeFlyout.GetNativeDialog();

			var decorView = dialog.Window?.DecorView;
			var timePickerView = FindTimePicker(decorView);

			var displayedHour = timePickerView.GetHourCompat();
			var displayedMinute = timePickerView.GetMinuteCompat();

			Assert.AreEqual(expectedCurrentTime.Hours, displayedHour, "Hours should match the current time.");
			Assert.AreEqual(expectedCurrentTime.Minutes, displayedMinute, "Minutes should match the current time.");
#elif __IOS__
			var nativeFlyout = (NativeTimePickerFlyout)associatedFlyout;

			var timeSelector = nativeFlyout.GetTimeSelector();
			var displayedTime = timeSelector.Time;

			Assert.AreEqual(expectedCurrentTime.Hours, displayedTime.Hours, "Hours should match the current time.");
			Assert.AreEqual(expectedCurrentTime.Minutes, displayedTime.Minutes, "Minutes should match the current time.");
#endif
		}

		private TimeSpan GetCurrentTime()
		{
			var calendar = new global::Windows.Globalization.Calendar();
			calendar.SetToNow();
			var now = calendar.GetDateTime();
			return new TimeSpan(now.Hour, now.Minute, now.Second);
		}
#if __ANDROID__
		private Android.Widget.TimePicker FindTimePicker(Android.Views.View root)
		{
			if (root is Android.Widget.TimePicker picker)
			{
				return picker;
			}

			if (root is not Android.Views.ViewGroup viewGroup)
			{
				return null;
			}

			for (var i = 0; i < viewGroup.ChildCount; i++)
			{
				var child = viewGroup.GetChildAt(i);
				var result = FindTimePicker(child);
				if (result != null)
				{
					return result;
				}
			}

			return null;
		}
#endif
#endif

		[TestMethod]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/21958")]
		public async Task When_Fast_Scrolling_Items_Should_Remain_Visible()
		{
			TimePicker timePicker = new()
			{
				Time = new TimeSpan(12, 30, 0)
			};
#if HAS_UNO
			timePicker.UseNativeStyle = false;
#endif
			TestServices.WindowHelper.WindowContent = timePicker;
			await TestServices.WindowHelper.WaitForLoaded(timePicker);

			await DateTimePickerHelper.OpenDateTimePicker(timePicker);
			await TestServices.WindowHelper.WaitForIdle();

			(var hourLoopingSelector, _, _) = await DateTimePickerHelper.GetHourMinutePeriodLoopingSelectorsFromOpenFlyout();

			Assert.IsNotNull(hourLoopingSelector, "HourLoopingSelector should be found");

			ScrollViewer scrollViewer = null;
			LoopingSelectorPanel panel = null;
			var initialRealizedItemCount = 0;

			await TestServices.RunOnUIThread(() =>
			{
				scrollViewer = (ScrollViewer)VisualTreeUtils.FindVisualChildByName(hourLoopingSelector, "ScrollViewer");
				Assert.IsNotNull(scrollViewer, "ScrollViewer should be found");

				panel = scrollViewer.Content as LoopingSelectorPanel;
				Assert.IsNotNull(panel, "LoopingSelectorPanel should be found");

				initialRealizedItemCount = panel.Children.Count;
				Assert.IsGreaterThan(0, initialRealizedItemCount, "Should have realized items initially");
			});

			// Simulate fast scrolling by rapidly changing the scroll position
			// This triggers multiple intermediate ViewChanged events
			var scrollPositions = new[] { 100.0, 200.0, 300.0, 400.0, 500.0, 600.0, 700.0, 800.0 };

			foreach (var scrollPosition in scrollPositions)
			{
				await TestServices.RunOnUIThread(() =>
				{
					scrollViewer.ChangeView(null, scrollPosition, null, disableAnimation: false);
				});

				await Task.Delay(10); // Small delay to allow event processing

				var currentItemCount = 0;
				await TestServices.RunOnUIThread(() =>
				{
					currentItemCount = panel.Children.Count;
				});

				Assert.IsGreaterThan(0,
currentItemCount, $"Should have visible items during fast scrolling at position {scrollPosition}, but found {currentItemCount} items");
			}

			await TestServices.WindowHelper.WaitForIdle();

			var finalRealizedItemCount = 0;
			await TestServices.RunOnUIThread(() =>
			{
				finalRealizedItemCount = panel.Children.Count;
			});

			Assert.IsGreaterThan(0, finalRealizedItemCount, "Should have realized items after scrolling completes");

			await ControlHelper.ClickFlyoutCloseButton(timePicker, false /* isAccept */);
			await TestServices.WindowHelper.WaitForIdle();
		}

		class MyContext
		{
			public object StartTime => null;
		}
	}
}
