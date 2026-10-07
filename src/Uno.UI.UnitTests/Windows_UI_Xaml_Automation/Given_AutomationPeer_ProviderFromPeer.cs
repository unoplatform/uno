#nullable enable

using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Uno.UI.Tests.Windows_UI_Xaml_Automation;

[TestClass]
public class Given_AutomationPeer_ProviderFromPeer
{
	[TestInitialize]
	public void Init() => UnitTestsApp.App.EnsureApplication();

	[TestMethod]
	public void When_Peer_Is_Null_Then_Provider_Is_Null()
	{
		var peer = new FrameworkElementAutomationPeer(new Border());

		Assert.IsNull(peer.ProviderFromPeer(null));
	}

	[TestMethod]
	public void When_Peer_Is_Set_Then_Provider_Wraps_It()
	{
		var peer = new FrameworkElementAutomationPeer(new Border());
		var other = new FrameworkElementAutomationPeer(new Border());

		var provider = peer.ProviderFromPeer(other);

		Assert.IsNotNull(provider);
		Assert.AreSame(other, provider.AutomationPeer);
	}
}
