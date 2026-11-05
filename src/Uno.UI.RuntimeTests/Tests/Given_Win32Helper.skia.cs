#nullable enable

using System;
using System.Reflection;

namespace Uno.UI.RuntimeTests.Tests;

[TestClass]
[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32)]
public class Given_Win32Helper
{
	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25373")]
	[DataRow(5u, false, DisplayName = "ERROR_ACCESS_DENIED")]
	[DataRow(0xDEADBEEFu, true, DisplayName = "Code with no system message")]
	public void When_Error_Message_Is_Requested_Then_It_Does_Not_Throw(uint errorCode, bool expectFallback)
	{
		// A code the system has no text for, like the ones some GPU drivers return from wglMakeCurrent, made the
		// message lookup throw on the render thread and replace the error it was describing.
		// 6.x names the type differently, so a backport of this test still finds it.
		var helperType = Type.GetType("Uno.UI.Runtime.Win32.Win32Helper, Uno.UI.Runtime.Win32")
			?? Type.GetType("Uno.UI.Runtime.Skia.Win32.Win32Helper, Uno.UI.Runtime.Skia.Win32", throwOnError: true)!;
		var getErrorMessage = helperType
			.GetMethod("GetErrorMessage", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static, [typeof(uint)])!;

		var message = (string)getErrorMessage.Invoke(null, [errorCode])!;

		Assert.IsFalse(string.IsNullOrWhiteSpace(message));
		if (expectFallback)
		{
			StringAssert.Contains(message, errorCode.ToString("X8"));
		}
		else
		{
			// The system text is localized, so only check that it isn't the fallback.
			Assert.IsFalse(message.StartsWith("Unknown error", StringComparison.Ordinal), message);
		}
	}
}
