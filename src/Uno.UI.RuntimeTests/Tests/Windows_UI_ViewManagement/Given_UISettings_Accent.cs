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
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI | RuntimeTestPlatforms.NativeAndroid | RuntimeTestPlatforms.NativeIOS)]
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
#endif
}
