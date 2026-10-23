#nullable enable

using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using SampleControl.Entities;
using SampleControl.Presentation;
using Uno.UI.RuntimeTests;
using Uno.UI.Samples.Helper;

namespace SamplesApp.Tests;

[TestClass]
[RunsOnUIThread]
public class Given_ShellBrowserPane
{
	[TestMethod]
	public void When_More_Than_Eight_Matches_Suggestions_End_With_See_All()
	{
		var samples = Enumerable.Range(0, 20).Select(i => new SampleChooserContent { ControlName = $"Sample{i}" }).ToList();

		var suggestions = ShellFunctions.TopSuggestions(samples, "sam", showShortcutHints: true);

		Assert.AreEqual(9, suggestions.Count);
		var seeAll = suggestions[8] as SearchSeeAllItem;
		Assert.IsNotNull(seeAll);
		Assert.AreEqual(20, seeAll.Count);
		Assert.AreEqual("sam", seeAll.ControlName, "Choosing the entry must keep the typed text.");
		Assert.AreEqual("See all 20 results", seeAll.Text);
		Assert.AreEqual("Ctrl+Enter", seeAll.Shortcut);
	}

	[TestMethod]
	public void When_Touch_See_All_Has_No_Shortcut()
	{
		var samples = Enumerable.Range(0, 20).Select(i => new SampleChooserContent { ControlName = $"Sample{i}" }).ToList();

		var seeAll = ShellFunctions.TopSuggestions(samples, "sam", showShortcutHints: false)[8] as SearchSeeAllItem;

		Assert.IsNotNull(seeAll);
		Assert.IsFalse(seeAll.HasShortcut);
	}

	[TestMethod]
	public void When_Eight_Or_Fewer_Matches_No_See_All()
	{
		var samples = Enumerable.Range(0, 8).Select(i => new SampleChooserContent { ControlName = $"Sample{i}" }).ToList();

		var suggestions = ShellFunctions.TopSuggestions(samples, "sam", showShortcutHints: true);

		Assert.AreEqual(8, suggestions.Count);
		Assert.IsFalse(suggestions.OfType<SearchSeeAllItem>().Any());
	}

	[TestMethod]
	public void When_Results_Flattened_Each_Group_Starts_With_Its_Heading()
	{
		SampleChooserContent Sample(string name, string category) => new() { ControlName = name, Categories = [category] };
		var groups = SampleSearch.Group([Sample("Help", "_None"), Sample("Button_Events", "Buttons"), Sample("ToggleButton_Basic", "Buttons")]);

		var rows = ShellFunctions.FlattenSearchResults(groups);

		CollectionAssert.AreEqual(
			new[] { "Tools", "Help", "Buttons", "Button_Events", "ToggleButton_Basic" },
			rows.Cast<object>().Select(row => row.ToString()).ToArray());
		Assert.IsInstanceOfType(rows[0], typeof(SearchResultsHeader));
		Assert.IsInstanceOfType(rows[2], typeof(SearchResultsHeader));
	}

	[TestMethod]
	public async Task When_Sample_Revealed_Library_Drills_Into_Its_Category_Without_Reloading()
	{
		var vm = SampleChooserViewModel.Instance;
		var browserState = vm.SaveBrowserState();
		var previousCategory = vm.SelectedCategory;
		var wasSplitVisible = vm.IsSplitVisible;
		var content = vm.ContentPhone;
		var current = vm.CurrentSelectedSample;
		var category = vm.Categories.First(c => !c.Equals(previousCategory) && c.Count > 2);
		var sample = category.SamplesContent.Skip(1).First();

		try
		{
			vm.IsSplitVisible = true;
			vm.ShowNewSectionCommand.Execute("Library");
			await TestServices.WindowHelper.WaitForIdle();

			Assert.IsTrue(vm.RevealInLibrary(sample));

			Assert.AreEqual(category.Category, vm.SelectedCategory.Category);
			Assert.IsTrue(vm.SampleVisibility, "The library should show the category's samples.");
			Assert.AreEqual(sample, vm.SelectedLibrarySample);

			// The row was selected without asking for the sample to load.
			await TestServices.WindowHelper.WaitForIdle();
			Assert.AreSame(content, vm.ContentPhone);
			Assert.AreSame(current, vm.CurrentSelectedSample);
		}
		finally
		{
			vm.RestoreBrowserState(browserState);
			vm.IsSplitVisible = wasSplitVisible;
			await TestServices.WindowHelper.WaitForIdle();
		}

		Assert.AreSame(content, vm.ContentPhone);
	}

	[TestMethod]
	[DataRow(true, DisplayName = "Category with manual tests")]
	[DataRow(false, DisplayName = "Category without manual tests")]
	public async Task When_Manual_Filter_Toggled_While_Drilled_In_List_Matches_Filter(bool hasManualTests)
	{
		var vm = SampleChooserViewModel.Instance;
		var browserState = vm.SaveBrowserState();
		var wasManualOnly = vm.ManualTestsOnly;
		var wasAutomation = vm.IsAutomationRun;
		var content = vm.ContentPhone;

		try
		{
			// Keeps the filter out of the persisted settings.
			vm.IsAutomationRun = true;
			vm.ManualTestsOnly = false;
			var category = vm.Categories.First(c => c.SamplesContent.Any(s => s.IsManualTest) == hasManualTests);
			vm.ShowNewSectionCommand.Execute("Library");
			vm.SelectedCategory = category;
			vm.ShowNewSectionCommand.Execute("Samples");
			await TestServices.WindowHelper.WaitForIdle();

			vm.ManualTestsOnly = true;
			await TestServices.WindowHelper.WaitForIdle();

			if (hasManualTests)
			{
				Assert.IsTrue(vm.SampleVisibility, "The category is still listed, so the list stays on it.");
				Assert.AreEqual(category.Category, vm.SelectedCategory.Category);
				Assert.IsTrue(vm.Categories.Any(c => ReferenceEquals(c, vm.SelectedCategory)), "The selection points into the filtered list.");
				Assert.IsTrue(vm.SampleContents.All(s => s.IsManualTest), "Only manual tests are listed.");
			}
			else
			{
				Assert.IsFalse(vm.SampleVisibility, "The category is gone, so the library backs out to the category list.");
				Assert.IsTrue(vm.CategoryVisibility);
				Assert.IsNull(vm.SelectedCategory);
			}

			vm.ManualTestsOnly = false;
			await TestServices.WindowHelper.WaitForIdle();

			Assert.IsTrue(vm.SelectedCategory is null || vm.Categories.Any(c => ReferenceEquals(c, vm.SelectedCategory)), "The selection points into the unfiltered list.");
			Assert.AreSame(content, vm.ContentPhone, "Filtering never opens a sample.");
		}
		finally
		{
			vm.ManualTestsOnly = wasManualOnly;
			vm.RestoreBrowserState(browserState);
			vm.IsAutomationRun = wasAutomation;
			await TestServices.WindowHelper.WaitForIdle();
		}
	}

	[TestMethod]
	public async Task When_Manual_Filter_On_Tool_Categories_Stay_Hidden()
	{
		var vm = SampleChooserViewModel.Instance;
		var wasManualOnly = vm.ManualTestsOnly;
		var wasAutomation = vm.IsAutomationRun;

		try
		{
			vm.IsAutomationRun = true;
			vm.ManualTestsOnly = true;

			Assert.IsFalse(vm.Categories.Any(c => c.Category.StartsWith('_')));
		}
		finally
		{
			vm.ManualTestsOnly = wasManualOnly;
			vm.IsAutomationRun = wasAutomation;
			await TestServices.WindowHelper.WaitForIdle();
		}
	}

	[TestMethod]
	public async Task When_All_Results_Shown_Pane_Lists_Them_Under_Category_Headings()
	{
		var vm = SampleChooserViewModel.Instance;
		var list = (ListView)vm.Owner.FindName("ShellSearchResultsList");
		var browserState = vm.SaveBrowserState();
		var wasSplitVisible = vm.IsSplitVisible;
		var content = vm.ContentPhone;

		try
		{
			vm.SearchTerm = "button";
			vm.ShowSearchResults();
			await TestServices.WindowHelper.WaitForIdle();

			Assert.IsTrue(vm.SearchVisibility);
			Assert.IsTrue(vm.IsSplitVisible);

			var rows = ((IList)list.ItemsSource).Cast<object>().ToList();
			Assert.IsInstanceOfType(rows[0], typeof(SearchResultsHeader));
			Assert.AreEqual(vm.SearchResultsGrouped.Count, rows.OfType<SearchResultsHeader>().Count());
			Assert.AreEqual(vm.SearchResultsGrouped.Sum(g => g.Count()), rows.OfType<SampleChooserContent>().Count());

			// Showing every result opens nothing by itself.
			Assert.AreSame(content, vm.ContentPhone);

			vm.SearchTerm = string.Empty;
			Assert.IsFalse(vm.SearchVisibility, "Clearing the search leaves the results.");
			Assert.IsFalse(vm.SearchSelected, "The search tab flag follows the section shown.");
		}
		finally
		{
			vm.SearchTerm = string.Empty;
			vm.RestoreBrowserState(browserState);
			vm.IsSplitVisible = wasSplitVisible;
			await TestServices.WindowHelper.WaitForIdle();
		}
	}
}
