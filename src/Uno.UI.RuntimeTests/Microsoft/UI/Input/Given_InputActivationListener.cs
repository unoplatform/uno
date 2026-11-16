#if HAS_UNO_WINUI || WINDOWS
using System.Collections.Generic;
using System.Reflection;
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

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaMacOS)]
	public async Task When_Key_Window_Changes_Activation_Follows()
	{
		var window = new Window();
		var activated = false;
		var states = new List<WindowActivationState>();
		try
		{
			window.Activated += (s, e) =>
			{
				activated = true;
				states.Add(e.WindowActivationState);
			};
			window.Activate();
			await TestServices.WindowHelper.WaitFor(() => activated);

			var listener = InputActivationListener.GetForWindowId(window.AppWindow.Id);

			RaiseNativeKeyWindowChange(window, isActive: false, byPointer: false);
			listener.State.Should().Be(InputActivationState.Deactivated);
			states[^1].Should().Be(WindowActivationState.Deactivated);

			RaiseNativeKeyWindowChange(window, isActive: true, byPointer: true);
			listener.State.Should().Be(InputActivationState.Activated);
			states[^1].Should().Be(WindowActivationState.PointerActivated);
		}
		finally
		{
			await TestServices.WindowHelper.WaitForIdle();
			window.Close();
		}
	}

	// Stands in for NSWindowDelegate windowDidBecomeKey/windowDidResignKey: CI can't reliably move key focus between windows.
	private static void RaiseNativeKeyWindowChange(Window window, bool isActive, bool byPointer)
	{
#if HAS_UNO
		var wrapper = window.AppWindow.NativeAppWindow;
		var method = wrapper.GetType().GetMethod("OnNativeActivationChanged", BindingFlags.Instance | BindingFlags.NonPublic);
		method.Should().NotBeNull();
		method!.Invoke(wrapper, [isActive, byPointer]);
#endif
	}
}
#endif
