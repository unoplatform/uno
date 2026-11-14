using System.Globalization;
using Windows.Globalization;
using Windows.Globalization.NumberFormatting;

namespace Uno.UI.RuntimeTests.Tests.Windows_Globalization;

[TestClass]
public class Given_CurrencyFormatter
{
	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/24761")]
	[DataRow("de-DE", "en-US", false)]
	[DataRow("en-US", "de-DE", true)]
	public void When_Language_Differs_From_CurrentCulture_Then_Symbol_Placement_Follows_Language(string language, string currentCulture, bool symbolFirst)
	{
		var culture = CultureInfo.CurrentCulture;
		var primaryLanguageOverride = ApplicationLanguages.PrimaryLanguageOverride;
		try
		{
			ApplicationLanguages.PrimaryLanguageOverride = language;
			CultureInfo.CurrentCulture = new CultureInfo(currentCulture);

			// WinRT marks the text direction with U+200E and U+200F.
			var text = new CurrencyFormatter("EUR").Format(1234.5).Replace("\u200E", "").Replace("\u200F", "");

			Assert.AreEqual(symbolFirst, text.StartsWith('€'), text);
			Assert.AreEqual(!symbolFirst, text.EndsWith('€'), text);
		}
		finally
		{
			ApplicationLanguages.PrimaryLanguageOverride = primaryLanguageOverride;
			CultureInfo.CurrentCulture = culture;
		}
	}
}
