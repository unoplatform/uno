using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.Extensions;
using Uno.UI.RuntimeTests.Helpers;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

[TestClass]
public class Given_ItemsView
{
	[TestMethod]
	[RunsOnUIThread]
	public async Task When_No_ItemTemplate_Then_Items_Shown_In_ItemContainers()
	{
		var sut = new ItemsView
		{
			Width = 200,
			Height = 200,
			ItemsSource = new[] { "Alpha", "Beta" },
		};

		await UITestHelper.Load(sut);

		var texts = sut
			.GetAllChildren()
			.OfType<ItemContainer>()
			.Select(c => (c.Child as TextBlock)?.Text)
			.ToArray();

		CollectionAssert.AreEqual(new[] { "Alpha", "Beta" }, texts);
	}
}
