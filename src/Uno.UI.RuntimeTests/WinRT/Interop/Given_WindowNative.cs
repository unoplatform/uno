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
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.SkiaIslands)]
	public void When_GetWindowHandle_Then_Matches_AppWindow_Id()
	{
		var window = TestServices.WindowHelper.CurrentTestWindow;

		var handle = WindowNative.GetWindowHandle(window);

		Assert.AreEqual(window.AppWindow.Id.Value, (ulong)handle.ToInt64());
	}

	[TestMethod]
	public void When_GetWindowHandle_Of_Non_Window_Then_Throws()
		=> Assert.ThrowsExactly<InvalidOperationException>(() => WindowNative.GetWindowHandle(new object()));

	[TestMethod]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.SkiaIslands)]
	public void When_InitializeWithWindow_Then_MessageDialog_Associated()
	{
		var window = TestServices.WindowHelper.CurrentTestWindow;
		var dialog = new MessageDialog("Content");

		InitializeWithWindow.Initialize(dialog, WindowNative.GetWindowHandle(window));

		Assert.AreSame(window, dialog.AssociatedWindow);
	}
}
#endif
