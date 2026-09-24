using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Uno.UI.RuntimeTests.Helpers;
using Windows.UI;
using Windows.UI.ViewManagement;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_ViewManagement;

[TestClass]
[RunsOnUIThread]
public class Given_UISettings_Accent
{
	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/4444")]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
	public async Task When_OverrideAccentColor_Updates_GetColorValue_And_Resources()
	{
#if HAS_UNO
		var settings = new UISettings();
		var previousOverride = Uno.UI.FeatureConfiguration.AccentColor.OverrideAccentColor;
		var customAccent = Color.FromArgb(0xFF, 0xE0, 0x12, 0x34);
		var originalAccent = settings.GetColorValue(UIColorType.Accent);

		try
		{
			Uno.UI.FeatureConfiguration.AccentColor.OverrideAccentColor = customAccent;
			await WindowHelper.WaitForIdle();

			// Public UISettings API reflects the override and derives distinct shades.
			Assert.AreEqual(customAccent, settings.GetColorValue(UIColorType.Accent),
				"GetColorValue(Accent) should reflect the override.");
			Assert.AreNotEqual(settings.GetColorValue(UIColorType.AccentLight1), settings.GetColorValue(UIColorType.AccentDark1),
				"Light and dark variants should be derived as distinct shades.");

			// The framework accent brush resource follows the override (resolved by a freshly-loaded element).
			Assert.AreEqual(customAccent, await ResolveAccentBrushColor(),
				"SystemColorControlAccentBrush should resolve to the override accent.");

			Uno.UI.FeatureConfiguration.AccentColor.OverrideAccentColor = null;
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(originalAccent, settings.GetColorValue(UIColorType.Accent),
				"Clearing the override should revert GetColorValue(Accent).");
			Assert.AreEqual(originalAccent, await ResolveAccentBrushColor(),
				"Clearing the override should revert the accent brush.");
		}
		finally
		{
			Uno.UI.FeatureConfiguration.AccentColor.OverrideAccentColor = previousOverride;
			WindowHelper.WindowContent = null;
		}
#endif
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/4444")]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
	public async Task When_OverrideAccentColor_Then_SystemColorHighlightColor_Unchanged()
	{
#if HAS_UNO
		// WinUI sources SystemColorHighlightColor from the system highlight color, independently of the accent.
		var previousOverride = Uno.UI.FeatureConfiguration.AccentColor.OverrideAccentColor;
		var originalHighlight = await ResolveColor("SystemColorHighlightColor");

		try
		{
			Uno.UI.FeatureConfiguration.AccentColor.OverrideAccentColor = Color.FromArgb(0xFF, 0xE0, 0x12, 0x34);
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(originalHighlight, await ResolveColor("SystemColorHighlightColor"));
		}
		finally
		{
			Uno.UI.FeatureConfiguration.AccentColor.OverrideAccentColor = previousOverride;
			WindowHelper.WindowContent = null;
		}
#endif
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/4444")]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
	public async Task When_NormalizeAccentColor_Toggled_Then_Accent_Follows_Windows_Adjustment()
	{
#if HAS_UNO
		var settings = new UISettings();
		var previousOverride = Uno.UI.FeatureConfiguration.AccentColor.OverrideAccentColor;
		var previousNormalize = Uno.UI.FeatureConfiguration.AccentColor.NormalizeAccentColor;
		var customAccent = Color.FromArgb(0xFF, 0xE0, 0x12, 0x34);

		try
		{
			Uno.UI.FeatureConfiguration.AccentColor.NormalizeAccentColor = false;
			Uno.UI.FeatureConfiguration.AccentColor.OverrideAccentColor = customAccent;
			await WindowHelper.WaitForIdle();
			Assert.AreEqual(customAccent, settings.GetColorValue(UIColorType.Accent));

			// Windows raises this red's lightness when it is picked as the system accent.
			Uno.UI.FeatureConfiguration.AccentColor.NormalizeAccentColor = true;
			await WindowHelper.WaitForIdle();
			var normalized = Color.FromArgb(0xFF, 0xE4, 0x1B, 0x37);
			Assert.AreEqual(normalized, settings.GetColorValue(UIColorType.Accent));
			Assert.AreEqual(normalized, await ResolveColor("SystemAccentColor"));
		}
		finally
		{
			Uno.UI.FeatureConfiguration.AccentColor.NormalizeAccentColor = previousNormalize;
			Uno.UI.FeatureConfiguration.AccentColor.OverrideAccentColor = previousOverride;
			WindowHelper.WindowContent = null;
		}
#endif
	}

#if HAS_UNO
	private static async Task<Color> ResolveAccentBrushColor()
	{
		var border = (Border)XamlReader.Load(
			"""
			<Border xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
					Width="10" Height="10"
					Background="{ThemeResource SystemColorControlAccentBrush}" />
			""");
		await UITestHelper.Load(border);
		return ((SolidColorBrush)border.Background).Color;
	}

	private static async Task<Color> ResolveColor(string key)
	{
		var border = (Border)XamlReader.Load(
			$$"""
			<Border xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" Width="10" Height="10">
				<Border.Background>
					<SolidColorBrush Color="{ThemeResource {{key}}}" />
				</Border.Background>
			</Border>
			""");
		await UITestHelper.Load(border);
		return ((SolidColorBrush)border.Background).Color;
	}
#endif
}
