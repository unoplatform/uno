#nullable enable

using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Windows.AppNotifications;

namespace Uno.UI.Tests.Microsoft_Windows_AppNotifications;

[TestClass]
public class Given_AppNotificationActivatedEventArgs
{
	[TestMethod]
	public void When_Argument_Is_Empty_Arguments_Are_Empty()
	{
		var args = new AppNotificationActivatedEventArgs(string.Empty);

		Assert.AreEqual(0, args.Arguments.Count);
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
