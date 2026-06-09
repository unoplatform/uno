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
	// Regression tests for {ThemeResource} resolution of controls living in a subtree pinned to an explicit
	// RequestedTheme that differs from the application theme (a Light-pinned subtree under a Dark application).
	[TestClass]
	[RunsOnUIThread]
	public class Given_CheckBox_ThemeResource_Regression
	{
#if HAS_UNO
		// App-level override (merged after XamlControlsResources) of the stock Fluent v2 CheckBox brushes. The stock
		// template applies its checked-state brushes through storyboard key-frames (e.g.
		// <DiscreteObjectKeyFrame Value="{ThemeResource CheckBoxCheckBackgroundFillChecked}" />), and the
		// per-storyboard-begin refresh re-resolves each key-frame against the dictionary it was pinned to. When the
		// override lives at application level it is reachable only through the top-level resource fallback, so it
		// must be re-pinned there; otherwise a checked CheckBox reverts to the stock brush on a visual-state re-entry.
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

		// kahua-private#482: a control's PointerOver background applied through a VisualState.Setter whose value is a
		// {ThemeResource} (the shape used by Kahua's flat icon buttons / DataGrid date cells) must resolve against the
		// owner's inherited theme, not the application theme. Here the button lives in a Light-pinned Frame under a
		// Dark application; the override resolves Blue in Light and Red in Default(Dark). Re-materialising the page
		// through Frame navigation and re-entering PointerOver must keep the Light (Blue) value.
		[TestMethod]
		[RequiresFullWindow]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.Native)]
		public async Task When_VisualStateSetter_PointerOver_Keeps_Light_After_Frame_Navigation_Under_Dark_App()
		{
			using var _ = ThemeHelper.UseApplicationDarkTheme();
			await TestServices.WindowHelper.WaitForIdle();

			var overrides = new ThemeNavReproOverrides();
			Application.Current.Resources.MergedDictionaries.Add(overrides);
			try
			{
				// The Light pin lives on the Frame, mirroring Kahua's ThemeAssist RequestedTheme=Light on the root Frame.
				var frame = new Frame { RequestedTheme = ElementTheme.Light };
				await UITestHelper.Load(frame);

				var firstButton = await NavigateAndGetFlatButton(frame);
				var first = await EnterPointerOverAndScreenShot(firstButton);
				ImageAssert.HasColorAt(first, first.Width / 2, first.Height / 2, Colors.Blue, tolerance: 40);

				// Navigate away, then back to a fresh instance of the page (re-materialisation under the Light pin).
				frame.Navigate(typeof(CheckBoxNavReproOtherPage));
				await TestServices.WindowHelper.WaitForIdle();

				var secondButton = await NavigateAndGetFlatButton(frame);
				var second = await EnterPointerOverAndScreenShot(secondButton);
				// Must still be Blue (Light); Red would mean the setter resolved the Dark sub-dictionary.
				ImageAssert.HasColorAt(second, second.Width / 2, second.Height / 2, Colors.Blue, tolerance: 40);
			}
			finally
			{
				Application.Current.Resources.MergedDictionaries.Remove(overrides);
			}
		}

		private static async Task<Button> NavigateAndGetFlatButton(Frame frame)
		{
			frame.Navigate(typeof(FlatButtonNavReproPage));
			await TestServices.WindowHelper.WaitForIdle();

			var page = (FlatButtonNavReproPage)frame.Content;
			await TestServices.WindowHelper.WaitForLoaded(page.SUT);
			await TestServices.WindowHelper.WaitForIdle();
			return page.SUT;
		}

		private static async Task<RawBitmap> EnterPointerOverAndScreenShot(Button button)
		{
			VisualStateManager.GoToState(button, "PointerOver", useTransitions: true);
			await TestServices.WindowHelper.WaitForIdle();
			return await UITestHelper.ScreenShot(button);
		}
#endif
	}
}
