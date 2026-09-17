using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.RuntimeTests.FlyoutPages;
using Uno.UI.RuntimeTests.Helpers;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

public partial class Given_MenuFlyout
{
	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Presenter_AutomationId_Is_Requested_Then_Object_Local_Flyout_Name_Is_Used()
	{
		var page = new NamedMenuFlyoutPage();
		await UITestHelper.Load(page);

		page.TargetButton.Flyout = null;
		page.SecondTargetButton.Flyout = page.NamedMenuFlyout;

		try
		{
			page.NamedMenuFlyout.ShowAt(page.SecondTargetButton);
			await WindowHelper.WaitForIdle();

			var presenter = VisualTreeHelper
				.GetOpenPopupsForXamlRoot(page.XamlRoot)
				.Select(popup => popup.Child)
				.OfType<MenuFlyoutPresenter>()
				.LastOrDefault();
			Assert.IsNotNull(presenter);
			var peer = FrameworkElementAutomationPeer.CreatePeerForElement(presenter);
			Assert.IsNotNull(peer);

			Assert.AreEqual("namedMenuFlyout", peer.GetAutomationId());
		}
		finally
		{
			page.NamedMenuFlyout.Hide();
			WindowHelper.WindowContent = null;
		}
	}
}
