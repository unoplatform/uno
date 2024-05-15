#if HAS_UNO
using System.Threading.Tasks;
using Private.Infrastructure;
using Uno.UI.Helpers.WinUI;

namespace Uno.UI.RuntimeTests.MUX;

[TestClass]
public class Given_DispatcherHelper
{
	[TestMethod]
	[RunsOnUIThread]
	public async Task When_RunAsync()
	{
		var dispatcherHelper = new DispatcherHelper();
		var actionTriggered = false;
		dispatcherHelper.RunAsync(() => actionTriggered = true);
		await TestServices.WindowHelper.WaitFor(() => actionTriggered);
	}
}
#endif
