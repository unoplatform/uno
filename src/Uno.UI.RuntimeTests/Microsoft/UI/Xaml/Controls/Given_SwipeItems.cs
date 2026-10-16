#if HAS_UNO
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation.Collections;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

// VectorChanged is reachable only through IObservableVector<SwipeItem>, which WinUI's projection does not expose.
[TestClass]
[RunsOnUIThread]
public class Given_SwipeItems
{
	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/23882")]
	public void When_Remove_Then_Raises_VectorChanged()
	{
		var item = new SwipeItem();
		var items = new SwipeItems { item };

		var changeCount = 0;
		((IObservableVector<SwipeItem>)items).VectorChanged += (_, _) => changeCount++;

		Assert.IsTrue(items.Remove(item));
		Assert.AreEqual(1, changeCount);
		Assert.AreEqual(0, items.Count);
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/23882")]
	public void When_Remove_Unknown_Item_Then_No_Notification()
	{
		var items = new SwipeItems { new SwipeItem() };

		var changeCount = 0;
		((IObservableVector<SwipeItem>)items).VectorChanged += (_, _) => changeCount++;

		Assert.IsFalse(items.Remove(new SwipeItem()));
		Assert.AreEqual(0, changeCount);
		Assert.AreEqual(1, items.Count);
	}
}
#endif
