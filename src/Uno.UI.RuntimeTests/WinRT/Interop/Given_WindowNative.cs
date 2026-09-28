#if HAS_UNO
using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using Windows.UI.Popups;
using WinRT.Interop;

namespace Uno.UI.RuntimeTests.Tests.WinRT_Interop;

[TestClass]
[RunsOnUIThread]
[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
public class Given_WindowNative
{
	[TestMethod]
	public void When_GetWindowHandle_Then_Matches_AppWindow_Id()
	{
		if (TestServices.WindowHelper.IsXamlIsland)
		{
			Assert.Inconclusive("Window handles are not available in Uno Islands.");
		}

		var window = TestServices.WindowHelper.CurrentTestWindow;

		var handle = WindowNative.GetWindowHandle(window);

		Assert.AreEqual(window.AppWindow.Id.Value, (ulong)handle.ToInt64());
	}

	[TestMethod]
	public void When_GetWindowHandle_Of_Non_Window_Then_Throws()
		=> Assert.ThrowsExactly<InvalidOperationException>(() => WindowNative.GetWindowHandle(new object()));

	[TestMethod]
	public void When_InitializeWithWindow_Then_MessageDialog_Associated()
	{
		if (TestServices.WindowHelper.IsXamlIsland)
		{
			Assert.Inconclusive("MessageDialog is not supported in Uno Islands.");
		}

		var window = TestServices.WindowHelper.CurrentTestWindow;
		var dialog = new MessageDialog("Content");

		InitializeWithWindow.Initialize(dialog, WindowNative.GetWindowHandle(window));

		Assert.AreSame(window, dialog.AssociatedWindow);
	}
}
#endif
