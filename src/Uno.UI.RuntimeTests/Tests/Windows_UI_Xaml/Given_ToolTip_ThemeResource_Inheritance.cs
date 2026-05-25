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

// Verifies that a {ThemeResource} used inside ToolTip (popup-hosted) content resolves against the
// content's own inherited ActualTheme — not the ambient/application theme.
//
// A label's foreground is a {ThemeResource} whose theme-keyed brush is declared in the surrounding
// view's themed ResourceDictionary (Light/Dark sub-dicts). When the label is shown in a ToolTip from
// a Light subtree under a Dark application, the label's ActualTheme is correctly Light, but the
// {ThemeResource} can wrongly resolve the Dark sub-dictionary value — so the label renders with the
// wrong theme's color (e.g. a light-on-light foreground that is invisible).
//
// The OS-vs-app mismatch is simulated deterministically by pinning the application theme to Dark and
// placing a host that pins RequestedTheme=Light. The host's ToolTip presents a TextBlock whose
// Foreground is {ThemeResource LabelForegroundBrush}, declared inline in the same XAML so it parses
// inside the host's resource scope (a standalone XamlReader.Load of a {ThemeResource} fragment throws
// on WinUI). ToolTip content is hosted in the PopupRoot, reparented out of the owner's scope.
//
// WinUI-correct behavior: the ToolTip label's {ThemeResource} resolves against the owner's inherited
// theme (Light), so the brush evaluates to the Light sentinel (Green) — even though the application
// theme is Dark. Uno regression: the popup-presented label resolves the {ThemeResource} against the
// global/application active theme (Dark), evaluating to the Dark sentinel (Red), despite the label's
// ActualTheme correctly being Light.
//
// The assertion is on a single robust scenario (Light host under Dark app) where the element's
// ActualTheme is verifiably Light but the resolved brush value reveals which theme the
// {ThemeResource} was resolved against. Runs identically on Skia Desktop and native WinUI.
[TestClass]
[RunsOnUIThread]
public class Given_ToolTip_ThemeResource_Inheritance
{
	// One XAML document: a RequestedTheme=Light host whose Resources declare a theme-keyed sentinel
	// brush (Light=Green, Dark=Red), an owner Border carrying a ToolTip, and the ToolTip's label using
	// the brush via {ThemeResource}. Mirrors a view declaring its own label brushes in a themed
	// ResourceDictionary.
	private static Border CreateLightHost()
		=> (Border)XamlReader.Load(
			"""
			<Border xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
					xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
					RequestedTheme="Light">
				<Border.Resources>
					<ResourceDictionary>
						<ResourceDictionary.ThemeDictionaries>
							<ResourceDictionary x:Key="Light">
								<SolidColorBrush x:Key="LabelForegroundBrush" Color="Green" />
							</ResourceDictionary>
							<ResourceDictionary x:Key="Dark">
								<SolidColorBrush x:Key="LabelForegroundBrush" Color="Red" />
							</ResourceDictionary>
						</ResourceDictionary.ThemeDictionaries>
					</ResourceDictionary>
				</Border.Resources>
				<Border x:Name="Owner" Width="120" Height="40">
					<ToolTipService.ToolTip>
						<ToolTip>
							<TextBlock Text="Label"
									Foreground="{ThemeResource LabelForegroundBrush}" />
						</ToolTip>
					</ToolTipService.ToolTip>
				</Border>
			</Border>
			""");

	private static Color? GetForegroundColor(TextBlock textBlock)
		=> (textBlock.Foreground as SolidColorBrush)?.Color;

	// A Light host under a Dark application (the literal OS-Dark / app-Light mismatch). The ToolTip
	// label's ActualTheme is Light, so its {ThemeResource} must resolve the host's Light sentinel
	// (Green). On Uno the popup-presented label resolves the {ThemeResource} against the
	// application/global Dark theme, producing the Dark sentinel (Red).
	[TestMethod]
	[RequiresFullWindow]
	[GitHubWorkItem("https://github.com/unoplatform/kahua-private/issues/484")]
	public async Task When_ToolTip_Label_Uses_Owner_Subtree_Theme_Light_Under_Dark_App()
	{
#if HAS_UNO
		using var darkApp = ThemeHelper.UseApplicationDarkTheme();
		await WindowHelper.WaitForIdle();
#endif

		var host = CreateLightHost();
		var owner = (Border)host.FindName("Owner");
		var tooltip = (ToolTip)ToolTipService.GetToolTip(owner);
		var label = (TextBlock)tooltip.Content;

		var root = new Border { Child = host };

		try
		{
			WindowHelper.WindowContent = root;
			await WindowHelper.WaitForLoaded(root);

			tooltip.IsOpen = true;
			await WindowHelper.WaitForIdle();
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(ElementTheme.Light, owner.ActualTheme, "Owner should be in the Light subtree.");
			Assert.AreEqual(ElementTheme.Light, label.ActualTheme, "ToolTip label should inherit the owner's Light theme.");

			var foreground = GetForegroundColor(label);
			Assert.IsNotNull(foreground, "ToolTip label should have resolved a SolidColorBrush foreground.");

			Assert.AreEqual(Colors.Green, foreground.Value,
				$"ToolTip label {{ThemeResource}} should resolve against the label's inherited Light theme " +
				$"(Light sentinel Green), but got {foreground.Value}. If it is Red (the Dark sentinel), the " +
				$"popup-presented label resolved the {{ThemeResource}} against the application/global Dark " +
				$"theme instead of its own inherited ActualTheme.");
		}
		finally
		{
			tooltip.IsOpen = false;
#if HAS_UNO
			VisualTreeHelper.CloseAllPopups(WindowHelper.XamlRoot);
#endif
		}
	}
}
