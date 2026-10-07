#if HAS_UNO
#nullable enable

using Microsoft.UI.System;
using Private.Infrastructure;

namespace Uno.UI.RuntimeTests.Tests.Microsoft_UI_System;

[TestClass]
[RunsOnUIThread]
public class Given_ThemeSettings
{
	[TestMethod]
	public void When_HighContrast_Changes_Then_Changed_Raised()
	{
		var themeSettings = ThemeSettings.CreateForWindowId(TestServices.WindowHelper.CurrentTestWindow.AppWindow.Id);
		var originalHighContrast = Uno.WinRTFeatureConfiguration.Accessibility.HighContrastOverride;
		var originalScheme = Uno.WinRTFeatureConfiguration.Accessibility.HighContrastSchemeOverride;
		var changedCount = 0;
		ThemeSettings? sender = null;
		themeSettings.Changed += (s, e) =>
		{
			changedCount++;
			sender = s;
		};

		try
		{
			Uno.WinRTFeatureConfiguration.Accessibility.HighContrastOverride = !(originalHighContrast ?? false);
			Assert.AreEqual(1, changedCount);
			Assert.AreSame(themeSettings, sender);
			Assert.AreEqual(!(originalHighContrast ?? false), themeSettings.HighContrast);

			Uno.WinRTFeatureConfiguration.Accessibility.HighContrastSchemeOverride = "High Contrast White";
			Assert.AreEqual(2, changedCount);
			Assert.AreEqual("High Contrast White", themeSettings.HighContrastScheme);
		}
		finally
		{
			Uno.WinRTFeatureConfiguration.Accessibility.HighContrastOverride = originalHighContrast;
			Uno.WinRTFeatureConfiguration.Accessibility.HighContrastSchemeOverride = originalScheme;
		}
	}

	[TestMethod]
	public void When_Created_Then_Reflects_AccessibilitySettings()
	{
		var themeSettings = ThemeSettings.CreateForWindowId(TestServices.WindowHelper.CurrentTestWindow.AppWindow.Id);
		var accessibilitySettings = new global::Windows.UI.ViewManagement.AccessibilitySettings();

		Assert.AreEqual(accessibilitySettings.HighContrast, themeSettings.HighContrast);
		Assert.AreEqual(accessibilitySettings.HighContrastScheme, themeSettings.HighContrastScheme);
	}
}
#endif
