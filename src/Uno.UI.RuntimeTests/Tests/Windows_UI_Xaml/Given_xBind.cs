using Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml.Controls;
using Private.Infrastructure;
using System.Threading.Tasks;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml;

[TestClass]
[RunsOnUIThread]
public class Given_xBind
{
	[TestMethod]
#if __IOS__
	[Ignore("Fails on iOS")]
#endif
	public async Task When_xBind_With_Cast()
	{
		var SUT = new When_xBind_With_Cast();
		TestServices.WindowHelper.WindowContent = SUT;
		await TestServices.WindowHelper.WaitForLoaded(SUT);

		Assert.AreEqual("ItemOther", SUT.tb.Text);

		SUT.ItemHelp.IsSelected = true;
		Assert.AreEqual("ItemHelp", SUT.tb.Text);

		SUT.ItemOther2.IsSelected = true;
		Assert.AreEqual("ItemOther2", SUT.tb.Text);

		SUT.ItemOther.IsSelected = true;
		Assert.AreEqual("ItemOther", SUT.tb.Text);
	}

	[TestMethod]
	public async Task When_xBind_With_Cast_Default_Namespace()
	{
		var SUT = new When_xBind_With_Cast_Default_Namespace();
		TestServices.WindowHelper.WindowContent = SUT;
		await TestServices.WindowHelper.WaitForLoaded(SUT);

		Assert.AreEqual("Hello", SUT.tb.Text);
	}
}
