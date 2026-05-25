using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;
using Windows.UI;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml;

// Verifies that a {ThemeResource} used inside list/grid row content resolves against the content's
// own inherited ActualTheme — not the ambient/application theme — when that content is presented
// AFTER tab-style navigation has unloaded and reloaded the tab subtree.
//
// This models the issue scenario: a view hosts an items grid inside one tab. The user navigates to a
// different tab (the grid tab subtree is unloaded), then navigates back (the grid tab subtree
// re-enters the tree). When grid row content is subsequently presented through a surface that is
// reparented out of the owner's visual scope (a Flyout's PopupRoot — the same mechanism a grid uses
// for a row's pop-up detail), its {ThemeResource} can resolve against the global/application active
// theme instead of the row's own inherited ActualTheme — so the row text renders with the wrong
// theme's color even though the row's ActualTheme is correct.
//
// The OS-vs-app mismatch is simulated deterministically by pinning the application theme to Dark and
// placing a host that pins RequestedTheme=Light. The host's Resources declare a theme-keyed sentinel
// brush (Light=Green, Dark=Red, Default=Red). A grid row's Foreground references that brush via
// {ThemeResource}, declared inline in the SAME XAML so it parses inside the host's resource scope (a
// standalone XamlReader.Load of a {ThemeResource} fragment throws on WinUI). A TabView provides the
// navigation trigger: tab 1 hosts the items grid and an owner button whose Flyout presents the row
// content; tab 2 is unrelated. The test switches to tab 2 (grid tab subtree unloads), switches back
// (grid tab subtree re-enters), then opens the flyout whose content is hosted in the PopupRoot,
// reparented out of the owner's visual scope.
//
// WinUI-correct behavior: the row content's {ThemeResource} resolves against the owner's inherited
// Light theme, so the brush evaluates to the Light sentinel (Green) — even though the application
// theme is Dark and the content was reached after tab navigation. Uno regression: the
// popup-presented row content resolves the {ThemeResource} against the global/application active
// theme (Dark), evaluating to the Dark sentinel (Red), despite the row's ActualTheme correctly being
// Light. Runs identically on Skia Desktop and native WinUI; the assertions encode the WinUI-correct
// behavior.
[TestClass]
[RunsOnUIThread]
public class Given_ListView_ThemeResource_AfterReload_Inheritance
{
	// One XAML document: a RequestedTheme=Light host (the themed dictionary owner) declares the
	// theme-keyed sentinel brush. A TabView gives the navigation trigger; tab 1 hosts an items grid
	// (ListView) plus an owner Button whose Flyout presents the grid row content (a TextBlock) using
	// the brush via {ThemeResource}, parsed inside the host's resource scope. Mirrors a view declaring
	// its own row text brushes in a themed ResourceDictionary and showing a row's detail in a popup.
	private static Border CreateLightHost()
		=> (Border)XamlReader.Load(
			"""
			<Border xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
					xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
					xmlns:muxc="using:Microsoft.UI.Xaml.Controls"
					RequestedTheme="Light">
				<Border.Resources>
					<ResourceDictionary>
						<ResourceDictionary.ThemeDictionaries>
							<ResourceDictionary x:Key="Light">
								<SolidColorBrush x:Key="RowTextBrush" Color="Green" />
							</ResourceDictionary>
							<ResourceDictionary x:Key="Dark">
								<SolidColorBrush x:Key="RowTextBrush" Color="Red" />
							</ResourceDictionary>
							<ResourceDictionary x:Key="Default">
								<SolidColorBrush x:Key="RowTextBrush" Color="Red" />
							</ResourceDictionary>
						</ResourceDictionary.ThemeDictionaries>
					</ResourceDictionary>
				</Border.Resources>
				<muxc:TabView x:Name="Tabs">
					<muxc:TabViewItem Header="One">
						<StackPanel>
							<ListView x:Name="Grid">
								<ListViewItem>Row</ListViewItem>
							</ListView>
							<Button x:Name="Owner" Content="Owner">
								<Button.Flyout>
									<Flyout>
										<TextBlock x:Name="Row"
												Text="Row"
												Foreground="{ThemeResource RowTextBrush}" />
									</Flyout>
								</Button.Flyout>
							</Button>
						</StackPanel>
					</muxc:TabViewItem>
					<muxc:TabViewItem Header="Two">
						<TextBlock Text="Other" />
					</muxc:TabViewItem>
				</muxc:TabView>
			</Border>
			""");

	private static Color? GetForegroundColor(TextBlock textBlock)
		=> (textBlock.Foreground as SolidColorBrush)?.Color;

	// Resolves the owner Button from the grid tab's logical content. The Button lives inside the
	// TabViewItem's content (a StackPanel), reachable without relying on visual-tree realization
	// timing (which differs between Uno and WinUI after a tab switch).
	private static Button GetOwner(TabView tabs)
	{
		var tabItem = (TabViewItem)tabs.TabItems[0];
		var panel = (StackPanel)tabItem.Content;
		foreach (var child in panel.Children)
		{
			if (child is Button { Name: "Owner" } owner)
			{
				return owner;
			}
		}

		return null;
	}

	// A Light host under a Dark application (the literal OS-Dark / app-Light mismatch). The grid tab
	// subtree is unloaded by switching to another tab and reloaded by switching back, then the row
	// content is presented through a Flyout. The row content's ActualTheme is Light, so its
	// {ThemeResource} must resolve the host's Light sentinel (Green). On Uno the popup-presented row
	// content resolves the {ThemeResource} against the application/global Dark theme, producing the
	// Dark sentinel (Red).
	[TestMethod]
	[RequiresFullWindow]
	[GitHubWorkItem("https://github.com/unoplatform/kahua-private/issues/482")]
	public async Task When_Grid_Row_Presented_After_Tab_Navigation_Light_Under_Dark_App()
	{
#if HAS_UNO
		using var darkApp = ThemeHelper.UseApplicationDarkTheme();
		await WindowHelper.WaitForIdle();
#endif

		var host = CreateLightHost();
		var tabs = (TabView)host.FindName("Tabs");

		var root = new Border { Child = host };

		Flyout flyout = null;

		try
		{
			WindowHelper.WindowContent = root;
			await WindowHelper.WaitForLoaded(host);
			await WindowHelper.WaitForIdle();

			// Navigate away from the grid tab: the grid tab subtree unloads.
			tabs.SelectedIndex = 1;
			await WindowHelper.WaitForIdle();
			await WindowHelper.WaitForIdle();

			// Navigate back to the grid tab: the grid tab subtree re-enters the tree.
			tabs.SelectedIndex = 0;
			await WindowHelper.WaitForIdle();
			await WindowHelper.WaitForIdle();

			// The grid-tab content lives in the TabViewItem's content, so resolve the owner/flyout
			// from its logical content after navigating back.
			var owner = GetOwner(tabs);
			Assert.IsNotNull(owner, "Owner button should be present in the grid tab after navigation.");
			await WindowHelper.WaitForLoaded(owner);
			flyout = (Flyout)owner.Flyout;
			var row = (TextBlock)flyout.Content;

			// Present the row content through the flyout (hosted in the PopupRoot).
			flyout.ShowAt(owner);
			await WindowHelper.WaitForIdle();
			await WindowHelper.WaitForLoaded(row);
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(ElementTheme.Light, owner.ActualTheme, "Owner should be in the Light subtree after tab navigation.");
			Assert.AreEqual(ElementTheme.Light, row.ActualTheme, "Row content should inherit the owner's Light theme.");

			var foreground = GetForegroundColor(row);
			Assert.IsNotNull(foreground, "Row content should have resolved a SolidColorBrush foreground.");

			// WinUI: the row content's {ThemeResource} resolves against its inherited Light theme
			// (Green). Uno regression: it resolves against the application/global Dark theme (Red).
			Assert.AreEqual(Colors.Green, foreground.Value,
				$"Row content has ActualTheme=Light, so its {{ThemeResource}} must resolve to the Light value " +
				$"(Green), but got {foreground.Value}. If it is Red (the Dark sentinel), the row content " +
				$"resolved the {{ThemeResource}} against the application/global Dark theme instead of its own " +
				$"inherited ActualTheme after tab navigation.");
		}
		finally
		{
			flyout?.Hide();
#if HAS_UNO
			VisualTreeHelper.CloseAllPopups(WindowHelper.XamlRoot);
#endif
		}
	}
}
