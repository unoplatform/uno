using System;
using System.Globalization;
using Windows.Globalization;
using Windows.Storage;

namespace Uno.UI.RuntimeTests.Tests.Windows_Globalization;

[TestClass]
public class Given_ApplicationLanguages
{
	[TestCleanup]
	public void CleanUp()
	{
		ApplicationLanguages.PrimaryLanguageOverride = string.Empty;
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
	public void Test_Chinese_With_Script_Subtag()
	{
		ApplicationLanguages.PrimaryLanguageOverride = "zh-Hans-CN";
		ApplicationLanguages.Languages[0].Should().Be("zh-Hans-CN");
		ApplicationData.Current.LocalSettings.Values["__Uno.PrimaryLanguageOverride"].Should().Be("zh-Hans-CN");
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
	public void Test_French_With_Script_Subtag()
	{
		ApplicationLanguages.PrimaryLanguageOverride = "fr-Latn-CA";
		ApplicationLanguages.Languages[0].Should().Be("fr-Latn-CA");
		ApplicationData.Current.LocalSettings.Values["__Uno.PrimaryLanguageOverride"].Should().Be("fr-Latn-CA");
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/13704")]
	public void When_PrimaryLanguageOverride_Then_Culture_Unchanged()
	{
		var culture = CultureInfo.CurrentCulture;
		var uiCulture = CultureInfo.CurrentUICulture;
		var threadCulture = CultureInfo.DefaultThreadCurrentCulture;
		var threadUICulture = CultureInfo.DefaultThreadCurrentUICulture;

		// Picked relative to the ambient culture so the assertion below cannot pass by coincidence.
		var language = culture.Name.StartsWith("fr", StringComparison.OrdinalIgnoreCase) ? "de-DE" : "fr-CA";

		try
		{
			ApplicationLanguages.PrimaryLanguageOverride = language;

			// The override still drives resource resolution immediately...
			ApplicationLanguages.Languages[0].Should().Be(language);

			// ...but the culture only follows on the next app start, as on WinUI.
			CultureInfo.CurrentCulture.Name.Should().Be(culture.Name);
			CultureInfo.CurrentUICulture.Name.Should().Be(uiCulture.Name);
		}
		finally
		{
			ApplicationLanguages.PrimaryLanguageOverride = string.Empty;
			CultureInfo.CurrentCulture = culture;
			CultureInfo.CurrentUICulture = uiCulture;
			CultureInfo.DefaultThreadCurrentCulture = threadCulture;
			CultureInfo.DefaultThreadCurrentUICulture = threadUICulture;
		}
	}
}
