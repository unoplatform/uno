#if HAS_UNO
using DirectUI.Components;
using Windows.UI.Xaml.Data;

using Windows.UI.Xaml.Tests.Enterprise;

namespace Uno.UI.RuntimeTests.MUX.Windows_UI_Xaml_Data;

[TestClass]
[RunsOnUIThread]
public partial class ItemIndexRangeHelperUnitTests : BaseDxamlTestClass
{
	private void VerifyRange(ItemIndexRangeHelper.Range range, int firstIndex, uint length)
	{
		VERIFY_ARE_EQUAL(range.FirstIndex, firstIndex);
		VERIFY_ARE_EQUAL(range.Length, length);
	}
}
#endif
