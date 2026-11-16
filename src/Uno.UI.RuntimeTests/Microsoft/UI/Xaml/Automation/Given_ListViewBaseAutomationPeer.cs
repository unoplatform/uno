#nullable enable

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.RuntimeTests.Helpers;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Automation;

[TestClass]
public class Given_ListViewBaseAutomationPeer
{
	[TestMethod]
	[RunsOnUIThread]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/24552")]
	public async Task When_GetChildrenCore_Overridden_With_WinUI_Signature()
	{
		var listView = new CustomPeerListView
		{
			ItemsSource = new[] { "One", "Two", "Three" },
		};
		await UITestHelper.Load(listView);

		var peer = FrameworkElementAutomationPeer.CreatePeerForElement(listView);
		Assert.IsInstanceOfType<CustomListViewAutomationPeer>(peer);

		var children = peer.GetChildren();

		var customPeer = (CustomListViewAutomationPeer)peer;
		Assert.IsTrue(customPeer.GetChildrenCoreCalled);
		Assert.IsNotNull(children);
		Assert.AreEqual(3, children.Count);
	}

	private sealed partial class CustomPeerListView : ListView
	{
		protected override AutomationPeer OnCreateAutomationPeer() => new CustomListViewAutomationPeer(this);
	}

	private sealed partial class CustomListViewAutomationPeer : ListViewAutomationPeer
	{
		public CustomListViewAutomationPeer(ListView owner) : base(owner)
		{
		}

		public bool GetChildrenCoreCalled { get; private set; }

		// Must compile with the WinUI signature (IList<AutomationPeer>), not a covariant List<AutomationPeer>.
		protected override IList<AutomationPeer> GetChildrenCore()
		{
			GetChildrenCoreCalled = true;
			return base.GetChildrenCore();
		}
	}
}
