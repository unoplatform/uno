using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Automation.Peers;
using Windows.UI.Xaml.Automation.Provider;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;
using Private.Infrastructure;
using SamplesApp.UITests;
using Uno.UI.RuntimeTests.Helpers;
using Uno.UI.RuntimeTests.MUX.Helpers;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	[TestClass]
	public class Given_TimePicker
	{
		[TestMethod]
		[RunsOnUIThread]
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

		[TestMethod]
		[RunsOnUIThread]
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
		[RunsOnUIThread]
		[UnoWorkItem("https://github.com/unoplatform/uno/issues/15409")]
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
		[RunsOnUIThread]
		[UnoWorkItem("https://github.com/unoplatform/uno/issues/15256")]
		public async Task When_Opened_And_Unloaded_Unloaded_Native() => await When_Opened_And_Unloaded(true);

		[TestMethod]
		[RunsOnUIThread]
		[UnoWorkItem("https://github.com/unoplatform/uno/issues/15256")]
		public async Task When_Opened_And_Unloaded_Managed() => await When_Opened_And_Unloaded(false);

		private async Task When_Opened_And_Unloaded(bool useNative)
		{
			var timePicker = new Windows.UI.Xaml.Controls.TimePicker();
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
			Assert.IsInstanceOfType(associatedFlyout, typeof(Windows.UI.Xaml.Controls.TimePickerFlyout));
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

#if __ANDROID__ || __IOS__
			if (useNative)
			{
				var nativeTimePickerFlyout = (NativeTimePickerFlyout)associatedFlyout;
				Assert.IsFalse(nativeTimePickerFlyout.IsNativeDialogOpen);
			}
#endif
		}

#if HAS_UNO
		[TestMethod]
		[RunsOnUIThread]
		[UnoWorkItem("https://github.com/unoplatform/uno/issues/15256")]
		public async Task When_Flyout_Closed_FlyoutBase_Closed_Native() => await When_Flyout_Closed_FlyoutBase_Closed_Invoked(true);

		[TestMethod]
		[RunsOnUIThread]
		[UnoWorkItem("https://github.com/unoplatform/uno/issues/15256")]
		public async Task When_Flyout_Closed_FlyoutBase_Closed_Managed() => await When_Flyout_Closed_FlyoutBase_Closed_Invoked(false);

		private async Task When_Flyout_Closed_FlyoutBase_Closed_Invoked(bool useNative)
		{
			// Open flyout, close it via method or via native dismiss, check if event on flyoutbase was invoked
			var timePicker = new Windows.UI.Xaml.Controls.TimePicker();
			timePicker.UseNativeStyle = useNative;

			TestServices.WindowHelper.WindowContent = timePicker;

			await TestServices.WindowHelper.WaitForLoaded(timePicker);

			await DateTimePickerHelper.OpenDateTimePicker(timePicker);

			var openFlyouts = FlyoutBase.OpenFlyouts;
			Assert.AreEqual(1, openFlyouts.Count);
			var associatedFlyout = openFlyouts[0];

			Assert.IsInstanceOfType(associatedFlyout, typeof(Windows.UI.Xaml.Controls.TimePickerFlyout));
			var timePickerFlyout = (TimePickerFlyout)associatedFlyout;
			bool flyoutClosed = false;
			timePickerFlyout.Closed += (s, e) => flyoutClosed = true;
			timePickerFlyout.Close();

			await TestServices.WindowHelper.WaitFor(() => flyoutClosed, message: "Flyout did not close");

#if __ANDROID__ || __IOS__
			if (useNative)
			{
				var nativeTimePickerFlyout = (NativeTimePickerFlyout)timePickerFlyout;
				Assert.IsFalse(nativeTimePickerFlyout.IsNativeDialogOpen);
			}
#endif
		}
#endif

#if __IOS__
		[TestMethod]
		[RunsOnUIThread]
		[UnoWorkItem("https://github.com/unoplatform/uno/issues/15263")]
		public async Task When_App_Theme_Dark_Native_Flyout_Theme()
		{
			using var _ = ThemeHelper.UseDarkTheme();
			await When_Native_Flyout_Theme(UIKit.UIUserInterfaceStyle.Dark);
		}

		[TestMethod]
		[RunsOnUIThread]
		[UnoWorkItem("https://github.com/unoplatform/uno/issues/15263")]
		public async Task When_App_Theme_Light_Native_Flyout_Theme() => await When_Native_Flyout_Theme(UIKit.UIUserInterfaceStyle.Light);

		private async Task When_Native_Flyout_Theme(UIKit.UIUserInterfaceStyle expectedStyle)
		{
			var timePicker = new Windows.UI.Xaml.Controls.TimePicker();
			timePicker.UseNativeStyle = true;

			TestServices.WindowHelper.WindowContent = timePicker;

			await TestServices.WindowHelper.WaitForLoaded(timePicker);

			await DateTimePickerHelper.OpenDateTimePicker(timePicker);

			var openFlyouts = FlyoutBase.OpenFlyouts;
			Assert.AreEqual(1, openFlyouts.Count);
			var associatedFlyout = openFlyouts[0];
			Assert.IsInstanceOfType(associatedFlyout, typeof(Windows.UI.Xaml.Controls.TimePickerFlyout));
			var timePickerFlyout = (TimePickerFlyout)associatedFlyout;

			var nativeTimePickerFlyout = (NativeTimePickerFlyout)timePickerFlyout;

			var nativeTimePicker = nativeTimePickerFlyout._timeSelector;
			Assert.AreEqual(expectedStyle, nativeTimePicker.OverrideUserInterfaceStyle);
		}
#endif
	}

	class MyContext
	{
		public object StartTime => null;
	}
}
