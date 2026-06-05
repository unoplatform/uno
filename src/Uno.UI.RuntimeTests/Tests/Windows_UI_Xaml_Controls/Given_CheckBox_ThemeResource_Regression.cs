using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MUXControlsTestApp.Utilities;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;

#if HAS_UNO_WINUI || WINAPPSDK || WINUI
using Colors = Microsoft.UI.Colors;
#else
using Colors = Windows.UI.Colors;
#endif

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	// Regression tests for an application-level {ThemeResource} override of the stock Fluent v2 CheckBox
	// brushes. The stock CheckBox template applies its checked-state brushes through storyboard key-frames
	// (e.g. <DiscreteObjectKeyFrame Value="{ThemeResource CheckBoxCheckBackgroundFillChecked}" />), and the
	// per-storyboard-begin refresh re-resolves each key-frame against the dictionary it was pinned to. When the
	// override lives at application level (merged after XamlControlsResources) it is reachable only through the
	// top-level resource fallback, so it must be re-pinned there; otherwise a checked CheckBox reverts to the
	// stock brush on a visual-state re-entry.
	[TestClass]
	[RunsOnUIThread]
	public class Given_CheckBox_ThemeResource_Regression
	{
#if HAS_UNO
		// App-level override (merged after XamlControlsResources), a Dark application with a Light-pinned
		// subtree, a checked CheckBox, and a post-load visual-state re-entry (IsChecked toggled off then on).
		// The re-entry runs ObjectAnimationUsingKeyFrames.Begin() -> EnsureKeyFrameThemeResources(), which
		// re-resolves the key-frame against its pinned dictionary only. Without the top-level re-pin the box
		// reverts from the Blue override to the stock brush.
		[TestMethod]
		[RequiresFullWindow]
		public async Task When_App_Override_Checked_Survives_IsChecked_Reentry_Under_Dark_App()
		{
			using var _ = ThemeHelper.UseApplicationDarkTheme();
			await TestServices.WindowHelper.WaitForIdle();

			// App-level override using StaticResource indirection inside ThemeDictionaries (Light=Blue, Default=Red).
			var overrides = new CheckBoxReproOverrides();
			Application.Current.Resources.MergedDictionaries.Add(overrides);
			try
			{
				var lightRoot = new Border { RequestedTheme = ElementTheme.Light };
				var checkBox = new CheckBox { IsChecked = true, MinWidth = 0 };
				lightRoot.Child = checkBox;

				await UITestHelper.Load(lightRoot);
				await TestServices.WindowHelper.WaitForIdle();

				var box = checkBox.FindVisualChildByName("NormalRectangle") as FrameworkElement;
				Assert.IsNotNull(box, "NormalRectangle template part not found.");

				// Baseline: the Light subtree resolves the Blue override.
				var initial = await UITestHelper.ScreenShot(checkBox);
				ImageAssert.HasColorAtChild(initial, box, box.ActualWidth / 2, box.ActualHeight / 2, Colors.Blue, tolerance: 40);

				// Pure visual-state re-entry into CheckedNormal (GoToState -> storyboard.Begin), with no full
				// resource-binding pass to correct the value afterwards.
				checkBox.IsChecked = false;
				await TestServices.WindowHelper.WaitForIdle();
				checkBox.IsChecked = true;
				await TestServices.WindowHelper.WaitForIdle();

				var after = await UITestHelper.ScreenShot(checkBox);
				// Must still be the Blue override, not the stock-pinned value.
				ImageAssert.HasColorAtChild(after, box, box.ActualWidth / 2, box.ActualHeight / 2, Colors.Blue, tolerance: 40);
			}
			finally
			{
				Application.Current.Resources.MergedDictionaries.Remove(overrides);
			}
		}

		// Same setup, but the re-entry is driven by toggling IsEnabled (CheckedNormal -> CheckedDisabled ->
		// CheckedNormal); asserts in the enabled CheckedNormal state after the round-trip.
		[TestMethod]
		[RequiresFullWindow]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.Native)]
		public async Task When_App_Override_Checked_Survives_IsEnabled_Toggle_Under_Dark_App()
		{
			using var _ = ThemeHelper.UseApplicationDarkTheme();
			await TestServices.WindowHelper.WaitForIdle();

			var overrides = new CheckBoxReproOverrides();
			Application.Current.Resources.MergedDictionaries.Add(overrides);
			try
			{
				var lightRoot = new Border { RequestedTheme = ElementTheme.Light };
				var checkBox = new CheckBox { IsChecked = true, MinWidth = 0 };
				lightRoot.Child = checkBox;

				await UITestHelper.Load(lightRoot);
				await TestServices.WindowHelper.WaitForIdle();

				var box = checkBox.FindVisualChildByName("NormalRectangle") as FrameworkElement;
				Assert.IsNotNull(box, "NormalRectangle template part not found.");

				var initial = await UITestHelper.ScreenShot(checkBox);
				ImageAssert.HasColorAtChild(initial, box, box.ActualWidth / 2, box.ActualHeight / 2, Colors.Blue, tolerance: 40);

				// CheckedNormal -> CheckedDisabled -> CheckedNormal (each transition re-begins a storyboard).
				checkBox.IsEnabled = false;
				await TestServices.WindowHelper.WaitForIdle();
				checkBox.IsEnabled = true;
				await TestServices.WindowHelper.WaitForIdle();

				var after = await UITestHelper.ScreenShot(checkBox);
				// Must still be the Blue override, not the stock-pinned value.
				ImageAssert.HasColorAtChild(after, box, box.ActualWidth / 2, box.ActualHeight / 2, Colors.Blue, tolerance: 40);
			}
			finally
			{
				Application.Current.Resources.MergedDictionaries.Remove(overrides);
			}
		}
#endif
	}
}
