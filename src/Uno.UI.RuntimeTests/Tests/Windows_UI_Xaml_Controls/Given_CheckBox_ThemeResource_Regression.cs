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

		// kahua-private#482: a control's PointerOver background applied through a VisualState.Setter whose value is a
		// {ThemeResource} (the shape used by Kahua's flat icon buttons / DataGrid date cells) must resolve against the
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

		// kahua-private#491 (Uno #23472): a checked CheckBox in the AMBIENT application theme (no explicit
		// RequestedTheme pin) must render its checked fill on FIRST display, with no pointer/state interaction.
		// The stock template applies the checked fill through a CheckedNormal storyboard key-frame
		// (NormalRectangle.Fill = {ThemeResource CheckBoxCheckBackgroundFillChecked} = accent). On the ambient
		// path the per-object owner-theme slot push is a no-op, so the resolved key-frame value was left
		// unapplied to the live target until a later state re-entry (e.g. a PointerOver hover) re-began the
		// storyboard - the box looked unchecked (near-white fill, white glyph) on first render. This asserts the
		// first-render fill equals the fill after a corrective state re-entry (the hover analog).
		[TestMethod]
		[RequiresFullWindow]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.Native)] // Skia/WASM enhanced-lifecycle theming
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/23472")]
		[DataRow(true, DisplayName = "Checked")]
		[DataRow(null, DisplayName = "Indeterminate")]
		public async Task When_Checked_Fill_Renders_On_First_Render_In_Ambient_Light_App(bool? isChecked)
		{
			using var _ = ThemeHelper.UseApplicationLightTheme();
			await TestServices.WindowHelper.WaitForIdle();

			var checkBox = new CheckBox { IsChecked = isChecked, IsThreeState = isChecked is null, MinWidth = 0 };
			try
			{
				// Ambient: no RequestedTheme pin -> the control lives in the application (Light) base theme.
				await UITestHelper.Load(checkBox);
				await TestServices.WindowHelper.WaitForIdle();

				var box = checkBox.FindVisualChildByName("NormalRectangle") as FrameworkElement;
				Assert.IsNotNull(box, "NormalRectangle template part not found.");

				// FIRST render, NO interaction.
				var firstColor = await GetCenterColor(box);

				// Corrective state re-entry (the analog of the hover that "fixed" it in the field): re-enter the
				// checked state so the storyboard re-begins and applies the resolved key-frame value.
				checkBox.IsChecked = false;
				await TestServices.WindowHelper.WaitForIdle();
				checkBox.IsChecked = isChecked;
				await TestServices.WindowHelper.WaitForIdle();

				var afterColor = await GetCenterColor(box);

				// Reference sanity: after re-entry the checked fill is the accent (a saturated blue), clearly
				// NOT the near-white Unchecked fill - guards against a vacuous pass if neither render were checked.
				var afterIsNearWhite = afterColor.R >= 235 && afterColor.G >= 235 && afterColor.B >= 235;
				Assert.IsFalse(afterIsNearWhite,
					$"Reference (post-re-entry) checked fill {afterColor} should be the accent, not a near-white Unchecked fill.");

				// PRIMARY: first render must already match the post-re-entry (correct) fill.
				Assert.IsTrue(ColorsClose(firstColor, afterColor, 24),
					$"Checked fill on FIRST render ({firstColor}) must match the fill after a state re-entry ({afterColor}); " +
					$"a mismatch means the checked-state key-frame brush was resolved but not applied on first materialization in the ambient theme.");
			}
			finally
			{
				TestServices.WindowHelper.WindowContent = null;
			}
		}

		// Variant of the above where the checked CheckBox is hosted in a Popup opened AFTER load (the Kahua
		// Gallery is a dialog/popup). Popup content materializes its lazy VisualState chain at open time, on a
		// path where the ambient owner theme may not be established - the suspected first-render gap.
		[TestMethod]
		[RequiresFullWindow]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.Native)]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/23472")]
		[DataRow(true, DisplayName = "Checked")]
		[DataRow(null, DisplayName = "Indeterminate")]
		public async Task When_Checked_Fill_Renders_On_First_Open_In_Popup_Ambient_Light(bool? isChecked)
		{
			using var _ = ThemeHelper.UseApplicationLightTheme();
			await TestServices.WindowHelper.WaitForIdle();

			var anchor = new Border { Width = 10, Height = 10 };
			await UITestHelper.Load(anchor);

			var checkBox = new CheckBox { IsChecked = isChecked, IsThreeState = isChecked is null, MinWidth = 0 };
			var popup = new Microsoft.UI.Xaml.Controls.Primitives.Popup
			{
				Child = new Border { Child = checkBox },
				XamlRoot = anchor.XamlRoot,
			};
			try
			{
				popup.IsOpen = true;
				await TestServices.WindowHelper.WaitForIdle();

				var box = checkBox.FindVisualChildByName("NormalRectangle") as FrameworkElement;
				Assert.IsNotNull(box, "NormalRectangle template part not found.");

				var firstColor = await GetCenterColor(box);

				checkBox.IsChecked = false;
				await TestServices.WindowHelper.WaitForIdle();
				checkBox.IsChecked = isChecked;
				await TestServices.WindowHelper.WaitForIdle();

				var afterColor = await GetCenterColor(box);

				var afterIsNearWhite = afterColor.R >= 235 && afterColor.G >= 235 && afterColor.B >= 235;
				Assert.IsFalse(afterIsNearWhite,
					$"Reference (post-re-entry) checked fill {afterColor} should be the accent, not a near-white Unchecked fill.");
				Assert.IsTrue(ColorsClose(firstColor, afterColor, 24),
					$"Popup: checked fill on FIRST open ({firstColor}) must match the fill after a state re-entry ({afterColor}).");
			}
			finally
			{
				popup.IsOpen = false;
				TestServices.WindowHelper.WindowContent = null;
			}
		}

		// Variant where the checked CheckBox is set as deferred content AFTER the host is loaded (mirrors a
		// tab-switch / ContentPresenter swap that re-materializes the subtree post-load).
		[TestMethod]
		[RequiresFullWindow]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.Native)]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/23472")]
		[DataRow(true, DisplayName = "Checked")]
		[DataRow(null, DisplayName = "Indeterminate")]
		public async Task When_Checked_Fill_Renders_On_Deferred_Content_Ambient_Light(bool? isChecked)
		{
			using var _ = ThemeHelper.UseApplicationLightTheme();
			await TestServices.WindowHelper.WaitForIdle();

			var host = new ContentControl();
			await UITestHelper.Load(host, h => h.IsLoaded);

			var checkBox = new CheckBox { IsChecked = isChecked, IsThreeState = isChecked is null, MinWidth = 0 };
			try
			{
				host.Content = checkBox; // deferred materialization, post-load
				await TestServices.WindowHelper.WaitForLoaded(checkBox);
				await TestServices.WindowHelper.WaitForIdle();

				var box = checkBox.FindVisualChildByName("NormalRectangle") as FrameworkElement;
				Assert.IsNotNull(box, "NormalRectangle template part not found.");

				var firstColor = await GetCenterColor(box);

				checkBox.IsChecked = false;
				await TestServices.WindowHelper.WaitForIdle();
				checkBox.IsChecked = isChecked;
				await TestServices.WindowHelper.WaitForIdle();

				var afterColor = await GetCenterColor(box);

				var afterIsNearWhite = afterColor.R >= 235 && afterColor.G >= 235 && afterColor.B >= 235;
				Assert.IsFalse(afterIsNearWhite,
					$"Reference (post-re-entry) checked fill {afterColor} should be the accent, not a near-white Unchecked fill.");
				Assert.IsTrue(ColorsClose(firstColor, afterColor, 24),
					$"Deferred content: checked fill on FIRST render ({firstColor}) must match the fill after a state re-entry ({afterColor}).");
			}
			finally
			{
				TestServices.WindowHelper.WindowContent = null;
			}
		}

		// Closest to the real Kahua repro: an APP-LEVEL CheckBox brush override (Kahua's FluentOverrides pattern:
		// Light=Blue, Default=Red) with the checked CheckBox in the AMBIENT application (Light) theme - no explicit
		// RequestedTheme pin. The checked fill must resolve the Light (Blue) override on FIRST render, with no
		// interaction. (The existing tests cover the same override under an EXPLICIT Light pin; this is the
		// untested ambient path that the Kahua Gallery exercises.)
		[TestMethod]
		[RequiresFullWindow]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.Native)]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/23472")]
		public async Task When_App_Override_Checked_Renders_On_First_Render_Ambient_Light()
		{
			// CheckBoxReproOverrides overrides the CHECKED fill brush (Light=Blue); it does not override the
			// Indeterminate fill, so this guard covers the Checked state only.
			using var _ = ThemeHelper.UseApplicationLightTheme();
			await TestServices.WindowHelper.WaitForIdle();

			var overrides = new CheckBoxReproOverrides();
			Application.Current.Resources.MergedDictionaries.Add(overrides);
			try
			{
				// Ambient: no RequestedTheme pin -> application (Light) base theme.
				var checkBox = new CheckBox { IsChecked = true, MinWidth = 0 };
				await UITestHelper.Load(checkBox);
				await TestServices.WindowHelper.WaitForIdle();

				var box = checkBox.FindVisualChildByName("NormalRectangle") as FrameworkElement;
				Assert.IsNotNull(box, "NormalRectangle template part not found.");

				// FIRST render, NO interaction: the Light override (Blue) must be applied to the box FILL.
				// Sample a corner (fill area) not the centre - the Indeterminate dash glyph sits dead-centre and
				// would spuriously read white even when the fill is correct.
				var initial = await UITestHelper.ScreenShot(checkBox);
				ImageAssert.HasColorAtChild(initial, box, box.ActualWidth * 0.22, box.ActualHeight * 0.22, Colors.Blue, tolerance: 40);
			}
			finally
			{
				Application.Current.Resources.MergedDictionaries.Remove(overrides);
				TestServices.WindowHelper.WindowContent = null;
			}
		}

		// Most faithful to the Kahua Gallery: an ambient checked CheckBox and a sibling RequestedTheme=Dark
		// checked CheckBox materialized together in the SAME tree (the gallery shows both panels side by side).
		// Tests whether the Dark sibling's theme establishment contaminates the ambient one's first-render fill.
		[TestMethod]
		[RequiresFullWindow]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.Native)]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/23472")]
		[DataRow(true, DisplayName = "Checked")]
		[DataRow(null, DisplayName = "Indeterminate")]
		public async Task When_Checked_Fill_Renders_With_Dark_Sibling_Ambient_Light(bool? isChecked)
		{
			using var _ = ThemeHelper.UseApplicationLightTheme();
			await TestServices.WindowHelper.WaitForIdle();

			var ambient = new CheckBox { IsChecked = isChecked, IsThreeState = isChecked is null, MinWidth = 0 };
			var darkSibling = new CheckBox { IsChecked = isChecked, IsThreeState = isChecked is null, MinWidth = 0 };
			var root = new StackPanel
			{
				Children =
				{
					ambient,
					new Border { RequestedTheme = ElementTheme.Dark, Child = darkSibling },
				},
			};
			try
			{
				await UITestHelper.Load(root);
				await TestServices.WindowHelper.WaitForIdle();

				var box = ambient.FindVisualChildByName("NormalRectangle") as FrameworkElement;
				Assert.IsNotNull(box, "NormalRectangle template part not found.");

				var firstColor = await GetCenterColor(box);

				ambient.IsChecked = false;
				await TestServices.WindowHelper.WaitForIdle();
				ambient.IsChecked = isChecked;
				await TestServices.WindowHelper.WaitForIdle();

				var afterColor = await GetCenterColor(box);

				var afterIsNearWhite = afterColor.R >= 235 && afterColor.G >= 235 && afterColor.B >= 235;
				Assert.IsFalse(afterIsNearWhite,
					$"Reference (post-re-entry) checked fill {afterColor} should be the accent, not a near-white Unchecked fill.");
				Assert.IsTrue(ColorsClose(firstColor, afterColor, 24),
					$"With Dark sibling: ambient checked fill on FIRST render ({firstColor}) must match the fill after a state re-entry ({afterColor}).");
			}
			finally
			{
				TestServices.WindowHelper.WindowContent = null;
			}
		}

		private static async Task<Windows.UI.Color> GetCenterColor(FrameworkElement box)
		{
			var bmp = await UITestHelper.ScreenShot(box);
			await bmp.Populate();
			return bmp.GetPixel(bmp.Width / 2, bmp.Height / 2);
		}

		private static bool ColorsClose(Windows.UI.Color a, Windows.UI.Color b, int tolerance) =>
			System.Math.Abs(a.R - b.R) <= tolerance &&
			System.Math.Abs(a.G - b.G) <= tolerance &&
			System.Math.Abs(a.B - b.B) <= tolerance &&
			System.Math.Abs(a.A - b.A) <= tolerance;

		// kahua-private#491 (Uno #23472) ROOT CAUSE repro: a SECOND template realization (in Kahua, triggered by
		// late ambient-theme-driven Style re-resolution) detaches the first template root via
		// Control.OnTemplateChanged -> RemoveChild WITHOUT stopping that root's Filling CheckedNormal storyboard
		// or tearing down its template namescope (RemoveNameScope is commented out at Control.cs:255). The checked
		// fill ends up on the superseded root's NormalRectangle, so the LIVE (rendered) NormalRectangle is left
		// unchecked. WinUI realizes the template once and destroys the old child + namescope atomically
		// (Control.cpp:42-77). Here we force the second template application directly (a distinct Template instance)
		// and assert the live NormalRectangle carries the CheckedNormal fill.
		private const string CheckedFillTemplateXaml =
			"<ControlTemplate xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" " +
			"xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\" TargetType=\"CheckBox\">" +
			"<Grid Background=\"Transparent\">" +
			"<VisualStateManager.VisualStateGroups>" +
			"<VisualStateGroup x:Name=\"CombinedStates\">" +
			"<VisualState x:Name=\"UncheckedNormal\" />" +
			"<VisualState x:Name=\"CheckedNormal\"><Storyboard>" +
			"<ObjectAnimationUsingKeyFrames Storyboard.TargetName=\"NormalRectangle\" Storyboard.TargetProperty=\"Fill\">" +
			"<DiscreteObjectKeyFrame KeyTime=\"0:0:0\"><DiscreteObjectKeyFrame.Value>" +
			"<SolidColorBrush Color=\"Blue\" /></DiscreteObjectKeyFrame.Value></DiscreteObjectKeyFrame>" +
			"</ObjectAnimationUsingKeyFrames></Storyboard></VisualState>" +
			"</VisualStateGroup>" +
			"</VisualStateManager.VisualStateGroups>" +
			"<Rectangle x:Name=\"NormalRectangle\" Width=\"20\" Height=\"20\" Fill=\"White\" />" +
			"</Grid></ControlTemplate>";

		[TestMethod]
		[RequiresFullWindow]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.Native)]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/23472")]
		public async Task When_Checked_Fill_Survives_Template_Reapply()
		{
			ControlTemplate Template() => (ControlTemplate)Microsoft.UI.Xaml.Markup.XamlReader.Load(CheckedFillTemplateXaml);

			var checkBox = new CheckBox { IsChecked = true, Template = Template() };
			try
			{
				// First template realization + first CheckedNormal (NormalRectangle.Fill -> Blue).
				await UITestHelper.Load(checkBox, cb => cb.GetTemplateRoot() != null);
				await TestServices.WindowHelper.WaitForIdle();

				var firstBox = checkBox.FindVisualChildByName("NormalRectangle") as Microsoft.UI.Xaml.Shapes.Rectangle;
				Assert.IsNotNull(firstBox, "NormalRectangle not found after first template.");
				Assert.AreEqual(Colors.Blue, (firstBox.Fill as Microsoft.UI.Xaml.Media.SolidColorBrush)?.Color, "Sanity: first template should render the checked (Blue) fill.");

				// Force the SECOND template realization (distinct instance) -> OnTemplateChanged -> RemoveChild.
				checkBox.Template = Template();
				await TestServices.WindowHelper.WaitForIdle();
				await TestServices.WindowHelper.WaitForIdle();

				// The LIVE NormalRectangle (current template root) must carry the checked fill on first render
				// after the re-template, with NO pointer/state interaction.
				var liveBox = checkBox.FindVisualChildByName("NormalRectangle") as Microsoft.UI.Xaml.Shapes.Rectangle;
				Assert.IsNotNull(liveBox, "NormalRectangle not found after re-template.");
				Assert.IsTrue(liveBox.ActualWidth > 0, "Live NormalRectangle should be laid out (ActualWidth > 0).");
				Assert.AreEqual(Colors.Blue, (liveBox.Fill as Microsoft.UI.Xaml.Media.SolidColorBrush)?.Color,
					"After a template re-apply, the LIVE NormalRectangle must carry the CheckedNormal fill (Blue); " +
					"a non-Blue fill means the checked-state storyboard themed a superseded/orphaned template root.");
			}
			finally
			{
				TestServices.WindowHelper.WindowContent = null;
			}
		}

		// Style carrying the same Checked-fill ControlTemplate, applied via the Style DP (the implicit/Style
		// re-resolution path). Assigning it after load fires OnStyleChanged -> ApplyStyleCore (ClearInvalidProperties
		// + ApplyTo) -> Template churn -> OnTemplateChanged -> RemoveChild of the first template root, i.e. the
		// SECOND template realization driven by a Style re-resolution rather than a bare Template set.
		private const string CheckedFillStyleXaml =
			"<Style xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" " +
			"xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\" TargetType=\"CheckBox\">" +
			"<Setter Property=\"Template\"><Setter.Value>" +
			"<ControlTemplate TargetType=\"CheckBox\">" +
			"<Grid Background=\"Transparent\">" +
			"<VisualStateManager.VisualStateGroups>" +
			"<VisualStateGroup x:Name=\"CombinedStates\">" +
			"<VisualState x:Name=\"UncheckedNormal\" />" +
			"<VisualState x:Name=\"CheckedNormal\"><Storyboard>" +
			"<ObjectAnimationUsingKeyFrames Storyboard.TargetName=\"NormalRectangle\" Storyboard.TargetProperty=\"Fill\">" +
			"<DiscreteObjectKeyFrame KeyTime=\"0:0:0\"><DiscreteObjectKeyFrame.Value>" +
			"<SolidColorBrush Color=\"Blue\" /></DiscreteObjectKeyFrame.Value></DiscreteObjectKeyFrame>" +
			"</ObjectAnimationUsingKeyFrames></Storyboard></VisualState>" +
			"</VisualStateGroup>" +
			"</VisualStateManager.VisualStateGroups>" +
			"<Rectangle x:Name=\"NormalRectangle\" Width=\"20\" Height=\"20\" Fill=\"White\" />" +
			"</Grid></ControlTemplate>" +
			"</Setter.Value></Setter></Style>";

		[TestMethod]
		[RequiresFullWindow]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.Native)]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/23472")]
		public async Task When_Checked_Fill_Survives_Late_Style_Reresolution()
		{
			// The first realization comes from an explicit Template; the second is driven through the Style DP
			// (OnStyleChanged -> ApplyStyleCore), the path a late implicit/{ThemeResource} Style re-resolution takes.
			ControlTemplate FirstTemplate() => (ControlTemplate)Microsoft.UI.Xaml.Markup.XamlReader.Load(CheckedFillTemplateXaml);
			Style ReStyle() => (Style)Microsoft.UI.Xaml.Markup.XamlReader.Load(CheckedFillStyleXaml);

			var checkBox = new CheckBox { IsChecked = true, Template = FirstTemplate() };
			try
			{
				// First template realization + first CheckedNormal (NormalRectangle.Fill -> Blue).
				await UITestHelper.Load(checkBox, cb => cb.GetTemplateRoot() != null);
				await TestServices.WindowHelper.WaitForIdle();

				var firstBox = checkBox.FindVisualChildByName("NormalRectangle") as Microsoft.UI.Xaml.Shapes.Rectangle;
				Assert.IsNotNull(firstBox, "NormalRectangle not found after first template.");
				Assert.AreEqual(Colors.Blue, (firstBox.Fill as Microsoft.UI.Xaml.Media.SolidColorBrush)?.Color, "Sanity: first template should render the checked (Blue) fill.");

				// Drive the SECOND template realization through the Style DP. The Style's Template overrides the
				// explicit one (ClearInvalidProperties clears the local Template, ApplyTo applies the Style's),
				// churning Control.TemplateProperty -> OnTemplateChanged -> RemoveChild of the first root.
				checkBox.ClearValue(Control.TemplateProperty);
				checkBox.Style = ReStyle();
				await TestServices.WindowHelper.WaitForIdle();
				await TestServices.WindowHelper.WaitForIdle();

				// The LIVE NormalRectangle (current template root) must carry the checked fill on first render
				// after the re-template, with NO pointer/state interaction.
				var liveBox = checkBox.FindVisualChildByName("NormalRectangle") as Microsoft.UI.Xaml.Shapes.Rectangle;
				Assert.IsNotNull(liveBox, "NormalRectangle not found after late Style re-template.");
				Assert.IsTrue(liveBox.ActualWidth > 0, "Live NormalRectangle should be laid out (ActualWidth > 0).");
				Assert.AreEqual(Colors.Blue, (liveBox.Fill as Microsoft.UI.Xaml.Media.SolidColorBrush)?.Color,
					"After a late Style re-resolution re-template, the LIVE NormalRectangle must carry the CheckedNormal fill (Blue); " +
					"a non-Blue fill means the checked-state storyboard themed a superseded/orphaned template root.");
			}
			finally
			{
				TestServices.WindowHelper.WindowContent = null;
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
