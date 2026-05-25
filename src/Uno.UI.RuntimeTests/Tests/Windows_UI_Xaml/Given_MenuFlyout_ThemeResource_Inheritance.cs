using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;
using Windows.UI;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml;

// Verifies that a {ThemeResource} used inside Flyout / MenuFlyout (popup-hosted) content resolves
// against the content's own inherited ActualTheme — not the ambient/application theme.
//
// A menu's commands reference a {ThemeResource} whose theme-keyed brush is declared in the
// surrounding view's themed ResourceDictionary (Light/Dark sub-dicts). When the menu is opened from
// a Light subtree under a Dark application, the menu content's ActualTheme is correctly Light, but
// the {ThemeResource} can wrongly resolve the Dark sub-dictionary value — a mixed result (correct
// inherited theme, wrong resolved value) that renders the menu with the wrong styling on open.
//
// The OS-vs-app mismatch is simulated deterministically by pinning the application theme to Dark and
// placing a host that pins RequestedTheme=Light. The host's Resources declare the theme-keyed sentinel
// brush (Light=Green, Dark=Red). The flyout/menu content references that brush via {ThemeResource},
// declared inline in the SAME XAML so it parses inside the host's resource scope (a standalone
// XamlReader.Load of a {ThemeResource} fragment throws on WinUI). The flyout content is hosted in the
// PopupRoot, reparented out of the owner's visual scope.
//
// WinUI-correct behavior: the menu content's {ThemeResource} resolves against the owner's inherited
// theme (Light), so the brush evaluates to the Light sentinel (Green) — even though the application
// theme is Dark. Uno regression: the popup-presented content resolves the {ThemeResource} against the
// global/application active theme (Dark), evaluating to the Dark sentinel (Red), despite the content's
// ActualTheme correctly being Light. Runs identically on Skia Desktop and native WinUI.
[TestClass]
[RunsOnUIThread]
public class Given_MenuFlyout_ThemeResource_Inheritance
{
	private static Color? GetForegroundColor(TextBlock textBlock)
		=> (textBlock.Foreground as SolidColorBrush)?.Color;

	// ------------------------------------------------------------------
	// Scenario A — Flyout. A RequestedTheme=Light host declares the theme-keyed
	// sentinel brush; a Flyout's TextBlock content references it via
	// {ThemeResource}. Under a Dark application, the popup-presented content's
	// {ThemeResource} must still resolve the owner's Light sentinel (Green).
	// ------------------------------------------------------------------
	[TestMethod]
	[RequiresFullWindow]
	[GitHubWorkItem("https://github.com/unoplatform/kahua-private/issues/480")]
	public async Task When_Flyout_Menu_Uses_Owner_Subtree_Theme_Light_Under_Dark_App()
	{
#if HAS_UNO
		using var darkApp = ThemeHelper.UseApplicationDarkTheme();
		await WindowHelper.WaitForIdle();
#endif

		// One XAML document: host pins Light + declares the theme-keyed brush; the Flyout content
		// (a TextBlock) references it via {ThemeResource}, parsed inside the host's resource scope.
		var host = (Border)XamlReader.Load(
			"""
			<Border xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
					xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
					RequestedTheme="Light">
				<Border.Resources>
					<ResourceDictionary>
						<ResourceDictionary.ThemeDictionaries>
							<ResourceDictionary x:Key="Light">
								<SolidColorBrush x:Key="MenuItemBrush" Color="Green" />
							</ResourceDictionary>
							<ResourceDictionary x:Key="Dark">
								<SolidColorBrush x:Key="MenuItemBrush" Color="Red" />
							</ResourceDictionary>
						</ResourceDictionary.ThemeDictionaries>
					</ResourceDictionary>
				</Border.Resources>
				<Button x:Name="Owner" Content="Owner">
					<Button.Flyout>
						<Flyout>
							<TextBlock x:Name="MenuLabel" Text="Menu"
									Foreground="{ThemeResource MenuItemBrush}" />
						</Flyout>
					</Button.Flyout>
				</Button>
			</Border>
			""");

		var owner = (Button)host.FindName("Owner");
		var flyout = (Flyout)owner.Flyout;
		var label = (TextBlock)flyout.Content;

		var root = new Border { Child = host };

		try
		{
			WindowHelper.WindowContent = root;
			await WindowHelper.WaitForLoaded(root);

			flyout.ShowAt(owner);
			await WindowHelper.WaitForIdle();
			await WindowHelper.WaitForLoaded(label);
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(ElementTheme.Light, owner.ActualTheme, "Owner should be in the Light subtree.");
			Assert.AreEqual(ElementTheme.Light, label.ActualTheme, "Flyout content should inherit the owner's Light theme.");

			var foreground = GetForegroundColor(label);
			Assert.IsNotNull(foreground, "Flyout content should have resolved a SolidColorBrush foreground.");

			Assert.AreEqual(Colors.Green, foreground.Value,
				$"Flyout content {{ThemeResource}} should resolve against the content's inherited Light theme " +
				$"(Light sentinel Green), but got {foreground.Value}. If it is Red (the Dark sentinel), the " +
				$"popup-presented content resolved the {{ThemeResource}} against the application/global Dark " +
				$"theme instead of its own inherited ActualTheme.");
		}
		finally
		{
			flyout.Hide();
#if HAS_UNO
			VisualTreeHelper.CloseAllPopups(WindowHelper.XamlRoot);
#endif
		}
	}

	// ------------------------------------------------------------------
	// Scenario B — MenuFlyout (the menu/context-menu control family). Same
	// host/brush setup; a MenuFlyoutItem's foreground references the host brush
	// via {ThemeResource} and must resolve the owner's Light sentinel (Green).
	// ------------------------------------------------------------------
	[TestMethod]
	[RequiresFullWindow]
	[GitHubWorkItem("https://github.com/unoplatform/kahua-private/issues/480")]
	public async Task When_MenuFlyout_Item_Uses_Owner_Subtree_Theme_Light_Under_Dark_App()
	{
#if HAS_UNO
		using var darkApp = ThemeHelper.UseApplicationDarkTheme();
		await WindowHelper.WaitForIdle();
#endif

		var host = (Border)XamlReader.Load(
			"""
			<Border xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
					xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
					RequestedTheme="Light">
				<Border.Resources>
					<ResourceDictionary>
						<ResourceDictionary.ThemeDictionaries>
							<ResourceDictionary x:Key="Light">
								<SolidColorBrush x:Key="MenuItemBrush" Color="Green" />
							</ResourceDictionary>
							<ResourceDictionary x:Key="Dark">
								<SolidColorBrush x:Key="MenuItemBrush" Color="Red" />
							</ResourceDictionary>
						</ResourceDictionary.ThemeDictionaries>
					</ResourceDictionary>
				</Border.Resources>
				<Button x:Name="Owner" Content="Owner">
					<Button.Flyout>
						<MenuFlyout>
							<MenuFlyoutItem x:Name="MenuItem" Text="Item"
									Foreground="{ThemeResource MenuItemBrush}" />
						</MenuFlyout>
					</Button.Flyout>
				</Button>
			</Border>
			""");

		var owner = (Button)host.FindName("Owner");
		var flyout = (MenuFlyout)owner.Flyout;
		var item = (MenuFlyoutItem)host.FindName("MenuItem");

		var root = new Border { Child = host };

		try
		{
			WindowHelper.WindowContent = root;
			await WindowHelper.WaitForLoaded(root);

			flyout.ShowAt(owner);
			await WindowHelper.WaitForIdle();
			await WindowHelper.WaitForLoaded(item);
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(ElementTheme.Light, owner.ActualTheme, "Owner should be in the Light subtree.");
			Assert.AreEqual(ElementTheme.Light, item.ActualTheme, "Menu item should inherit the owner's Light theme.");

			var foreground = (item.Foreground as SolidColorBrush)?.Color;
			Assert.IsNotNull(foreground, "Menu item should have resolved a SolidColorBrush foreground.");

			Assert.AreEqual(Colors.Green, foreground.Value,
				$"Menu item {{ThemeResource}} should resolve against the item's inherited Light theme " +
				$"(Light sentinel Green), but got {foreground.Value}. If it is Red (the Dark sentinel), the " +
				$"popup-presented menu resolved the {{ThemeResource}} against the application/global Dark " +
				$"theme instead of its own inherited ActualTheme.");
		}
		finally
		{
			flyout.Hide();
#if HAS_UNO
			VisualTreeHelper.CloseAllPopups(WindowHelper.XamlRoot);
#endif
		}
	}
}
