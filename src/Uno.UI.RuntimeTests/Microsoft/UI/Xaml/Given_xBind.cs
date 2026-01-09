using System.Threading.Tasks;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml.Controls;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml;

[TestClass]
[RunsOnUIThread]
public class Given_xBind
{
	[TestMethod]
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

	[TestMethod]
	public async Task When_xBind_InterfaceProperty()
	{
		var SUT = new When_xBind_InterfaceProperty();
		TestServices.WindowHelper.WindowContent = SUT;
		await TestServices.WindowHelper.WaitForLoaded(SUT);

		// Test accessing Count property on IReadOnlyList<T> where concrete type is array
		Assert.AreEqual("3", SUT.arrayCountText.Text, "Array Count should be accessible via IReadOnlyList interface");

		// Test accessing Count property on IReadOnlyList<T> where concrete type is List<T>
		Assert.AreEqual("2", SUT.listCountText.Text, "List Count should be accessible");

		// Test accessing indexer via interface
		Assert.AreEqual("Item1", SUT.indexerText.Text, "Indexer should be accessible via IReadOnlyList interface");
	}
}
