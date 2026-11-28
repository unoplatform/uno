using Windows.Globalization;

namespace Uno.UI.RuntimeTests.Tests.Windows_Globalization;

[TestClass]
public class Given_Language
{
	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25451")]
	public void When_CurrentInputMethodLanguageTag()
	{
		Language.CurrentInputMethodLanguageTag.Should().NotBeNull();
	}
}
