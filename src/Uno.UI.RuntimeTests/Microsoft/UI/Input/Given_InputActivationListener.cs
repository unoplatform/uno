#if HAS_UNO_WINUI || WINDOWS
using System.Threading.Tasks;
using AwesomeAssertions;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Private.Infrastructure;

namespace Uno.UI.RuntimeTests.Tests.Microsoft_UI_Input;

[TestClass]
[RunsOnUIThread]
public class Given_InputActivationListener
{
	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32)]
	public async Task When_Window_Activated_State_Is_Not_Deactivated()
	{
		var window = new Window();
		var activated = false;
		try
		{
			window.Activated += (s, e) => activated |= e.WindowActivationState != WindowActivationState.Deactivated;
			window.Activate();
			await TestServices.WindowHelper.WaitFor(() => activated);

			var listener = InputActivationListener.GetForWindowId(window.AppWindow.Id);

			listener.Should().BeSameAs(InputActivationListener.GetForWindowId(window.AppWindow.Id));
			listener.State.Should().NotBe(InputActivationState.Deactivated);
		}
		finally
		{
			await TestServices.WindowHelper.WaitForIdle();
			window.Close();
		}
	}
}
#endif
