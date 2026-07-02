#if HAS_INPUT_INJECTOR || WINAPPSDK
#nullable enable

using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;
using Uno.UI.Toolkit.DevTools.Input;
using Windows.Foundation;
using Windows.UI.Input.Preview.Injection;
using static Private.Infrastructure.TestServices.WindowHelper;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

// Migrated from SamplesApp.UITests UnoSamples_CheckBox.cs — exercises the real pointer-tap path:
// two-state toggling and three-state cycling, asserting the Checked/Unchecked/Indeterminate events
// fire with the expected IsChecked value (as the CheckBox_Automated sample surfaced via a TextBlock).
[TestClass]
[RunsOnUIThread]
public class Given_CheckBox_UITest
{
	[TestMethod]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI | RuntimeTestPlatforms.NativeAndroid | RuntimeTestPlatforms.NativeIOS | RuntimeTestPlatforms.NativeWasm)]
	public async Task When_TwoState_Tapped_Toggles()
	{
		if (TestServices.WindowHelper.IsXamlIsland)
		{
			return;
		}

		string? result = null;
		var checkBox = new CheckBox { Name = "twoState01", Content = "Two State" };
		checkBox.Checked += (s, e) => result = $"Checked {checkBox.Name} {checkBox.IsChecked}";
		checkBox.Unchecked += (s, e) => result = $"Unchecked {checkBox.Name} {checkBox.IsChecked}";
		checkBox.Indeterminate += (s, e) => result = $"Indeterminate {checkBox.Name} {checkBox.IsChecked}";

		var panel = new StackPanel { Children = { checkBox } };

		try
		{
			await UITestHelper.Load(panel);

			var injector = InputInjector.TryCreate();
			Assert.IsNotNull(injector);
			using var mouse = injector.GetMouse();

			async Task Tap()
			{
				var center = checkBox.TransformToVisual(TestServices.WindowHelper.XamlRoot.Content)
					.TransformPoint(new Point(checkBox.ActualWidth / 2, checkBox.ActualHeight / 2));
				mouse.Press(center);
				mouse.Release();
				await WaitForIdle();
			}

			// Initial state: unchecked.
			Assert.AreEqual(false, checkBox.IsChecked);

			await Tap();
			Assert.AreEqual(true, checkBox.IsChecked);
			Assert.AreEqual("Checked twoState01 True", result);

			await Tap();
			Assert.AreEqual(false, checkBox.IsChecked);
			Assert.AreEqual("Unchecked twoState01 False", result);

			await Tap();
			Assert.AreEqual(true, checkBox.IsChecked);
			Assert.AreEqual("Checked twoState01 True", result);
		}
		finally
		{
			WindowContent = null;
		}
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI | RuntimeTestPlatforms.NativeAndroid | RuntimeTestPlatforms.NativeIOS | RuntimeTestPlatforms.NativeWasm)]
	public async Task When_ThreeState_Tapped_Cycles()
	{
		if (TestServices.WindowHelper.IsXamlIsland)
		{
			return;
		}

		string? result = null;
		var checkBox = new CheckBox { Name = "threeState01", Content = "Three State", IsThreeState = true };
		checkBox.Checked += (s, e) => result = $"Checked {checkBox.Name} {checkBox.IsChecked}";
		checkBox.Unchecked += (s, e) => result = $"Unchecked {checkBox.Name} {checkBox.IsChecked}";
		checkBox.Indeterminate += (s, e) => result = $"Indeterminate {checkBox.Name} {checkBox.IsChecked}";

		var panel = new StackPanel { Children = { checkBox } };

		try
		{
			await UITestHelper.Load(panel);

			var injector = InputInjector.TryCreate();
			Assert.IsNotNull(injector);
			using var mouse = injector.GetMouse();

			async Task Tap()
			{
				var center = checkBox.TransformToVisual(TestServices.WindowHelper.XamlRoot.Content)
					.TransformPoint(new Point(checkBox.ActualWidth / 2, checkBox.ActualHeight / 2));
				mouse.Press(center);
				mouse.Release();
				await WaitForIdle();
			}

			// Initial state: unchecked.
			Assert.AreEqual(false, checkBox.IsChecked);

			await Tap();
			Assert.AreEqual(true, checkBox.IsChecked);
			Assert.AreEqual("Checked threeState01 True", result);

			await Tap();
			Assert.IsNull(checkBox.IsChecked);
			Assert.AreEqual("Indeterminate threeState01 ", result);

			await Tap();
			Assert.AreEqual(false, checkBox.IsChecked);
			Assert.AreEqual("Unchecked threeState01 False", result);

			await Tap();
			Assert.AreEqual(true, checkBox.IsChecked);
			Assert.AreEqual("Checked threeState01 True", result);
		}
		finally
		{
			WindowContent = null;
		}
	}
}

#endif