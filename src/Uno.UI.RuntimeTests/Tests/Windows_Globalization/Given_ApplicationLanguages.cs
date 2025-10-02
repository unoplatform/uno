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
	public void Test_Chinese_With_Script_Subtag()
	{
		ApplicationLanguages.PrimaryLanguageOverride = "zh-Hans-CN";
		ApplicationLanguages.Languages[0].Should().Be("zh-Hans-CN");
		ApplicationData.Current.LocalSettings.Values["__Uno.PrimaryLanguageOverride"].Should().Be("zh-Hans-CN");
	}

	[TestMethod]
	public void Test_French_With_Script_Subtag()
	{
		ApplicationLanguages.PrimaryLanguageOverride = "fr-Latn-CA";
		ApplicationLanguages.Languages[0].Should().Be("fr-Latn-CA");
		ApplicationData.Current.LocalSettings.Values["__Uno.PrimaryLanguageOverride"].Should().Be("fr-Latn-CA");
	}
}
