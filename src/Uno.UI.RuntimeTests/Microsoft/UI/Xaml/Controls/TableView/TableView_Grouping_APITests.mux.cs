// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\TableView\APITests\TableView_Grouping_APITests.cs, tag winui3/main, commit 79209e041

#pragma warning disable CS8305 // The Tabular types are [Experimental], as in WinUI

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Tabular;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
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
using static Microsoft.UI.Xaml.Tests.MUXControls.ApiTests.TableViewEditingTestHelpers;

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
	[TestProperty("Description", "Verifies TableViewGroupInfo describes its own properties to {Binding} through ICustomPropertyProvider, for an expanded group, a collapsed group and a non-string key.")]
	public async Task VerifyGroupInfoProvidesCustomProperties()
	{
		// A trimmed app's metadata can't describe TableViewGroupInfo, so {Binding} relies on this.
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateGroupedTable();
			LoadContent(tableView);
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			VerifyCustomProperties(GetGroupInfo(tableView, 0), "expanded group");
			GetExpandCollapseProvider(tableView, 1).Collapse();
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			var info = GetGroupInfo(tableView, 1);
			Verify.IsTrue(info?.IsExpandable == true && info?.IsExpanded == false, "Precondition: the group must be expandable but collapsed, so the two getters differ.");
			VerifyCustomProperties(info, "collapsed group");

			var source = TableViewSource.From(MakeShapedPeople());
			source.GroupBy(o => Person(o).Name.Length);
			tableView = CreateGroupedTable(source);
			LoadContent(tableView);
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			var info = GetGroupInfo(tableView, 1);
			// An int key differs from its string KeyText by type; it must also differ from ItemCount.
			Verify.IsTrue(info?.Key is int && !Equals(info.Key, (object)info.ItemCount), "Precondition: the group must have an int key that differs from its item count.");
			VerifyCustomProperties(info, "int key");
		});
	}

	private static void VerifyCustomProperties(TableViewGroupInfo info, string context)
	{
		var provider = (object)info as ICustomPropertyProvider;
		Verify.IsNotNull(provider, $"TableViewGroupInfo must implement ICustomPropertyProvider ({context}).");
		if (provider == null)
		{
			return;
		}

		Verify.AreEqual(info.KeyText, provider.GetStringRepresentation(), $"The string representation is the key text ({context}).");
		// C#/WinRT routes ToString() only to an IStringable declared in metadata, so ask for it explicitly.
		Verify.AreEqual(info.KeyText, ((global::Windows.Foundation.IStringable)(object)info).ToString(), $"IStringable reports the key text too ({context}).");
		Verify.AreEqual("Microsoft.UI.Xaml.Controls.Tabular.TableViewGroupInfo", provider.Type?.FullName, $"The provider's type is the runtime class ({context}).");
		Verify.IsNull(provider.GetCustomProperty("NoSuchProperty"), $"An unknown name has no property ({context}).");
		Verify.IsNull(provider.GetIndexedProperty("KeyText", typeof(string)), $"There are no indexed properties ({context}).");

		var expected = new (string Name, Type Type, object Value)[]
		{
			("Key", typeof(object), info.Key),
			("KeyText", typeof(string), info.KeyText),
			("ItemCount", typeof(int), info.ItemCount),
			("ItemCountText", typeof(string), info.ItemCountText),
			("Level", typeof(int), info.Level),
			("IsExpandable", typeof(bool), info.IsExpandable),
			("IsExpanded", typeof(bool), info.IsExpanded),
		};
		foreach (var (name, type, value) in expected)
		{
			var property = provider.GetCustomProperty(name);
			Verify.IsNotNull(property, $"{name} must be resolvable by name ({context}).");
			if (property == null)
			{
				continue;
			}

			Verify.AreEqual(name, property.Name, $"{name}: Name ({context}).");
			Verify.IsTrue(property.CanRead, $"{name}: CanRead ({context}).");
			Verify.IsFalse(property.CanWrite, $"{name}: read-only ({context}).");
			Verify.AreEqual(type, property.Type, $"{name}: Type ({context}).");
			Verify.AreEqual(value, property.GetValue(info), $"{name}: GetValue must read the projected value ({context}).");
		}
	}

	[TestMethod]
	[TestProperty("Description", "Verifies the default group header shows each group's key and count text and follows an in-place count change.")]
	public async Task VerifyDefaultGroupHeaderShowsGroupKeyAndCountText()
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

		RunOnUIThread.Execute(() =>
		{
			var keys = new[] { "Designer", "Engineer", "Architect" };
			for (var i = 0; i < keys.Length; i++)
			{
				VerifyDefaultGroupHeaderText(GetGroupHeader(tableView, i), keys[i], $"group {i}");
			}

			items.Add(new ShapedPerson("Nadia", "Designer", "Studio"));
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			Verify.AreEqual(3, GetGroupInfo(tableView, 0).ItemCount, "Precondition: the Designer group grew in place.");
			VerifyDefaultGroupHeaderText(GetGroupHeader(tableView, 0), "Designer", "after an in-place count change");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies a copy of the default group header template keeps its own binding converter.")]
	public async Task VerifyCopiedDefaultGroupHeaderTemplateKeepsItsConverter()
	{
		const string converterKey = "TableViewTestGroupKeyBracketConverter";
		TableView tableView = null;
		ObservableCollection<ShapedPerson> items = null;

		RunOnUIThread.Execute(() =>
		{
			Application.Current.Resources[converterKey] = new GroupKeyBracketConverter();

			// Same root and element names as the built-in template, as a copy-and-edit restyle would have.
			var template = (DataTemplate)XamlReader.Load(
				$@"<DataTemplate xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
								 xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
					   <StackPanel x:Name=""TableViewDefaultGroupHeaderContentRoot"" Orientation=""Horizontal"">
						   <TextBlock x:Name=""{DefaultGroupHeaderKeyTextName}""
									  Text=""{{Binding KeyText, Mode=OneWay, Converter={{StaticResource {converterKey}}}}}"" />
						   <TextBlock x:Name=""{DefaultGroupHeaderCountTextName}"" Text=""{{Binding ItemCountText, Mode=OneWay}}"" />
					   </StackPanel>
				   </DataTemplate>");

			items = new ObservableCollection<ShapedPerson>(MakeShapedPeople());
			var source = TableViewSource.From(items);
			source.GroupBy(o => Person(o).Role);
			tableView = CreateGroupedTable(source);
			tableView.GroupHeaderTemplate = template;
			LoadContent(tableView);
		});

		try
		{
			await SettleLayout(tableView);

			RunOnUIThread.Execute(() =>
			{
				VerifyDefaultGroupHeaderText(GetGroupHeader(tableView, 0), "[Designer]", "copied template");
				items.Add(new ShapedPerson("Nadia", "Designer", "Studio"));
			});

			await SettleLayout(tableView);

			RunOnUIThread.Execute(() =>
				VerifyDefaultGroupHeaderText(GetGroupHeader(tableView, 0), "[Designer]", "copied template after an in-place count change"));
		}
		finally
		{
			RunOnUIThread.Execute(() => Application.Current.Resources.Remove(converterKey));
		}
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

			VerifyDefaultGroupHeaderText(GetGroupHeader(tableView, 0), "Designer", "after clearing the template");
			VerifyDefaultGroupHeaderText(GetGroupHeader(tableView, 1), "Engineer", "after clearing the template");
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
	// moves to the interaction tests rather than being weakened into a no-op here.

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

	[TestMethod]
	[TestProperty("Description", "Verifies a collapse does not pull focus back to its header once focus has left the table.")]
	public async Task VerifyGroupFocusRestoreSkippedWhenFocusLeftTable()
	{
		TableView tableView = null;
		Button button = null;

		RunOnUIThread.Execute(() =>
		{
			tableView = CreateGroupedTable();
			button = new Button { Content = "Outside" };
			var panel = new StackPanel();
			panel.Children.Add(tableView);
			panel.Children.Add(button);
			LoadContent(panel);
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			// The collapse captures the focused header for a restore after the deferred reshape.
			Verify.IsTrue(GetGroupHeader(tableView, 0).Focus(FocusState.Keyboard), "Precondition: the header takes keyboard focus.");
			GetExpandCollapseProvider(tableView, 0).Collapse();
			Verify.IsTrue(button.Focus(FocusState.Programmatic), "Precondition: the button takes focus.");
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			Verify.IsTrue(ReferenceEquals(button, FocusManager.GetFocusedElement(tableView.XamlRoot)),
				"Focus that has left the table must not be pulled back to the collapsed header.");
			Verify.IsFalse(GetGroupInfo(tableView, 0)?.IsExpanded ?? true, "The group must still collapse.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies collapsing the group of the selected row clears selection, and neither re-expanding nor a later reshape restores it.")]
	public async Task VerifyGroupCollapseDropsSelectionOfHiddenRow()
	{
		TableView tableView = null;
		TableViewSource source = null;
		var changes = 0;

		RunOnUIThread.Execute(() =>
		{
			source = TableViewSource.From(MakeShapedPeople());
			source.GroupBy(o => Person(o).Role);

			tableView = CreateGroupedTable(source);
			LoadContent(tableView);
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			tableView.SelectionChanged += (s, e) => changes++;
			tableView.Select(GetProjectedLabels(tableView, includeCount: false).IndexOf("Asha"));
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			Verify.AreEqual("Asha", Person(tableView.SelectedItem)?.Name, "Asha is selected.");
			Verify.AreEqual(1, changes, "One SelectionChanged for the select.");
			GetExpandCollapseProvider(tableView, 0).Collapse();
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			Verify.IsNull(tableView.SelectedItem, "Collapsing Designer hides Asha and clears selection.");
			Verify.AreEqual(2, changes, "One SelectionChanged for the clear.");
			tableView.ExpandAllGroups();
		});

		await SettleLayout(tableView);

		// A Reset-driven reshape must not resurrect the selection the collapse cleared.
		RunOnUIThread.Execute(() => source.Sort(o => Person(o).Name, SortDirection.Ascending));

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			Verify.IsNull(tableView.SelectedItem, "Re-expanding and reshaping must not restore the selection.");
			Verify.AreEqual(2, changes, "No further SelectionChanged.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies toggling a focused group header at the bottom of a long grouped table keeps it in view instead of jumping the viewport to an earlier group.")]
	public async Task VerifyFocusedBottomGroupToggleKeepsViewport()
	{
		const int groupCount = 40;
		const int perGroup = 3;
		TableView tableView = null;

		RunOnUIThread.Execute(() =>
		{
			var people = new List<ShapedPerson>();
			for (var g = 0; g < groupCount; g++)
			{
				for (var p = 0; p < perGroup; p++)
				{
					people.Add(new ShapedPerson($"P{g:D2}-{p}", $"G{g:D2}", "Dept"));
				}
			}

			var source = TableViewSource.From(people);
			source.GroupBy(o => Person(o).Role);
			tableView = CreateTableView(source, headers: new[] { "Name", "Role" }, width: 600, height: 300);
			LoadContent(tableView);
		});

		await SettleLayout(tableView);

		// Virtualized extents are estimates, so re-target the end until it stops moving.
		for (var attempt = 0; attempt < 5; attempt++)
		{
			var atEnd = false;
			RunOnUIThread.Execute(() =>
			{
				var scroller = GetBodyScroller(tableView);
				atEnd = scroller.ScrollableHeight > 0 && Math.Abs(scroller.VerticalOffset - scroller.ScrollableHeight) < 0.5;
				if (!atEnd)
				{
					scroller.ChangeView(null, scroller.ScrollableHeight, null, true);
				}
			});
			await SettleLayout(tableView);
			if (atEnd)
			{
				break;
			}
		}

		foreach (var targetGroup in new[] { groupCount - 1, groupCount - 2 })
		{
			var key = $"G{targetGroup:D2}";
			double groupHeight = 0.0;

			RunOnUIThread.Execute(() =>
			{
				var header = FindRealizedGroupHeader(tableView, key);
				Verify.IsNotNull(header, $"Precondition: the header of {key} is realized at the bottom.");
				groupHeight = header.ActualHeight + (perGroup * GetRealizedRows(tableView).Select(r => r.ActualHeight).DefaultIfEmpty(0.0).Max());
				Verify.IsTrue(header.Focus(FocusState.Keyboard), $"Precondition: the {key} header takes keyboard focus.");
				VerifyGroupHeaderInViewport(tableView, key, "before any toggle");
			});

			for (var cycle = 0; cycle < 2; cycle++)
			{
				foreach (var expand in new[] { false, true })
				{
					double offsetBefore = 0.0;
					var context = $"{key} cycle {cycle} after {(expand ? "expand" : "collapse")}";

					RunOnUIThread.Execute(() =>
					{
						offsetBefore = GetBodyScroller(tableView).VerticalOffset;
						var header = FindRealizedGroupHeader(tableView, key);
						Verify.IsNotNull(header, $"The {key} header is realized ({context}).");
						var provider = FrameworkElementAutomationPeer.CreatePeerForElement(header)
							.GetPattern(PatternInterface.ExpandCollapse) as IExpandCollapseProvider;
						if (expand)
						{
							provider.Expand();
						}
						else
						{
							provider.Collapse();
						}
					});

					// The focus restore runs on LayoutUpdated after the deferred reshape.
					await SettleLayout(tableView);
					await SettleLayout(tableView);
					await SettleScrollAnimation(tableView);

					RunOnUIThread.Execute(() =>
					{
						var scroller = GetBodyScroller(tableView);
						var offsetAfter = scroller.VerticalOffset;
						Log.Comment($"{context}: offset {offsetBefore:F1} -> {offsetAfter:F1}, scrollable {scroller.ScrollableHeight:F1}.");

						var focused = FocusManager.GetFocusedElement(tableView.XamlRoot) as TableViewGroupHeader;
						Verify.AreEqual(key, (focused?.Content as TableViewGroupInfo)?.Key as string,
							$"Keyboard focus stays on the toggled header ({context}).");
						Verify.AreEqual(expand, GetGroupInfoByKey(tableView, key)?.IsExpanded ?? !expand,
							$"The group reports the requested state ({context}).");
						VerifyGroupHeaderInViewport(tableView, key, context);

						// Beyond clamping at the end of the extent, scroll anchoring may shift by at most one group.
						var maxBackward = Math.Max(0.0, offsetBefore - scroller.ScrollableHeight) + groupHeight + 0.5;
						Verify.IsLessThanOrEqual(offsetBefore - offsetAfter, maxBackward,
							$"The viewport must not jump to an earlier group ({context}).");
					});
				}
			}
		}
	}

	[TestMethod]
	[TestProperty("Description", "Verifies toggling a focused group header whose realized neighbours are collapsed group headers keeps the viewport, even when ItemsRepeater rescues focus onto a header outside it.")]
	[TestProperty("data:Edge", "{Top, Bottom}")]
	[DataRow("Top")]
	[DataRow("Bottom")]
	public async Task VerifyFocusedGroupToggleAmongCollapsedHeadersKeepsViewport(string edgeRow)
	{
		var edge = edgeRow.Trim();
		const int groupCount = 40;
		const int perGroup = 3;
		const string key = "G20";
		TableView tableView = null;
		var focusLog = new List<string>();
		var rescuedHeaders = new List<string>();
		var toggling = false;

		RunOnUIThread.Execute(() =>
		{
			var people = new List<ShapedPerson>();
			for (var g = 0; g < groupCount; g++)
			{
				for (var p = 0; p < perGroup; p++)
				{
					people.Add(new ShapedPerson($"P{g:D2}-{p}", $"G{g:D2}", "Dept"));
				}
			}

			var source = TableViewSource.From(people);
			source.GroupBy(o => Person(o).Role);
			tableView = CreateTableView(source, headers: new[] { "Name", "Role" }, width: 600, height: 300);
			tableView.GotFocus += (s, e) =>
			{
				if (!toggling)
				{
					return;
				}

				var scroller = GetBodyScroller(tableView);
				var element = e.OriginalSource as FrameworkElement;
				var header = element as TableViewGroupHeader;
				var top = double.NaN;
				try
				{
					top = element?.TransformToVisual(scroller).TransformPoint(new global::Windows.Foundation.Point(0.0, 0.0)).Y ?? double.NaN;
				}
				catch (ArgumentException)
				{
					// The element left the tree between focus and this handler.
				}

				var inView = top >= -0.5 && top + element.ActualHeight <= scroller.ViewportHeight + 0.5;
				var name = header != null ? $"header {(header.Content as TableViewGroupInfo)?.Key}" : element?.GetType().Name ?? "<null>";
				focusLog.Add($"{name} top {top:F1} {(inView ? "in view" : "outside the viewport")}");
				if (header != null && !Equals((header.Content as TableViewGroupInfo)?.Key, key) && !inView)
				{
					rescuedHeaders.Add(name);
				}
			};
			LoadContent(tableView);
		});
		await SettleLayout(tableView);

		RunOnUIThread.Execute(() => tableView.CollapseAllGroups());
		await SettleLayout(tableView);

		// Bring the target header in, expand it, then align it with the requested viewport edge so its
		// neighbour on that side is a collapsed header realized outside the viewport.
		for (var attempt = 0; attempt < 6; attempt++)
		{
			var done = false;
			RunOnUIThread.Execute(() =>
			{
				var scroller = GetBodyScroller(tableView);
				var header = FindRealizedGroupHeader(tableView, key);
				if (header == null)
				{
					var repeater = GetRowsRepeater(tableView);
					var anyHeight = (VisualTreeHelper.GetChild(repeater, 0) as FrameworkElement)?.ActualHeight ?? 0.0;
					scroller.ChangeView(null, 20 * Math.Max(anyHeight, 30.0), null, true);
					return;
				}

				var info = header.Content as TableViewGroupInfo;
				if (!info.IsExpanded)
				{
					(FrameworkElementAutomationPeer.CreatePeerForElement(header).GetPattern(PatternInterface.ExpandCollapse) as IExpandCollapseProvider).Expand();
					return;
				}

				var top = header.TransformToVisual(scroller).TransformPoint(new global::Windows.Foundation.Point(0.0, 0.0)).Y;
				var delta = edge == "Top" ? top : top + header.ActualHeight - scroller.ViewportHeight;
				done = Math.Abs(delta) < 0.5;
				if (!done)
				{
					scroller.ChangeView(null, scroller.VerticalOffset + delta, null, true);
				}
			});
			await SettleLayout(tableView);
			if (done)
			{
				break;
			}
		}

		RunOnUIThread.Execute(() =>
		{
			var header = FindRealizedGroupHeader(tableView, key);
			Verify.IsNotNull(header, $"Precondition: the {key} header is realized.");
			Verify.IsTrue((header.Content as TableViewGroupInfo).IsExpanded, $"Precondition: {key} is expanded.");
			Verify.IsTrue(header.Focus(FocusState.Keyboard), $"Precondition: the {key} header takes keyboard focus.");
			var scroller = GetBodyScroller(tableView);
			var headerTop = header.TransformToVisual(scroller).TransformPoint(new global::Windows.Foundation.Point(0.0, 0.0)).Y;
			var edgeDelta = edge == "Top" ? headerTop : headerTop + header.ActualHeight - scroller.ViewportHeight;
			Verify.IsLessThan(Math.Abs(edgeDelta), 1.0,
				$"Precondition: the {key} header is aligned with the {edge} viewport edge (header top {headerTop:F1}, height {header.ActualHeight:F1}, viewport {scroller.ViewportHeight:F1}).");
			VerifyGroupHeaderInViewport(tableView, key, $"{edge} edge before any toggle");
		});
		await SettleLayout(tableView);

		for (var cycle = 0; cycle < 2; cycle++)
		{
			foreach (var expand in new[] { false, true })
			{
				double offsetBefore = 0.0;
				double topBefore = 0.0;
				var context = $"{edge} edge cycle {cycle} after {(expand ? "expand" : "collapse")}";

				RunOnUIThread.Execute(() =>
				{
					var scroller = GetBodyScroller(tableView);
					offsetBefore = scroller.VerticalOffset;
					var header = FindRealizedGroupHeader(tableView, key);
					Verify.IsNotNull(header, $"The {key} header is realized ({context}).");
					topBefore = header.TransformToVisual(scroller).TransformPoint(new global::Windows.Foundation.Point(0.0, 0.0)).Y;
					focusLog.Clear();
					toggling = true;
					var provider = FrameworkElementAutomationPeer.CreatePeerForElement(header)
						.GetPattern(PatternInterface.ExpandCollapse) as IExpandCollapseProvider;
					if (expand)
					{
						provider.Expand();
					}
					else
					{
						provider.Collapse();
					}
				});

				await SettleLayout(tableView);
				await SettleLayout(tableView);

				RunOnUIThread.Execute(() =>
				{
					toggling = false;
					var scroller = GetBodyScroller(tableView);
					var header = FindRealizedGroupHeader(tableView, key);
					var topAfter = header?.TransformToVisual(scroller).TransformPoint(new global::Windows.Foundation.Point(0.0, 0.0)).Y ?? double.NaN;
					Log.Comment($"{context}: offset {offsetBefore:F1} -> {scroller.VerticalOffset:F1}, header top {topBefore:F1} -> {topAfter:F1}, scrollable {scroller.ScrollableHeight:F1}; focus: {(focusLog.Count == 0 ? "<unchanged>" : string.Join(" | ", focusLog))}.");

					var focused = FocusManager.GetFocusedElement(tableView.XamlRoot) as TableViewGroupHeader;
					Verify.AreEqual(key, (focused?.Content as TableViewGroupInfo)?.Key as string,
						$"Keyboard focus stays on the toggled header ({context}).");
					VerifyGroupHeaderInViewport(tableView, key, context);

					// Estimated extents can settle by a few pixels; revealing a rescued header jumps far more.
					Verify.IsLessThan(Math.Abs(topAfter - topBefore), header?.ActualHeight ?? 0.0,
						$"The toggled header must keep its place in the viewport ({context}).");
				});
			}
		}

		if (rescuedHeaders.Count == 0)
		{
			Log.Warning($"ItemsRepeater's focus rescue never landed on a group header outside the viewport ({edge} edge), so the rescued-focus path was not exercised; the viewport and focus checks above still passed.");
		}
		else
		{
			Log.Comment($"Focus rescues on group headers outside the viewport: {string.Join(", ", rescuedHeaders)}.");
		}
	}

	[TestMethod]
	[TestProperty("Description", "Verifies a focused group toggle dropped because the open edit would not end does not stop a row from being brought into view afterwards.")]
	[TestProperty("data:Reveal", "{KeyboardFocus, StartBringIntoView}")]
	[DataRow("KeyboardFocus")]
	[DataRow("StartBringIntoView")]
	public async Task VerifyDroppedGroupToggleKeepsRowBringIntoView(string revealRow)
	{
		var reveal = revealRow.Trim();
		TableView tableView = null;
		List<EditablePerson> items = null;
		var vetoing = true;

		RunOnUIThread.Execute(() =>
		{
			items = Enumerable.Range(0, 30).Select(i => new EditablePerson { Name = $"P{i:D2}", Role = $"R{i / 10}" }).ToList();
			var source = TableViewSource.From(items);
			source.GroupBy(o => ((EditablePerson)o).Role);
			tableView = CreateTableView(source, headers: new[] { "Name", "Role" }, width: 600, height: 300);
			tableView.IsReadOnly = false;
			tableView.CellEditEnding += (s, e) => e.Cancel = vetoing;
			LoadContent(tableView);
		});
		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			Verify.IsNotNull(CaptureSetValue(GetValueProvider(tableView, items[0], columnIndex: 0), "Vetoed"),
				"Precondition: the vetoed commit fails.");
			Verify.IsTrue(tableView.IsEditing, "Precondition: the edit stays open.");
			var header = FindRealizedGroupHeader(tableView, "R0");
			Verify.IsTrue(header.Focus(FocusState.Keyboard), "Precondition: the header takes keyboard focus.");
			Verify.IsTrue(tableView.IsEditing, "Precondition: the edit is still open.");
			(FrameworkElementAutomationPeer.CreatePeerForElement(header).GetPattern(PatternInterface.ExpandCollapse) as IExpandCollapseProvider).Collapse();
		});
		await SettleLayout(tableView);
		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			Verify.IsTrue(GetGroupInfoByKey(tableView, "R0")?.IsExpanded ?? false, "Precondition: the toggle is dropped while the edit cannot end.");
			vetoing = false;
			Verify.IsTrue(tableView.CancelEdit(), "Precondition: the edit closes.");
		});
		await SettleLayout(tableView);

		TableViewRow target = null;
		double offsetBefore = 0.0;
		RunOnUIThread.Execute(() =>
		{
			var scroller = GetBodyScroller(tableView);
			offsetBefore = scroller.VerticalOffset;
			target = GetRealizedRows(tableView)
				.Select(r => (Row: r, Top: r.TransformToVisual(scroller).TransformPoint(new global::Windows.Foundation.Point(0.0, 0.0)).Y))
				.Where(r => r.Top >= scroller.ViewportHeight)
				.OrderBy(r => r.Top)
				.Select(r => r.Row)
				.FirstOrDefault();
			Verify.IsNotNull(target, "Precondition: a realized row lies below the viewport.");
			if (reveal == "KeyboardFocus")
			{
				Verify.IsTrue(target.Focus(FocusState.Keyboard), "Precondition: the row takes keyboard focus.");
			}
			else
			{
				target.StartBringIntoView();
			}
		});
		await SettleLayout(tableView);
		await SettleScrollAnimation(tableView);

		RunOnUIThread.Execute(() =>
		{
			var scroller = GetBodyScroller(tableView);
			var top = target.TransformToVisual(scroller).TransformPoint(new global::Windows.Foundation.Point(0.0, 0.0)).Y;
			Verify.IsTrue(scroller.VerticalOffset > offsetBefore + 0.5 && top < scroller.ViewportHeight,
				$"{reveal}: the row must be brought into view after the dropped toggle: offset {offsetBefore:F1} -> {scroller.VerticalOffset:F1}, top {top:F1}, viewport {scroller.ViewportHeight:F1}.");
		});
	}

	private static TableViewGroupHeader FindRealizedGroupHeader(TableView tableView, string key)
	{
		var repeater = GetRowsRepeater(tableView);
		return Enumerable.Range(0, VisualTreeHelper.GetChildrenCount(repeater))
			.Select(i => VisualTreeHelper.GetChild(repeater, i) as TableViewGroupHeader)
			.FirstOrDefault(h => h != null && repeater.GetElementIndex(h) >= 0 &&
				Equals((h.Content as TableViewGroupInfo)?.Key, key));
	}

	private static TableViewGroupInfo GetGroupInfoByKey(TableView tableView, string key)
		=> FindRealizedGroupHeader(tableView, key)?.Content as TableViewGroupInfo;

	private static void VerifyGroupHeaderInViewport(TableView tableView, string key, string context)
	{
		var header = FindRealizedGroupHeader(tableView, key);
		Verify.IsNotNull(header, $"The {key} header must stay realized ({context}).");
		if (header == null)
		{
			return;
		}

		var scroller = GetBodyScroller(tableView);
		var top = header.TransformToVisual(scroller).TransformPoint(new global::Windows.Foundation.Point(0.0, 0.0)).Y;
		Verify.IsTrue(top >= -0.5 && top + header.ActualHeight <= scroller.ViewportHeight + 0.5,
			$"The {key} header must stay fully in the viewport ({context}): top {top:F1}, height {header.ActualHeight:F1}, viewport {scroller.ViewportHeight:F1}.");
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

	#region 9.6 Selection across a regroup

	[TestMethod]
	[TestProperty("Description", "Verifies a regroup that moves the selected row into a later group keeps it selected without raising SelectionChanged.")]
	public async Task VerifySelectionSurvivesRegroupWhenKeyChanges()
	{
		TableView tableView = null;
		TableViewSource source = null;
		ShapedPerson ines = null;
		var changes = 0;

		RunOnUIThread.Execute(() =>
		{
			var people = MakeShapedPeople();
			ines = people.First(person => person.Name == "Ines");
			source = TableViewSource.From(people);
			source.GroupBy(o => Person(o).Role);

			tableView = CreateGroupedTable(source);
			LoadContent(tableView);
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			tableView.Select(GetProjectedItems(tableView).IndexOf(ines));
			Verify.AreSame(ines, tableView.SelectedItem, "Precondition: Ines is selected.");

			// Designer is bucketed before Engineer, so the old group is refilled before the new one.
			ines.Role = "Engineer";
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			tableView.SelectionChanged += (s, e) => changes++;
			// GroupBy takes a delegate, so the app re-declares it after a key change.
			source.GroupBy(o => Person(o).Role);
		});

		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			VerifySequence(
				new[] { "#Designer", "Asha", "#Engineer", "Diego", "Rafa", "Ines", "Owen", "#Architect", "Mei" },
				GetProjectedLabels(tableView, includeCount: false),
				"the regroup must move Ines into Engineer");
			Verify.AreSame(ines, tableView.SelectedItem, "The moved row must stay selected.");
			Verify.AreEqual(GetProjectedItems(tableView).IndexOf(ines), tableView.SelectedIndex,
				"SelectedIndex must follow the row to its new position.");
			Verify.AreEqual(0, changes, "The selected item did not change, so no SelectionChanged may be raised.");
		});
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

	internal const string DefaultGroupHeaderKeyTextName = "TableViewGroupHeaderKeyText";
	internal const string DefaultGroupHeaderCountTextName = "TableViewGroupHeaderItemCountText";

	internal static void VerifyDefaultGroupHeaderText(TableViewGroupHeader header, string expectedKeyText, string context)
	{
		Verify.IsNotNull(header, $"The header must be realized ({context}).");
		var info = header?.Content as TableViewGroupInfo;
		Verify.IsNotNull(info, $"The header's Content must be its TableViewGroupInfo ({context}).");
		if (info == null)
		{
			return;
		}

		var presenter = FindVisualChildrenByType<ContentPresenter>(header).FirstOrDefault();
		var textBlocks = presenter == null ? new List<TextBlock>() : FindVisualChildrenByType<TextBlock>(presenter).ToList();
		var key = textBlocks.FirstOrDefault(textBlock => textBlock.Name == DefaultGroupHeaderKeyTextName);
		var count = textBlocks.FirstOrDefault(textBlock => textBlock.Name == DefaultGroupHeaderCountTextName);
		Verify.IsNotNull(key, $"The key TextBlock must be found by name ({context}).");
		Verify.IsNotNull(count, $"The count TextBlock must be found by name ({context}).");

		Verify.AreEqual(expectedKeyText, key?.Text, $"The header must show its group's key ({context}).");
		Verify.IsFalse(string.IsNullOrEmpty(info.ItemCountText), $"Precondition: the group has count text ({context}).");
		Verify.AreEqual(info.ItemCountText, count?.Text, $"The header must show its group's count text ({context}).");
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

internal sealed class GroupKeyBracketConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, string language) => $"[{value}]";

	public object ConvertBack(object value, Type targetType, object parameter, string language) => value;
}
