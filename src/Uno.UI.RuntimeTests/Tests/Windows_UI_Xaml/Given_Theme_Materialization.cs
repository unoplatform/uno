using System;
using System.Linq;
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
/// These tests target the materialization paths that today's "push the theme at the
/// materialization site" band-aids miss (virtualized realization, recycle, runtime-add,
/// popup/flyout first-open). They are expected to be GREEN on native WinUI and (today) RED
/// on Uno; later phases turn them green.
///
/// Authoring notes:
/// - Deterministic sentinel brushes via local <c>ThemeDictionaries["Light"]/["Dark"]</c>
///   (Light = #FF111111, Dark = #FFEEEEEE), asserting exact <see cref="Color"/> values.
/// - This class intentionally does NOT inherit <see cref="Given_ElementTheme"/>'s
///   native-platform exclusion, so the popup/flyout first-open tests (S4) run on iOS/Android.
/// - App/OS-theme-only behaviors use <c>ThemeHelper.UseApplication*Theme</c> (Uno-only) under
///   <c>#if HAS_UNO</c>; on WinUI those pins compile out (the app is Light by default) so the
///   test still runs as the WinUI oracle wherever its assertions are WinUI-portable.
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

	// ---- T1 — S1: virtualized list item in a themed island resolves the island theme ----

	[TestMethod]
	[RequiresFullWindow]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeAndroid | RuntimeTestPlatforms.NativeIOS)]
	public async Task When_Virtualized_Item_In_Themed_Subtree_Resolves_Subtree_Theme()
	{
		// S1. A ListView item realized (initially and after ScrollIntoView) inside a
		// RequestedTheme="Dark" island must resolve the Dark sentinel, regardless of the
		// ambient app theme. WinUI: the container inherits Dark at Enter → Dark. Uno today:
		// items realized outside the theme walk read the process-global ambient (app Light).
#if HAS_UNO
		using var _ = ThemeHelper.UseApplicationLightTheme();
		await WindowHelper.WaitForIdle();
#endif
		var root = (Grid)XamlReader.Load(
			$$"""
			<Grid xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
			      xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
				<Grid.Resources>
					<ResourceDictionary>
						{{SentinelDictsXaml}}
					</ResourceDictionary>
				</Grid.Resources>
				<ListView x:Name="list" RequestedTheme="Dark" Height="300" Width="200">
					<ListView.ItemTemplate>
						<DataTemplate>
							<Border x:Name="cell" Height="40" Background="{ThemeResource SentinelBrush}" />
						</DataTemplate>
					</ListView.ItemTemplate>
				</ListView>
			</Grid>
			""");

		var list = (ListView)root.FindName("list");
		list.ItemsSource = Enumerable.Range(0, 200).Select(i => $"Item {i}").ToArray();

		WindowHelper.WindowContent = root;
		await WindowHelper.WaitForLoaded(root);
		await WindowHelper.WaitForIdle();

		var first = await WindowHelper.WaitForNonNull(() => list.ContainerFromIndex(0) as ListViewItem);
		var firstCell = first.FindFirstDescendant<Border>("cell");
		Assert.AreEqual(DarkSentinel, ColorOf(firstCell),
			"Initially-realized item should resolve the Dark island sentinel.");

		list.ScrollIntoView(list.Items[150]);
		await WindowHelper.WaitForIdle();

		var scrolled = await WindowHelper.WaitForNonNull(() => list.ContainerFromIndex(150) as ListViewItem);
		var scrolledCell = scrolled.FindFirstDescendant<Border>("cell");
		Assert.AreEqual(DarkSentinel, ColorOf(scrolledCell),
			"Item realized after ScrollIntoView should also resolve the Dark island sentinel.");
	}

	// ---- T2 — S2: list item recycled across unload/reload keeps its theme ----

	[TestMethod]
	[RequiresFullWindow]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeAndroid | RuntimeTestPlatforms.NativeIOS)]
	public async Task When_Item_Recycled_Across_Unload_Reload_Keeps_Theme()
	{
		// S2. A row in a Dark island is realized, the island is unloaded (tab switch / recycle),
		// then reloaded. After reload the row must still resolve the Dark sentinel. Uno today
		// clears the element theme on unload (ClearThemeStateOnUnloaded), so the reloaded row
		// resolves the ambient app theme. WinUI keeps m_theme and re-themes from the parent on
		// re-Enter.
#if HAS_UNO
		using var _ = ThemeHelper.UseApplicationLightTheme();
		await WindowHelper.WaitForIdle();
#endif
		var host = new Border { Width = 220, Height = 320 };
		var island = (Grid)XamlReader.Load(
			$$"""
			<Grid xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
			      xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
			      RequestedTheme="Dark">
				<Grid.Resources>
					<ResourceDictionary>
						{{SentinelDictsXaml}}
					</ResourceDictionary>
				</Grid.Resources>
				<ListView x:Name="list" Height="300" Width="200">
					<ListView.ItemTemplate>
						<DataTemplate>
							<Border x:Name="cell" Height="40" Background="{ThemeResource SentinelBrush}" />
						</DataTemplate>
					</ListView.ItemTemplate>
				</ListView>
			</Grid>
			""");
		var list = (ListView)island.FindName("list");
		list.ItemsSource = Enumerable.Range(0, 20).Select(i => $"Item {i}").ToArray();

		host.Child = island;
		WindowHelper.WindowContent = host;
		await WindowHelper.WaitForLoaded(island);
		await WindowHelper.WaitForIdle();

		var firstCell = (await WindowHelper.WaitForNonNull(() => list.ContainerFromIndex(0) as ListViewItem))
			.FindFirstDescendant<Border>("cell");
		Assert.AreEqual(DarkSentinel, ColorOf(firstCell), "Row should resolve Dark before recycle.");

		// Unload the island (simulates tab navigation away), then reload it.
		host.Child = null;
		await WindowHelper.WaitForIdle();
		host.Child = island;
		await WindowHelper.WaitForLoaded(island);
		await WindowHelper.WaitForIdle();

		var reloadedCell = (await WindowHelper.WaitForNonNull(() => list.ContainerFromIndex(0) as ListViewItem))
			.FindFirstDescendant<Border>("cell");
		Assert.AreEqual(DarkSentinel, ColorOf(reloadedCell),
			"Row should still resolve the Dark island sentinel after unload/reload (no recycle staleness).");
	}

	// ---- T3 — S3: cell scrolled into view under pure app theme resolves the app theme ----

	[TestMethod]
	[RequiresFullWindow]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeAndroid | RuntimeTestPlatforms.NativeIOS)]
	public async Task When_Cell_Scrolled_Into_View_Under_App_Theme_Resolves_App_Theme()
	{
		// S3. Pure app-level theme (no element RequestedTheme anywhere). A cell materialized on
		// scroll must resolve the app theme. This is the OS-vs-app precedence shape: the app is
		// set Dark; a cell realized outside the walk must use Dark, not a stale/ambient value.
		// (Confirmed in WinUI probe app: a cell realized on scroll under a Dark-themed app
		// renders the Dark value.)
#if HAS_UNO
		using var _ = ThemeHelper.UseApplicationDarkTheme();
		await WindowHelper.WaitForIdle();
#endif
		var root = (Grid)XamlReader.Load(
			$$"""
			<Grid xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
			      xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
				<Grid.Resources>
					<ResourceDictionary>
						{{SentinelDictsXaml}}
					</ResourceDictionary>
				</Grid.Resources>
				<ListView x:Name="list" Height="300" Width="200">
					<ListView.ItemTemplate>
						<DataTemplate>
							<Border x:Name="cell" Height="40" Background="{ThemeResource SentinelBrush}" />
						</DataTemplate>
					</ListView.ItemTemplate>
				</ListView>
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
		Assert.AreEqual(DarkSentinel, ColorOf(scrolledCell),
			"Cell scrolled into view under a Dark app theme should resolve the Dark sentinel.");
	}

	// ---- T4 — S4: flyout first open from a themed region uses that region's theme ----

	[TestMethod]
	public async Task When_Flyout_First_Open_From_Themed_Region_Uses_Region_Theme()
	{
		// S4. A flyout opened from a Dark island must show Dark content on the FIRST open
		// (today it heals only on the second open). Asserts the resolved sentinel value, not
		// just ActualTheme, and that first-open == second-open.
		// The sentinel ThemeDictionaries live on the flyout content itself so the resource is
		// always reachable from the popup namescope; the THEME selection (Light vs Dark) is what
		// this test exercises.
		var root = (Grid)XamlReader.Load(
			$$"""
			<Grid xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
			      xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
			      RequestedTheme="Dark">
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
			Assert.AreEqual(DarkSentinel, firstOpen,
				"Flyout content should resolve the Dark region sentinel on the FIRST open.");

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
	public async Task When_Popup_First_Open_In_Themed_Region_Has_Region_Theme()
	{
		// S4 (isolated popup path). A bare Popup whose child binds the sentinel must resolve the
		// opener island's Dark theme on the FIRST open. Today this relies on a second-open heal.
		var root = (Grid)XamlReader.Load(
			$$"""
			<Grid xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
			      xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
			      Width="200" Height="200"
			      RequestedTheme="Dark">
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
			Assert.AreEqual(DarkSentinel, ColorOf(cell),
				"Popup child should resolve the Dark opener sentinel on the FIRST open.");
		}
		finally
		{
			popup.IsOpen = false;
			await WindowHelper.WaitForIdle();
		}
	}

	// ---- T6 — S5: control added at runtime into a themed island resolves the island theme ----

	[TestMethod]
	[RequiresFullWindow]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeAndroid | RuntimeTestPlatforms.NativeIOS)]
	public async Task When_Control_Added_At_Runtime_Into_Themed_Subtree_Resolves_Subtree_Theme()
	{
		// S5. A control created and added at runtime into an already-loaded RequestedTheme="Dark"
		// parent must resolve the Dark sentinel. Also covers D1: a non-FrameworkElement
		// DependencyObject ({ThemeResource} on a Brush/DO) resolves the subtree theme.
#if HAS_UNO
		using var _ = ThemeHelper.UseApplicationLightTheme();
		await WindowHelper.WaitForIdle();
#endif
		var parent = (Grid)XamlReader.Load(
			$$"""
			<Grid xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
			      xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
			      RequestedTheme="Dark">
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

		Assert.AreEqual(DarkSentinel, ColorOf(added),
			"Control added at runtime into a Dark island should resolve the Dark sentinel.");
	}

	// ---- T7 — public regression guard: app theme switch updates ThemeResource values ----

	[TestMethod]
	[RequiresFullWindow]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeAndroid | RuntimeTestPlatforms.NativeIOS)]
	public async Task When_App_Theme_Switches_ThemeResource_Values_Update()
	{
		// Public app-dark-switch regression guard. Switching app Light→Dark must flip bound
		// {ThemeResource} values (and nested elements) and report the new ActualTheme inside the
		// ActualThemeChanged handler. (Largely fixed already; this guards against regression.)
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
		// the Light default text brush even when the ambient app theme is Dark, and stay light
		// across an app dark→light→dark cycle.
#if HAS_UNO
		using var _ = ThemeHelper.UseApplicationDarkTheme();
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
		var expectedLight = (lightRef.Foreground as SolidColorBrush)?.Color;

		WindowHelper.WindowContent = root;
		await WindowHelper.WaitForLoaded(root);
		await WindowHelper.WaitForIdle();

		Assert.AreEqual(expectedLight, (text.Foreground as SolidColorBrush)?.Color,
			"TextBlock in a Light boundary should use the Light default foreground regardless of ambient.");
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

		// Simulate an OS theme change to Dark.
		Application.Current.OnRequestedThemeChanged();
		await WindowHelper.WaitForIdle();

		Assert.AreEqual(ElementTheme.Light, Application.Current.ActualElementTheme,
			"Explicit app theme must not follow the OS to Dark.");
		Assert.AreEqual(LightSentinel, ColorOf(root), "Bound value should stay Light.");
	}
#endif

	// ---- T10 — custom-theme ditched + theme-dictionary fallback (Uno-only; Phase 6) ----

#if HAS_UNO
	[TestMethod]
	[RequiresFullWindow]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeAndroid | RuntimeTestPlatforms.NativeIOS)]
	public async Task When_Custom_Theme_Ditched_And_Fallback_Does_Not_Leak_Dark()
	{
		// Decision = DITCH custom themes. (a) RequestedCustomTheme="Light" still resolves the
		// standard Light value. (b) element RequestedTheme="Dark" under app Light resolves the
		// standard Dark sentinel. (c) a dictionary defining only a Dark "Default" entry, consumed
		// under app Light, must resolve the app base Light value, not the dark "Default".
		using var _ = ThemeHelper.UseApplicationLightTheme();
		await WindowHelper.WaitForIdle();

		// (b) element Dark island under app Light → standard Dark sentinel.
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

		// (c) fallback robustness: a dictionary with only a Dark "Default" entry, consumed under
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
#endif
}
