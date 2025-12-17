using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using MUXControlsTestApp.Utilities;
using Private.Infrastructure;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

[TestClass]
[RunsOnUIThread]
public class Given_CommandBarFlyout
{
	[TestMethod]
	public async Task When_CommandBarFlyoutCommandBar_AlwaysExpanded()
	{
		var commandBarFlyout = new CommandBarFlyout
		{
			AlwaysExpanded = true
		};
		commandBarFlyout.PrimaryCommands.Add(new AppBarButton { Label = "Primary Command" });
		commandBarFlyout.SecondaryCommands.Add(new AppBarButton { Label = "Secondary Command" });

		var button = new Button
		{
			Content = "Open CommandBarFlyout",
		};

		FlyoutBase.SetAttachedFlyout(button, commandBarFlyout);

		TestServices.WindowHelper.WindowContent = button;
		await TestServices.WindowHelper.WaitForLoaded(button);

		FlyoutBase.ShowAttachedFlyout(button);
		await TestServices.WindowHelper.WaitForIdle();

		var popups = VisualTreeHelper.GetOpenPopupsForXamlRoot(TestServices.WindowHelper.XamlRoot);

		var commandBarPopup = popups.FirstOrDefault(p => VisualTreeUtils.FindVisualChildByType<CommandBarFlyoutCommandBar>(p.Child) is not null);
		Assert.IsNotNull(commandBarPopup);
		var commandBar = VisualTreeUtils.FindVisualChildByType<CommandBarFlyoutCommandBar>(commandBarPopup?.Child);
		Assert.IsNotNull(commandBar);

		bool wasClosed = false;
		commandBar.Closed += (s, e) => wasClosed = true;
		commandBar.IsOpen = false;

		await TestServices.WindowHelper.WaitForIdle();
		Assert.IsTrue(commandBar.IsOpen, "CommandBarFlyout should remain open when AlwaysExpanded is true.");
		Assert.IsFalse(wasClosed, "CommandBarFlyout should not close when AlwaysExpanded is true.");
	}

#if HAS_UNO
	[TestMethod]
	public async Task When_CommandBarFlyout_Without_Secondary_Commands()
	{
		var commandBarFlyout = new CommandBarFlyout();
		commandBarFlyout.PrimaryCommands.Add(new AppBarButton { Icon = new SymbolIcon(Symbol.Home), Label = "Primary Command" });

		var button = new Button
		{
			Content = "Open CommandBarFlyout",
		};

		FlyoutBase.SetAttachedFlyout(button, commandBarFlyout);

		TestServices.WindowHelper.WindowContent = button;
		await TestServices.WindowHelper.WaitForLoaded(button);

		FlyoutBase.ShowAttachedFlyout(button);
		await TestServices.WindowHelper.WaitForIdle();

		var popups = VisualTreeHelper.GetOpenPopupsForXamlRoot(TestServices.WindowHelper.XamlRoot);

		var commandBar = popups.Select(p => (p.Child as FlyoutPresenter)?.Content as CommandBarFlyoutCommandBar).Where(c => c is not null).FirstOrDefault();
		Assert.IsNotNull(commandBar);

		var state = VisualStateManager.GetCurrentState(commandBar, "PrimaryLabelStates");
		Assert.AreEqual("HasPrimaryLabels", state.Name);
		Assert.AreEqual(Visibility.Visible, commandBar.CommandBarTemplateSettings.EffectiveOverflowButtonVisibility);
	}
#endif
}
