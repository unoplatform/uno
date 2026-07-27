#nullable enable

using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
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
}
