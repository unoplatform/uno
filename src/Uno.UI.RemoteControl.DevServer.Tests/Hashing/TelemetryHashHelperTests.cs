using FluentAssertions.Execution;
using Uno.UI.RemoteControl.Server.Helpers;

namespace Uno.UI.RemoteControl.DevServer.Tests.Hashing;

[TestClass]
public class Given_TelemetryHashHelper
{
	[TestMethod]
	public void When_Hash_IsStable_ForSameInput_Then_ResultIsDeterministic()
	{
		const string input = "test-value";

		var hash1 = TelemetryHashHelper.Hash(input);
		var hash2 = TelemetryHashHelper.Hash(input);
		hash1.Should().Be(hash2);
	}

	[TestMethod]
	public void When_Hash_Differs_ForDifferentInputs_Then_ResultsAreDistinct()
	{
		var hash1 = TelemetryHashHelper.Hash("foo");
		var hash2 = TelemetryHashHelper.Hash("bar");
		hash1.Should().NotBe(hash2);
	}

	[TestMethod]
	public void When_Hash_EmptyString_Then_ReturnsEmpty()
	{
		TelemetryHashHelper.Hash("").Should().Be("empty");
	}

	[TestMethod]
	public void When_Hash_Null_Then_ReturnsUnknown()
	{
		TelemetryHashHelper.Hash(null).Should().Be("unknown");
	}

	[TestMethod]
	public void When_Hash_Known_RainbowTable_Then_ReturnsExpectedHash()
	{
		using var c = new AssertionScope();

		TelemetryHashHelper.Hash("abcdefghijklmnopqrstuvwxyz").Should().Be("c3fcd3d76192e4007dfb496cca67e13b");
		TelemetryHashHelper.Hash("1234567890").Should().Be("e807f1fcf82d132f9bb018ca6738a19f");
		TelemetryHashHelper.Hash("password").Should().Be("5f4dcc3b5aa765d61d8327deb882cf99");
		TelemetryHashHelper.Hash("letmein").Should().Be("0d107d09f5bbe40cade3de5c71e9e9b7");
		TelemetryHashHelper.Hash("qwerty").Should().Be("d8578edf8458ce06fbc5bb76a58c5ca4");
		TelemetryHashHelper.Hash("240610708").Should().Be("0e462097431906509019562988736854");
		TelemetryHashHelper.Hash("QNKCDZO").Should().Be("0e830400451993494058024219903391");
	}

	[TestMethod]
	public void When_Hash_ObjectTypes_Then_HandlesStringCharArrayAndOtherTypes()
	{
		using var c = new AssertionScope();

		TelemetryHashHelper.Hash(null).Should().Be("unknown");
		TelemetryHashHelper.Hash(string.Empty).Should().Be("empty");
		TelemetryHashHelper.Hash("foo").Should().Be(TelemetryHashHelper.Hash("foo"));
		TelemetryHashHelper.Hash(new char[] { 'f', 'o', 'o' }).Should().Be(TelemetryHashHelper.Hash("foo"));
		TelemetryHashHelper.Hash(123).Should().Be(TelemetryHashHelper.Hash("123"));
		TelemetryHashHelper.Hash(true).Should().Be(TelemetryHashHelper.Hash("True"));
		TelemetryHashHelper.Hash(new { X = 1, Y = 2 }).Should().Be(TelemetryHashHelper.Hash(new { X = 1, Y = 2 }.ToString()));
	}
}
