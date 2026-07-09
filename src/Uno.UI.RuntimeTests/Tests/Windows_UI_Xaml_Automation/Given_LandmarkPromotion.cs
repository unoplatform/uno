using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Automation
{
	/// <summary>
	/// Runtime tests for SH-04: an element that sets AutomationProperties.LandmarkType or
	/// LocalizedLandmarkType is force-promoted into the UIA tree (via LandmarkTargetAutomationPeer,
	/// reporting Group) even when it isn't a control, matching WinUI. Excluded on native Android/iOS
	/// (their OnCreateAutomationPeer comes from generated mixins, not the shared promotion path).
	/// </summary>
	[TestClass]
	public class Given_LandmarkPromotion
	{
		[TestMethod]
		[RunsOnUIThread]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeAndroid | RuntimeTestPlatforms.NativeIOS)]
		public void When_LandmarkType_Set_Then_Promoted_As_Group()
		{
			var grid = new Grid();
			AutomationProperties.SetLandmarkType(grid, AutomationLandmarkType.Navigation);

			var peer = FrameworkElementAutomationPeer.CreatePeerForElement(grid);

			Assert.IsNotNull(peer, "A landmark element must be promoted into the UIA tree");
			Assert.AreEqual(AutomationControlType.Group, peer.GetAutomationControlType());
			Assert.AreEqual(AutomationLandmarkType.Navigation, peer.GetLandmarkType());
		}

		[TestMethod]
		[RunsOnUIThread]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeAndroid | RuntimeTestPlatforms.NativeIOS)]
		public void When_LocalizedLandmarkType_Set_Then_Promoted()
		{
			var grid = new Grid();
			AutomationProperties.SetLocalizedLandmarkType(grid, "Primary navigation");

			var peer = FrameworkElementAutomationPeer.CreatePeerForElement(grid);

			Assert.IsNotNull(peer, "An element with a localized landmark must be promoted");
			Assert.AreEqual(AutomationControlType.Group, peer.GetAutomationControlType());
		}

		[TestMethod]
		[RunsOnUIThread]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeAndroid | RuntimeTestPlatforms.NativeIOS)]
		public void When_Name_And_Landmark_Then_NamedContainer_Precedence_But_Landmark_Exposed()
		{
			var grid = new Grid();
			AutomationProperties.SetName(grid, "Navigation");
			AutomationProperties.SetLandmarkType(grid, AutomationLandmarkType.Navigation);

			var peer = FrameworkElementAutomationPeer.CreatePeerForElement(grid);

			Assert.IsNotNull(peer);
			Assert.IsInstanceOfType(peer, typeof(NamedContainerAutomationPeer), "Name/LabeledBy takes precedence over the landmark peer");
			Assert.AreEqual(AutomationLandmarkType.Navigation, peer.GetLandmarkType(), "The landmark must still be exposed on the named-container peer");
		}

		[TestMethod]
		[RunsOnUIThread]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeAndroid | RuntimeTestPlatforms.NativeIOS | RuntimeTestPlatforms.NativeWinUI)]
		public void When_No_Name_Or_Landmark_Then_Not_Promoted()
		{
			var grid = new Grid();

			var peer = FrameworkElementAutomationPeer.CreatePeerForElement(grid);

			Assert.IsNull(peer, "A plain element with no name/landmark must not be force-promoted");
		}
	}
}
