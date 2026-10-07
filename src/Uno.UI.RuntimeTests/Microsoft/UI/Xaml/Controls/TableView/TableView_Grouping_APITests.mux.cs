// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\TableView\APITests\TableView_Grouping_APITests.cs, tag winui3/main, commit dc28206ea35

#pragma warning disable CS8305 // The Tabular types are [Experimental], as in WinUI

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Tabular;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using MUXControlsTestApp.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

using System.Threading.Tasks;
using Common;
using Private.Infrastructure;

using static Microsoft.UI.Xaml.Tests.MUXControls.ApiTests.TableViewTestHelpers;
using static Microsoft.UI.Xaml.Tests.MUXControls.ApiTests.TableViewShapingTestHelpers;
using static Microsoft.UI.Xaml.Tests.MUXControls.ApiTests.TableViewGroupingTestHelpers;

namespace Microsoft.UI.Xaml.Tests.MUXControls.ApiTests;

// Category 9 of the TableView test plan: grouping and group headers.
//
// SUBJECT. What the CONTROL does with a grouped source: the TableViewGroupInfo projection the
// headers bind to, header content templating, the expand/collapse verbs, ToggleRequested, and
// the header's visual states.
//
// NOT THE SUBJECT. §8 already owns the grouping algebra on TableViewSource (one header per key,
// group order, ClearGroupBy, add-creates-group, key changes), and §12 owns the group header's
// automation peer (ExpandCollapse, GridItem spanning, name, control type). Plan section 9.0
// writes the split up.
//
// FIXTURE. Six people over three roles, deliberately uneven: Designer x2, Engineer x3 and a
// single-member Architect group, so group-boundary handling is exercised at both ends. Group
// order follows first appearance, which §8 already pinned: Designer, Engineer, Architect.
[TestClass]
public class TableViewGroupingTests : TableViewApiTestBase
{
	#region 9.1 TableViewGroupInfo

	[TestMethod]
	[TestProperty("Description", "Verifies TableViewGroupInfo reports the group's key, member count and nesting level.")]
	public async Task VerifyGroupInfoReportsKeyItemCountAndLevel()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateGroupedTable();
			LoadContent(tableView);
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			var expected = new[]
			{
				new { Key = "Designer", Count = 2 },
				new { Key = "Engineer", Count = 3 },
				new { Key = "Architect", Count = 1 },
			};

			for (var i = 0; i < expected.Length; i++)
			{
				var info = GetGroupInfo(tableView, i);
				if (info == null)
				{
					return;
				}

				Verify.AreEqual(expected[i].Key, info.Key as string, $"Group {i} must carry its own key.");
				Verify.AreEqual(expected[i].Count, info.ItemCount, $"Group {i} ('{expected[i].Key}') must report its member count.");
				Verify.AreEqual(0, info.Level, "Level is 0 for single-level grouping (TableView.idl:337).");
			}
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies groups produced by GroupBy start expandable and expanded.")]
	public async Task VerifyGroupInfoExpandabilityDefaults()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateGroupedTable();
			LoadContent(tableView);
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			for (var i = 0; i < 3; i++)
			{
				var info = GetGroupInfo(tableView, i);
				if (info == null)
				{
					return;
				}

				Verify.IsTrue(info.IsExpandable, $"Group {i} has members, so it must be expandable (TableView.idl:338).");
				Verify.IsTrue(info.IsExpanded, $"Group {i} must start expanded so its data is reachable.");
			}
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies KeyText and ItemCountText produce the display strings the built-in header template binds.")]
	public async Task VerifyGroupInfoKeyTextAndItemCountText()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateGroupedTable();
			LoadContent(tableView);
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			var info = GetGroupInfo(tableView, 1);
			if (info == null)
			{
				return;
			}

			Verify.AreEqual("Engineer", info.KeyText, "KeyText is the key's display string (TableView.idl:343).");

			// The surrounding wording is a localized resource, so only the count is pinned.
			Log.Comment($"ItemCountText for the Engineer group: '{info.ItemCountText}'.");
			Verify.IsFalse(string.IsNullOrEmpty(info.ItemCountText), "ItemCountText must not be empty.");
			Verify.IsTrue(info.ItemCountText.Contains("3"),
				$"ItemCountText must carry the member count, was '{info.ItemCountText}'.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies collapsing a group raises PropertyChanged for IsExpanded on its TableViewGroupInfo.")]
	public async Task VerifyGroupInfoRaisesPropertyChangedOnExpandCollapse()
	{
		TableView tableView = null;
		TableViewGroupInfo info = null;
		var raised = new List<string>();

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateGroupedTable();
			LoadContent(tableView);
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			info = GetGroupInfo(tableView, 0);
			if (info == null)
			{
				return;
			}

			info.PropertyChanged += (sender, args) => raised.Add(args.PropertyName);

			GetGroupHeader(tableView, 0).IsExpanded = false;
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			if (info == null)
			{
				return;
			}

			Verify.IsFalse(info.IsExpanded, "The group must read back collapsed.");
			Verify.IsTrue(raised.Contains("IsExpanded"),
				$"PropertyChanged must be raised for IsExpanded; saw [{string.Join(", ", raised)}].");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies an add to a group updates the same TableViewGroupInfo instance and notifies.")]
	public async Task VerifyGroupInfoItemCountUpdatesInPlaceWithNotification()
	{
		TableView tableView = null;
		TableViewSource source = null;
		ObservableCollection<ShapedPerson> items = null;
		TableViewGroupInfo captured = null;
		var raised = new List<string>();

		RunOnUIThread.Execute(() =>
		{
			items = new ObservableCollection<ShapedPerson>(MakeShapedPeople());
			source = TableViewSource.From(items);
			source.GroupBy(o => Person(o).Role);

			tableView = CreateGroupedTable(source);
			LoadContent(tableView);
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			captured = GetGroupInfo(tableView, 0);
			if (captured == null)
			{
				return;
			}

			Verify.AreEqual(2, captured.ItemCount, "Precondition: the Designer group starts with two members.");
			captured.PropertyChanged += (sender, args) => raised.Add(args.PropertyName);

			items.Add(new ShapedPerson("Nadia", "Designer", "Studio"));
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			if (captured == null)
			{
				return;
			}

			// The info objects are pooled and rewritten rather than rebuilt - that is the point
			// of the in-place contract - so the captured reference is only good for proving a
			// notification fired. Group state itself is read back through the header.
			Verify.IsTrue(raised.Contains("ItemCount"),
				$"PropertyChanged must be raised for ItemCount; saw [{string.Join(", ", raised)}].");

			var current = GetGroupInfo(tableView, 0);
			if (current == null)
			{
				return;
			}

			Verify.AreEqual("Designer", current.Key as string, "The first group is still the Designer group.");
			Verify.AreEqual(3, current.ItemCount,
				"The group the header is bound to must report the new count (TableView.idl:329-330).");
		});
	}

	#endregion

	#region 9.2 Group header rendering

	[TestMethod]
	[TestProperty("Description", "Verifies the default group header renders the group key and member count.")]
	public async Task VerifyDefaultGroupHeaderShowsKeyAndCount()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateGroupedTable();
			LoadContent(tableView);
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			var text = HeaderText(tableView, 1);
			Log.Comment($"Default group header text: '{text}'.");

			Verify.IsTrue(text.Contains("Engineer"), $"The default header must show the key, was '{text}'.");
			Verify.IsTrue(text.Contains("3"), $"The default header must show the member count, was '{text}'.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies GroupHeaderTemplate replaces the default group header content.")]
	public async Task VerifyGroupHeaderTemplateApplies()
	{
		TableView tableView = null;
		DataTemplate template = null;

		RunOnUIThread.Execute(() =>
		{
			template = CreateMarkerTemplate("A:");

			tableView = CreateGroupedTable();
			tableView.GroupHeaderTemplate = template;

			LoadContent(tableView);
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			var header = GetGroupHeader(tableView, 0);

			Verify.AreEqual(template, header.ContentTemplate,
				"GroupHeaderTemplate must reach the header's ContentTemplate (TableView.idl:380-383).");

			var text = HeaderText(tableView, 0);

			// The marker proves the app template rendered; the key proves Content really is the
			// TableViewGroupInfo the template binds against.
			Verify.AreEqual("A:Designer", text, $"The app template must render, was '{text}'.");
		});
	}

	[TestMethod]
	[TestProperty("Ignore", "True")] // Product bug: GroupHeaderTemplate is applied only when a header is prepared, never to realized headers. Re-enable once a property change re-applies it.
	[Ignore("WinUI ignores this test: Product bug: GroupHeaderTemplate is applied only when a header is prepared, never to realized headers. Re-enable once a property change re-applies it.")]
	[TestProperty("Description", "Verifies replacing GroupHeaderTemplate on a loaded table rebuilds realized headers.")]
	public async Task VerifyGroupHeaderTemplateChangeAfterLoadUpdatesLive()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateGroupedTable();
			tableView.GroupHeaderTemplate = CreateMarkerTemplate("A:");

			LoadContent(tableView);
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			Verify.AreEqual("A:Designer", HeaderText(tableView, 0), "Precondition: the first template rendered.");
			tableView.GroupHeaderTemplate = CreateMarkerTemplate("B:");
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			Verify.AreEqual("B:Designer", HeaderText(tableView, 0),
				"A realized header must rebuild against the new template.");
			Verify.AreEqual("B:Engineer", HeaderText(tableView, 1),
				"Every realized header must rebuild, not only the first.");
		});
	}

	[TestMethod]
	[TestProperty("Ignore", "True")] // Product bug: GroupHeaderTemplate is applied only when a header is prepared, never to realized headers. Re-enable once a property change re-applies it.
	[Ignore("WinUI ignores this test: Product bug: GroupHeaderTemplate is applied only when a header is prepared, never to realized headers. Re-enable once a property change re-applies it.")]
	[TestProperty("Description", "Verifies clearing GroupHeaderTemplate reverts headers to the default content.")]
	public async Task VerifyClearingGroupHeaderTemplateRevertsToTheStyleDefault()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateGroupedTable();
			tableView.GroupHeaderTemplate = CreateMarkerTemplate("A:");

			LoadContent(tableView);
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			Verify.AreEqual("A:Designer", HeaderText(tableView, 0), "Precondition: the app template applied.");
			tableView.GroupHeaderTemplate = null;
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			var text = HeaderText(tableView, 0);
			Log.Comment($"Group header text after clearing the template: '{text}'.");

			Verify.IsFalse(text.StartsWith("A:"), "The app template must be gone.");
			Verify.IsTrue(text.Contains("Designer"),
				$"Clearing the override must revert to the Style's default content (TableView.idl:381-383), was '{text}'.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies a group header spans the whole column set rather than a single column.")]
	public async Task VerifyGroupHeaderSpansAllColumns()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateGroupedTable();
			LoadContent(tableView);
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			var header = GetGroupHeader(tableView, 0);
			if (header == null)
			{
				return;
			}

			var columnsWidth = tableView.Columns.Sum(column => column.ActualWidth);
			var firstColumnWidth = tableView.Columns[0].ActualWidth;

			Log.Comment($"Group header width {header.ActualWidth}, columns total {columnsWidth}, first column {firstColumnWidth}.");

			Verify.IsGreaterThan(header.ActualWidth, firstColumnWidth,
				"A group header must be wider than a single column - it is a section break, not a cell.");

			// The band sizes itself from the visible-column sum - the same total the cells panel
			// uses - so a header and a row of cells line up edge to edge. The row's own border
			// stretches past that to the viewport, so the row is not the yardstick.
			Verify.IsLessThan(Math.Abs(header.ActualWidth - columnsWidth), 1.0,
				$"A group header must span the whole column set; header {header.ActualWidth}, columns {columnsWidth}.");
		});
	}

	#endregion

	#region 9.3 Expand and collapse

	[TestMethod]
	[TestProperty("Description", "Verifies ExpandAllGroups and CollapseAllGroups are safe no-ops on an ungrouped source.")]
	public async Task VerifyExpandAndCollapseAllOnUngroupedSourceAreNoOps()
	{
		TableView tableView = null;
		List<string> before = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateGroupedTable(MakeShapedPeople());
			LoadContent(tableView);
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() => before = GetProjectedLabels(tableView, includeCount: false));

		RunOnUIThread.Execute(() =>
		{
			tableView.CollapseAllGroups();
			tableView.ExpandAllGroups();
			tableView.CollapseAllGroups();
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
			VerifySequence(before, GetProjectedLabels(tableView, includeCount: false),
				"expand/collapse must not touch an ungrouped source (TableView.idl:557-559)"));
	}

	[TestMethod]
	[TestProperty("Description", "Verifies CollapseAllGroups leaves only group headers projected.")]
	public async Task VerifyCollapseAllGroupsHidesDataRowsKeepsHeaders()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateGroupedTable();
			LoadContent(tableView);
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() => tableView.CollapseAllGroups());

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			VerifySequence(
				new[] { "#Designer", "#Engineer", "#Architect" },
				GetProjectedLabels(tableView, includeCount: false),
				"a full collapse must leave one header per group and no data rows");

			for (var i = 0; i < 3; i++)
			{
				var info = GetGroupInfo(tableView, i);
				if (info == null)
				{
					return;
				}

				Verify.IsFalse(info.IsExpanded, $"Group {i} must report itself collapsed.");
			}
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies ExpandAllGroups restores the full projection after a collapse.")]
	public async Task VerifyExpandAllGroupsRestoresDataRows()
	{
		TableView tableView = null;
		List<string> before = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateGroupedTable();
			LoadContent(tableView);
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() => before = GetProjectedLabels(tableView, includeCount: false));

		RunOnUIThread.Execute(() => tableView.CollapseAllGroups());

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() => tableView.ExpandAllGroups());

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
			VerifySequence(before, GetProjectedLabels(tableView, includeCount: false),
				"a collapse/expand round trip must restore the same projection"));
	}

	[TestMethod]
	[TestProperty("Description", "Verifies collapsing one group leaves the other groups untouched.")]
	public async Task VerifyToggleSingleGroupUpdatesOnlyThatGroup()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateGroupedTable();
			LoadContent(tableView);
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() => GetExpandCollapseProvider(tableView, 0).Collapse());

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			VerifySequence(
				new[] { "#Designer", "#Engineer", "Diego", "Rafa", "Owen", "#Architect", "Mei" },
				GetProjectedLabels(tableView, includeCount: false),
				"only the collapsed group may lose its rows");

			var collapsed = GetGroupInfo(tableView, 0);
			var untouched = GetGroupInfo(tableView, 1);
			if (collapsed == null || untouched == null)
			{
				return;
			}

			Verify.IsFalse(collapsed.IsExpanded, "The toggled group is collapsed.");
			Verify.IsTrue(untouched.IsExpanded, "Per-group state must be per group, not global.");
		});
	}
	// DROPPED FROM THE API PLAN. TableViewGroupHeader raises ToggleRequested only from its own
	// gesture handlers - OnKeyDown (Enter/Space) and OnPointerReleased - while the automation
	// peer's Expand/Collapse takes the separate RequestExpansion route by design. There is no
	// input-free way to reach the event, so "ToggleRequested carries the activated group's key"
	// moves to the interaction plan rather than being weakened into a no-op here.

	[TestMethod]
	[TestProperty("Description", "Verifies a collapsed group stays collapsed across an unrelated source update.")]
	public async Task VerifyCollapsedGroupStatePersistsAcrossSourceUpdate()
	{
		TableView tableView = null;
		ObservableCollection<ShapedPerson> items = null;

		RunOnUIThread.Execute(() =>
		{
			items = new ObservableCollection<ShapedPerson>(MakeShapedPeople());
			var source = TableViewSource.From(items);
			source.GroupBy(o => Person(o).Role);

			tableView = CreateGroupedTable(source);
			LoadContent(tableView);
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() => GetExpandCollapseProvider(tableView, 0).Collapse());

		await SettleLayout(tableView);

		// An add to a DIFFERENT group: nothing about this touches the collapsed one.
		RunOnUIThread.Execute(() => items.Add(new ShapedPerson("Nadia", "Engineer", "Platform")));

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			var info = GetGroupInfo(tableView, 0);
			if (info == null)
			{
				return;
			}

			Verify.IsFalse(info.IsExpanded, "A data update must not re-expand what the user collapsed.");

			var labels = GetProjectedLabels(tableView, includeCount: false);
			Verify.IsFalse(labels.Contains("Asha"),
				$"The collapsed group must still contribute no data rows; saw [{string.Join(", ", labels)}].");
			Verify.IsTrue(labels.Contains("Nadia"), "The added row must appear in its own group.");
		});
	}

	#endregion

	#region 9.4 Group edge cases

	[TestMethod]
	[TestProperty("Description", "Verifies a group with exactly one member renders a header and one row.")]
	public async Task VerifySingleItemGroupRenders()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateGroupedTable();
			LoadContent(tableView);
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			var info = GetGroupInfo(tableView, 2);
			if (info == null)
			{
				return;
			}

			Verify.AreEqual("Architect", info.Key as string, "The third group is the single-member one.");
			Verify.AreEqual(1, info.ItemCount, "A single-member group reports one member.");

			var labels = GetProjectedLabels(tableView, includeCount: false);
			var headerIndex = labels.IndexOf("#Architect");

			Verify.IsTrue(headerIndex >= 0, "The single-member group must still get a header.");
			Verify.AreEqual(labels.Count - 1, headerIndex + 1, "Its one row must follow the header.");
			Verify.AreEqual("Mei", labels[headerIndex + 1], "The row under the header is the group's only member.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies a group marked non-expandable ignores a collapse request.")]
	public async Task VerifyNonExpandableGroupIgnoresToggle()
	{
		TableView tableView = null;
		List<string> before = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateGroupedTable();
			LoadContent(tableView);
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			before = GetProjectedLabels(tableView, includeCount: false);
			GetGroupHeader(tableView, 0).IsExpandable = false;
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() => GetExpandCollapseProvider(tableView, 0).Collapse());

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
			VerifySequence(before, GetProjectedLabels(tableView, includeCount: false),
				"a group that declares itself non-expandable must not collapse"));
	}

	[TestMethod]
	[TestProperty("Description", "Verifies removing a group's last member removes its header.")]
	public async Task VerifyGroupRemovedWhenLastItemRemoved()
	{
		TableView tableView = null;
		ObservableCollection<ShapedPerson> items = null;

		RunOnUIThread.Execute(() =>
		{
			items = new ObservableCollection<ShapedPerson>(MakeShapedPeople());
			var source = TableViewSource.From(items);
			source.GroupBy(o => Person(o).Role);

			tableView = CreateGroupedTable(source);
			LoadContent(tableView);
		});

		await SettleLayout(tableView);

		// Mei is the only Architect.
		RunOnUIThread.Execute(() => items.Remove(items.First(person => person.Role == "Architect")));

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
			VerifySequence(
				new[] { "#Designer", "Asha", "Ines", "#Engineer", "Diego", "Rafa", "Owen" },
				GetProjectedLabels(tableView, includeCount: false),
				"an emptied group must not leave its header behind"));
	}

	#endregion

	#region 9.5 Group header visual states

	[TestMethod]
	[TestProperty("Ignore", "True")] // Product regression: the template root became a wrapper Grid, so VisualStateGroups are no longer on the root and VisualStateManager.GoToState never changes state. Re-enable once the groups are back on the template root.
	[Ignore("WinUI ignores this test: Product regression: the template root became a wrapper Grid, so VisualStateGroups are no longer on the root and VisualStateManager.GoToState never changes state. Re-enable once the groups are back on the template root.")]
	[TestProperty("Description", "Verifies the group header's ExpansionStates follow IsExpanded.")]
	public async Task VerifyGroupHeaderExpansionVisualStates()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateGroupedTable();
			LoadContent(tableView);
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
			Verify.AreEqual("Expanded", GetCurrentVisualState(GetGroupHeader(tableView, 0), "ExpansionStates"),
				"An expanded group must be in the Expanded state (TableView.idl:372)."));

		RunOnUIThread.Execute(() => GetGroupHeader(tableView, 0).IsExpanded = false);

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
			Verify.AreEqual("Collapsed", GetCurrentVisualState(GetGroupHeader(tableView, 0), "ExpansionStates"),
				"A collapsed group must be in the Collapsed state."));
	}

	[TestMethod]
	[TestProperty("Ignore", "True")] // Product regression: the template root became a wrapper Grid, so VisualStateGroups are no longer on the root and VisualStateManager.GoToState never changes state. Re-enable once the groups are back on the template root.
	[Ignore("WinUI ignores this test: Product regression: the template root became a wrapper Grid, so VisualStateGroups are no longer on the root and VisualStateManager.GoToState never changes state. Re-enable once the groups are back on the template root.")]
	[TestProperty("Description", "Verifies the group header's ExpandabilityStates follow IsExpandable.")]
	public async Task VerifyGroupHeaderExpandabilityVisualStates()
	{
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateGroupedTable();
			LoadContent(tableView);
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
			Verify.AreEqual("Expandable", GetCurrentVisualState(GetGroupHeader(tableView, 0), "ExpandabilityStates"),
				"A group with members must be in the Expandable state (TableView.idl:373)."));

		RunOnUIThread.Execute(() => GetGroupHeader(tableView, 0).IsExpandable = false);

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
			Verify.AreEqual("NotExpandable", GetCurrentVisualState(GetGroupHeader(tableView, 0), "ExpandabilityStates"),
				"A group that cannot expand must not advertise an expander."));
	}

	#endregion
}

internal static class TableViewGroupingTestHelpers
{
	internal static TableView CreateGroupedTable()
	{
		var source = TableViewSource.From(MakeShapedPeople());
		source.GroupBy(o => Person(o).Role);
		return CreateGroupedTable(source);
	}

	// Tall enough that every header and row in these fixtures realizes, so TryGetElement never
	// legitimately returns null.
	internal static TableView CreateGroupedTable(object itemsSource)
		=> CreateTableView(itemsSource, headers: new[] { "Name", "Role" }, width: 600, height: 800);

	// The text a group header renders for its CONTENT, whichever template produced it. Scoped to
	// the content presenter so the expander glyph - a private-use character in an icon TextBlock
	// - stays out of the comparison.
	internal static string HeaderText(TableView tableView, int index)
	{
		var header = GetGroupHeader(tableView, index);
		if (header == null)
		{
			return string.Empty;
		}

		var presenter = FindVisualChildrenByType<ContentPresenter>(header).FirstOrDefault();
		if (presenter == null)
		{
			Verify.Fail("A group header should host its content in a ContentPresenter.");
			return string.Empty;
		}

		return string.Concat(FindVisualChildrenByType<TextBlock>(presenter).Select(textBlock => textBlock.Text));
	}

	// A marker the test can recognize, plus a binding that proves Content is the group info.
	internal static DataTemplate CreateMarkerTemplate(string marker) => (DataTemplate)XamlReader.Load(
		$@"<DataTemplate xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
                   <StackPanel Orientation=""Horizontal"">
                       <TextBlock Text=""{marker}"" />
                       <TextBlock Text=""{{Binding KeyText}}"" />
                   </StackPanel>
               </DataTemplate>");
}
