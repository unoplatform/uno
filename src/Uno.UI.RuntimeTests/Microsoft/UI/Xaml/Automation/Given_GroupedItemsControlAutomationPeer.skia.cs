#nullable enable

using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Uno.UI.RuntimeTests.Helpers;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Automation;

[TestClass]
[RunsOnUIThread]
public class Given_GroupedItemsControlAutomationPeer
{
	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/23934")]
	[DataRow(false, false)]
	[DataRow(true, false)]
	[DataRow(false, true)]
	[DataRow(true, true)]
	public async Task When_Grouped_Items_Change_Then_Only_Removed_Peers_Are_Evicted(bool replace, bool changeGroup)
	{
		var removed = new object();
		var sameGroupSurvivor = new object();
		var shared = new object();
		var otherGroupSurvivor = new object();
		var firstGroup = new ObservableCollection<object> { removed, sameGroupSurvivor, shared };
		var secondGroup = new ObservableCollection<object> { shared, otherGroupSurvivor };
		var groups = new ObservableCollection<ObservableCollection<object>> { firstGroup, secondGroup };
		var source = new CollectionViewSource { IsSourceGrouped = true, Source = groups };
		var listView = new ListView { ItemsSource = source.View, Width = 320, Height = 400 };

		try
		{
			await UITestHelper.Load(listView);
			Assert.IsTrue(listView.IsGrouping);
			var peer = FrameworkElementAutomationPeer.CreatePeerForElement(listView) as ItemsControlAutomationPeer;
			Assert.IsNotNull(peer);
			var removedPeer = peer.CreateItemAutomationPeer(removed);
			var sameGroupPeer = peer.CreateItemAutomationPeer(sameGroupSurvivor);
			var sharedPeer = peer.CreateItemAutomationPeer(shared);
			var otherGroupPeer = peer.CreateItemAutomationPeer(otherGroupSurvivor);

			if (changeGroup)
			{
				if (replace)
				{
					groups[0] = new ObservableCollection<object> { new object() };
				}
				else
				{
					groups.RemoveAt(0);
				}
			}
			else if (replace)
			{
				firstGroup[0] = new object();
			}
			else
			{
				firstGroup.RemoveAt(0);
			}

			await WindowHelper.WaitForIdle();

			Assert.AreSame(otherGroupPeer, peer.CreateItemAutomationPeer(otherGroupSurvivor));
			Assert.AreSame(sharedPeer, peer.CreateItemAutomationPeer(shared),
				"Removing one occurrence must not evict an item still present in another group.");
			if (!changeGroup)
			{
				Assert.AreSame(sameGroupPeer, peer.CreateItemAutomationPeer(sameGroupSurvivor));
			}
			Assert.AreNotSame(removedPeer, peer.CreateItemAutomationPeer(removed),
				"The removed item's cached peer must be released.");
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}
}
