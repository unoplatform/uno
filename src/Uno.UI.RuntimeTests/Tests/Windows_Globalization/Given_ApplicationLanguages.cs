using Windows.Globalization;
using FluentAssertions;
using Windows.Storage;

namespace Uno.UI.RuntimeTests.Tests.Windows_Globalization;

[TestClass]
public class Given_ApplicationLanguages
{
	private const string Key =
#if (__ANDROID__ || __IOS__ || __MACOS__) && NET6_0_OR_GREATER
		"__Uno.PrimaryLanguageOverride.SamplesApp.netcoremobile";
#elif __ANDROID__
		"__Uno.PrimaryLanguageOverride.SamplesApp.Droid";
#elif __IOS__
		"__Uno.PrimaryLanguageOverride.SamplesApp.iOS";
#elif __MACOS__
		"__Uno.PrimaryLanguageOverride.SamplesApp.macOS";
#elif __SKIA__
		"__Uno.PrimaryLanguageOverride.SamplesApp.Skia";
#elif __WASM__
		"__Uno.PrimaryLanguageOverride.SamplesApp.Wasm";
#else
#error "Missing key value"
#endif

	[TestCleanup]
	public void CleanUp()
	{
		ApplicationLanguages.PrimaryLanguageOverride = string.Empty;
	}

	[TestMethod]
	public void Test_Chinese_With_Script_Subtag()
	{
		ApplicationLanguages.PrimaryLanguageOverride = "zh-Hans-CN";
		ApplicationLanguages.Languages[0].Should().Be("zh-Hans-CN");
		ApplicationLanguages.GetAppSpecificSettingKey().Should().Be(Key);
		ApplicationData.Current.LocalSettings.Values[Key].Should().Be("zh-Hans-CN");
	}

	[TestMethod]
	public void Test_French_With_Script_Subtag()
	{
		ApplicationLanguages.PrimaryLanguageOverride = "fr-Latn-CA";
		ApplicationLanguages.Languages[0].Should().Be("fr-Latn-CA");
		ApplicationLanguages.GetAppSpecificSettingKey().Should().Be(Key);
		ApplicationData.Current.LocalSettings.Values[Key].Should().Be("fr-Latn-CA");
	}
}
