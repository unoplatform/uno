using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Uno.UI.Tests.Windows_UI_Xaml_Automation;

[UITestClass]
public class Given_ListViewAutomationName
{
	[TestInitialize]
	public void Init() => UnitTestsApp.App.EnsureApplication();

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/26391")]
	public void When_ListView_Header_Set_Then_Automation_Name_Is_Header()
	{
		var listView = new ListView { Header = "Header" };

		var peer = FrameworkElementAutomationPeer.CreatePeerForElement(listView);

		Assert.AreEqual("Header", peer.GetName());
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/26391")]
	public void When_GridView_Header_Set_Then_Automation_Name_Is_Header()
	{
		var gridView = new GridView { Header = "Header" };

		var peer = FrameworkElementAutomationPeer.CreatePeerForElement(gridView);

		Assert.AreEqual("Header", peer.GetName());
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/26391")]
	public void When_ListView_Has_No_Header_Then_Automation_Name_Is_Empty()
	{
		var peer = FrameworkElementAutomationPeer.CreatePeerForElement(new ListView());

		Assert.AreEqual(string.Empty, peer.GetName());
	}
}
