#nullable enable

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Windows.AppLifecycle;

namespace Uno.UI.Tests.Microsoft_Windows_AppLifecycle;

[TestClass]
public class Given_CommandLineActivationParser
{
	[TestMethod]
	public void When_Protocol_Marker()
	{
		var result = CommandLineActivationParser.TryParse(["----ms-protocol:myscheme://a/b?c=d"], allowBareUri: false, out var uri, out var remaining);

		Assert.IsTrue(result);
		Assert.AreEqual("myscheme://a/b?c=d", uri!.OriginalString);
		Assert.AreEqual("", remaining);
	}

	[TestMethod]
	public void When_Data_Contains_Colons()
	{
		var result = CommandLineActivationParser.TryParse(["----ms-protocol:myscheme:host/path?a=b:c"], allowBareUri: false, out var uri, out _);

		Assert.IsTrue(result);
		Assert.AreEqual("myscheme:host/path?a=b:c", uri!.OriginalString);
	}

	[TestMethod]
	public void When_Marker_Not_At_Start_Of_Argument()
	{
		var result = CommandLineActivationParser.TryParse(["x----ms-protocol:myscheme:d"], allowBareUri: false, out var uri, out var remaining);

		Assert.IsTrue(result);
		Assert.AreEqual("myscheme:d", uri!.OriginalString);
		Assert.AreEqual("", remaining);
	}

	[TestMethod]
	public void When_Other_Arguments_Surround_Marker()
	{
		var result = CommandLineActivationParser.TryParse(
			["--first", "----ms-protocol:myscheme://x", "with space", "quote\"d", ""],
			allowBareUri: false,
			out var uri,
			out var remaining);

		Assert.IsTrue(result);
		Assert.AreEqual("myscheme://x", uri!.OriginalString);
		Assert.AreEqual("--first \"with space\" \"quote\\\"d\" \"\"", remaining);
	}

	[TestMethod]
	public void When_Protocol_Marker_Wins_Over_Push_Marker()
	{
		var result = CommandLineActivationParser.TryParse(
			["----WindowsAppRuntimePushServer:payload", "----ms-protocol:myscheme://x"],
			allowBareUri: false,
			out var uri,
			out var remaining);

		Assert.IsTrue(result);
		Assert.AreEqual("myscheme://x", uri!.OriginalString);
		Assert.AreEqual("----WindowsAppRuntimePushServer:payload", remaining);
	}

	[TestMethod]
	[DataRow("----WindowsAppRuntimePushServer:-Payload:\"abc\"")]
	[DataRow("----AppNotificationActivated:")]
	public void When_Unsupported_Marker(string argument)
	{
		var result = CommandLineActivationParser.TryParse(["--keep", argument], allowBareUri: false, out var uri, out var remaining);

		Assert.IsFalse(result);
		Assert.IsNull(uri);
		Assert.AreEqual("--keep", remaining);
	}

	[TestMethod]
	[DataRow("----ms-protocol:")]
	[DataRow("----ms-protocol:::")]
	public void When_Protocol_Data_Is_Not_A_Uri(string argument)
	{
		var result = CommandLineActivationParser.TryParse([argument, "--keep"], allowBareUri: false, out var uri, out var remaining);

		Assert.IsFalse(result);
		Assert.IsNull(uri);
		Assert.AreEqual("--keep", remaining);
	}

	[TestMethod]
	public void When_No_Marker()
	{
		var result = CommandLineActivationParser.TryParse(["--sample=Foo", "myscheme://x"], allowBareUri: false, out var uri, out var remaining);

		Assert.IsFalse(result);
		Assert.IsNull(uri);
		Assert.IsNull(remaining);
	}

	[TestMethod]
	public void When_No_Arguments()
	{
		var result = CommandLineActivationParser.TryParse([], allowBareUri: true, out var uri, out var remaining);

		Assert.IsFalse(result);
		Assert.IsNull(uri);
		Assert.IsNull(remaining);
	}

	[TestMethod]
	public void When_Bare_Uri_Allowed()
	{
		var result = CommandLineActivationParser.TryParse(["myscheme://a/b?c=d"], allowBareUri: true, out var uri, out var remaining);

		Assert.IsTrue(result);
		Assert.AreEqual("myscheme://a/b?c=d", uri!.OriginalString);
		Assert.AreEqual("", remaining);
	}

	[TestMethod]
	public void When_Bare_Uri_Not_Allowed()
	{
		var result = CommandLineActivationParser.TryParse(["myscheme://a/b"], allowBareUri: false, out var uri, out var remaining);

		Assert.IsFalse(result);
		Assert.IsNull(uri);
		Assert.IsNull(remaining);
	}

	[TestMethod]
	[DataRow("/home/user/document.txt")]
	[DataRow("file:///home/user/document.txt")]
	[DataRow("--verbose")]
	[DataRow("relative/path")]
	public void When_Bare_Argument_Is_Not_A_Protocol_Uri(string argument)
	{
		var result = CommandLineActivationParser.TryParse([argument], allowBareUri: true, out var uri, out var remaining);

		Assert.IsFalse(result);
		Assert.IsNull(uri);
		Assert.IsNull(remaining);
	}

	[TestMethod]
	public void When_Bare_Uri_Is_Not_The_Only_Argument()
	{
		var result = CommandLineActivationParser.TryParse(["--verbose", "myscheme://a/b"], allowBareUri: true, out var uri, out var remaining);

		Assert.IsFalse(result);
		Assert.IsNull(uri);
		Assert.IsNull(remaining);
	}

	[TestMethod]
	public void When_Marker_And_Bare_Uri_Allowed()
	{
		var result = CommandLineActivationParser.TryParse(["----ms-protocol:myscheme://x"], allowBareUri: true, out var uri, out _);

		Assert.IsTrue(result);
		Assert.AreEqual("myscheme://x", uri!.OriginalString);
	}

	[TestMethod]
	public void When_Encoded_Launch_Falls_Back_To_Protocol()
	{
		// No typed File/StartupTask args yet, so this matches the Windows App SDK's fallback for an unknown contract.
		const string Data = "ms-encodedlaunch:App/?ContractId=Windows.File&Verb=open&File=C%3A%5Ca.txt";

		var result = CommandLineActivationParser.TryParse(["----ms-protocol:" + Data], allowBareUri: false, out var uri, out _);

		Assert.IsTrue(result);
		Assert.AreEqual(Data, uri!.OriginalString);
	}
}
