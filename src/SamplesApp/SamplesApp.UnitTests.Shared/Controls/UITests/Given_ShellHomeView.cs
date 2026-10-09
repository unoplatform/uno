#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using SampleControl.Entities;
using SampleControl.Presentation;
using Uno.UI.RuntimeTests;
using Uno.UI.Samples.Controls;
using Uno.UI.Samples.Helper;
using Windows.Foundation;

namespace SamplesApp.Tests;

[TestClass]
[RunsOnUIThread]
public class Given_ShellHomeView
{
	[TestMethod]
	[DataRow(StartupPage.Home, false, true, null, StartupPage.Home, DisplayName = "Home by default")]
	[DataRow(StartupPage.Home, true, true, null, StartupPage.Playground, DisplayName = "Automation never lands on Home")]
	[DataRow(StartupPage.Home, false, false, null, StartupPage.Playground, DisplayName = "No Home view yet")]
	[DataRow(StartupPage.Playground, false, true, null, StartupPage.Playground, DisplayName = "Playground")]
	[DataRow(StartupPage.LastSample, false, true, null, StartupPage.LastSample, DisplayName = "Last sample, recents not read yet")]
	[DataRow(StartupPage.LastSample, false, true, true, StartupPage.LastSample, DisplayName = "Last sample")]
	[DataRow(StartupPage.LastSample, false, true, false, StartupPage.Home, DisplayName = "Last sample without history")]
	[DataRow(StartupPage.LastSample, true, true, true, StartupPage.Playground, DisplayName = "Automation ignores the last sample")]
	public void When_Resolving_Startup_Page(StartupPage requested, bool isAutomationRun, bool supportsHomeView, bool? hasLastSample, StartupPage expected)
		=> Assert.AreEqual(expected, SampleChooserViewModel.ResolveStartupPage(requested, isAutomationRun, supportsHomeView, hasLastSample));

	[TestMethod]
	public async Task When_Sample_Opens_While_Recents_Load_Startup_Page_Keeps_It()
	{
		var vm = SampleChooserViewModel.Instance;
		var sample = vm.CurrentSelectedSample;
		Assert.IsNotNull(sample);

		var wasPage = vm.StartupPage;
		var wasAutomation = vm.IsAutomationRun;
		try
		{
			vm.IsShellPersistenceSuspended = true;
			vm.IsAutomationRun = false;
			vm.StartupPage = StartupPage.LastSample;

			// The app asks only when no sample is open, but one (here the runner) can open while the recents load.
			await vm.ApplyStartupPageAsync(CancellationToken.None);

			Assert.AreSame(sample, vm.CurrentSelectedSample);
			Assert.IsFalse(vm.IsHomeVisible);
		}
		finally
		{
			vm.IsHomeVisible = false;
			vm.StartupPage = wasPage;
			vm.IsAutomationRun = wasAutomation;
			vm.IsShellPersistenceSuspended = false;
			await TestServices.WindowHelper.WaitForIdle();
		}
	}

	[TestMethod]
	public void When_Shell_Exists_Home_Is_Supported()
		=> Assert.IsTrue(SampleChooserViewModel.SupportsHomeView, "A cold start would fall back to Playground.");

	[TestMethod]
	public void When_No_Sample_Is_Open_Header_Still_Reads_Home()
	{
		Assert.AreEqual("Home", ShellFunctions.HeaderTitle(null, isHomeVisible: true));
		Assert.AreEqual("", ShellFunctions.HeaderTitle(null, isHomeVisible: false));
	}

	[TestMethod]
	[DataRow(ShellDestination.Home, true, true)]
	[DataRow(ShellDestination.Playground, true, true)]
	[DataRow(ShellDestination.Help, true, true)]
	[DataRow(ShellDestination.Benchmarks, true, true)]
	[DataRow(ShellDestination.RuntimeTests, true, false, DisplayName = "Re-invoking the runner stays on it")]
	[DataRow(ShellDestination.Samples, true, false, DisplayName = "The browser opens beside the runner")]
	[DataRow(ShellDestination.Settings, true, false, DisplayName = "Settings live in the browser pane")]
	[DataRow(ShellDestination.Home, false, false, DisplayName = "No run in progress")]
	public void When_Leaving_Runner_Confirmation_Follows_Run_State(ShellDestination destination, bool isRunActive, bool expected)
		=> Assert.AreEqual(expected, SampleChooserControl.LeavesRunner(destination, ShellDestination.RuntimeTests, isRunActive));

	[TestMethod]
	public async Task When_Runner_Leave_Bar_Shown_Header_Content_Is_Hidden_Until_Escape()
	{
		var owner = SampleChooserViewModel.Instance.Owner;
		var content = (FrameworkElement)owner.FindName("ShellHeaderContent");
		var bar = (FrameworkElement)owner.FindName("ShellRunnerLeaveBar");
		var left = false;

		Button invoker = new() { Content = "Invoker" };
		TestServices.WindowHelper.WindowContent = invoker;
		await TestServices.WindowHelper.WaitForLoaded(invoker);
		invoker.Focus(FocusState.Keyboard);
		await TestServices.WindowHelper.WaitForIdle();

		try
		{
			owner.ShowRunnerLeaveBar(() =>
			{
				left = true;
				return Task.CompletedTask;
			});
			await TestServices.WindowHelper.WaitForIdle();

			Assert.AreEqual(Visibility.Collapsed, content.Visibility, "The covered search box and commands must leave the tab order.");
			Assert.AreEqual(Visibility.Visible, bar.Visibility);
			Assert.AreSame(owner.FindName("ShellRunnerLeaveCancel"), FocusManager.GetFocusedElement(owner.XamlRoot!), "Keyboard users land on Stay.");

			await TestServices.KeyboardHelper.Escape();
			await TestServices.WindowHelper.WaitForIdle();

			Assert.AreEqual(Visibility.Collapsed, bar.Visibility, "Escape means Stay.");
			Assert.AreEqual(Visibility.Visible, content.Visibility);
			Assert.AreSame(invoker, FocusManager.GetFocusedElement(owner.XamlRoot!), "Focus goes back to the invoker.");
			Assert.IsFalse(left);
		}
		finally
		{
			owner.HideRunnerLeaveBar();
			await TestServices.WindowHelper.WaitForIdle();
		}
	}

	[TestMethod]
	public async Task When_Runner_Leave_Bar_Shown_By_Pointer_Focus_Stays_Put()
	{
		var owner = SampleChooserViewModel.Instance.Owner;

		Button invoker = new() { Content = "Invoker" };
		TestServices.WindowHelper.WindowContent = invoker;
		await TestServices.WindowHelper.WaitForLoaded(invoker);
		invoker.Focus(FocusState.Pointer);
		await TestServices.WindowHelper.WaitForIdle();

		try
		{
			owner.ShowRunnerLeaveBar(() => Task.CompletedTask);
			await TestServices.WindowHelper.WaitForIdle();

			Assert.AreSame(invoker, FocusManager.GetFocusedElement(owner.XamlRoot!), "A running test keeps its focus.");
		}
		finally
		{
			owner.HideRunnerLeaveBar();
			await TestServices.WindowHelper.WaitForIdle();
		}
	}

	[TestMethod]
	public void When_Picking_Last_Sample_Tool_Pages_Are_Skipped()
	{
		var runner = Sample("UnitTestsPage", "_None");
		var playground = Sample("Playground", "_None");
		var button = Sample("Button_Events", "Buttons");

		Assert.AreSame(button, SampleChooserViewModel.PickLastSample([runner, playground, button]));
		Assert.IsNull(SampleChooserViewModel.PickLastSample([runner, playground]), "Only tools: Home instead.");
		Assert.IsTrue(SampleChooserViewModel.IsToolPage(runner));
		Assert.IsFalse(SampleChooserViewModel.IsToolPage(button));

		static SampleChooserContent Sample(string name, string category)
			=> new() { ControlName = name, Categories = [category], ControlType = typeof(Button) };
	}

	[TestMethod]
	public void When_Counting_Samples_Only_The_Given_Categories_Count()
	{
		var shared = new SampleChooserContent { ControlName = "Shared", Categories = ["A", "B"] };
		var a = Category("A", sampleCount: 2);
		var b = Category("B", sampleCount: 3);
		a.SamplesContent.Add(shared);
		b.SamplesContent.Add(shared);

		Assert.AreEqual(6, SampleChooserViewModel.CountSamples([a, b]), "A sample in two categories counts once.");
		Assert.AreEqual(3, SampleChooserViewModel.CountSamples([a]));
		Assert.AreEqual(0, SampleChooserViewModel.CountSamples(null));
	}

	[TestMethod]
	public void When_Many_Categories_Largest_Are_Shown_Alphabetically()
	{
		var categories = Enumerable.Range(1, 12)
			.Select(i => Category($"C{i:00}", sampleCount: i))
			.Reverse()
			.ToList();

		var shown = HomeView.TopCategories(categories).Select(c => c.Category).ToArray();

		CollectionAssert.AreEqual(new[] { "C05", "C06", "C07", "C08", "C09", "C10", "C11", "C12" }, shown);
	}

	[TestMethod]
	[DataRow(360d, 1, DisplayName = "Phone")]
	[DataRow(752d, 2, DisplayName = "800 wide window minus the rail")]
	[DataRow(1232d, 4, DisplayName = "1280 wide window minus the rail")]
	public async Task When_Width_Changes_Card_Grid_Reflows(double width, int expectedColumns)
	{
		var vm = SampleChooserViewModel.Instance;
		Assert.IsTrue(vm.Categories?.Count >= 4, "The runner's sample index should be loaded.");

		// Tall enough to realize every card.
		var view = await LoadHomeView(width, height: 3000);
		var list = (ItemsRepeater)view.FindName("ShellHomeCategoriesList");
		var cards = RealizedCards(list);

		Assert.AreEqual(HomeView.MaxCards, ((System.Collections.ICollection)list.ItemsSource).Count, "Categories are capped.");
		Assert.AreEqual(HomeView.MaxCards, cards.Count);

		var columns = cards.Select(c => Math.Round(c.Left)).Distinct().Count();
		Assert.AreEqual(expectedColumns, columns);

		// The cards stretch: the last column ends at the grid's edge.
		var right = cards.Max(c => c.Left + c.Width);
		Assert.AreEqual(list.ActualWidth, right, 1, $"Cards end at {right}, grid is {list.ActualWidth} wide.");
		Assert.IsTrue(cards.All(c => c.Width >= 260 - 0.5), "No card is narrower than the minimum.");
	}

	[TestMethod]
	public async Task When_Category_Card_Invoked_Browser_Opens_On_It()
	{
		var vm = SampleChooserViewModel.Instance;
		var state = vm.SaveBrowserState();
		var wasSplitVisible = vm.IsSplitVisible;

		try
		{
			var view = await LoadHomeView(1232);
			var list = (ItemsRepeater)view.FindName("ShellHomeCategoriesList");
			var card = (Button)list.TryGetElement(0);
			var category = ((HomeCard)card.DataContext).Title;

			card.Command.Execute(card.CommandParameter);
			await TestServices.WindowHelper.WaitForIdle();

			Assert.AreEqual(category, vm.SelectedCategory?.Category);
			Assert.IsTrue(vm.SampleVisibility, "The browser drills into the category.");
			Assert.IsTrue(vm.IsSplitVisible);
		}
		finally
		{
			vm.RestoreBrowserState(state);
			vm.IsSplitVisible = wasSplitVisible;
			await TestServices.WindowHelper.WaitForIdle();
		}
	}

	[TestMethod]
	public async Task When_Shortcut_Hints_Off_Quick_Actions_Drop_Them()
	{
		var view = await LoadHomeView(1232);
		var actions = (ItemsRepeater)view.FindName("ShellHomeQuickActions");

		Assert.IsTrue(Cards(actions).Any(c => c.ShortcutText == "Ctrl+T"));

		view.ShowShortcutHints = false;
		await TestServices.WindowHelper.WaitForIdle();

		Assert.IsTrue(Cards(actions).All(c => c.ShortcutText.Length == 0));
		Assert.AreEqual("Ctrl+T", Cards(actions).First(c => c.AutomationId == "ShellHomeRuntimeTests").Shortcut, "Screen readers keep the shortcut.");
		Assert.IsFalse(((TextBlock)view.FindName("ShellHomeFavoritesEmpty")).Text.Contains("Ctrl", StringComparison.Ordinal));

		static IEnumerable<HomeCard> Cards(ItemsRepeater repeater) => (IEnumerable<HomeCard>)repeater.ItemsSource;
	}

	[TestMethod]
	public void When_Counting_Summary_Uses_Singular_And_Plural()
	{
		Assert.AreEqual($"{1_475:N0} samples in 136 categories", HomeView.SummaryText(1_475, 136));
		Assert.AreEqual("1 sample in 1 category", HomeView.SummaryText(1, 1));
	}

	private static async Task<HomeView> LoadHomeView(double width, double height = 800)
	{
		HomeView view = new() { DataContext = SampleChooserViewModel.Instance };
		Border host = new() { Width = width, Height = height, Child = view };

		TestServices.WindowHelper.WindowContent = host;
		await TestServices.WindowHelper.WaitForLoaded(view);
		await TestServices.WindowHelper.WaitForIdle();

		return view;
	}

	private static List<Rect> RealizedCards(ItemsRepeater repeater)
	{
		var count = ((System.Collections.ICollection)repeater.ItemsSource).Count;
		List<Rect> cards = new();
		for (var i = 0; i < count; i++)
		{
			if (repeater.TryGetElement(i) is FrameworkElement element)
			{
				var origin = element.TransformToVisual(repeater).TransformPoint(default);
				cards.Add(new Rect(origin.X, origin.Y, element.ActualWidth, element.ActualHeight));
			}
		}

		return cards;
	}

	private static SampleChooserCategory Category(string name, int sampleCount)
		=> new(name, Enumerable.Range(0, sampleCount).Select(i => new SampleChooserContent { ControlName = $"{name}_{i}", Categories = new[] { name } }));
}
