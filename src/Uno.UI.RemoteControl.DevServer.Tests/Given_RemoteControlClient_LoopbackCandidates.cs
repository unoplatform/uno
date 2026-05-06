using Uno.UI.RemoteControl;

namespace Uno.UI.RemoteControl.DevServer.Tests;

[TestClass]
public class Given_RemoteControlClient_LoopbackCandidates
{
	[TestMethod]
	[DataRow("127.0.0.1", true)]
	[DataRow("[::1]", true)]
	[DataRow("::1", false)] // no brackets — not a valid URI endpoint
	[DataRow("localhost", false)]
	[DataRow("192.168.1.1", false)]
	[DataRow("10.0.2.2", false)]
	public void IsLoopbackAddress_ShouldMatchOnlyLoopbackLiterals(string endpoint, bool expected)
		=> RemoteControlClient.IsLoopbackAddress(endpoint).Should().Be(expected);

	[TestMethod]
	public void BuildAddressListWithLoopback_WhenNoCandidates_ReturnsSameArray()
	{
		(string, int)[] addresses = [("192.168.1.10", 5000)];

		var result = RemoteControlClient.BuildAddressListWithLoopback(addresses, [], 5000);

		result.Should().BeSameAs(addresses);
	}

	[TestMethod]
	public void BuildAddressListWithLoopback_PrependsCandidatesBeforeExistingAddresses()
	{
		(string, int)[] addresses = [("192.168.1.10", 5000)];
		string[] candidates = ["127.0.0.1", "[::1]"];

		var result = RemoteControlClient.BuildAddressListWithLoopback(addresses, candidates, 5000);

		result.Should().Equal(
			("127.0.0.1", 5000),
			("[::1]", 5000),
			("192.168.1.10", 5000));
	}

	[TestMethod]
	public void BuildAddressListWithLoopback_SkipsCandidateAlreadyPresent()
	{
		(string, int)[] addresses = [("127.0.0.1", 5000), ("192.168.1.10", 5000)];
		string[] candidates = ["127.0.0.1", "[::1]"];

		var result = RemoteControlClient.BuildAddressListWithLoopback(addresses, candidates, 5000);

		result.Should().Equal(
			("[::1]", 5000),
			("127.0.0.1", 5000),
			("192.168.1.10", 5000));
	}

	[TestMethod]
	public void BuildAddressListWithLoopback_SkipCandidateIsCaseInsensitive()
	{
		// Address already present with different casing — must still be skipped
		(string, int)[] addresses = [("[::1]", 5000)];
		string[] candidates = ["[::1]".ToUpperInvariant()];

		var result = RemoteControlClient.BuildAddressListWithLoopback(addresses, candidates, 5000);

		result.Should().Equal(("[::1]", 5000));
	}

	[TestMethod]
	public void BuildAddressListWithLoopback_DoesNotSkipCandidateWithDifferentPort()
	{
		// Address 127.0.0.1 exists but on a different port — candidate should still be prepended
		(string, int)[] addresses = [("127.0.0.1", 9999), ("192.168.1.10", 5000)];
		string[] candidates = ["127.0.0.1"];

		var result = RemoteControlClient.BuildAddressListWithLoopback(addresses, candidates, 5000);

		result.Should().Equal(
			("127.0.0.1", 5000),
			("127.0.0.1", 9999),
			("192.168.1.10", 5000));
	}

	[TestMethod]
	public void BuildAddressListWithLoopback_PreservesOrderOfCandidates()
	{
		(string, int)[] addresses = [("192.168.1.10", 5000)];
		string[] candidates = ["10.0.2.2", "127.0.0.1"];

		var result = RemoteControlClient.BuildAddressListWithLoopback(addresses, candidates, 5000);

		result.Should().Equal(
			("10.0.2.2", 5000),
			("127.0.0.1", 5000),
			("192.168.1.10", 5000));
	}
}
