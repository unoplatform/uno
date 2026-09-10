#nullable enable

using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Windows.AppNotifications;

namespace Uno.UI.Tests.Microsoft_Windows_AppNotifications;

[TestClass]
public class Given_AppNotificationActivatedEventArgs
{
	[TestMethod]
	[DataRow("", 1)]
	[DataRow(";", 1)]
	[DataRow("one=1;", 2)]
	[DataRow(";one=1", 2)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/22462")]
	public void When_Argument_Contains_Empty_Pairs_They_Are_Preserved(string argument, int expectedCount)
	{
		var args = new AppNotificationActivatedEventArgs(argument);

		Assert.AreEqual(expectedCount, args.Arguments.Count);
		Assert.AreEqual(string.Empty, args.Arguments[string.Empty]);
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/22462")]
	public void When_Arguments_Repeat_A_Key_The_Last_Value_Wins()
	{
		var args = new AppNotificationActivatedEventArgs("key=first;key=second%3Dvalue");

		Assert.AreEqual(1, args.Arguments.Count);
		Assert.AreEqual("second=value", args.Arguments["key"]);
	}

	[TestMethod]
	public void When_Created_It_Decodes_Arguments_And_Copies_User_Input()
	{
		var userInput = new Dictionary<string, string>
		{
			["reply"] = "hello",
		};
		var args = new AppNotificationActivatedEventArgs("action=open%3Bthread;empty", userInput);

		userInput["reply"] = "changed";

		Assert.AreEqual("action=open%3Bthread;empty", args.Argument);
		Assert.AreEqual("open;thread", args.Arguments["action"]);
		Assert.AreEqual(string.Empty, args.Arguments["empty"]);
		Assert.AreEqual("hello", args.UserInput["reply"]);
	}
}
