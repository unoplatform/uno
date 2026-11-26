using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Uno.UI.RuntimeTests.Helpers;
using Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Markup;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

public partial class Given_Flyout
{
	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Flyout_Named_Then_Presenter_AutomationId_Is_Flyout_Name()
	{
		var page = new FindName_NonFrameworkElement_Page();
		await UITestHelper.Load(page);

		try
		{
			var presenter = await OpenAndGetPresenter(page.CompiledFlyoutElement, page.FlyoutButtonElement);

			Assert.AreEqual("CompiledFlyout", FrameworkElementAutomationPeer.CreatePeerForElement(presenter).GetAutomationId());
		}
		finally
		{
			page.CompiledFlyoutElement.Hide();
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Runtime_Xaml_Flyout_Named_Then_Presenter_AutomationId_Is_Flyout_Name()
	{
		var button = (Button)Microsoft.UI.Xaml.Markup.XamlReader.Load(
			"""
			<Button xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml" Content="Open">
				<Button.Flyout>
					<Flyout x:Name="runtimeFlyout">
						<TextBlock Text="Content" />
					</Flyout>
				</Button.Flyout>
			</Button>
			""");
		await UITestHelper.Load(button);

		var flyout = (Flyout)button.Flyout;
		try
		{
			var presenter = await OpenAndGetPresenter(flyout, button);

			Assert.AreEqual("runtimeFlyout", FrameworkElementAutomationPeer.CreatePeerForElement(presenter).GetAutomationId());
		}
		finally
		{
			flyout.Hide();
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Flyout_Named_And_Presenter_AutomationId_Set_Then_Presenter_AutomationId_Wins()
	{
		var page = new FindName_NonFrameworkElement_Page();
		await UITestHelper.Load(page);

		var flyout = page.CompiledFlyoutElement;
		Style presenterStyle = new(typeof(FlyoutPresenter));
		presenterStyle.Setters.Add(new Setter(AutomationProperties.AutomationIdProperty, "PresenterId"));
		flyout.FlyoutPresenterStyle = presenterStyle;

		try
		{
			var presenter = await OpenAndGetPresenter(flyout, page.FlyoutButtonElement);

			Assert.AreEqual("PresenterId", FrameworkElementAutomationPeer.CreatePeerForElement(presenter).GetAutomationId());
		}
		finally
		{
			flyout.Hide();
			WindowHelper.WindowContent = null;
		}
	}

	private static async Task<FlyoutPresenter> OpenAndGetPresenter(Flyout flyout, FrameworkElement target)
	{
		flyout.ShowAt(target);
		await WindowHelper.WaitForIdle();

		var presenter = VisualTreeHelper
			.GetOpenPopupsForXamlRoot(target.XamlRoot)
			.Select(popup => popup.Child)
			.OfType<FlyoutPresenter>()
			.LastOrDefault();
		Assert.IsNotNull(presenter);

		return presenter;
	}
}
