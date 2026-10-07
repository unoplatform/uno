#nullable enable

using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using SampleControl.Presentation;
using Uno.UI.RuntimeTests;

namespace SamplesApp.Tests;

[TestClass]
[RunsOnUIThread]
public class Given_ShellViewModelState
{
	[TestMethod]
	[DataRow(ShellCommands.ToggleFavorite, "Ctrl+Shift+D")]
	[DataRow(ShellCommands.ShowSettings, "Ctrl+,")]
	[DataRow(ShellCommands.ShowHome, "Alt+Shift+H")]
	[DataRow(ShellCommands.PreviousSample, "Alt+Left")]
	[DataRow(ShellCommands.ToggleFocusMode, "F11")]
	public void When_Describing_Shortcut(string id, string expected)
		=> Assert.AreEqual(expected, ShellCommands.Describe(id));

	[TestMethod]
	public void When_Catalogue_Ids_Are_Unique_And_Only_FocusMode_Is_Always_Enabled()
	{
		var ids = ShellCommands.All.Select(c => c.Id).ToList();
		Assert.AreEqual(ids.Count, ids.Distinct().Count());
		CollectionAssert.AreEqual(
			new[] { ShellCommands.ToggleFocusMode },
			ShellCommands.All.Where(c => c.AlwaysEnabled).Select(c => c.Id).ToArray());
	}

	[TestMethod]
	public async Task When_ShowHome_Under_Automation_Home_Stays_Hidden()
	{
		var vm = SampleChooserViewModel.Instance;
		var wasAutomation = vm.IsAutomationRun;
		try
		{
			vm.IsAutomationRun = true;
			vm.ShowHomeCommand.Execute(null);

			Assert.IsFalse(vm.IsHomeVisible);
		}
		finally
		{
			vm.IsAutomationRun = wasAutomation;
			await TestServices.WindowHelper.WaitForIdle();
		}
	}

	[TestMethod]
	public async Task When_ShowHome_Destination_Follows_Home()
	{
		var vm = SampleChooserViewModel.Instance;
		var wasAutomation = vm.IsAutomationRun;
		var wasSplitVisible = vm.IsSplitVisible;
		try
		{
			vm.IsAutomationRun = false;
			vm.ShowHomeCommand.Execute(null);

			Assert.IsTrue(vm.IsHomeVisible);
			Assert.IsFalse(vm.IsSplitVisible);
			Assert.AreEqual(ShellDestination.Home, vm.ShellDestination);
		}
		finally
		{
			vm.IsHomeVisible = false;
			vm.IsAutomationRun = wasAutomation;
			vm.IsSplitVisible = wasSplitVisible;
			await TestServices.WindowHelper.WaitForIdle();
		}

		Assert.AreEqual(ShellDestination.RuntimeTests, vm.ShellDestination);
		CollectionAssert.AreEqual(new[] { "Tools" }, vm.CurrentBreadcrumb.ToArray());
	}

	[TestMethod]
	public async Task When_ToggleFocusMode_Chrome_Visibility_Flips()
	{
		var vm = SampleChooserViewModel.Instance;
		var initial = vm.IsShellChromeVisible;
		try
		{
			vm.ToggleFocusModeCommand.Execute(null);

			Assert.AreEqual(!initial, vm.IsShellChromeVisible);
		}
		finally
		{
			vm.IsShellChromeVisible = initial;
			await TestServices.WindowHelper.WaitForIdle();
		}
	}

	[TestMethod]
	public async Task When_ThemeChanged_AppThemeIndex_Notifies()
	{
		var vm = SampleChooserViewModel.Instance;
		List<string?> raised = new();
		void OnChanged(object? sender, PropertyChangedEventArgs e) => raised.Add(e.PropertyName);

		// Keeps the theme flips out of the persisted Shell.Theme when run from the in-app runner.
		var wasAutomation = vm.IsAutomationRun;
		vm.IsAutomationRun = true;
		vm.IsAppThemeLight = true;
		vm.PropertyChanged += OnChanged;
		try
		{
			vm.IsAppThemeDark = true;

			CollectionAssert.Contains(raised, nameof(vm.AppThemeIndex));
			Assert.AreEqual(2, vm.AppThemeIndex);
		}
		finally
		{
			vm.PropertyChanged -= OnChanged;
			vm.IsAppThemeLight = true;
			vm.IsAutomationRun = wasAutomation;
			await TestServices.WindowHelper.WaitForIdle();
		}
	}

	[TestMethod]
	[DataRow(-1)]
	[DataRow(3)]
	public async Task When_AppThemeIndex_Out_Of_Range_Theme_Is_Kept(int index)
	{
		var vm = SampleChooserViewModel.Instance;
		var wasAutomation = vm.IsAutomationRun;
		vm.IsAutomationRun = true;
		try
		{
			vm.IsAppThemeDark = true;
			vm.AppThemeIndex = index;

			Assert.AreEqual(2, vm.AppThemeIndex);
		}
		finally
		{
			vm.IsAppThemeLight = true;
			vm.IsAutomationRun = wasAutomation;
			await TestServices.WindowHelper.WaitForIdle();
		}
	}

	[TestMethod]
	public async Task When_UITest_Automation_Entered_Theme_Resets_And_Persistence_Stops()
	{
		var vm = SampleChooserViewModel.Instance;
		var wasAutomation = vm.IsAutomationRun;
		try
		{
			vm.IsAutomationRun = true;
			vm.IsAppThemeDark = true;
			vm.IsAutomationRun = false;

			vm.EnterUITestAutomation();

			Assert.IsTrue(vm.IsAutomationRun);
			Assert.AreEqual(0, vm.AppThemeIndex);
		}
		finally
		{
			vm.IsAutomationRun = true;
			vm.IsAppThemeLight = true;
			vm.IsAutomationRun = wasAutomation;
			await TestServices.WindowHelper.WaitForIdle();
		}
	}

	[TestMethod]
	[DataRow("theme=Dark", "")]
	[DataRow(" theme=Dark", "")]
	[DataRow("sample=Buttons/Button_Events theme=Dark", "sample=Buttons/Button_Events")]
	[DataRow("sample=Buttons/Button_Events &theme=Dark", "sample=Buttons/Button_Events")]
	[DataRow("sample=Buttons/Button_Events&theme=Dark", "sample=Buttons/Button_Events")]
	[DataRow("theme=Dark&sample=Buttons/Button_Events", "sample=Buttons/Button_Events")]
	public void When_Theme_Argument_Removed_No_Separator_Is_Left(string args, string expected)
	{
		Assert.AreEqual(expected, App.RemoveLaunchTheme(args, out var theme));
		Assert.AreEqual("Dark", theme);
	}
}
