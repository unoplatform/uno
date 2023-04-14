using System.Threading.Tasks;
using Private.Infrastructure;
using Windows.UI.Xaml.Controls;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls.Icons;

[TestClass]
public class Given_SymbolIcon
{
	[TestMethod]
	public void When_Defaults()
	{
		var symbolIcon = new SymbolIcon();
		Assert.AreEqual(Symbol.Emoji, symbolIcon.Symbol);
	}

	[TestMethod]
	public async Task Validate_Size()
	{
		var symbolIcon = new SymbolIcon() { Symbol = Symbol.Home };
		TestServices.WindowHelper.WindowContent = symbolIcon;
		await TestServices.WindowHelper.WaitForLoaded(symbolIcon);

		Assert.AreEqual(20.0, symbolIcon.ActualWidth);
		Assert.AreEqual(20.0, symbolIcon.ActualHeight);
	}
}
