using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Tests.Enterprise;

namespace Uno.UI.RuntimeTests.MUX.Windows_UI_Xaml_Data;

[TestClass]
public partial class ItemIndexRangeIntegrationTests : BaseDxamlTestClass
{
	[TestMethod]
	[RunsOnUIThread]
	public void ValidateFirstIndex()
	{
		var itemIndexRange = new ItemIndexRange(1, 5);
		VERIFY_ARE_EQUAL(itemIndexRange.FirstIndex, 1);
	}

	[TestMethod]
	[RunsOnUIThread]
	public void ValidateLength()
	{
		var itemIndexRange = new ItemIndexRange(0, 5);
		VERIFY_ARE_EQUAL(itemIndexRange.Length, (uint)5);
	}

	[TestMethod]
	[RunsOnUIThread]
	public void ValidateLastIndex()
	{
		var itemIndexRange = new ItemIndexRange(0, 5);
		VERIFY_ARE_EQUAL(itemIndexRange.LastIndex, 4);
	}

	[TestMethod]
	public void CanCreateAndUseOffUIThread()
	{
		var itemIndexRange = new ItemIndexRange(0, 5);

		VERIFY_ARE_EQUAL(itemIndexRange.FirstIndex, 0);
		VERIFY_ARE_EQUAL(itemIndexRange.Length, (uint)5);
		VERIFY_ARE_EQUAL(itemIndexRange.LastIndex, 4);
	}
}
