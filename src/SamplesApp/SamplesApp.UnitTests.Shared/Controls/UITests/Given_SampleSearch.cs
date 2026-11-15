#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using SampleControl.Entities;
using SampleControl.Presentation;
using Uno.UI.RuntimeTests;

namespace SamplesApp.Tests;

[TestClass]
public class Given_SampleSearch
{
	private static SampleChooserContent Sample(string name, string category = "Misc", Type? type = null) => new()
	{
		ControlName = name,
		Categories = new[] { category },
		ControlType = type ?? typeof(object),
	};

	private static string[] Names(IEnumerable<SampleChooserContent> items) => items.Select(i => i.ControlName).ToArray();

	[TestMethod]
	[DataRow(null)]
	[DataRow("")]
	[DataRow("   ")]
	public void When_Term_Empty_Returns_Nothing(string? term)
	{
		var results = SampleSearch.Rank(term, new[] { Sample("Button_Events") });

		Assert.AreEqual(0, results.Count);
	}

	[TestMethod]
	public void When_Ranked_Tiers_Are_Ordered()
	{
		var items = new[]
		{
			Sample("Unrelated", type: typeof(Microsoft.UI.Xaml.Controls.ScrollViewer)),
			Sample("Other", category: "ScrollViewer"),
			Sample("Myscrollviewer"),
			Sample("Scroll_Viewer_Snap"),
			Sample("ScrollViewer_Basic"),
			Sample("NoMatch"),
		};

		var results = SampleSearch.Rank("scrollv", items);

		CollectionAssert.AreEqual(
			new[] { "ScrollViewer_Basic", "Scroll_Viewer_Snap", "Myscrollviewer", "Other", "Unrelated" },
			Names(results));
	}

	[TestMethod]
	[DataRow("btev", "Button_Events")]
	[DataRow("btev", "ButtonEvents")]
	[DataRow("BE", "Button_Events")]
	[DataRow("xr", "XAMLReader_Basic")]
	[DataRow("lvsi", "ListView_ScrollIntoView")]
	[DataRow("button ev", "Button_Events")]
	[DataRow("g2l", "Grid2_Layout")]
	public void When_Word_Starts_Match(string term, string name)
	{
		var results = SampleSearch.Rank(term, new[] { Sample(name) });

		CollectionAssert.AreEqual(new[] { name }, Names(results));
	}

	[TestMethod]
	[DataRow("tev", "Button_Events")]
	[DataRow("evbt", "Button_Events")]
	[DataRow("bz", "Button_Events")]
	public void When_Word_Starts_Do_Not_Match(string term, string name) =>
		Assert.IsFalse(SampleSearch.IsWordStartMatch(term, name));

	[TestMethod]
	[DataRow("list", "MediaPlayerElement_Playlist", "LineSegmentPage")]
	[DataRow("list", "MediaPlayerElement_Playlist", "LightSensorTests")]
	[DataRow("list", "MediaPlayerElement_Playlist", "LoopingSelector_Items")]
	[DataRow("list", "MediaPlayerElement_Playlist", "LinearGradientBrush_Change_Stops")]
	[DataRow("popup", "ComboBox_FullScreen_Popup", "PersonPicturePage")]
	[DataRow("text box", "TextBox_Basic", "Border_Simple_No_Background_With_TextBox")]
	[DataRow("button events", "Button_Events", "AButton_Events")]
	[DataRow("tb", "TextBox_Basic", "GetBounds")]
	[DataRow("tb", "TextBox_Basic", "AnimatedVisualsTestbedPage")]
	[DataRow("tb", "TextBox_Basic", "AppBarToggleButtonTest")]
	[DataRow("lvsi", "ListView_ScrollIntoView", "ListViewSnapPointsMandatorySingle")]
	[DataRow("btev", "Button_Events", "TextBlock_TextTrimming_VerticalAlign")]
	public void When_Ranked_Better_Match_First(string term, string better, string worse)
	{
		var results = SampleSearch.Rank(term, new[] { Sample(worse), Sample(better) });

		Assert.AreEqual(better, results.FirstOrDefault()?.ControlName);
	}

	[TestMethod]
	public void When_Term_Has_Separators_Name_Prefix_Wins()
	{
		var items = new[]
		{
			Sample("Border_Simple_No_Background_With_TextBox"),
			Sample("TextBox_Focus"),
			Sample("Text_Box_Legacy"),
		};

		var results = SampleSearch.Rank("text box", items);

		CollectionAssert.AreEqual(
			new[] { "TextBox_Focus", "Text_Box_Legacy", "Border_Simple_No_Background_With_TextBox" },
			Names(results));
	}

	[TestMethod]
	[DataRow("tb", "TextBox", 4)]
	[DataRow("lvsi", "ListView_ScrollIntoView", 8)]
	[DataRow("lvsi", "ListViewSnapPointsMandatorySingle", 12)]
	[DataRow("button ev", "Button_Events", 4)]
	[DataRow("btev", "Button_Events", 7)]
	[DataRow("btev", "TextBlock_TextTrimming_VerticalAlign", 8)]
	[DataRow("btev", "Events_Button", 0)]
	public void When_Word_Start_Cost_Computed(string term, string name, int expected) =>
		Assert.AreEqual(expected, SampleSearch.GetWordStartCost(new string(term.Where(char.IsLetterOrDigit).ToArray()), name));

	[TestMethod]
	public void When_Same_Tier_Sorted_By_Name()
	{
		var items = new[] { Sample("button_b"), Sample("Button_C"), Sample("Button_A") };

		var results = SampleSearch.Rank("button", items);

		CollectionAssert.AreEqual(new[] { "Button_A", "button_b", "Button_C" }, Names(results));
	}

	[TestMethod]
	public void When_Duplicate_Names_Returned_Once()
	{
		var items = new[] { Sample("Button_Events", "Buttons"), Sample("Button_Events", "Input") };

		var results = SampleSearch.Rank("button", items);

		Assert.AreEqual(1, results.Count);
	}

	[TestMethod]
	public void When_Grouped_Keeps_Rank_Order_And_Cap()
	{
		var ranked = new[]
		{
			Sample("A1", "A"),
			Sample("B1", "B"),
			Sample("A2", "A"),
			Sample("C1", "C"),
		};

		var groups = SampleSearch.Group(ranked, max: 3);

		CollectionAssert.AreEqual(new[] { "A", "B" }, groups.Select(g => g.Key).ToArray());
		CollectionAssert.AreEqual(new[] { "A1", "A2" }, Names(groups[0]));
		CollectionAssert.AreEqual(new[] { "B1" }, Names(groups[1]));
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_RunSearchNow_Updates_Results_Without_Debounce()
	{
		var vm = SampleChooserViewModel.Instance;
		var originalTerm = vm.SearchTerm;
		List<string?> raised = new();
		void OnChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e) => raised.Add(e.PropertyName);

		try
		{
			vm.SearchTerm = "btev";
			vm.PropertyChanged += OnChanged;
			vm.RunSearchNow();

			Assert.IsNotNull(vm.FilteredSamples);
			Assert.AreEqual("Button_Events", vm.FilteredSamples[0].ControlName);
			Assert.AreEqual(vm.FilteredSamples.Count, vm.SearchResultCount);
			Assert.AreEqual(
				Math.Min(vm.SearchResultCount, SampleSearch.MaxGroupedResults),
				vm.SearchResultsGrouped.Sum(g => g.Count()));
			Assert.AreEqual("Buttons", vm.SearchResultsGrouped[0].Key);
			CollectionAssert.IsSubsetOf(
				new[] { nameof(vm.FilteredSamples), nameof(vm.SearchResultsGrouped), nameof(vm.SearchResultCount) },
				raised);
		}
		finally
		{
			vm.PropertyChanged -= OnChanged;
			vm.SearchTerm = originalTerm;
			vm.RunSearchNow();
			await TestServices.WindowHelper.WaitForIdle();
		}
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_No_Results_TryOpenTopSearchResult_Returns_False()
	{
		var vm = SampleChooserViewModel.Instance;
		var originalTerm = vm.SearchTerm;

		try
		{
			vm.SearchTerm = "zzqqxx_no_such_sample";

			Assert.IsFalse(vm.TryOpenTopSearchResult());
			Assert.AreEqual(0, vm.SearchResultCount);
			Assert.AreEqual(0, vm.SearchResultsGrouped.Count);
		}
		finally
		{
			vm.SearchTerm = originalTerm;
			vm.RunSearchNow();
			await TestServices.WindowHelper.WaitForIdle();
		}
	}
}
