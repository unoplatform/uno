using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.Helpers;

namespace Uno.UI.Tests.Windows_UI_Xaml_Automation;

[TestClass]
public class Given_AutomationPeerProviderTargets
{
	[TestInitialize]
	public void Init() => UnitTestsApp.App.EnsureApplication();

	[TestMethod]
	public void When_ListViewItem_EventsSource_Resolved_Then_ItemPeer_Is_Preserved()
	{
		var listView = CreateListView("One");
		var container = (ListViewItem)listView.ContainerFromIndex(0);

		// Drive the automation tree so EventsSource is wired on container peers.
		// ListView lacks an OnCreateAutomationPeer override, so construct directly.
		var listPeer = new ListViewAutomationPeer(listView);
		listPeer.GetChildren();

		var containerPeer = FrameworkElementAutomationPeer.CreatePeerForElement(container);

		Assert.IsNotNull(containerPeer, "Container peer should exist for the realized ListViewItem.");

		var resolvedPeer = containerPeer.ResolveProviderPeer(resolveEventsSource: true);

		Assert.AreNotSame(containerPeer, resolvedPeer, "List item containers should resolve to their data/item automation peer.");
		Assert.IsInstanceOfType(resolvedPeer, typeof(ItemAutomationPeer));
		Assert.IsTrue(resolvedPeer.TryGetProviderOwner(out var owner));
		Assert.AreSame(container, owner, "The resolved item peer should stay anchored to its realized container.");
	}

	[TestMethod]
	public void When_ListViewChildren_Are_Resolved_Then_ProviderOwners_Are_DistinctContainers()
	{
		var listView = CreateListView("One", "Two");

		// ListView lacks an OnCreateAutomationPeer override, so construct directly.
		var listPeer = new ListViewAutomationPeer(listView);

		Assert.IsNotNull(listPeer, "ListView peer should exist after loading the control.");

		var children = listPeer.GetChildren();
		Assert.IsNotNull(children, "ListView should expose item peers in its automation children.");
		Assert.AreEqual(2, children.Count);

		var firstResolvedPeer = children[0].ResolveProviderPeer(resolveEventsSource: true);
		var secondResolvedPeer = children[1].ResolveProviderPeer(resolveEventsSource: true);

		Assert.IsTrue(firstResolvedPeer.TryGetProviderOwner(out var firstOwner));
		Assert.IsTrue(secondResolvedPeer.TryGetProviderOwner(out var secondOwner));

		Assert.AreSame(listView.ContainerFromIndex(0), firstOwner);
		Assert.AreSame(listView.ContainerFromIndex(1), secondOwner);
		Assert.AreNotSame(firstOwner, secondOwner, "Distinct item peers must resolve to distinct container anchors.");
	}

	private static ListView CreateListView(params string[] items)
	{
		var listView = new ListView
		{
			Template = new ControlTemplate(() => new ItemsPresenter()),
			ItemsPanel = new ItemsPanelTemplate(() => new StackPanel()),
			ItemContainerStyle = BuildBasicContainerStyle(),
			ItemsSource = items,
		};

		listView.ForceLoaded();
		return listView;
	}

	private static Style BuildBasicContainerStyle() => XamlHelper.LoadXaml<Style>("""
		<Style TargetType="ListViewItem">
			<Setter Property="Template">
				<Setter.Value>
					<ControlTemplate>
						<Grid>
							<ContentPresenter
								ContentTemplate="{Binding Path=ContentTemplate, RelativeSource={RelativeSource TemplatedParent}}"
								Content="{Binding Path=Content, RelativeSource={RelativeSource TemplatedParent}}"
								/>
						</Grid>
					</ControlTemplate>
				</Setter.Value>
			</Setter>
		</Style>
		""");
}
