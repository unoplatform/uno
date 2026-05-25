#nullable enable

using System.Threading.Tasks;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml;

/// <summary>
/// Verifies that a <c>{ThemeResource}</c> used inside Flyout (popup-hosted) content
/// resolves against the content's own inherited <c>ActualTheme</c> — not against the
/// ambient/application theme.
///
/// Flyout content is reparented into a separate PopupRoot visual-tree branch. When the
/// placement target sits inside an element-level theme boundary (or under an application
/// theme) that differs from the ambient theme, the popup content still inherits the
/// target's theme. WinUI resolves a theme-keyed <c>{ThemeResource}</c> in that content
/// against the inherited theme; Uno regresses by resolving it against the ambient/global
/// active theme, so the content gets the wrong theme's value.
///
/// The scenario is reproduced deterministically — without changing the OS theme — using
/// nested element-level theme boundaries: an OUTER <c>RequestedTheme=Dark</c> boundary
/// containing an INNER <c>RequestedTheme=Light</c> boundary that hosts the button. A
/// flyout opened from the inner-Light button has <c>ActualTheme == Light</c>, so its
/// theme-keyed <c>{ThemeResource}</c> must resolve to the Light value (Green). On Uno it
/// resolves to the outer Dark value (Red) instead.
///
/// Runs identically on Skia Desktop and native WinUI; the assertions encode the
/// WinUI-correct behavior.
/// </summary>
[TestClass]
[RunsOnUIThread]
public class Given_Flyout_ThemeResource_Inheritance
{
	private const string RootXaml =
		"""
		<Border xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
				xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
				RequestedTheme="Dark">
			<Border.Resources>
				<ResourceDictionary>
					<ResourceDictionary.ThemeDictionaries>
						<ResourceDictionary x:Key="Light">
							<SolidColorBrush x:Key="FlyoutContentBrush" Color="Green" />
						</ResourceDictionary>
						<ResourceDictionary x:Key="Dark">
							<SolidColorBrush x:Key="FlyoutContentBrush" Color="Red" />
						</ResourceDictionary>
						<ResourceDictionary x:Key="Default">
							<SolidColorBrush x:Key="FlyoutContentBrush" Color="Red" />
						</ResourceDictionary>
					</ResourceDictionary.ThemeDictionaries>
				</ResourceDictionary>
			</Border.Resources>
			<Border RequestedTheme="Light">
				<Button x:Name="btn" Content="Open">
					<Button.Flyout>
						<Flyout>
							<Border x:Name="flyoutContent"
									Width="50"
									Height="50"
									Background="{ThemeResource FlyoutContentBrush}" />
						</Flyout>
					</Button.Flyout>
				</Button>
			</Border>
		</Border>
		""";

	/// <summary>
	/// A flyout opened from a button inside an inner Light theme boundary (nested in an
	/// outer Dark boundary). The flyout content inherits Light (verified), so its
	/// theme-keyed {ThemeResource} background must resolve to the Light value (Green). On
	/// Uno it resolves against the outer/ambient Dark theme (Red).
	/// </summary>
	[TestMethod]
	[RequiresFullWindow]
	[GitHubWorkItem("https://github.com/unoplatform/kahua-private/issues/475")]
	public async Task When_Flyout_Opened_From_Inner_Light_Boundary_Resolves_Light_ThemeResource()
	{
		var root = (Border)XamlReader.Load(RootXaml);

		WindowHelper.WindowContent = root;
		await WindowHelper.WaitForLoaded(root);
		await WindowHelper.WaitForIdle();

		var button = (Button)root.FindName("btn");
		var flyout = (Flyout)button.Flyout;
		var flyoutContent = (Border)flyout.Content;

		try
		{
			flyout.ShowAt(button);
			await WindowHelper.WaitForIdle();
			await WindowHelper.WaitForIdle();

			// The flyout content inherits the inner Light boundary's theme.
			Assert.AreEqual(ElementTheme.Light, flyoutContent.ActualTheme,
				"Flyout content should inherit Light from the inner theme boundary that hosts the button.");

			var brush = flyoutContent.Background as SolidColorBrush;
			Assert.IsNotNull(brush, "Flyout content background should be a SolidColorBrush.");

			// WinUI: resolves {ThemeResource} against the content's inherited Light theme
			// (Green). Uno regression: resolves against the outer/ambient Dark theme (Red).
			Assert.AreEqual(Colors.Green, brush.Color,
				$"Flyout content has ActualTheme=Light, so its {{ThemeResource}} must " +
				$"resolve to the Light value (Green), but it is {brush.Color}. If Red, the " +
				$"{{ThemeResource}} was resolved against the outer/application (Dark) theme " +
				$"instead of the element's own inherited ActualTheme.");
		}
		finally
		{
			flyout.Hide();
			await WindowHelper.WaitForIdle();
		}
	}

#if HAS_UNO
	/// <summary>
	/// Application/OS theme is Dark, a content-root subtree pins RequestedTheme=Light, and a
	/// flyout is opened from a button inside it. The flyout content inherits Light, so its
	/// theme-keyed {ThemeResource} must resolve to the Light value (Green), not the
	/// application/OS Dark value (Red).
	/// </summary>
	[TestMethod]
	[RequiresFullWindow]
	[GitHubWorkItem("https://github.com/unoplatform/kahua-private/issues/475")]
	public async Task When_Flyout_Opened_From_Light_Subtree_Under_Dark_App_Resolves_Light_ThemeResource()
	{
		using var darkApp = ThemeHelper.UseApplicationDarkTheme();
		await WindowHelper.WaitForIdle();

		var root = (Border)XamlReader.Load(
			"""
			<Border xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
					xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
					RequestedTheme="Light">
				<Border.Resources>
					<ResourceDictionary>
						<ResourceDictionary.ThemeDictionaries>
							<ResourceDictionary x:Key="Light">
								<SolidColorBrush x:Key="FlyoutContentBrush" Color="Green" />
							</ResourceDictionary>
							<ResourceDictionary x:Key="Dark">
								<SolidColorBrush x:Key="FlyoutContentBrush" Color="Red" />
							</ResourceDictionary>
							<ResourceDictionary x:Key="Default">
								<SolidColorBrush x:Key="FlyoutContentBrush" Color="Red" />
							</ResourceDictionary>
						</ResourceDictionary.ThemeDictionaries>
					</ResourceDictionary>
				</Border.Resources>
				<Button x:Name="btn" Content="Open">
					<Button.Flyout>
						<Flyout>
							<Border x:Name="flyoutContent"
									Width="50"
									Height="50"
									Background="{ThemeResource FlyoutContentBrush}" />
						</Flyout>
					</Button.Flyout>
				</Button>
			</Border>
			""");

		WindowHelper.WindowContent = root;
		await WindowHelper.WaitForLoaded(root);
		await WindowHelper.WaitForIdle();

		var button = (Button)root.FindName("btn");
		var flyout = (Flyout)button.Flyout;
		var flyoutContent = (Border)flyout.Content;

		try
		{
			flyout.ShowAt(button);
			await WindowHelper.WaitForIdle();
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(ElementTheme.Light, flyoutContent.ActualTheme,
				"Flyout content should inherit Light from the host even though the app is Dark.");

			var brush = flyoutContent.Background as SolidColorBrush;
			Assert.IsNotNull(brush, "Flyout content background should be a SolidColorBrush.");

			Assert.AreEqual(Colors.Green, brush.Color,
				$"Flyout content has ActualTheme=Light, so its {{ThemeResource}} must " +
				$"resolve to the Light value (Green), but it is {brush.Color}. If Red, the " +
				$"{{ThemeResource}} was resolved against the application/OS Dark theme " +
				$"instead of the element's own inherited ActualTheme.");
		}
		finally
		{
			flyout.Hide();
			await WindowHelper.WaitForIdle();
		}
	}
#endif
}
