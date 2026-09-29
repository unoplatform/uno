#if HAS_UNO
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.RuntimeTests.Helpers;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Markup;

/// <summary>
/// Compiled XAML accepts a plain Name where Name is get-only, like x:Name. This is an intentional WinUI divergence:
/// WinUI's XAML compiler rejects it (WMC0050), while its runtime parser accepts it (see <see cref="Given_XamlReader_Name"/>).
/// </summary>
[TestClass]
[RunsOnUIThread]
public class Given_PlainName
{
	[TestMethod]
	public async Task When_Compiled_Plain_Name_On_GetOnly_Name()
	{
		var control = new PlainName_GetOnly();

		try
		{
			await UITestHelper.Load(control, x => x.IsLoaded);

			Assert.AreEqual("PlainRun", control.PlainRunElement.Name);
			Assert.AreEqual("PlainLink", control.PlainLinkElement.Name);

			var root = (Grid)control.Content;
			var group = VisualStateManager.GetVisualStateGroups(root)[0];
			Assert.AreEqual("PlainGroup", group.Name);
			Assert.AreEqual("Wide", group.States[0].Name);

			Assert.IsTrue(VisualStateManager.GoToState(control, "Wide", false));
			Assert.AreEqual(42, ((Border)root.Children[0]).Width);
			Assert.AreEqual("Wide", group.CurrentState?.Name);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}
}
#endif
