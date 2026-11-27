using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Uno.UI.Tests.Windows_UI_Xaml_Automation;

[UITestClass]
public class Given_CalendarDatePickerAutomation
{
	[TestInitialize]
	public void Init() => UnitTestsApp.App.EnsureApplication();

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/26391")]
	public void When_Header_Set_Then_Automation_Name_Is_Header()
	{
		var picker = new CalendarDatePicker { Header = "Header" };

		var peer = FrameworkElementAutomationPeer.CreatePeerForElement(picker);

		Assert.AreEqual("Header", peer.GetName());
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/26391")]
	public void When_No_Header_Then_Automation_Name_Is_Empty()
	{
		var peer = FrameworkElementAutomationPeer.CreatePeerForElement(new CalendarDatePicker());

		Assert.AreEqual(string.Empty, peer.GetName());
	}
}
