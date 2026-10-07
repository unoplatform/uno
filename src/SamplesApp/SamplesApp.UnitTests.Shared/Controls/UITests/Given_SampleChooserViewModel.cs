#nullable enable

using System.Collections.Generic;
using System.ComponentModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SampleControl.Presentation;
using Uno.UI.RuntimeTests;

namespace SamplesApp.Tests;

[TestClass]
[RunsOnUIThread]
public class Given_SampleChooserViewModel
{
	[TestMethod]
	public async Task When_ThemeChanged_All_Theme_Flags_Notify()
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

			CollectionAssert.IsSubsetOf(
				new[] { nameof(vm.IsAppThemeDark), nameof(vm.IsAppThemeLight), nameof(vm.IsAppThemeSystem) },
				raised);
		}
		finally
		{
			vm.PropertyChanged -= OnChanged;
			vm.IsAppThemeLight = true;
			vm.IsAutomationRun = wasAutomation;
			await TestServices.WindowHelper.WaitForIdle();
		}
	}
}
