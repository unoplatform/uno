using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;

namespace Uno.UI.RuntimeTests.Tests.Microsoft_UI_Xaml_Controls;

[TestClass]
[RunsOnUIThread]
public class Given_InfoBadge
{
	[TestMethod]
	public async Task When_CornerRadius_Not_Set_Then_Half_Of_Height()
	{
		var SUT = new InfoBadge { Value = 5 };

		await UITestHelper.Load(SUT);

		var expected = SUT.ActualHeight / 2;
		Assert.IsTrue(expected > 0);
		Assert.AreEqual(new CornerRadius(expected), SUT.TemplateSettings.InfoBadgeCornerRadius);
	}

	[TestMethod]
	public async Task When_CornerRadius_Set_Locally_Then_Used_By_TemplateSettings()
	{
		var SUT = new InfoBadge { Value = 5, CornerRadius = new CornerRadius(1, 2, 3, 4) };

		await UITestHelper.Load(SUT);

		Assert.AreEqual(new CornerRadius(1, 2, 3, 4), SUT.TemplateSettings.InfoBadgeCornerRadius);
	}
}
