#nullable enable

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.Runtime.Skia.Win32;

namespace Uno.UI.Tests.Microsoft_Windows_AppNotifications;

[TestClass]
public class Given_Win32AppNotificationActivation
{
	private const string ToastArgument = "----AppNotificationActivated:";

	[TestMethod]
	public void When_No_Activation_Arguments_Do_Not_Decode_Startup()
		=> Assert.IsFalse(Win32AppNotificationActivation.IsAppNotificationLaunch(Array.Empty<string>()));

	[TestMethod]
	[DataRow("", false)]
	[DataRow("--other-activation=background", false)]
	[DataRow("----AppNotificationActivated:", true)]
	[DataRow("----AppNotificationActivated", false)]
	[DataRow("--AppNotificationActivated:", false)]
	[DataRow("----appnotificationactivated:", false)]
	[DataRow("----AppNotificationActivated:payload", false)]
	[DataRow("prefix----AppNotificationActivated:", false)]
	[DataRow("--value=----AppNotificationActivated:", false)]
	[DataRow("----WindowsAppRuntimePushServer:", false)]
	[DataRow("----WindowsAppRuntimePushServer:-Payload:push", false)]
	[DataRow("----ms-protocol:https://example.com", false)]
	[DataRow("----ms-protocol:ms-encodedlaunch://App?ContractId=Windows.Push", false)]
	[DataRow("----ms-protocol:ms-encodedlaunch://App?ContractId=Windows.Toast", false)]
	public void When_Only_The_Registered_Toast_Com_Argument_Allows_Startup_Decode(string argument, bool expected)
		=> Assert.AreEqual(expected, Win32AppNotificationActivation.IsAppNotificationLaunch(new[] { "TestApp.exe", argument }));

	[TestMethod]
	[DataRow("----WindowsAppRuntimePushServer:", false)]
	[DataRow("----WindowsAppRuntimePushServer:", true)]
	[DataRow("----WindowsAppRuntimePushServer:-Payload:push", false)]
	[DataRow("----WindowsAppRuntimePushServer:-Payload:push", true)]
	[DataRow("----ms-protocol:https://example.com", false)]
	[DataRow("----ms-protocol:https://example.com", true)]
	[DataRow("----ms-protocol:ms-encodedlaunch://App?ContractId=Windows.Push", false)]
	[DataRow("----ms-protocol:ms-encodedlaunch://App?ContractId=Windows.Push", true)]
	[DataRow("--value=----WindowsAppRuntimePushServer:", false)]
	[DataRow("--value=----WindowsAppRuntimePushServer:", true)]
	[DataRow("--value=----ms-protocol:https://example.com", false)]
	[DataRow("--value=----ms-protocol:https://example.com", true)]
	public void When_Higher_Priority_Contract_Prevents_Toast_Decode_Regardless_Of_Order(string otherArgument, bool toastFirst)
	{
		var arguments = toastFirst
			? new[] { "TestApp.exe", ToastArgument, otherArgument }
			: new[] { "TestApp.exe", otherArgument, ToastArgument };

		Assert.IsFalse(Win32AppNotificationActivation.IsAppNotificationLaunch(arguments));
	}

	[TestMethod]
	[DataRow("----WindowsAppRuntimePushServerExtra:")]
	[DataRow("----WindowsAppRuntimePushServer")]
	[DataRow("----windowsappruntimepushserver:")]
	[DataRow("----ms-protocol-extra:")]
	[DataRow("----MS-PROTOCOL:")]
	public void When_Another_Argument_Is_Not_An_Exact_Sdk_Qualifier_It_Does_Not_Shadow_The_Toast(string argument)
		=> Assert.IsTrue(Win32AppNotificationActivation.IsAppNotificationLaunch(new[] { "TestApp.exe", argument, ToastArgument }));
}
