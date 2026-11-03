#nullable enable

using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Automation;

[TestClass]
public class Given_AutomationPropertiesDefaults
{
	[TestMethod]
	[RunsOnUIThread]
	public void When_Properties_Are_Unset_Then_WinUI_Defaults_Are_Returned()
	{
		var button = new Button();

		Assert.AreEqual(string.Empty, AutomationProperties.GetName(button));
		Assert.AreEqual(-1, AutomationProperties.GetLevel(button));
		Assert.AreEqual(-1, AutomationProperties.GetPositionInSet(button));
		Assert.AreEqual(-1, AutomationProperties.GetSizeOfSet(button));
	}

	[TestMethod]
	[RunsOnUIThread]
	public void When_String_Properties_Are_Unset_Then_Empty_String_Is_Returned()
	{
		var button = new Button();

		Assert.AreEqual(string.Empty, AutomationProperties.GetAcceleratorKey(button), "AcceleratorKey");
		Assert.AreEqual(string.Empty, AutomationProperties.GetAccessKey(button), "AccessKey");
		Assert.AreEqual(string.Empty, AutomationProperties.GetAutomationId(button), "AutomationId");
		Assert.AreEqual(string.Empty, AutomationProperties.GetFullDescription(button), "FullDescription");
		Assert.AreEqual(string.Empty, AutomationProperties.GetHelpText(button), "HelpText");
		Assert.AreEqual(string.Empty, AutomationProperties.GetItemStatus(button), "ItemStatus");
		Assert.AreEqual(string.Empty, AutomationProperties.GetItemType(button), "ItemType");
		Assert.AreEqual(string.Empty, AutomationProperties.GetLocalizedControlType(button), "LocalizedControlType");
		Assert.AreEqual(string.Empty, AutomationProperties.GetLocalizedLandmarkType(button), "LocalizedLandmarkType");
	}

	[TestMethod]
	[RunsOnUIThread]
	public void When_String_Properties_Are_Unset_Then_Peer_Returns_Empty_String()
	{
		var button = new Button();
		var peer = FrameworkElementAutomationPeer.CreatePeerForElement(button);

		Assert.IsNotNull(peer);
		Assert.AreEqual(string.Empty, peer.GetAcceleratorKey(), "AcceleratorKey");
		Assert.AreEqual(string.Empty, peer.GetAccessKey(), "AccessKey");
		Assert.AreEqual(string.Empty, peer.GetFullDescription(), "FullDescription");
		Assert.AreEqual(string.Empty, peer.GetHelpText(), "HelpText");
		Assert.AreEqual(string.Empty, peer.GetItemStatus(), "ItemStatus");
		Assert.AreEqual(string.Empty, peer.GetItemType(), "ItemType");
		Assert.AreEqual(string.Empty, peer.GetLocalizedLandmarkType(), "LocalizedLandmarkType");
	}
}
