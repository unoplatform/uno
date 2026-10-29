#nullable enable

using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using SampleControl.Presentation;
using Uno.UI.RuntimeTests;
using Uno.UI.Samples.Controls;
using Windows.System;

namespace SamplesApp.Tests;

[TestClass]
[RunsOnUIThread]
public class Given_ShellShortcuts
{
	[TestMethod]
	public void When_Catalogue_Has_No_Ctrl_Alt_Combination()
	{
		const VirtualKeyModifiers ctrlAlt = VirtualKeyModifiers.Control | VirtualKeyModifiers.Menu;

		var offenders = ShellCommands.All.Where(c => (c.Modifiers & ctrlAlt) == ctrlAlt).Select(c => c.Id).ToArray();

		Assert.AreEqual(0, offenders.Length, string.Join(", ", offenders));
	}

	[TestMethod]
	public void When_Catalogue_Key_Combinations_Are_Unique()
	{
		var duplicates = ShellCommands.All
			.GroupBy(c => (c.Key, c.Modifiers))
			.Where(g => g.Count() > 1)
			.Select(g => ShellCommands.Describe(g.Key.Key, g.Key.Modifiers))
			.ToArray();

		Assert.AreEqual(0, duplicates.Length, string.Join(", ", duplicates));
	}

	[TestMethod]
	[DataRow(ShellCommands.FocusNextRegion, "F6")]
	[DataRow(ShellCommands.FocusPreviousRegion, "Shift+F6")]
	public void When_Describing_Region_Shortcut(string id, string expected)
		=> Assert.AreEqual(expected, ShellCommands.Describe(id));

	[TestMethod]
	[DataRow(ShellCommands.FocusNextRegion)]
	[DataRow(ShellCommands.FocusPreviousRegion)]
	public void When_Region_Shortcut_On_Wasm(string id)
		=> Assert.IsTrue(ShellCommands.Find(id)!.WasmUnsafe, "Browsers use F6 / Shift+F6 to cycle the address bar, toolbars and page.");

	[TestMethod]
	[DataRow(false, true, false, true)]
	[DataRow(false, false, false, false)]
	[DataRow(false, true, true, true)]
	[DataRow(true, false, false, true)]
	[DataRow(true, true, true, false)]
	public void When_Gating_Command(bool alwaysEnabled, bool shortcutsEnabled, bool automation, bool expected)
	{
		var command = alwaysEnabled
			? ShellCommands.Find(ShellCommands.ToggleFocusMode)!
			: ShellCommands.Find(ShellCommands.ReloadSample)!;

		Assert.AreEqual(expected, ShellCommands.IsEnabled(command, shortcutsEnabled, automation));
	}

	[TestMethod]
	public void When_Focus_Is_In_Text_Input()
	{
		Assert.IsTrue(ShellCommands.IsTextInput(new TextBox()));
		Assert.IsTrue(ShellCommands.IsTextInput(new PasswordBox()));
		Assert.IsTrue(ShellCommands.IsTextInput(new AutoSuggestBox()));
		Assert.IsFalse(ShellCommands.IsTextInput(new Button()));
		Assert.IsFalse(ShellCommands.IsTextInput(null));
	}

	[TestMethod]
	[DataRow(-1, 4, false, new[] { 0, 1, 2, 3 })]
	[DataRow(-1, 4, true, new[] { 3, 2, 1, 0 })]
	[DataRow(1, 4, false, new[] { 2, 3, 0 })]
	[DataRow(1, 4, true, new[] { 0, 3, 2 })]
	[DataRow(0, 1, false, new int[0])]
	public void When_Cycling_Regions(int current, int count, bool backward, int[] expected)
		=> CollectionAssert.AreEqual(expected, ShellCommands.GetRegionCycle(current, count, backward).ToArray());

	[TestMethod]
	public void When_Root_Has_One_Accelerator_Per_Command()
	{
		var owner = SampleChooserViewModel.Instance.Owner;

		foreach (var command in ShellCommands.All)
		{
			Assert.AreEqual(
				1,
				owner.KeyboardAccelerators.Count(a => a.Key == command.Key && a.Modifiers == command.Modifiers),
				command.ToString());
		}
	}

	[TestMethod]
	public void When_Runner_Is_Current_Sample_Shortcuts_Are_Disabled()
	{
		var vm = SampleChooserViewModel.Instance;
		Assert.IsFalse(vm.KeyboardShortcutsEnabled, "The runtime-test runner should disable shortcuts.");

		var enabled = vm.Owner.KeyboardAccelerators
			.Where(a => a.IsEnabled)
			.Select(a => ShellCommands.Describe(a.Key, a.Modifiers))
			.ToArray();
		var expected = vm.IsAutomationRun ? new string[0] : new[] { "F11" };

		CollectionAssert.AreEqual(expected, enabled);
	}

	[TestMethod]
	[DataRow(SplitViewDisplayMode.Overlay, false)]
	[DataRow(SplitViewDisplayMode.CompactOverlay, false)]
	[DataRow(SplitViewDisplayMode.Inline, true)]
	public async Task When_Moving_Focus_Out_Of_Open_Pane(SplitViewDisplayMode mode, bool expectedPaneOpen)
	{
		Button headerButton = new() { Content = "Header" };
		Button paneButton = new() { Content = "Pane" };
		Button hostButton = new() { Content = "Host" };
		Grid header = new() { Children = { headerButton } };
		Grid pane = new() { Children = { paneButton } };
		Grid host = new() { Children = { hostButton } };
		Grid.SetRow(host, 1);

		SplitView splitView = new()
		{
			DisplayMode = mode,
			IsPaneOpen = true,
			// Leaves the inline content some width when the runner's test area is phone-narrow.
			OpenPaneLength = 100,
			Pane = pane,
			Content = new Grid
			{
				RowDefinitions = { new() { Height = GridLength.Auto }, new() },
				Children = { header, host },
			},
		};

		TestServices.WindowHelper.WindowContent = splitView;
		await TestServices.WindowHelper.WaitForLoaded(paneButton);
		Assert.IsTrue(paneButton.Focus(FocusState.Keyboard));

		FrameworkElement?[] regions = { null, header, pane, host };
		Assert.IsTrue(SampleChooserControl.MoveFocusRegion(regions, splitView, pane, backward: false));
		await TestServices.WindowHelper.WaitForIdle();

		Assert.AreSame(hostButton, FocusManager.GetFocusedElement(splitView.XamlRoot!));
		Assert.AreEqual(expectedPaneOpen, splitView.IsPaneOpen);
	}

	[TestMethod]
	public async Task When_Recording_Search_Does_Not_Open_Browser()
	{
		var vm = SampleChooserViewModel.Instance;
		var wasRecording = vm.IsRecordAllTests;
		var wasSplitVisible = vm.IsSplitVisible;

		try
		{
			vm.IsSplitVisible = false;
			vm.IsRecordAllTests = true;

			await vm.Owner.FocusSearchAsync(vm);
			Assert.IsFalse(vm.IsSplitVisible, "Ctrl+F, Ctrl+K and '/'");

			ShellCommands.Find(ShellCommands.ToggleBrowser)!.Execute(vm, vm.Owner);
			Assert.IsFalse(vm.IsSplitVisible, "Ctrl+B");
		}
		finally
		{
			vm.IsRecordAllTests = wasRecording;
			vm.IsSplitVisible = wasSplitVisible;
			await TestServices.WindowHelper.WaitForIdle();
		}
	}

	[TestMethod]
	public async Task When_Toggling_Browser_In_Focus_Mode()
	{
		var vm = SampleChooserViewModel.Instance;
		var wasSplitVisible = vm.IsSplitVisible;
		var wasChromeVisible = vm.IsShellChromeVisible;

		try
		{
			vm.IsSplitVisible = true;
			vm.IsShellChromeVisible = false;

			ShellCommands.Find(ShellCommands.ToggleBrowser)!.Execute(vm, vm.Owner);

			Assert.IsTrue(vm.IsShellChromeVisible, "Focus mode should be left.");
			Assert.IsTrue(vm.IsSplitVisible, "The browser should open, not toggle closed behind hidden chrome.");
		}
		finally
		{
			vm.IsSplitVisible = wasSplitVisible;
			vm.IsShellChromeVisible = wasChromeVisible;
			await TestServices.WindowHelper.WaitForIdle();
		}
	}
}
