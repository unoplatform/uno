#if HAS_INPUT_INJECTOR || WINAPPSDK

using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;
using Uno.UI.Toolkit.DevTools.Input;
using Windows.Foundation;
using Windows.UI.Input.Preview.Injection;
using static Private.Infrastructure.TestServices.WindowHelper;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	// Migrated from SamplesApp.UITests UnoSamples_Tests.RadioButton.cs — exercises the real
	// pointer-tap path (selection, disabled gating, re-tap, state preservation across IsEnabled).
	partial class Given_RadioButton
	{
		[TestMethod]
		[RunsOnUIThread]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI | RuntimeTestPlatforms.NativeAndroid | RuntimeTestPlatforms.NativeIOS | RuntimeTestPlatforms.NativeWasm)]
		public async Task When_Tapped_Selects_And_Respects_IsEnabled()
		{
			if (TestServices.WindowHelper.IsXamlIsland)
			{
				return;
			}

			var rb1 = new RadioButton { Content = "RadioButton 1" };
			var rb2 = new RadioButton { Content = "RadioButton 2" };
			var panel = new StackPanel { Children = { rb1, rb2 } };

			try
			{
				await UITestHelper.Load(panel);

				var injector = InputInjector.TryCreate();
				Assert.IsNotNull(injector);
				using var mouse = injector.GetMouse();

				async Task Tap(RadioButton target)
				{
					var center = target.TransformToVisual(TestServices.WindowHelper.XamlRoot.Content)
						.TransformPoint(new Point(target.ActualWidth / 2, target.ActualHeight / 2));
					mouse.Press(center);
					mouse.Release();
					await WaitForIdle();
				}

				// Initial state: nothing selected.
				Assert.IsFalse(rb1.IsChecked ?? false);
				Assert.IsFalse(rb2.IsChecked ?? false);

				// Tap while enabled selects the first radio button.
				await Tap(rb1);
				Assert.IsTrue(rb1.IsChecked);
				Assert.IsFalse(rb2.IsChecked ?? false);

				// While disabled, tapping the second radio button must not change the selection.
				rb1.IsEnabled = false;
				rb2.IsEnabled = false;
				await Tap(rb2);
				Assert.IsTrue(rb1.IsChecked);
				Assert.IsFalse(rb2.IsChecked ?? false);

				// Re-enabling and tapping the second radio button moves the selection.
				rb1.IsEnabled = true;
				rb2.IsEnabled = true;
				await Tap(rb2);
				Assert.IsTrue(rb2.IsChecked);
				Assert.IsFalse(rb1.IsChecked ?? false);
			}
			finally
			{
				TestServices.WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		[RunsOnUIThread]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI | RuntimeTestPlatforms.NativeAndroid | RuntimeTestPlatforms.NativeIOS | RuntimeTestPlatforms.NativeWasm)]
		public async Task When_Tapped_Twice_Does_Not_Uncheck()
		{
			if (TestServices.WindowHelper.IsXamlIsland)
			{
				return;
			}

			var rb1 = new RadioButton { Content = "RadioButton 1" };
			var rb2 = new RadioButton { Content = "RadioButton 2" };
			var panel = new StackPanel { Children = { rb1, rb2 } };

			try
			{
				await UITestHelper.Load(panel);

				var injector = InputInjector.TryCreate();
				Assert.IsNotNull(injector);
				using var mouse = injector.GetMouse();

				async Task Tap(RadioButton target)
				{
					var center = target.TransformToVisual(TestServices.WindowHelper.XamlRoot.Content)
						.TransformPoint(new Point(target.ActualWidth / 2, target.ActualHeight / 2));
					mouse.Press(center);
					mouse.Release();
					await WaitForIdle();
				}

				await Tap(rb1);
				Assert.IsTrue(rb1.IsChecked);

				// Tapping an already-checked radio button keeps it checked.
				await Tap(rb1);
				Assert.IsTrue(rb1.IsChecked);

				await Tap(rb2);
				Assert.IsTrue(rb2.IsChecked);
				Assert.IsFalse(rb1.IsChecked ?? false);

				await Tap(rb2);
				Assert.IsTrue(rb2.IsChecked);
			}
			finally
			{
				TestServices.WindowHelper.WindowContent = null;
			}
		}

		[TestMethod]
		[RunsOnUIThread]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI | RuntimeTestPlatforms.NativeAndroid | RuntimeTestPlatforms.NativeIOS | RuntimeTestPlatforms.NativeWasm)]
		public async Task When_IsEnabled_Toggled_State_Is_Preserved()
		{
			if (TestServices.WindowHelper.IsXamlIsland)
			{
				return;
			}

			var rb1 = new RadioButton { Content = "RadioButton 1" };
			var rb2 = new RadioButton { Content = "RadioButton 2" };
			var panel = new StackPanel { Children = { rb1, rb2 } };

			try
			{
				await UITestHelper.Load(panel);

				var injector = InputInjector.TryCreate();
				Assert.IsNotNull(injector);
				using var mouse = injector.GetMouse();

				async Task Tap(RadioButton target)
				{
					var center = target.TransformToVisual(TestServices.WindowHelper.XamlRoot.Content)
						.TransformPoint(new Point(target.ActualWidth / 2, target.ActualHeight / 2));
					mouse.Press(center);
					mouse.Release();
					await WaitForIdle();
				}

				await Tap(rb1);
				Assert.IsTrue(rb1.IsChecked);

				// Selection survives being disabled and re-enabled.
				rb1.IsEnabled = false;
				rb2.IsEnabled = false;
				Assert.IsTrue(rb1.IsChecked);

				rb1.IsEnabled = true;
				rb2.IsEnabled = true;
				Assert.IsTrue(rb1.IsChecked);

				await Tap(rb2);
				Assert.IsTrue(rb2.IsChecked);

				rb1.IsEnabled = false;
				rb2.IsEnabled = false;
				Assert.IsTrue(rb2.IsChecked);
			}
			finally
			{
				TestServices.WindowHelper.WindowContent = null;
			}
		}
	}
}

#endif
