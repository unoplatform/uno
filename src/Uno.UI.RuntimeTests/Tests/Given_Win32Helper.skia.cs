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
	[DataRow(5u, DisplayName = "ERROR_ACCESS_DENIED")]
	[DataRow(0xDEADBEEFu, DisplayName = "Code with no system message")]
	public void When_Error_Message_Is_Requested_Then_It_Does_Not_Throw(uint errorCode)
	{
		// A code the system has no text for, like the ones some GPU drivers return from wglMakeCurrent, made the
		// message lookup throw on the render thread and replace the error it was describing.
		var getErrorMessage = Type.GetType("Uno.UI.Runtime.Win32.Win32Helper, Uno.UI.Runtime.Win32", throwOnError: true)!
			.GetMethod("GetErrorMessage", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static, [typeof(uint)])!;

		var message = (string)getErrorMessage.Invoke(null, [errorCode])!;

		Assert.IsFalse(string.IsNullOrWhiteSpace(message));
	}
}
