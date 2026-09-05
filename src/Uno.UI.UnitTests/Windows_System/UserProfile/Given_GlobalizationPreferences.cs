#nullable enable

using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.Globalization.NumberFormatting;
using Windows.System.UserProfile;

namespace Uno.UI.Tests.Windows_System.UserProfile;

[TestClass]
public class Given_GlobalizationPreferences
{
	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/6908")]
	public void When_HomeGeographicRegion_Then_ReturnsValidRegion()
	{
		var region = GlobalizationPreferences.HomeGeographicRegion;

		Assert.IsTrue(
			region.Length == 2 && region.All(char.IsAsciiLetterUpper) ||
			region.Length == 3 && region.All(char.IsAsciiDigit));
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/6908")]
	public void When_HomeGeographicRegion_Then_ReturnsNonEmptyRegion()
	{
		var region = GlobalizationPreferences.HomeGeographicRegion;

		Assert.IsFalse(string.IsNullOrWhiteSpace(region));
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/6908")]
	public void When_Languages_Then_ReturnsValidLanguage()
	{
		var languages = GlobalizationPreferences.Languages;

		Assert.IsTrue(languages.Count > 0);
		Assert.IsFalse(string.IsNullOrWhiteSpace(languages[0]));
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/6908")]
	public void When_Languages_Then_ContainsNoEmptyEntry()
	{
		var languages = GlobalizationPreferences.Languages;

		Assert.IsFalse(
			languages.Any(string.IsNullOrWhiteSpace),
			$"Expected no empty language tag, got [{string.Join(", ", languages)}].");
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/6908")]
	public void When_Languages_Then_EveryEntryIsAcceptedByDecimalFormatter()
	{
		var languages = GlobalizationPreferences.Languages;

		var sut = new DecimalFormatter(new[] { languages[0].Split('_')[0] }, GlobalizationPreferences.HomeGeographicRegion);

		Assert.AreEqual(GlobalizationPreferences.HomeGeographicRegion, sut.GeographicRegion);
	}
}
