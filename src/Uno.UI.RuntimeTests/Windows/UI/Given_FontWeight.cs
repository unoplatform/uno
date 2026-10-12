namespace Uno.UI.RuntimeTests.Tests.Windows_UI;

[TestClass]
[RunsOnUIThread]
public class Given_FontWeight
{
	[TestMethod]
	public void When_PageContainingNumericFontWeight()
	{
		Assert.AreEqual(800, new PageContainingNumericFontWeight().SUT.FontWeight.Weight);
	}
}
