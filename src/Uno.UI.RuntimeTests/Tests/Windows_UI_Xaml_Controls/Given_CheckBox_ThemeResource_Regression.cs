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
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.Native)] // Owner-subtree theme override is Skia-only; native UI targets honor OS/app theme only
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
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.Native)] // Owner-subtree theme override is Skia-only; native UI targets honor OS/app theme only
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

		// A control's PointerOver background applied through a VisualState.Setter whose value is a
		// {ThemeResource} (e.g. a flat icon button / grid date cell) must resolve against the
		// owner's inherited theme, not the application theme. Here the button lives in a Light-pinned Frame under a
		// Dark application; the override resolves Blue in Light and Red in Default(Dark). Re-materialising the page
		// through Frame navigation and re-entering PointerOver must keep the Light (Blue) value.
		[TestMethod]
		[RequiresFullWindow]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.Native)] // Owner-subtree theme override is Skia-only; native UI targets honor OS/app theme only
		public async Task When_VisualStateSetter_PointerOver_Keeps_Light_After_Frame_Navigation_Under_Dark_App()
		{
			using var _ = ThemeHelper.UseApplicationDarkTheme();
			await TestServices.WindowHelper.WaitForIdle();

			var overrides = new ThemeNavReproOverrides();
			Application.Current.Resources.MergedDictionaries.Add(overrides);
			try
			{
				// The Light pin lives on the Frame, mirroring a ThemeAssist-style RequestedTheme=Light on the root Frame.
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

		// Same app-level override, but the re-resolution is driven by an actual APPLICATION THEME CHANGE
		// (a NotifyThemeChanged tree walk that re-resolves the keyframe's {ThemeResource} via RefreshValue
		// against its pinned system dictionary), not a visual-state re-entry. The Light-pinned subtree must
		// keep the Blue (Light) override across an app Dark->Light->Dark round-trip. Uno-only: WinUI cannot
		// switch the application theme at runtime.
		[TestMethod]
		[RequiresFullWindow]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.Native)]
		public async Task When_App_Override_Checked_Survives_Application_Theme_Change()
		{
			var originalTheme = Application.Current.RequestedTheme;
			var wasExplicit = Application.Current.IsThemeSetExplicitly;
			var overrides = new CheckBoxReproOverrides();
			Application.Current.Resources.MergedDictionaries.Add(overrides);
			try
			{
				Application.Current.SetExplicitRequestedTheme(ApplicationTheme.Dark);
				await TestServices.WindowHelper.WaitForIdle();

				var lightRoot = new Border { RequestedTheme = ElementTheme.Light };
				var checkBox = new CheckBox { IsChecked = true, MinWidth = 0 };
				lightRoot.Child = checkBox;

				await UITestHelper.Load(lightRoot);
				await TestServices.WindowHelper.WaitForIdle();

				var box = checkBox.FindVisualChildByName("NormalRectangle") as FrameworkElement;
				Assert.IsNotNull(box, "NormalRectangle template part not found.");

				var initial = await UITestHelper.ScreenShot(checkBox);
				ImageAssert.HasColorAtChild(initial, box, box.ActualWidth / 2, box.ActualHeight / 2, Colors.Blue, tolerance: 40);

				// Drive real application theme changes; each walks the tree and re-resolves the Light-pinned
				// subtree's keyframe ThemeResource via RefreshValue.
				Application.Current.SetExplicitRequestedTheme(ApplicationTheme.Light);
				await TestServices.WindowHelper.WaitForIdle();
				Application.Current.SetExplicitRequestedTheme(ApplicationTheme.Dark);
				await TestServices.WindowHelper.WaitForIdle();

				var after = await UITestHelper.ScreenShot(checkBox);
				// Light-pinned subtree => Blue override regardless of the application theme.
				ImageAssert.HasColorAtChild(after, box, box.ActualWidth / 2, box.ActualHeight / 2, Colors.Blue, tolerance: 40);
			}
			finally
			{
				if (wasExplicit)
				{
					Application.Current.SetExplicitRequestedTheme(originalTheme);
				}
				else
				{
					Application.Current.SetExplicitRequestedTheme(null);
				}

				Application.Current.Resources.MergedDictionaries.Remove(overrides);
			}
		}

		// A stock CheckBox drives its checked fill through a storyboard keyframe
		// {ThemeResource CheckBoxCheckBackgroundFillChecked}. When that key is overridden at APPLICATION level
		// via a StaticResource alias to a flat palette brush in a SIBLING app dictionary (see
		// CheckBoxAppOverride{Palette,ThemeResources}), the keyframe must resolve the override on the theme leaf
		// that MATCHES the application theme (app theme + content both Light). #23416 dropped the top-level
		// re-pin, so the keyframe kept its parse-time pin to the stock control dictionary and reverted to the
		// stock brush — the checked box rendered blank until a pointer-over repainted it. Assert the applied
		// NormalRectangle.Fill DP (the reliable signal — the box centre shows the white check glyph either way).
		// OverrideAccentBrush = #FF0062A9. (The non-matching leaf — Light-under-Dark — was never affected.)
		[TestMethod]
		[RequiresFullWindow]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.Native)]
		public async Task When_App_Override_Checked_Resolves_On_Matching_App_Theme_Leaf()
		{
			// SampleFont is aliased by the overrides dictionary (ContentControlThemeFontFamily); merge it plus the
			// palette + overrides as app-level siblings (palette first so the StaticResource aliases resolve).
			var stubs = new ResourceDictionary
			{
				["SampleFont"] = new Microsoft.UI.Xaml.Media.FontFamily("Segoe UI"),
			};
			var palette = new CheckBoxAppOverridePalette();
			var overrides = new CheckBoxAppOverrideThemeResources();
			Application.Current.Resources.MergedDictionaries.Add(stubs);
			Application.Current.Resources.MergedDictionaries.Add(palette);
			Application.Current.Resources.MergedDictionaries.Add(overrides);
			try
			{
				using var _ = ThemeHelper.UseApplicationLightTheme();
				await TestServices.WindowHelper.WaitForIdle();

				var lightRoot = new Border { RequestedTheme = ElementTheme.Light };
				var checkBox = new CheckBox { IsChecked = true, MinWidth = 0 };
				lightRoot.Child = checkBox;
				await UITestHelper.Load(lightRoot);
				await TestServices.WindowHelper.WaitForIdle();

				var box = checkBox.FindVisualChildByName("NormalRectangle") as Microsoft.UI.Xaml.Shapes.Shape;
				Assert.IsNotNull(box, "NormalRectangle template part not found.");

				var fill = box.Fill as Microsoft.UI.Xaml.Media.SolidColorBrush;
				Assert.IsNotNull(fill, "Checked CheckBox NormalRectangle.Fill is not a SolidColorBrush (rendered blank — app-level keyframe override lost).");
				var expected = Windows.UI.Color.FromArgb(0xFF, 0x00, 0x62, 0xA9);
				Assert.AreEqual(expected, fill.Color,
					$"Checked CheckBox fill should be the app-level override #FF0062A9 but was #{fill.Color.A:X2}{fill.Color.R:X2}{fill.Color.G:X2}{fill.Color.B:X2}.");
			}
			finally
			{
				Application.Current.Resources.MergedDictionaries.Remove(overrides);
				Application.Current.Resources.MergedDictionaries.Remove(palette);
				Application.Current.Resources.MergedDictionaries.Remove(stubs);
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
