#nullable enable

using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using SampleControl.Presentation;
using Uno.UI.RuntimeTests;
using Uno.UI.Samples.Controls;
using Uno.UI.Samples.Helper;

namespace SamplesApp.Tests;

[TestClass]
[RunsOnUIThread]
public class Given_ShellCommandBar
{
	[TestMethod]
	public void When_Tool_Page_Shows_Only_Info_And_Quick_Settings()
	{
		var vm = SampleChooserViewModel.Instance;
		if (vm.CurrentBreadcrumb is not ["Tools"])
		{
			Assert.Inconclusive("Only meaningful while the runner (a tool page) is the current sample.");
		}

		foreach (var name in new[] { "ShellPreviousSampleButton", "ShellNextSampleButton", "ShellReloadSampleButton", "ShellSampleCommandsSeparator", "ShellFavoriteToggle" })
		{
			Assert.AreEqual(Visibility.Collapsed, Find<UIElement>(vm, name).Visibility, name);
		}

		Assert.AreEqual(Visibility.Visible, Find<UIElement>(vm, "InfoButton").Visibility);
		Assert.AreEqual(Visibility.Visible, Find<UIElement>(vm, "OverflowSettingsButton").Visibility);
	}

	[TestMethod]
	public async Task When_Command_Is_Hidden_Flyout_Anchors_To_Command_Bar()
	{
		var vm = SampleChooserViewModel.Instance;
		var previous = Find<AppBarButton>(vm, "ShellPreviousSampleButton");
		var wasVisibility = previous.Visibility;

		try
		{
			previous.Visibility = Visibility.Collapsed;
			Assert.AreSame(Find<CommandBar>(vm, "ShellCommandBar"), vm.Owner.GetCommandAnchor(previous));
		}
		finally
		{
			previous.Visibility = wasVisibility;
			await TestServices.WindowHelper.WaitForIdle();
		}
	}

	[TestMethod]
	[DataRow("Paints things", false, false, true, DisplayName = "Shown")]
	[DataRow("Paints things", true, false, false, DisplayName = "Collapsed by the user")]
	[DataRow("Paints things", false, true, false, DisplayName = "Home has no description")]
	[DataRow(" ", false, false, false, DisplayName = "Blank description")]
	[DataRow(null, false, false, false, DisplayName = "No description")]
	public void When_Description_State_Changes_Strip_Follows(string? description, bool isCollapsed, bool isHomeVisible, bool isShown)
	{
		Assert.AreEqual(isShown ? Visibility.Visible : Visibility.Collapsed, ShellFunctions.ShowDescription(description, isCollapsed, isHomeVisible));
		Assert.AreEqual(description is not (null or " ") && !isHomeVisible, ShellFunctions.HasDescription(description, isHomeVisible), "The quick-settings item is only enabled with a description to show.");
	}

	[TestMethod]
	public async Task When_Quick_Settings_Description_Item_Toggled_Strip_Setting_Follows()
	{
		var vm = SampleChooserViewModel.Instance;
		if (string.IsNullOrEmpty(vm.CurrentSelectedSample?.Description))
		{
			Assert.Inconclusive("The current sample has no description, so the item is disabled.");
		}

		var button = Find<AppBarButton>(vm, "OverflowSettingsButton");
		var flyout = (MenuFlyout)button.Flyout;
		var item = Find<ToggleMenuFlyoutItem>(vm, "ShellQuickDescriptionItem");
		var wasCollapsed = vm.IsDescriptionCollapsed;

		try
		{
			vm.IsShellPersistenceSuspended = true;
			vm.IsDescriptionCollapsed = true;

			flyout.ShowAt(vm.Owner.GetCommandAnchor(button));
			await TestServices.WindowHelper.WaitForIdle();
			Assert.IsTrue(item.IsEnabled);
			Assert.IsFalse(item.IsChecked, "The item reflects the collapsed strip when the menu opens.");

			new ToggleMenuFlyoutItemAutomationPeer(item).Toggle();
			await TestServices.WindowHelper.WaitForIdle();
			Assert.IsFalse(vm.IsDescriptionCollapsed);
		}
		finally
		{
			flyout.Hide();
			vm.IsDescriptionCollapsed = wasCollapsed;
			vm.IsShellPersistenceSuspended = false;

			// Showing the strip resized the host running these tests; let it settle back before the next test.
			await TestServices.WindowHelper.WaitForIdle();
		}
	}

	[TestMethod]
	[DataRow("ccc|cc|c", "...1..1.", DisplayName = "Every command shown")]
	[DataRow("ccc|hh|h", "........", DisplayName = "Nothing after the separators")]
	[DataRow("ccc|hc|h", "...1....", DisplayName = "One group overflowed")]
	[DataRow("ccc|hh|c", "...1....", DisplayName = "Separators do not double up")]
	[DataRow("hhh|hc|c", "......1.", DisplayName = "Nothing before the first separator")]
	public void When_Commands_Overflow_Separators_Follow(string layout, string expected)
	{
		var slots = layout.Select(c => c switch
		{
			'|' => SampleChooserControl.CommandSlot.Separator,
			'c' => SampleChooserControl.CommandSlot.Shown,
			_ => SampleChooserControl.CommandSlot.Hidden,
		}).ToArray();

		var needed = SampleChooserControl.GetNeededSeparators(slots);

		Assert.AreEqual(expected, new string(needed.Select(n => n ? '1' : '.').ToArray()));
	}

	[TestMethod]
	[DataRow(500, 200, 60, 150, 480, true, true, 290, DisplayName = "Everything fits")]
	[DataRow(300, 200, 60, 150, 480, false, true, 240, DisplayName = "Breadcrumb yields first")]
	[DataRow(120, 200, 60, 150, 480, false, false, 120, DisplayName = "Tag yields to a minimum title")]
	[DataRow(40, 200, 60, 0, 480, false, false, 96, DisplayName = "Title keeps its minimum")]
	[DataRow(900, 200, 0, 150, 480, true, false, 480, DisplayName = "Title capped by its layout maximum")]
	[DataRow(202, 210, 70, 0, 200, false, true, 132, DisplayName = "641 wide: the commands keep their room, the title trims")]
	public void When_Header_Location_Is_Fitted(double budget, double title, double tag, double breadcrumb, double titleMax, bool showBreadcrumb, bool showTag, double titleMaxWidth)
	{
		var fit = SampleChooserControl.FitHeaderLocation(budget, title, tag, breadcrumb, titleMax);

		Assert.AreEqual(showBreadcrumb, fit.ShowBreadcrumb, nameof(fit.ShowBreadcrumb));
		Assert.AreEqual(showTag, fit.ShowTag, nameof(fit.ShowTag));
		Assert.AreEqual(titleMaxWidth, fit.TitleMaxWidth, nameof(fit.TitleMaxWidth));
	}

	[TestMethod]
	[DataRow(40, 200, 0, 200, 144, DisplayName = "Phone: the title keeps about 16 characters")]
	public void When_Header_Location_Is_Fitted_On_A_Phone(double budget, double title, double tag, double titleMax, double titleMaxWidth)
	{
		var fit = SampleChooserControl.FitHeaderLocation(budget, title, tag, breadcrumb: 0, titleMax, minTitle: 144);

		Assert.AreEqual(titleMaxWidth, fit.TitleMaxWidth, nameof(fit.TitleMaxWidth));
	}

	// TODO Uno: FindName misses commands moved to the closed overflow (uno--findname-misses-commandbar-overflow-items.md).
	private static T Find<T>(SampleChooserViewModel vm, string name) where T : class
		=> vm.Owner.FindName(name) as T
			?? FindCommand(vm, name) as T
			?? throw new AssertFailedException($"{name} is not a {typeof(T).Name}.");

	private static object? FindCommand(SampleChooserViewModel vm, string name)
		=> vm.Owner.FindName("ShellCommandBar") is CommandBar bar
			? bar.PrimaryCommands.Concat(bar.SecondaryCommands).OfType<FrameworkElement>().FirstOrDefault(c => c.Name == name)
			: null;
}
