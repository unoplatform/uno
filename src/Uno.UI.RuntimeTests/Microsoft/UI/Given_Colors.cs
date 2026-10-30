using Microsoft.UI;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Uno.UI.RuntimeTests.Tests.Microsoft_UI;

[TestClass]
public class Given_Colors
{
	[TestMethod]
	public void When_Colors_Then_SameAssemblyAsWindowId()
	{
		// Colors, ColorHelper and WindowId all ship in Microsoft.InteractiveExperiences.Projection on WinAppSDK.
		Assert.AreSame(typeof(WindowId).Assembly, typeof(Colors).Assembly);
		Assert.AreSame(typeof(WindowId).Assembly, typeof(ColorHelper).Assembly);
	}
}
