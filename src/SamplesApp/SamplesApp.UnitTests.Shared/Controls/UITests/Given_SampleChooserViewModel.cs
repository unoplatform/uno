#nullable enable

using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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

	[TestMethod]
	public async Task When_Favorites_Load_For_All_Samples_Library_Keeps_Selected_Category()
	{
		var vm = SampleChooserViewModel.Instance;
		var previous = vm.SelectedCategory;
		var category = vm.Categories.First(c => !c.Equals(previous) && c.Count > 1);

		try
		{
			vm.SelectedCategory = category;

			// The startup load flags favorites across every sample.
			await vm.GetFavoriteSamples(CancellationToken.None, getAllSamples: true);

			CollectionAssert.AreEquivalent(category.SamplesContent.ToList(), vm.SampleContents);
		}
		finally
		{
			vm.SelectedCategory = previous;
			await TestServices.WindowHelper.WaitForIdle();
		}
	}
}
