using System.Globalization;
using System.Linq;
using Windows.Globalization;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Uno.UI.RuntimeTests.Tests.Windows_Globalization
{
	[TestClass]
	public class Given_ApplicationLanguages
	{
		[TestCleanup]
		public void CleanUp()
		{
			ApplicationLanguages.PrimaryLanguageOverride = null;
		}

		[TestMethod]
		public void Test_Chinese_With_Script_Subtag()
		{
			ApplicationLanguages.PrimaryLanguageOverride = "zh-Hans-CN";

			ApplicationLanguages.Languages.First().Should().Be("zh-Hans-CN");
			CultureInfo.CurrentCulture.Name.Should().BeOneOf("zh-CN", "zh-Hans-CN");
			CultureInfo.CurrentUICulture.Name.Should().BeOneOf("zh-CN", "zh-Hans-CN");
		}

		[TestMethod]
		public void Test_French_With_Script_Subtag()
		{
			ApplicationLanguages.PrimaryLanguageOverride = "fr-Latn-CA";

			ApplicationLanguages.Languages.First().Should().Be("fr-Latn-CA");
			CultureInfo.CurrentCulture.Name.Should().BeOneOf("fr-CA", "fr-Latn-CA");
			CultureInfo.CurrentUICulture.Name.Should().BeOneOf("fr-CA", "fr-Latn-CA");
		}
	}
}
