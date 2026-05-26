using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using MUXControlsTestApp.Utilities;
using Uno.UI.Extensions;
using Uno.UI.RuntimeTests.Helpers;
using Windows.UI;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml;

/// <summary>
/// Regression repros for the WinUI theming-alignment refactor (scenarios S1–S5 and the
/// public app-dark-switch regression). Each test encodes <b>correct WinUI behavior</b>:
/// a value resolved through <c>{ThemeResource}</c> is a pure function of (key, the resolving
/// owner's effective theme), where the owner's theme is established at tree-<c>Enter</c> from
/// its (logical) inheritance parent — never from a process-global ambient.
///
/// <para><b>Polarity (important).</b> The reported defects all share one shape: the ambient
/// OS theme is <b>Dark</b> while the relevant subtree is themed <b>Light</b> at the
/// <i>element</i> level (the real app sets <c>root.RequestedTheme = Light</c>, not
/// <c>Application.RequestedTheme</c>, so the app follows the OS). A materialized / recycled /
/// scrolled-in / first-opened element with no governing theme walk then resolves the global
/// ambient (Dark) instead of its own Light theme. These repros reproduce exactly that:
/// a <b>Light element-level island under a (simulated) Dark ambient</b>, asserting the
/// materialized child resolves <b>Light</b>.</para>
///
/// <para><b>Determinism.</b> The ambient OS theme is pinned via
/// <see cref="ThemeHelper.UseSystemThemeOverride"/> so the repros are RED on current master on
/// <i>any</i> machine OS theme — not only when the developer's OS happens to be Dark (which is
/// why the existing theming suite "passes" on a Light OS and fails on a Dark OS today).</para>
///
/// Authoring notes:
/// - Deterministic sentinel brushes via local <c>ThemeDictionaries["Light"]/["Dark"]/["Default"]</c>
///   (Light = #FF111111, Dark = #FFEEEEEE), asserting exact <see cref="Color"/> values.
/// - The outer region is themed <c>RequestedTheme="Dark"</c> so the test also exercises a real
///   "Light island inside a Dark region" boundary on native WinUI (where the Uno-only ambient
///   override compiles out and the app is Light by default) — keeping the test a valid oracle.
/// - This class intentionally does NOT inherit <see cref="Given_ElementTheme"/>'s class-level
///   native exclusion; instead every element-level theme test is excluded on native per-method
///   (<c>[PlatformCondition(Exclude, NativeAndroid | NativeIOS)]</c>). Element-level theming
///   (incl. the popup/flyout first-open tests T4/T5) is a Skia/WASM feature — native targets
///   support OS + application theme only (plan.md Phase 7) — so these repros do not apply to
///   iOS/Android. The per-method form (vs a blanket class exclusion) keeps each test's native
///   scope explicit at the test site.
/// </summary>
[TestClass]
[RunsOnUIThread]
public class Given_Theme_Materialization
{
	// Sentinel colors (visually unambiguous; do not collide with system brushes).
	private static readonly Color LightSentinel = Color.FromArgb(0xFF, 0x11, 0x11, 0x11);
	private static readonly Color DarkSentinel = Color.FromArgb(0xFF, 0xEE, 0xEE, 0xEE);

	private const string SentinelDictsXaml =
		"""
		<ResourceDictionary.ThemeDictionaries>
			<ResourceDictionary x:Key="Light">
				<SolidColorBrush x:Key="SentinelBrush" Color="#FF111111" />
			</ResourceDictionary>
			<ResourceDictionary x:Key="Dark">
				<SolidColorBrush x:Key="SentinelBrush" Color="#FFEEEEEE" />
			</ResourceDictionary>
			<ResourceDictionary x:Key="Default">
				<SolidColorBrush x:Key="SentinelBrush" Color="#FFEEEEEE" />
			</ResourceDictionary>
		</ResourceDictionary.ThemeDictionaries>
		""";

	private static Color? ColorOf(object element)
		=> (element as Border)?.Background is SolidColorBrush b ? b.Color : null;

	private static Color? ForegroundOf(object element)
		=> (element as TextBlock)?.Foreground is SolidColorBrush b ? b.Color : null;

	// ---- T1 — S1: virtualized list item in a Light island under a Dark ambient resolves Light ----

	[TestMethod]
	[RequiresFullWindow]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeAndroid | RuntimeTestPlatforms.NativeIOS)]
	public async Task When_Virtualized_Item_In_Light_Island_Under_Dark_Ambient_Resolves_Light()
	{
		// S1. A ListView item realized (initially and after ScrollIntoView) inside a
		// RequestedTheme="Light" island must resolve the Light sentinel even though the ambient OS
		// theme is Dark. WinUI: the container inherits Light at Enter → Light. Uno today: items
		// realized outside the theme walk read the process-global ambient (Dark) → leak.
#if HAS_UNO
		using var _ = ThemeHelper.UseSystemThemeOverride(ApplicationTheme.Dark);
		await WindowHelper.WaitForIdle();
#endif
		var root = (Grid)XamlReader.Load(
			$$"""
			<Grid xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
			      xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
			      RequestedTheme="Dark">
				<Border RequestedTheme="Light">
					<Border.Resources>
						<ResourceDictionary>
							{{SentinelDictsXaml}}
						</ResourceDictionary>
					</Border.Resources>
					<ListView x:Name="list" Height="300" Width="200">
						<ListView.ItemTemplate>
							<DataTemplate>
								<Border x:Name="cell" Height="40" Background="{ThemeResource SentinelBrush}" />
							</DataTemplate>
						</ListView.ItemTemplate>
					</ListView>
				</Border>
			</Grid>
			""");

		var list = (ListView)root.FindName("list");
		list.ItemsSource = Enumerable.Range(0, 200).Select(i => $"Item {i}").ToArray();

		WindowHelper.WindowContent = root;
		await WindowHelper.WaitForLoaded(root);
		await WindowHelper.WaitForIdle();

		var first = await WindowHelper.WaitForNonNull(() => list.ContainerFromIndex(0) as ListViewItem);
		var firstCell = first.FindFirstDescendant<Border>("cell");
		Assert.AreEqual(LightSentinel, ColorOf(firstCell),
			"Initially-realized item in a Light island should resolve Light, not the Dark ambient.");

		list.ScrollIntoView(list.Items[150]);
		await WindowHelper.WaitForIdle();

		var scrolled = await WindowHelper.WaitForNonNull(() => list.ContainerFromIndex(150) as ListViewItem);
		var scrolledCell = scrolled.FindFirstDescendant<Border>("cell");
		Assert.AreEqual(LightSentinel, ColorOf(scrolledCell),
			"Item realized after ScrollIntoView should also resolve the Light island sentinel.");
	}

	// ---- T2 — S2: list item recycled across unload/reload keeps its theme (D4) ----

	[TestMethod]
	[RequiresFullWindow]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeAndroid | RuntimeTestPlatforms.NativeIOS)]
	public async Task When_Item_Recycled_Across_Unload_Reload_Keeps_Light()
	{
		// S2. A row in a Light island is realized, the island is unloaded (tab switch / recycle),
		// then reloaded. After reload the row must still resolve the Light sentinel. Earlier Uno
		// cleared the element theme on unload (ClearThemeStateOnUnloaded), so a reloaded row could
		// resolve the Dark ambient; like WinUI, the per-object theme is now kept across unload and
		// re-Enter re-themes from the parent (D2/D4), so the row stays Light.
#if HAS_UNO
		using var _ = ThemeHelper.UseSystemThemeOverride(ApplicationTheme.Dark);
		await WindowHelper.WaitForIdle();
#endif
		var host = new Border { Width = 220, Height = 320, RequestedTheme = ElementTheme.Dark };
		var island = (Border)XamlReader.Load(
			$$"""
			<Border xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
			        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
			        RequestedTheme="Light">
				<Border.Resources>
					<ResourceDictionary>
						{{SentinelDictsXaml}}
					</ResourceDictionary>
				</Border.Resources>
				<ListView x:Name="list" Height="300" Width="200">
					<ListView.ItemTemplate>
						<DataTemplate>
							<Border x:Name="cell" Height="40" Background="{ThemeResource SentinelBrush}" />
						</DataTemplate>
					</ListView.ItemTemplate>
				</ListView>
			</Border>
			""");
		var list = (ListView)island.FindName("list");
		list.ItemsSource = Enumerable.Range(0, 20).Select(i => $"Item {i}").ToArray();

		host.Child = island;
		WindowHelper.WindowContent = host;
		await WindowHelper.WaitForLoaded(island);
		await WindowHelper.WaitForIdle();

		var firstCell = (await WindowHelper.WaitForNonNull(() => list.ContainerFromIndex(0) as ListViewItem))
			.FindFirstDescendant<Border>("cell");
		Assert.AreEqual(LightSentinel, ColorOf(firstCell), "Row should resolve Light before recycle.");

		// Unload the island (simulates tab navigation away), then reload it.
		host.Child = null;
		await WindowHelper.WaitForIdle();
		host.Child = island;
		await WindowHelper.WaitForLoaded(island);
		await WindowHelper.WaitForIdle();

		var reloadedCell = (await WindowHelper.WaitForNonNull(() => list.ContainerFromIndex(0) as ListViewItem))
			.FindFirstDescendant<Border>("cell");
		Assert.AreEqual(LightSentinel, ColorOf(reloadedCell),
			"Row should still resolve the Light island sentinel after unload/reload (no recycle staleness leaking the Dark ambient).");
	}

	// ---- T3 — S3: nested-template cell materialized in a Light island under a Dark ambient ----

	[TestMethod]
	[RequiresFullWindow]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeAndroid | RuntimeTestPlatforms.NativeIOS)]
	public async Task When_Nested_Template_Cell_Scrolled_Into_View_Resolves_Light()
	{
		// S3. A cell materialized on scroll — through a nested ContentControl template, like a data
		// grid cell — inside a Light island must resolve the Light sentinel under a Dark ambient.
		// Targets the "columns scrolled into view use dark styling" case where deep template
		// materialization happens outside the theme walk.
#if HAS_UNO
		using var _ = ThemeHelper.UseSystemThemeOverride(ApplicationTheme.Dark);
		await WindowHelper.WaitForIdle();
#endif
		var root = (Grid)XamlReader.Load(
			$$"""
			<Grid xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
			      xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
			      RequestedTheme="Dark">
				<Border RequestedTheme="Light">
					<Border.Resources>
						<ResourceDictionary>
							{{SentinelDictsXaml}}
						</ResourceDictionary>
					</Border.Resources>
					<ListView x:Name="list" Height="300" Width="200">
						<ListView.ItemTemplate>
							<DataTemplate>
								<ContentControl HorizontalContentAlignment="Stretch">
									<ContentControl.ContentTemplate>
										<DataTemplate>
											<Border x:Name="cell" Height="40" Background="{ThemeResource SentinelBrush}" />
										</DataTemplate>
									</ContentControl.ContentTemplate>
								</ContentControl>
							</DataTemplate>
						</ListView.ItemTemplate>
					</ListView>
				</Border>
			</Grid>
			""");
		var list = (ListView)root.FindName("list");
		list.ItemsSource = Enumerable.Range(0, 200).Select(i => $"Item {i}").ToArray();

		WindowHelper.WindowContent = root;
		await WindowHelper.WaitForLoaded(root);
		await WindowHelper.WaitForIdle();

		list.ScrollIntoView(list.Items[150]);
		await WindowHelper.WaitForIdle();

		var scrolled = await WindowHelper.WaitForNonNull(() => list.ContainerFromIndex(150) as ListViewItem);
		var scrolledCell = scrolled.FindFirstDescendant<Border>("cell");
		Assert.AreEqual(LightSentinel, ColorOf(scrolledCell),
			"Nested-template cell materialized on scroll in a Light island should resolve Light, not the Dark ambient.");
	}

	// ---- T4 — S4: flyout first open from a Light region uses that region's theme ----

	[TestMethod]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeAndroid | RuntimeTestPlatforms.NativeIOS)]
	public async Task When_Flyout_First_Open_From_Light_Region_Uses_Region_Theme()
	{
		// S4. A flyout opened from a Light island must show Light content on the FIRST open under a
		// Dark ambient (today it heals only on the second open). Asserts the resolved sentinel value,
		// not just ActualTheme, and that first-open == second-open.
		// Excluded on native: element-level theme inheritance is a Skia/WASM feature — native targets
		// support OS + application theme only (the flyout follows the app/OS theme there).
#if HAS_UNO
		using var _ = ThemeHelper.UseSystemThemeOverride(ApplicationTheme.Dark);
		await WindowHelper.WaitForIdle();
#endif
		var root = (Grid)XamlReader.Load(
			$$"""
			<Grid xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
			      xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
			      RequestedTheme="Light">
				<Button x:Name="target" Content="Open">
					<FlyoutBase.AttachedFlyout>
						<Flyout>
							<Border x:Name="flyoutCell" Width="60" Height="60"
							        Background="{ThemeResource SentinelBrush}">
								<Border.Resources>
									<ResourceDictionary>
										{{SentinelDictsXaml}}
									</ResourceDictionary>
								</Border.Resources>
							</Border>
						</Flyout>
					</FlyoutBase.AttachedFlyout>
				</Button>
			</Grid>
			""");
		var target = (Button)root.FindName("target");
		var flyout = (Flyout)FlyoutBase.GetAttachedFlyout(target);

		WindowHelper.WindowContent = root;
		await WindowHelper.WaitForLoaded(target);

		try
		{
			FlyoutBase.ShowAttachedFlyout(target);
			await WindowHelper.WaitForIdle();
			var firstOpen = ColorOf(flyout.Content);
			Assert.AreEqual(LightSentinel, firstOpen,
				"Flyout content should resolve the Light region sentinel on the FIRST open.");

			// Close and reopen — the value must be identical (no "fix on second open").
			flyout.Hide();
			await WindowHelper.WaitForIdle();
			FlyoutBase.ShowAttachedFlyout(target);
			await WindowHelper.WaitForIdle();
			Assert.AreEqual(firstOpen, ColorOf(flyout.Content),
				"Flyout content should resolve the same sentinel on the second open.");
		}
		finally
		{
			flyout.Hide();
			await WindowHelper.WaitForIdle();
		}
	}

	// ---- T5 — S4: popup first open inherits the opener's theme ----

	[TestMethod]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeAndroid | RuntimeTestPlatforms.NativeIOS)]
	public async Task When_Popup_First_Open_In_Light_Region_Has_Region_Theme()
	{
		// S4 (isolated popup path). A bare Popup whose child binds the sentinel must resolve the
		// opener island's Light theme on the FIRST open under a Dark ambient.
		// Excluded on native: element-level theme inheritance is a Skia/WASM feature — native targets
		// support OS + application theme only (the popup child follows the app/OS theme there).
#if HAS_UNO
		using var _ = ThemeHelper.UseSystemThemeOverride(ApplicationTheme.Dark);
		await WindowHelper.WaitForIdle();
#endif
		var root = (Grid)XamlReader.Load(
			$$"""
			<Grid xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
			      xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
			      Width="200" Height="200"
			      RequestedTheme="Light">
				<Popup x:Name="popup">
					<Border x:Name="popupCell" Width="60" Height="60"
					        Background="{ThemeResource SentinelBrush}">
						<Border.Resources>
							<ResourceDictionary>
								{{SentinelDictsXaml}}
							</ResourceDictionary>
						</Border.Resources>
					</Border>
				</Popup>
			</Grid>
			""");
		var popup = (Popup)root.FindName("popup");
		var cell = (Border)root.FindName("popupCell");

		WindowHelper.WindowContent = root;
		await WindowHelper.WaitForLoaded(root);

		try
		{
			popup.IsOpen = true;
			await WindowHelper.WaitForIdle();
			Assert.AreEqual(LightSentinel, ColorOf(cell),
				"Popup child should resolve the Light opener sentinel on the FIRST open.");
		}
		finally
		{
			popup.IsOpen = false;
			await WindowHelper.WaitForIdle();
		}
	}

	// ---- T6 — S5: control added at runtime into a Light island resolves the island theme ----

	[TestMethod]
	[RequiresFullWindow]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeAndroid | RuntimeTestPlatforms.NativeIOS)]
	public async Task When_Control_Added_At_Runtime_Into_Light_Island_Resolves_Light()
	{
		// S5. A control created and added at runtime into an already-loaded RequestedTheme="Light"
		// parent must resolve the Light sentinel under a Dark ambient. Also covers D1: a non-FE
		// DependencyObject ({ThemeResource} on a Brush/DO) resolves the subtree theme.
#if HAS_UNO
		using var _ = ThemeHelper.UseSystemThemeOverride(ApplicationTheme.Dark);
		await WindowHelper.WaitForIdle();
#endif
		var parent = (Grid)XamlReader.Load(
			$$"""
			<Grid xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
			      xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
			      RequestedTheme="Light">
				<Grid.Resources>
					<ResourceDictionary>
						{{SentinelDictsXaml}}
					</ResourceDictionary>
				</Grid.Resources>
			</Grid>
			""");

		WindowHelper.WindowContent = parent;
		await WindowHelper.WaitForLoaded(parent);
		await WindowHelper.WaitForIdle();

		// Add a real control AFTER load.
		var added = (Border)XamlReader.Load(
			"""
			<Border xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
			        x:Name="added" Width="60" Height="60"
			        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
			        Background="{ThemeResource SentinelBrush}" />
			""");
		parent.Children.Add(added);
		await WindowHelper.WaitForLoaded(added);
		await WindowHelper.WaitForIdle();

		Assert.AreEqual(LightSentinel, ColorOf(added),
			"Control added at runtime into a Light island should resolve the Light sentinel, not the Dark ambient.");
	}

	// ---- T7 — public regression guard: app theme switch updates ThemeResource values ----

	[TestMethod]
	[RequiresFullWindow]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeAndroid | RuntimeTestPlatforms.NativeIOS)]
	public async Task When_App_Theme_Switches_ThemeResource_Values_Update()
	{
		// Public app-dark-switch regression guard. Switching app Light→Dark must flip bound
		// {ThemeResource} values (and nested elements). (Largely fixed already; this guards against
		// regression.)
#if HAS_UNO
		using var _ = ThemeHelper.UseApplicationLightTheme();
		await WindowHelper.WaitForIdle();

		var root = (StackPanel)XamlReader.Load(
			$$"""
			<StackPanel xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
			            xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
				<StackPanel.Resources>
					<ResourceDictionary>
						{{SentinelDictsXaml}}
					</ResourceDictionary>
				</StackPanel.Resources>
				<Border x:Name="outer" Width="50" Height="50" Background="{ThemeResource SentinelBrush}">
					<Border x:Name="inner" Background="{ThemeResource SentinelBrush}" />
				</Border>
			</StackPanel>
			""");
		var outer = (Border)root.FindName("outer");
		var inner = (Border)root.FindName("inner");

		WindowHelper.WindowContent = root;
		await WindowHelper.WaitForLoaded(root);
		await WindowHelper.WaitForIdle();

		Assert.AreEqual(LightSentinel, ColorOf(outer), "Should start Light.");
		Assert.AreEqual(LightSentinel, ColorOf(inner), "Nested should start Light.");

		using (ThemeHelper.UseApplicationDarkTheme())
		{
			await WindowHelper.WaitForIdle();
			Assert.AreEqual(DarkSentinel, ColorOf(outer), "Outer should flip to Dark.");
			Assert.AreEqual(DarkSentinel, ColorOf(inner), "Nested should flip to Dark.");
		}
#else
		await Task.CompletedTask;
		Assert.Inconclusive("App-level theme switching is Uno-only; confirmed in WinUI probe app.");
#endif
	}

	// ---- T8 — inherited foreground frozen at a theme boundary ----

	[TestMethod]
	[RequiresFullWindow]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeAndroid | RuntimeTestPlatforms.NativeIOS)]
	public async Task When_Inherited_Foreground_At_Theme_Boundary_Stays_Boundary_Theme()
	{
		// General correctness (foreground-freeze emulation) underpinning the S1/S2 text symptoms.
		// A TextBlock with no local Foreground inside a RequestedTheme="Light" boundary must use
		// the Light default text brush even when the ambient OS theme is Dark.
#if HAS_UNO
		using var _ = ThemeHelper.UseSystemThemeOverride(ApplicationTheme.Dark);
		await WindowHelper.WaitForIdle();
#endif
		var root = (Border)XamlReader.Load(
			"""
			<Border xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
			        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
			        RequestedTheme="Light">
				<TextBlock x:Name="text" Text="boundary" />
			</Border>
			""");
		var text = (TextBlock)root.FindName("text");

		// Reference Light default text color.
		var lightRef = (TextBlock)XamlReader.Load(
			"""
			<TextBlock xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
			           RequestedTheme="Light" Text="ref" />
			""");
		WindowHelper.WindowContent = lightRef;
		await WindowHelper.WaitForLoaded(lightRef);
		var expectedLight = ForegroundOf(lightRef);

		WindowHelper.WindowContent = root;
		await WindowHelper.WaitForLoaded(root);
		await WindowHelper.WaitForIdle();

		Assert.AreEqual(expectedLight, ForegroundOf(text),
			"TextBlock in a Light boundary should use the Light default foreground regardless of the Dark ambient.");
	}

	// ---- T9 — explicit app theme suppresses OS following (Uno-only; Phase 6) ----

#if HAS_UNO
	[TestMethod]
	[RequiresFullWindow]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeAndroid | RuntimeTestPlatforms.NativeIOS)]
	public async Task When_App_Theme_Explicit_OS_Change_Is_Suppressed()
	{
		// OS/theme-leak narrative. With an explicit app theme (Light), a simulated OS switch to
		// Dark must NOT flip the app, and bound {ThemeResource} values stay Light.
		// (Uno-only API surface; confirmed in WinUI probe app: Application.RequestedTheme="Light"
		// wins over the OS dark setting.)
		using var _ = ThemeHelper.UseApplicationLightTheme();
		await WindowHelper.WaitForIdle();

		Assert.IsTrue(Application.Current.IsThemeSetExplicitly, "App theme should be explicit.");
		Assert.AreEqual(ElementTheme.Light, Application.Current.ActualElementTheme, "App should be Light.");

		var root = (Border)XamlReader.Load(
			$$"""
			<Border xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
			        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
			        Width="50" Height="50" Background="{ThemeResource SentinelBrush}">
				<Border.Resources>
					<ResourceDictionary>
						{{SentinelDictsXaml}}
					</ResourceDictionary>
				</Border.Resources>
			</Border>
			""");
		WindowHelper.WindowContent = root;
		await WindowHelper.WaitForLoaded(root);

		Assert.AreEqual(LightSentinel, ColorOf(root), "Value should be Light before the OS change.");

		// Simulate an OS theme change to Dark via the system-theme override (raises the same path a
		// real OS change would).
		using (ThemeHelper.UseSystemThemeOverride(ApplicationTheme.Dark))
		{
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(ElementTheme.Light, Application.Current.ActualElementTheme,
				"Explicit app theme must not follow the OS to Dark.");
			Assert.AreEqual(LightSentinel, ColorOf(root), "Bound value should stay Light.");
		}
	}
#endif

	// ---- T10 — custom-theme ditched + theme-dictionary fallback (Uno-only; Phase 6) ----

#if HAS_UNO
	[TestMethod]
	[RequiresFullWindow]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeAndroid | RuntimeTestPlatforms.NativeIOS)]
	public async Task When_Element_Dark_Island_And_Fallback_Does_Not_Leak_Dark()
	{
		// Decision = DITCH custom themes (Phase 6). (a) Element RequestedTheme="Dark" under app Light
		// resolves the standard Dark sentinel. (b) A dictionary defining only a Dark "Default" entry,
		// consumed under app Light, must resolve the app base Light value, not the dark "Default".
		using var _ = ThemeHelper.UseApplicationLightTheme();
		await WindowHelper.WaitForIdle();

		// (a) element Dark island under app Light → standard Dark sentinel.
		var island = (Border)XamlReader.Load(
			$$"""
			<Border xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
			        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
			        RequestedTheme="Dark" Width="50" Height="50" Background="{ThemeResource SentinelBrush}">
				<Border.Resources>
					<ResourceDictionary>
						{{SentinelDictsXaml}}
					</ResourceDictionary>
				</Border.Resources>
			</Border>
			""");
		WindowHelper.WindowContent = island;
		await WindowHelper.WaitForLoaded(island);
		Assert.AreEqual(DarkSentinel, ColorOf(island),
			"Element RequestedTheme=Dark under app Light should resolve the standard Dark sentinel.");

		// (b) fallback robustness: a dictionary with only a Dark "Default" entry, consumed under
		// app Light, must NOT silently resolve the dark "Default".
		var fallback = (Border)XamlReader.Load(
			"""
			<Border xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
			        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
			        Width="50" Height="50" Background="{ThemeResource FallbackBrush}">
				<Border.Resources>
					<ResourceDictionary>
						<ResourceDictionary.ThemeDictionaries>
							<ResourceDictionary x:Key="Default">
								<SolidColorBrush x:Key="FallbackBrush" Color="#FFEEEEEE" />
							</ResourceDictionary>
							<ResourceDictionary x:Key="Light">
								<SolidColorBrush x:Key="FallbackBrush" Color="#FF111111" />
							</ResourceDictionary>
						</ResourceDictionary.ThemeDictionaries>
					</ResourceDictionary>
				</Border.Resources>
			</Border>
			""");
		WindowHelper.WindowContent = fallback;
		await WindowHelper.WaitForLoaded(fallback);
		Assert.AreEqual(LightSentinel, ColorOf(fallback),
			"Under app Light, resolution should use the Light entry, not the dark Default fallback.");
	}

	// ---- D7 — the app-level custom-theme axis is ditched (Uno-only; Phase 6) ----

#if HAS_UNO
	[TestMethod]
	[RequiresFullWindow]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeAndroid | RuntimeTestPlatforms.NativeIOS)]
	public async Task When_Custom_Theme_Name_Is_Ditched_Resolves_Standard()
	{
		// Decision = DITCH custom themes (custom-theme.md → Option B). A non-Light/Dark custom name must no
		// longer become the active theme key (Themes.Active is strictly Light/Dark, +HC), so it can never
		// select a custom ThemeDictionaries["Foo"] entry nor fall back to the dark "Default". A "Light"/
		// "Dark" custom name is the harmless bucket and still resolves the standard theme. The standard
		// per-element resolution (element RequestedTheme=Light/Dark) is unaffected.
		// (Before the ditch, setting RequestedCustomTheme made GetActiveTheme() that arbitrary name; this
		// test is RED on that code and GREEN after the ditch — confirmed in WinUI probe app: WinUI has no
		// custom-theme axis, the application theme is strictly Light/Dark.)
		using var _ = ThemeHelper.UseApplicationLightTheme();
		await WindowHelper.WaitForIdle();

		try
		{
#pragma warning disable CS0618 // RequestedCustomTheme is obsolete: assert it is now a no-op.
			Uno.UI.ApplicationHelper.RequestedCustomTheme = "UnoBrandCustom";
#pragma warning restore CS0618
			await WindowHelper.WaitForIdle();

			// (a) The active theme key stays the standard app theme (Light) — the custom name is ignored.
			Assert.AreEqual("Light", ResourceDictionary.GetActiveTheme().Key,
				"A non-Light/Dark custom name must not become the active theme key (custom axis ditched).");

			// (b) A standard sentinel dictionary, consumed under app Light with a custom name set, resolves
			// the Light sentinel — not the dark "Default" the old custom axis fell back to for a missing key.
			var standard = (Border)XamlReader.Load(
				$$"""
				<Border xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
				        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
				        Width="50" Height="50" Background="{ThemeResource SentinelBrush}">
					<Border.Resources>
						<ResourceDictionary>
							{{SentinelDictsXaml}}
						</ResourceDictionary>
					</Border.Resources>
				</Border>
				""");
			WindowHelper.WindowContent = standard;
			await WindowHelper.WaitForLoaded(standard);
			Assert.AreEqual(LightSentinel, ColorOf(standard),
				"With a custom name set, resolution stays the standard app Light theme (not the dark Default).");

			// (c) An element RequestedTheme="Dark" island still resolves the standard Dark dictionary while a
			// custom name is set — element theme is strictly Light/Dark and the custom axis never participates.
			var darkIsland = (Border)XamlReader.Load(
				$$"""
				<Border xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
				        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
				        RequestedTheme="Dark" Width="50" Height="50" Background="{ThemeResource SentinelBrush}">
					<Border.Resources>
						<ResourceDictionary>
							{{SentinelDictsXaml}}
						</ResourceDictionary>
					</Border.Resources>
				</Border>
				""");
			WindowHelper.WindowContent = darkIsland;
			await WindowHelper.WaitForLoaded(darkIsland);
			Assert.AreEqual(DarkSentinel, ColorOf(darkIsland),
				"Element RequestedTheme=Dark resolves the standard Dark dictionary regardless of any custom name.");

			// (d) Harmless bucket: a "Light" custom name under app Light still resolves the standard theme.
#pragma warning disable CS0618
			Uno.UI.ApplicationHelper.RequestedCustomTheme = "Light";
#pragma warning restore CS0618
			await WindowHelper.WaitForIdle();
			Assert.AreEqual("Light", ResourceDictionary.GetActiveTheme().Key,
				"A \"Light\" custom name still resolves the standard Light theme (harmless bucket).");
		}
		finally
		{
#pragma warning disable CS0618
			Uno.UI.ApplicationHelper.RequestedCustomTheme = null;
#pragma warning restore CS0618
		}
	}
#endif

	// ---- D8 — high-contrast composition selects the HC sub-dictionary (Uno-only; Phase 6) ----

#if HAS_UNO
	[TestMethod]
	[RequiresFullWindow]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeAndroid | RuntimeTestPlatforms.NativeIOS)]
	public async Task When_HighContrast_Active_Selects_HighContrast_Dictionary()
	{
		// D8 (Phase 6): high contrast is an OS/app-global dimension OR-ed onto the base theme. When HC is
		// active, a {ThemeResource} whose dictionary defines a HighContrast sub-dictionary resolves the HC
		// value — the HC sub-dictionary is selected ahead of Light/Dark — matching WinUI's
		// EnsureActiveThemeDictionary (Resources.cpp:718-758). Here app Light maps to "HighContrastWhite"
		// first, then the generic "HighContrast" key defined by this dictionary. (Uno-only HC override via
		// the accessibility settings; confirmed in WinUI probe app: a HighContrast ThemeDictionaries entry
		// is used when the OS high-contrast feature is on.)
		var hcSentinel = Color.FromArgb(0xFF, 0x00, 0xFF, 0x00);

		using var lightApp = ThemeHelper.UseApplicationLightTheme();
		await WindowHelper.WaitForIdle();

		// Activate high contrast BEFORE the content loads so its first resolution selects the HC dictionary.
		Uno.WinRTFeatureConfiguration.Accessibility.HighContrast = true;
		try
		{
			await WindowHelper.WaitForIdle();

			var root = (Border)XamlReader.Load(
				"""
				<Border xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
				        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
				        Width="50" Height="50" Background="{ThemeResource SentinelBrush}">
					<Border.Resources>
						<ResourceDictionary>
							<ResourceDictionary.ThemeDictionaries>
								<ResourceDictionary x:Key="Light">
									<SolidColorBrush x:Key="SentinelBrush" Color="#FF111111" />
								</ResourceDictionary>
								<ResourceDictionary x:Key="Dark">
									<SolidColorBrush x:Key="SentinelBrush" Color="#FFEEEEEE" />
								</ResourceDictionary>
								<ResourceDictionary x:Key="HighContrast">
									<SolidColorBrush x:Key="SentinelBrush" Color="#FF00FF00" />
								</ResourceDictionary>
							</ResourceDictionary.ThemeDictionaries>
						</ResourceDictionary>
					</Border.Resources>
				</Border>
				""");
			WindowHelper.WindowContent = root;
			await WindowHelper.WaitForLoaded(root);

			Assert.AreEqual(hcSentinel, ColorOf(root),
				"With high contrast active, the HighContrast sub-dictionary must be selected ahead of Light/Dark.");
		}
		finally
		{
			Uno.WinRTFeatureConfiguration.Accessibility.HighContrast = false;
			await WindowHelper.WaitForIdle();
		}
	}
#endif

	// ---- §B leak-check guard — no global theme stack reintroduced (Phase 4) ----

	[TestMethod]
	public void When_Phase4_Global_Theme_Stack_Removed_Guard()
	{
		// tests.md §B: Phase 4 deleted the process-global requested-theme stack and the band-aid push API.
		// This reflection guard fails if PushRequestedThemeForSubTree (or the _requestedThemeForSubTree stack)
		// is reintroduced — preventing a regression back to global-ambient theme selection. Sibling/context
		// isolation without the stack is covered by Given_ElementTheme's "Context Isolation" tests; non-FE
		// owner theming by When_ThemeResource_On_NonFE_DependencyObject_*.
		const BindingFlags allStatic = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
		const BindingFlags allMembers = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;

		var resourceDictionary = typeof(ResourceDictionary);
		Assert.IsNull(resourceDictionary.GetMethod("PushRequestedThemeForSubTree", allStatic),
			"ResourceDictionary.PushRequestedThemeForSubTree must not be reintroduced (Phase 4 removed the global theme stack).");
		Assert.IsNull(resourceDictionary.GetMethod("PushRequestedThemeForSubTreeByName", allStatic),
			"ResourceDictionary.PushRequestedThemeForSubTreeByName must not be reintroduced.");

		var themes = resourceDictionary.GetNestedType("Themes", BindingFlags.NonPublic);
		Assert.IsNotNull(themes, "ResourceDictionary.Themes should still exist (it holds the app-level Active theme).");
		Assert.IsNull(themes.GetField("_requestedThemeForSubTree", allMembers),
			"Themes._requestedThemeForSubTree stack must not be reintroduced.");
	}
#endif
}
