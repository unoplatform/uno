using Uno.UI.RemoteControl;

namespace Uno.UI.RemoteControl.DevServer.Tests;

[TestClass]
public class Given_RemoteControlClient_HostCandidates
{
	[TestMethod]
	public void GetDevServerHostCandidates_Browser_ReturnsLoopback()
	{
		var result = RemoteControlClient.GetDevServerHostCandidates(
			isBrowser: true, isAppleMobile: false, isAndroid: false, isAppleSimulator: false, isAndroidEmulator: false);

		result.Should().Equal("127.0.0.1", "[::1]");
	}

	[TestMethod]
	public void GetDevServerHostCandidates_Browser_TakesPriorityOverOtherFlags()
	{
		// isBrowser is checked first, so it wins even if the (impossible in practice) other flags are also set.
		var result = RemoteControlClient.GetDevServerHostCandidates(
			isBrowser: true, isAppleMobile: true, isAndroid: true, isAppleSimulator: true, isAndroidEmulator: true);

		result.Should().Equal("127.0.0.1", "[::1]");
	}

	[TestMethod]
	public void GetDevServerHostCandidates_AppleSimulator_ReturnsLoopback()
	{
		var result = RemoteControlClient.GetDevServerHostCandidates(
			isBrowser: false, isAppleMobile: true, isAndroid: false, isAppleSimulator: true, isAndroidEmulator: false);

		result.Should().Equal("127.0.0.1", "[::1]");
	}

	[TestMethod]
	public void GetDevServerHostCandidates_ApplePhysicalDevice_ReturnsNoCandidates()
	{
		var result = RemoteControlClient.GetDevServerHostCandidates(
			isBrowser: false, isAppleMobile: true, isAndroid: false, isAppleSimulator: false, isAndroidEmulator: false);

		result.Should().BeEmpty();
	}

	[TestMethod]
	public void GetDevServerHostCandidates_AndroidEmulator_ReturnsAvdAliasThenLoopback()
	{
		var result = RemoteControlClient.GetDevServerHostCandidates(
			isBrowser: false, isAppleMobile: false, isAndroid: true, isAppleSimulator: false, isAndroidEmulator: true);

		result.Should().Equal("10.0.2.2", "127.0.0.1");
	}

	[TestMethod]
	public void GetDevServerHostCandidates_AndroidPhysicalDevice_ReturnsNoCandidates()
	{
		var result = RemoteControlClient.GetDevServerHostCandidates(
			isBrowser: false, isAppleMobile: false, isAndroid: true, isAppleSimulator: false, isAndroidEmulator: false);

		result.Should().BeEmpty();
	}

	[TestMethod]
	public void GetDevServerHostCandidates_Desktop_ReturnsLoopback()
	{
		var result = RemoteControlClient.GetDevServerHostCandidates(
			isBrowser: false, isAppleMobile: false, isAndroid: false, isAppleSimulator: false, isAndroidEmulator: false);

		result.Should().Equal("127.0.0.1", "[::1]");
	}
}
