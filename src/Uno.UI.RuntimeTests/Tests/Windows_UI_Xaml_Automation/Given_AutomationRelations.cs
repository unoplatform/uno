using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.RuntimeTests.Helpers;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Automation
{
	/// <summary>
	/// Runtime tests for W32-03: the DescribedBy / ControllerFor / FlowsTo / FlowsFrom relations must
	/// be surfaced by the peer (they feed the Win32 UIA relation properties, previously hard-coded null).
	/// Validates the shared peer-level plumbing that the Win32 backend consumes.
	/// </summary>
	[TestClass]
	public class Given_AutomationRelations
	{
		[TestMethod]
		[RunsOnUIThread]
		public async Task When_DescribedBy_Set_Then_Peer_Returns_It()
		{
			var target = new TextBlock { Text = "Description", Name = "DescTarget" };
			var described = new TextBox();
			var panel = new StackPanel { Children = { target, described } };
			await UITestHelper.Load(panel);

			described.SetValue(AutomationProperties.DescribedByProperty, new List<DependencyObject> { target });

			var peer = FrameworkElementAutomationPeer.CreatePeerForElement(described);
			Assert.IsNotNull(peer);

			var describedBy = peer.GetDescribedBy()?.ToList();
			Assert.IsNotNull(describedBy, "DescribedBy must not be null when set");
			Assert.IsTrue(describedBy.Count > 0, "DescribedBy must include the target peer");
		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_ControlledPeers_Set_Then_Peer_Returns_It()
		{
			var controlled = new TextBox { Name = "ControlledField" };
			var controller = new Button { Content = "Controller" };
			var panel = new StackPanel { Children = { controller, controlled } };
			await UITestHelper.Load(panel);

			controller.SetValue(AutomationProperties.ControlledPeersProperty, new List<UIElement> { controlled });

			var peer = FrameworkElementAutomationPeer.CreatePeerForElement(controller);
			Assert.IsNotNull(peer);

			var controlledPeers = peer.GetControlledPeers()?.ToList();
			Assert.IsNotNull(controlledPeers, "ControlledPeers must not be null when set");
			Assert.IsTrue(controlledPeers.Count > 0, "ControlledPeers must include the controlled peer");
		}
	}
}
