using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Uno.UI.RuntimeTests.MUX.Helpers;
using Microsoft.UI.Xaml.Media;
using FluentAssertions;
using Microsoft.UI.Xaml.Controls.Primitives;

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
		public async Task When_Opened_And_Unloaded_Unloaded_Native() => await When_Opened_And_Unloaded_Unloaded(true);

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_Opened_And_Unloaded_Managed() => await When_Opened_And_Unloaded_Unloaded(false);

		private async Task When_Opened_And_Unloaded_Unloaded(bool useNative)
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

#if __ANDROID__ || __IOS__
			if (useNative)
			{
				var nativeTimePickerFlyout = (NativeTimePickerFlyout)associatedFlyout;
				Assert.IsFalse(nativeTimePickerFlyout.IsNativeDialogOpen);
			}
#endif
		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_Flyout_Closed_FlyoutBase_Closed_Native() => await When_Flyout_Closed_FlyoutBase_Closed_Invoked(true);

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_Flyout_Closed_FlyoutBase_Closed_Managed() => await When_Flyout_Closed_FlyoutBase_Closed_Invoked(false);

		private async Task When_Flyout_Closed_FlyoutBase_Closed_Invoked(bool useNative)
		{
			// Open flyout, close it via method or via native dismiss, check if event on flyoutbase was invoked
			var timePicker = new Microsoft.UI.Xaml.Controls.TimePicker();
#if HAS_UNO
			timePicker.UseNativeStyle = useNative;
#endif

			TestServices.WindowHelper.WindowContent = timePicker;

			await TestServices.WindowHelper.WaitForLoaded(timePicker);

			await DateTimePickerHelper.OpenDateTimePicker(timePicker);

#if !HAS_UNO
			var openFlyouts = VisualTreeHelper.GetOpenPopupsForXamlRoot(TestServices.WindowHelper.XamlRoot);
			var flyoutBase = openFlyouts[0];
			var associatedFlyout = flyoutBase.AssociatedFlyout;
#else // FlyoutBase.OpenFlyouts also includes native popups like NativeTimePickerFlyout
			var openFlyouts = FlyoutBase.OpenFlyouts;
			Assert.AreEqual(1, openFlyouts.Count);
			var associatedFlyout = openFlyouts[0];
#endif
			Assert.IsInstanceOfType(associatedFlyout, typeof(Microsoft.UI.Xaml.Controls.TimePickerFlyout));
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
	}

	class MyContext
	{
		public object StartTime => null;
	}
}
