// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\TableView\APITests\TableView_Editing_APITests.cs, tag winui3/main, commit 79209e041

#pragma warning disable CS8305 // The Tabular types are [Experimental], as in WinUI

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Controls.Tabular;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Markup;
using MUXControlsTestApp.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;

using System.Threading.Tasks;
using Common;
using Private.Infrastructure;

using static Microsoft.UI.Xaml.Tests.MUXControls.ApiTests.TableViewTestHelpers;
using static Microsoft.UI.Xaml.Tests.MUXControls.ApiTests.TableViewEditingTestHelpers;

namespace Microsoft.UI.Xaml.Tests.MUXControls.ApiTests;

// Category 10.1 of the TableView test plan: editing gates.
//
// Spec basis: TableView.idl:493-530. IsReadOnly is "[MUX_DEFAULT_VALUE(true)]" and editing is
// "opt-in: while true (the default) no cell can be edited, whatever the column says". IsEditing is
// "true while an edit is in flight on a cell", and explicitly "also covers the brief windows where
// an edit is still opening and where a close is already underway".
//
// How these tests reach editing without a gesture: the IDL says there is no programmatic BeginEdit,
// but TableViewCellAutomationPeer implements IValueProvider and its SetValue drives the real edit
// lifecycle (BeginEdit -> write -> CommitEdit) precisely so that "a programmatic set must not be
// able to do what a user cannot". The Value pattern is advertised only when editing is actually
// possible, so the gates below are observable on the peer without opening an edit at all.
//
// NOT IMPLEMENTED HERE:
//   VerifyEditingDefaultsAreReadOnly - TableViewTests.cs:130-131 asserts IsReadOnly == true and
//                                   IsEditing == false on a fresh control, :160-161 asserts
//                                   TableViewColumn.IsReadOnly == false and CellEditingTemplate
//                                   == null, and :246/:272-273/:323-325/:388-393 cover the DP
//                                   statics and settable roundtrips.
//   Gesture-driven editor tests    - double-click, F2, Enter, Esc, focus loss, editor focus and the
//                                   editor's initial value are all in section 11. SetValue
//                                   overwrites the initial value inside the same call, so no API
//                                   test can observe it.
[TestClass]
public class TableViewEditingGateTests : TableViewApiTestBase
{
	[TestMethod]
	[TestProperty("Description", "Verifies a read-only TableView advertises no Value pattern and refuses SetValue, leaving the item untouched.")]
	public async Task VerifyReadOnlyTableViewOffersNoValuePatternAndRejectsEditing()
	{
		TableView tableView = null;
		List<EditablePerson> items = null;
		AutomationPeer cellPeer = null;

		RunOnUIThread.Execute(() =>
		{
			items = MakeEditableItems();
			tableView = CreateEditableTable(items);

			// Deliberately left at the default. The IDL calls this the opt-in gate.
			Verify.IsTrue(tableView.IsReadOnly, "The test relies on IsReadOnly defaulting to true.");

			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			cellPeer = GetCellPeer(tableView, items[0], columnIndex: 0);

			var pattern = cellPeer.GetPattern(PatternInterface.Value);
			Verify.IsTrue(pattern == null, "A read-only TableView must not advertise the Value pattern on its cells.");

			// The peer still implements IValueProvider; it reports itself read-only and rejects
			// the call. Assistive technology that asks anyway must be told no, not silently fail.
			var provider = (IValueProvider)cellPeer;
			Verify.IsTrue(provider.IsReadOnly, "IValueProvider.IsReadOnly must be true while the TableView is read-only.");

			var error = CaptureSetValue(provider, "Rejected");
			Verify.IsTrue(error != null, "SetValue must throw on a read-only cell rather than editing it.");
			Log.Comment($"SetValue threw as expected: {error.Message}");

			Verify.AreEqual("Asha", items[0].Name, "A rejected SetValue must not change the bound item.");
			Verify.IsFalse(tableView.IsEditing, "A rejected SetValue must not leave an edit open.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies a writable TableView with a writable column advertises the Value pattern.")]
	public async Task VerifyWritableTableViewAndColumnOfferValuePattern()
	{
		TableView tableView = null;
		List<EditablePerson> items = null;

		RunOnUIThread.Execute(() =>
		{
			items = MakeEditableItems();
			tableView = CreateEditableTable(items);
			tableView.IsReadOnly = false;

			Verify.IsFalse(tableView.Columns[0].IsReadOnly, "Columns are writable by default.");

			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var cellPeer = GetCellPeer(tableView, items[0], columnIndex: 0);

			var provider = cellPeer.GetPattern(PatternInterface.Value) as IValueProvider;
			Verify.IsTrue(provider != null, "A writable cell must advertise the Value pattern so assistive technology can edit it.");
			Verify.IsFalse(provider.IsReadOnly, "IValueProvider.IsReadOnly must be false once the app has opted in to editing.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies a read-only column blocks only its own cells while sibling columns stay editable.")]
	public async Task VerifyPerColumnReadOnlyBlocksOnlyThatColumn()
	{
		TableView tableView = null;
		List<EditablePerson> items = null;

		RunOnUIThread.Execute(() =>
		{
			items = MakeEditableItems();
			tableView = CreateEditableTable(items);
			tableView.IsReadOnly = false;
			tableView.Columns[0].IsReadOnly = true;

			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var lockedPeer = GetCellPeer(tableView, items[0], columnIndex: 0);
			Verify.IsTrue(
				lockedPeer.GetPattern(PatternInterface.Value) == null,
				"A cell in a read-only column must not advertise the Value pattern.");

			var lockedError = CaptureSetValue((IValueProvider)lockedPeer, "Blocked");
			Verify.IsTrue(lockedError != null, "SetValue on a read-only column must throw.");
			Verify.AreEqual("Asha", items[0].Name, "A read-only column must not let a value reach the item.");

			var openPeer = GetCellPeer(tableView, items[0], columnIndex: 1);
			var openProvider = openPeer.GetPattern(PatternInterface.Value) as IValueProvider;
			Verify.IsTrue(openProvider != null, "A writable sibling column must stay editable when another column is read-only.");

			openProvider.SetValue("Principal");
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			Verify.AreEqual("Principal", items[0].Role, "The writable column's edit must reach the item.");
			Verify.AreEqual("Asha", items[0].Name, "Editing one column must not disturb another.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies a column carrying a CellEditingTemplate does not advertise the text Value pattern.")]
	public async Task VerifyColumnWithCellEditingTemplateOffersNoValuePattern()
	{
		TableView tableView = null;
		List<EditablePerson> items = null;

		RunOnUIThread.Execute(() =>
		{
			items = MakeEditableItems();
			tableView = CreateEditableTable(items);
			tableView.IsReadOnly = false;

			// A custom editor replaces the built-in TextBox, so a text-valued SetValue has nothing
			// it can reliably write. Advertising the pattern would open an edit on screen and only
			// then fail.
			tableView.Columns[0].CellEditingTemplate = MakeCustomEditorTemplate();

			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var cellPeer = GetCellPeer(tableView, items[0], columnIndex: 0);

			Verify.IsTrue(
				cellPeer.GetPattern(PatternInterface.Value) == null,
				"A column with a CellEditingTemplate must not advertise the text Value pattern.");
			Verify.IsTrue(
				((IValueProvider)cellPeer).IsReadOnly,
				"The peer must report itself read-only when it cannot set a text value.");

			// The column is still editable by gesture - only the text-value contract is withheld.
			Verify.IsFalse(tableView.Columns[0].IsReadOnly, "The column itself is still writable.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies IsEditing is false before an edit, true while one is closing, and false once it has closed.")]
	public async Task VerifyIsEditingIsTrueOnlyWhileAnEditIsOpen()
	{
		TableView tableView = null;
		List<EditablePerson> items = null;
		bool? isEditingInsideHandler = null;

		RunOnUIThread.Execute(() =>
		{
			items = MakeEditableItems();
			tableView = CreateEditableTable(items);
			tableView.IsReadOnly = false;

			tableView.CellEditEnding += (s, e) =>
			{
				isEditingInsideHandler = tableView.IsEditing;
			};

			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			Verify.IsFalse(tableView.IsEditing, "IsEditing must be false before any edit starts.");

			var provider = GetValueProvider(tableView, items[0], columnIndex: 0);
			provider.SetValue("Asha Renamed");
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			Verify.IsTrue(isEditingInsideHandler.HasValue, "CellEditEnding must have been raised.");
			Verify.IsTrue(
				isEditingInsideHandler.Value,
				"IsEditing must be true inside CellEditEnding - the IDL says it covers the window where a close is already underway.");
			Verify.IsFalse(tableView.IsEditing, "IsEditing must return to false once the edit has closed.");
		});
	}
}

// Category 10.3 of the TableView test plan: commit and cancel.
//
// Spec basis: TableView.idl:493-530. CommitEdit and CancelEdit are cell-scoped and return Boolean -
// "False when it did not close: a veto or failed validation leaves the edit open and IsEditing
// true." There is no post-close event by design.
[TestClass]
public class TableViewEditCommitTests : TableViewApiTestBase
{
	[TestMethod]
	[TestProperty("Description", "Verifies a committed edit reaches the bound item, the peer's Value, and the displayed cell text.")]
	public async Task VerifySetValueCommitsThroughToTheBoundItem()
	{
		TableView tableView = null;
		List<EditablePerson> items = null;

		RunOnUIThread.Execute(() =>
		{
			items = MakeEditableItems();
			tableView = CreateEditableTable(items);
			tableView.IsReadOnly = false;
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			Verify.AreEqual("Asha", items[0].Name, "Pre-edit value.");

			var provider = GetValueProvider(tableView, items[0], columnIndex: 0);
			provider.SetValue("Asha Kapoor");
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			Content.UpdateLayout();
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			Verify.AreEqual("Asha Kapoor", items[0].Name, "A committed edit must write the new value to the bound item property.");
			Verify.IsFalse(tableView.IsEditing, "A successful commit must close the edit.");

			var provider = GetValueProvider(tableView, items[0], columnIndex: 0);
			Verify.AreEqual("Asha Kapoor", provider.Value, "IValueProvider.Value must report the committed value.");

			var texts = GetCellTexts(tableView, items[0]);
			Verify.IsTrue(
				texts.Contains("Asha Kapoor"),
				$"The displayed cell must show the committed value. Saw: {string.Join(" | ", texts)}");

			Verify.AreEqual("Diego", items[1].Name, "Editing one row must not touch another.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies CommitEdit and CancelEdit return false and raise nothing when no edit is open, in both read-only and writable configurations.")]
	public async Task VerifyCommitAndCancelEditReturnFalseWhenNoEditIsOpen()
	{
		TableView tableView = null;
		List<EditablePerson> items = null;
		int beginningEditCount = 0;
		int cellEditEndingCount = 0;

		RunOnUIThread.Execute(() =>
		{
			items = MakeEditableItems();
			tableView = CreateEditableTable(items);
			tableView.BeginningEdit += (s, e) => beginningEditCount++;
			tableView.CellEditEnding += (s, e) => cellEditEndingCount++;
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			// Read-only configuration.
			Verify.IsFalse(tableView.CommitEdit(), "CommitEdit must return false when no edit is open (read-only).");
			Verify.IsFalse(tableView.CancelEdit(), "CancelEdit must return false when no edit is open (read-only).");

			// Writable configuration - still nothing open.
			tableView.IsReadOnly = false;
			Verify.IsFalse(tableView.CommitEdit(), "CommitEdit must return false when no edit is open (writable).");
			Verify.IsFalse(tableView.CancelEdit(), "CancelEdit must return false when no edit is open (writable).");

			// Calling twice in a row must stay stable - an app may commit defensively before navigating.
			Verify.IsFalse(tableView.CommitEdit(), "A repeated CommitEdit must stay false.");

			Verify.IsFalse(tableView.IsEditing, "Closing nothing must not open anything.");
			Verify.AreEqual(0, beginningEditCount, "No edit was started, so BeginningEdit must not fire.");
			Verify.AreEqual(0, cellEditEndingCount, "No edit was open, so CellEditEnding must not fire.");
			Verify.AreEqual("Asha", items[0].Name, "No item value may change.");
		});
	}
}

// Category 10.4 of the TableView test plan: editing events.
//
// Spec basis: TableView.idl:274-290. TableViewBeginningEditEventArgs carries Item, Column and a
// settable Cancel; TableViewCellEditEndingEventArgs carries Item, Column, EditAction and a settable
// Cancel. TableViewEditAction (:59-63) has exactly two values, Commit = 0 and Cancel = 1. The IDL
// states that a veto "leaves the edit open and IsEditing true".
[TestClass]
public class TableViewEditEventTests : TableViewApiTestBase
{
	[TestMethod]
	[TestProperty("Description", "Verifies BeginningEdit reports the exact item and column instances being edited.")]
	public async Task VerifyBeginningEditReportsItemAndColumn()
	{
		TableView tableView = null;
		List<EditablePerson> items = null;
		int callCount = 0;
		object reportedItem = null;
		TableViewColumn reportedColumn = null;
		bool cancelDefault = true;

		RunOnUIThread.Execute(() =>
		{
			items = MakeEditableItems();
			tableView = CreateEditableTable(items);
			tableView.IsReadOnly = false;

			tableView.BeginningEdit += (s, e) =>
			{
				callCount++;
				reportedItem = e.Item;
				reportedColumn = e.Column;
				cancelDefault = e.Cancel;
			};

			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var provider = GetValueProvider(tableView, items[2], columnIndex: 1);
			provider.SetValue("Lead Architect");
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			Verify.AreEqual(1, callCount, "BeginningEdit must fire exactly once for one edit.");
			Verify.IsFalse(cancelDefault, "BeginningEdit.Cancel must default to false, so an app that ignores the event does not block editing.");

			// Reference identity, not equality: the IDL calls SelectedItem "the selected data item",
			// and the same rule applies here. A wrapper would break every app that keys off the item.
			Verify.AreSame(items[2], reportedItem, "BeginningEdit.Item must be the data item itself, not a wrapper.");
			Verify.AreSame(tableView.Columns[1], reportedColumn, "BeginningEdit.Column must be the column object being edited.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies cancelling BeginningEdit blocks the edit entirely: nothing opens, nothing is written, and CellEditEnding does not fire.")]
	public async Task VerifyBeginningEditCancelPreventsTheEdit()
	{
		TableView tableView = null;
		List<EditablePerson> items = null;
		int endingCount = 0;

		RunOnUIThread.Execute(() =>
		{
			items = MakeEditableItems();
			tableView = CreateEditableTable(items);
			tableView.IsReadOnly = false;

			tableView.BeginningEdit += (s, e) => e.Cancel = true;
			tableView.CellEditEnding += (s, e) => endingCount++;

			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var provider = GetValueProvider(tableView, items[0], columnIndex: 0);

			var error = CaptureSetValue(provider, "Should not stick");
			Verify.IsTrue(error != null, "A vetoed edit must report failure rather than silently succeeding.");
			Log.Comment($"SetValue threw as expected: {error.Message}");

			Verify.AreEqual("Asha", items[0].Name, "A vetoed edit must not write to the item.");
			Verify.IsFalse(tableView.IsEditing, "A vetoed edit must leave nothing open.");
			Verify.AreEqual(0, endingCount, "Nothing opened, so no edit can end: CellEditEnding must not fire.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies a successful commit raises CellEditEnding once with the Commit action and the edited item and column.")]
	public async Task VerifyCellEditEndingReportsCommitAction()
	{
		TableView tableView = null;
		List<EditablePerson> items = null;
		int callCount = 0;
		TableViewEditAction reportedAction = TableViewEditAction.Cancel;
		object reportedItem = null;
		TableViewColumn reportedColumn = null;
		bool cancelDefault = true;

		RunOnUIThread.Execute(() =>
		{
			items = MakeEditableItems();
			tableView = CreateEditableTable(items);
			tableView.IsReadOnly = false;

			tableView.CellEditEnding += (s, e) =>
			{
				callCount++;
				reportedAction = e.EditAction;
				reportedItem = e.Item;
				reportedColumn = e.Column;
				cancelDefault = e.Cancel;
			};

			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			GetValueProvider(tableView, items[1], columnIndex: 0).SetValue("Diego Munoz");
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			Verify.AreEqual(1, callCount, "CellEditEnding must fire exactly once per closing edit.");
			Verify.AreEqual(TableViewEditAction.Commit, reportedAction, "A commit must be reported as TableViewEditAction.Commit.");
			Verify.IsFalse(cancelDefault, "CellEditEnding.Cancel must default to false.");
			Verify.AreSame(items[1], reportedItem, "CellEditEnding.Item must be the edited data item.");
			Verify.AreSame(tableView.Columns[0], reportedColumn, "CellEditEnding.Column must be the edited column.");
			Verify.AreEqual("Diego Munoz", items[1].Name, "The commit must still reach the item.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies cancelling CellEditEnding blocks the value and leaves the edit open, per the IDL's stay-open rule.")]
	public async Task VerifyCellEditEndingCancelBlocksCommitAndLeavesEditOpen()
	{
		TableView tableView = null;
		List<EditablePerson> items = null;
		bool vetoing = true;
		var actions = new List<TableViewEditAction>();

		RunOnUIThread.Execute(() =>
		{
			items = MakeEditableItems();
			tableView = CreateEditableTable(items);
			tableView.IsReadOnly = false;

			tableView.CellEditEnding += (s, e) =>
			{
				actions.Add(e.EditAction);
				e.Cancel = vetoing;
			};

			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var error = CaptureSetValue(GetValueProvider(tableView, items[0], columnIndex: 0), "Vetoed value");
			Verify.IsTrue(error != null, "A vetoed commit must report failure to the caller.");
			Log.Comment($"SetValue threw as expected: {error.Message}");

			Verify.AreEqual("Asha", items[0].Name, "A vetoed commit must not let the value reach the item.");

			// The IDL is explicit: "a veto or failed validation leaves the edit open and IsEditing true".
			Verify.IsTrue(tableView.IsEditing, "A vetoed commit must leave the edit open so the user can correct or discard it.");
			Verify.AreEqual(1, actions.Count, "CellEditEnding must have been raised once for the vetoed commit.");
			Verify.AreEqual(TableViewEditAction.Commit, actions[0], "The vetoed attempt was a commit.");

			// Stop vetoing and close the still-open edit. A normal cancel is itself vetoable, so the
			// handler has to stand down first.
			vetoing = false;
			Verify.IsTrue(tableView.CancelEdit(), "CancelEdit must close an edit that is genuinely open.");
			Verify.IsFalse(tableView.IsEditing, "The edit must be closed afterwards.");
			Verify.AreEqual("Asha", items[0].Name, "The cancel must leave the pre-edit value in place.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies a CanSort = false clear queued behind a sort on another column, both requested from CellEditEnding, does not clear that newer sort.")]
	public async Task VerifyQueuedCanSortClearDoesNotClearNewerSortRequestedDuringEditEnding()
	{
		TableView tableView = null;
		List<EditablePerson> items = null;
		bool requested = false;

		RunOnUIThread.Execute(() =>
		{
			items = MakeEditableItems();
			tableView = CreateEditableTable(items);
			tableView.IsReadOnly = false;
			LoadContent(tableView);
		});
		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			Verify.IsTrue(tableView.SortByColumn(tableView.Columns[0], SortDirection.Ascending), "Precondition: the Name column sorts.");
		});
		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var name = tableView.Columns[0];
			var role = tableView.Columns[1];
			tableView.CellEditEnding += (s, e) =>
			{
				if (requested)
				{
					return;
				}
				requested = true;
				tableView.SortByColumn(role, SortDirection.Ascending);
				name.CanSort = false;
			};

			var error = CaptureSetValue(GetValueProvider(tableView, items[0], columnIndex: 0), "Asha K");
			Verify.IsNull(error, "The commit must succeed.");
			Verify.IsTrue(requested, "Precondition: CellEditEnding requested both changes.");
		});
		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			Verify.AreEqual(SortDirection.None, tableView.Columns[0].SortDirection, "The opted-out column must not stay sorted.");
			Verify.AreEqual(SortDirection.Ascending, tableView.Columns[1].SortDirection,
				"The stale opt-out clear must not remove the newer sort on another column.");
			var roles = GetProjectedItems(tableView).Select(item => ((EditablePerson)item).Role).ToList();
			Verify.AreEqual("Architect,Designer,Engineer", string.Join(",", roles), "The rows must stay sorted by Role.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies a sort opt-out made from a BeginningEdit handler, cancelled or not, still clears the opted-out sort.")]
	[TestProperty("data:Change", "{CanSortCancel, CanSortContinue, CanUserSortColumnsCancel}")]
	[DataRow("CanSortCancel")]
	[DataRow("CanSortContinue")]
	[DataRow("CanUserSortColumnsCancel")]
	public async Task VerifySortOptOutDuringBeginningEditClearsSort(string changeRow)
	{
		var change = changeRow.Trim();
		TableView tableView = null;
		List<EditablePerson> items = null;
		bool requested = false;
		int clearedCount = 0;

		RunOnUIThread.Execute(() =>
		{
			items = MakeEditableItems();
			tableView = CreateEditableTable(items);
			tableView.IsReadOnly = false;
			LoadContent(tableView);
		});
		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			Verify.IsTrue(tableView.SortByColumn(tableView.Columns[0], SortDirection.Descending), "Precondition: the Name column sorts.");
		});
		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var name = tableView.Columns[0];
			tableView.Sorted += (s, e) =>
			{
				if (e.Direction == SortDirection.None)
				{
					clearedCount++;
				}
			};
			tableView.BeginningEdit += (s, e) =>
			{
				if (requested)
				{
					return;
				}
				requested = true;
				if (change == "CanUserSortColumnsCancel")
				{
					tableView.CanUserSortColumns = false;
				}
				else
				{
					name.CanSort = false;
				}
				e.Cancel = change != "CanSortContinue";
			};

			var error = CaptureSetValue(GetValueProvider(tableView, items[0], columnIndex: 1), "Pilot");
			Log.Comment($"SetValue: {error?.Message ?? "succeeded"}");
			Verify.IsTrue(requested, "Precondition: BeginningEdit opted the sort out.");
			if (change == "CanSortContinue")
			{
				Verify.IsNull(error, "An edit BeginningEdit let continue must accept the value.");
				Verify.AreEqual("Pilot", items[0].Role, "The value must be committed.");
				Verify.IsFalse(tableView.IsEditing, "The committed edit must be closed.");
			}
		});
		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			Verify.AreEqual(SortDirection.None, tableView.Columns[0].SortDirection, $"{change}: the opted-out sort must be cleared.");
			Verify.AreEqual(1, clearedCount, $"{change}: the clear must be reported once.");
			var names = GetProjectedItems(tableView).Select(item => ((EditablePerson)item).Name).ToList();
			Verify.AreEqual("Asha,Diego,Mei", string.Join(",", names), $"{change}: the rows must return to source order.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies a sort opt-out made from a BeginningEdit handler that lets the edit open keeps the edit open, and clears the sort when the edit closes.")]
	public async Task VerifySortOptOutDuringBeginningEditKeepsTheEditOpen()
	{
		TableView tableView = null;
		List<EditablePerson> items = null;
		bool requested = false;
		bool vetoClose = true;
		int clearedCount = 0;

		RunOnUIThread.Execute(() =>
		{
			items = MakeEditableItems();
			tableView = CreateEditableTable(items);
			tableView.IsReadOnly = false;
			LoadContent(tableView);
		});
		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			Verify.IsTrue(tableView.SortByColumn(tableView.Columns[0], SortDirection.Descending), "Precondition: the Name column sorts.");
		});
		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var name = tableView.Columns[0];
			tableView.Sorted += (s, e) =>
			{
				if (e.Direction == SortDirection.None)
				{
					clearedCount++;
				}
			};
			tableView.BeginningEdit += (s, e) =>
			{
				if (!requested)
				{
					requested = true;
					name.CanSort = false;
				}
			};
			// Holds the edit open after SetValue so its state can be observed.
			tableView.CellEditEnding += (s, e) => e.Cancel = vetoClose;

			var error = CaptureSetValue(GetValueProvider(tableView, items[0], columnIndex: 1), "Pilot");
			Log.Comment($"SetValue: {error?.Message ?? "succeeded"}");
			Verify.IsTrue(requested, "Precondition: BeginningEdit opted the sort out.");
			Verify.IsNotNull(error, "Precondition: the vetoed commit is reported.");
			Verify.AreEqual(unchecked((int)0x80004005), error.HResult, "The edit must open and only the vetoed commit fail.");
			Verify.IsTrue(tableView.IsEditing, "The opened edit must stay open.");
		});
		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			Verify.IsTrue(tableView.IsEditing, "The deferred sort clear must not close the open edit.");
			Verify.AreEqual(0, clearedCount, "The sort clear waits for the edit to close.");

			vetoClose = false;
			Verify.IsTrue(tableView.CommitEdit(), "The edit commits.");
			Verify.AreEqual("Pilot", items[0].Role, "The value must be committed.");
		});
		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			Verify.AreEqual(SortDirection.None, tableView.Columns[0].SortDirection, "The opted-out sort must be cleared once the edit closes.");
			Verify.AreEqual(1, clearedCount, "The clear must be reported once.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies a sort opt-out made from BeginningEdit still clears the sort when the edit is then force-closed from a property-change callback (IsReadOnly), which cannot replay it in place, and applies before a newer sort requested in the meantime.")]
	[TestProperty("data:OptOut", "{CanSort, CanUserSortColumns, CanUserSortColumnsThenSort}")]
	[DataRow("CanSort")]
	[DataRow("CanUserSortColumns")]
	[DataRow("CanUserSortColumnsThenSort")]
	public async Task VerifySortOptOutDuringBeginningEditClearsAfterForcedClose(string optOutRow)
	{
		var optOut = optOutRow.Trim();
		TableView tableView = null;
		List<EditablePerson> items = null;
		bool requested = false;
		bool vetoClose = true;
		int clearedCount = 0;

		RunOnUIThread.Execute(() =>
		{
			items = MakeEditableItems();
			tableView = CreateEditableTable(items);
			tableView.IsReadOnly = false;
			LoadContent(tableView);
		});
		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			Verify.IsTrue(tableView.SortByColumn(tableView.Columns[0], SortDirection.Descending), "Precondition: the Name column sorts.");
		});
		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var name = tableView.Columns[0];
			tableView.Sorted += (s, e) =>
			{
				if (e.Direction == SortDirection.None)
				{
					clearedCount++;
				}
			};
			tableView.BeginningEdit += (s, e) =>
			{
				if (!requested)
				{
					requested = true;
					if (optOut == "CanSort")
					{
						name.CanSort = false;
					}
					else
					{
						tableView.CanUserSortColumns = false;
					}
				}
			};

			// Holds the edit open after SetValue so a forced close can end it.
			tableView.CellEditEnding += (s, e) => e.Cancel = vetoClose;

			CaptureSetValue(GetValueProvider(tableView, items[0], columnIndex: 1), "Pilot");
			Verify.IsTrue(requested, "Precondition: BeginningEdit opted the sort out.");
			Verify.IsTrue(tableView.IsEditing, "Precondition: the edit stays open.");
			Verify.AreEqual(0, clearedCount, "Precondition: the sort clear waits for the edit to close.");

			// A forced close from a property-change callback, which must not reshape in place.
			vetoClose = false;
			tableView.IsReadOnly = true;
			Verify.IsFalse(tableView.IsEditing, "Precondition: IsReadOnly closes the edit.");
			if (optOut == "CanUserSortColumnsThenSort")
			{
				Verify.IsTrue(tableView.SortByColumn(tableView.Columns[1], SortDirection.Ascending), "The newer sort applies.");
			}
		});
		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			if (optOut == "CanUserSortColumnsThenSort")
			{
				Verify.AreEqual(SortDirection.Ascending, tableView.Columns[1].SortDirection, "The older deferred clear must not undo the newer sort.");
			}
			Verify.AreEqual(SortDirection.None, tableView.Columns[0].SortDirection, $"{optOut}: the opted-out sort must be cleared once the edit closes.");
			Verify.AreEqual(1, clearedCount, $"{optOut}: the clear must be reported once.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies sort opt-outs deferred past a forced edit close still run after an earlier one's Sorting handler throws.")]
	public async Task VerifyDeferredSortOptOutsSurviveThrowingSortingHandler()
	{
		TableView tableView = null;
		List<EditablePerson> items = null;
		bool requested = false;
		bool vetoClose = true;
		bool thrown = false;
		int clearedCount = 0;

		RunOnUIThread.Execute(() =>
		{
			items = MakeEditableItems();
			tableView = CreateEditableTable(items);
			tableView.IsReadOnly = false;
			LoadContent(tableView);
		});
		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			Verify.IsTrue(tableView.SortByColumn(tableView.Columns[0], SortDirection.Descending), "Precondition: the Name column sorts.");
		});
		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var name = tableView.Columns[0];
			tableView.Sorting += (s, e) =>
			{
				if (!thrown)
				{
					thrown = true;
					throw new InvalidOperationException("Sorting handler fault");
				}
			};
			tableView.Sorted += (s, e) =>
			{
				if (e.Direction == SortDirection.None)
				{
					clearedCount++;
				}
			};
			tableView.BeginningEdit += (s, e) =>
			{
				if (!requested)
				{
					requested = true;
					tableView.CanUserSortColumns = false;
					name.CanSort = false;
				}
			};
			tableView.CellEditEnding += (s, e) => e.Cancel = vetoClose;

			CaptureSetValue(GetValueProvider(tableView, items[0], columnIndex: 1), "Pilot");
			Verify.IsTrue(requested, "Precondition: BeginningEdit opted the sort out.");
			Verify.IsTrue(tableView.IsEditing, "Precondition: the edit stays open.");

			vetoClose = false;
			tableView.IsReadOnly = true;
			Verify.IsFalse(tableView.IsEditing, "Precondition: IsReadOnly closes the edit.");
		});
		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			Verify.IsTrue(thrown, "Precondition: the first deferred clear's Sorting handler threw.");
			Verify.AreEqual(SortDirection.None, tableView.Columns[0].SortDirection, "The remaining deferred clear must still run.");
			Verify.AreEqual(1, clearedCount, "The clear must be reported once.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies a sort opt-out deferred past an edit force-closed while the table is unloaded is applied by the time the table is back, and is not replayed over a newer sort made after reload or while unloaded.")]
	[TestProperty("data:When", "{AfterReload, WhileUnloaded}")]
	[DataRow("AfterReload")]
	[DataRow("WhileUnloaded")]
	public async Task VerifyDeferredSortOptOutAppliesBeforeSortAfterReload(string whenRow)
	{
		var sortWhileUnloaded = whenRow.Trim() == "WhileUnloaded";
		TableView tableView = null;
		List<EditablePerson> items = null;
		bool requested = false;
		bool vetoClose = true;
		int clearedCount = 0;

		RunOnUIThread.Execute(() =>
		{
			items = MakeEditableItems();
			tableView = CreateEditableTable(items);
			tableView.IsReadOnly = false;
			LoadContent(tableView);
		});
		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			Verify.IsTrue(tableView.SortByColumn(tableView.Columns[0], SortDirection.Descending), "Precondition: the Name column sorts.");
		});
		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			tableView.Sorted += (s, e) =>
			{
				if (e.Direction == SortDirection.None)
				{
					clearedCount++;
				}
			};
			tableView.BeginningEdit += (s, e) =>
			{
				if (!requested)
				{
					requested = true;
					tableView.CanUserSortColumns = false;
				}
			};
			tableView.CellEditEnding += (s, e) => e.Cancel = vetoClose;

			CaptureSetValue(GetValueProvider(tableView, items[0], columnIndex: 1), "Pilot");
			Verify.IsTrue(tableView.IsEditing, "Precondition: the edit stays open.");
			// Still vetoed, so the focus loss on unload cannot commit it in place.
			Content = null;
		});
		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			vetoClose = false;
			tableView.IsReadOnly = true;
			Verify.IsFalse(tableView.IsEditing, "Precondition: the edit is closed while unloaded.");
			Log.Comment($"Cleared synchronously by the forced close: {clearedCount}.");
		});
		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			Log.Comment($"Before reload: Name sort={tableView.Columns[0].SortDirection}, cleared={clearedCount}.");
			if (sortWhileUnloaded)
			{
				Verify.IsTrue(tableView.SortByColumn(tableView.Columns[1], SortDirection.Ascending), "The app's sort applies while unloaded.");
			}
			tableView.IsReadOnly = false;
			LoadContent(tableView);
		});
		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			if (sortWhileUnloaded)
			{
				Verify.AreEqual(SortDirection.Ascending, tableView.Columns[1].SortDirection, "The deferred clear must not undo the sort applied while unloaded.");
				Verify.AreEqual(1, clearedCount, "The clear must be reported once.");
				return;
			}
			Verify.AreEqual(SortDirection.None, tableView.Columns[0].SortDirection, "The deferred clear applies by the time the table is back, without a newer request.");
			Verify.AreEqual(1, clearedCount, "The clear must be reported once.");
			Verify.IsTrue(tableView.SortByColumn(tableView.Columns[1], SortDirection.Ascending), "The newer sort applies.");
		});
		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			Verify.AreEqual(SortDirection.Ascending, tableView.Columns[1].SortDirection, "The older deferred clear must not undo the newer sort.");
			Verify.AreEqual(1, clearedCount, "The clear must be reported once.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies a sort opt-out deferred past a forced edit close on grouped data, flushed ahead of a newer CollapseAllGroups, does not drop that collapse.")]
	public async Task VerifyDeferredSortOptOutKeepsNewerGroupCollapse()
	{
		TableView tableView = null;
		List<EditablePerson> items = null;
		bool requested = false;
		bool vetoClose = true;

		RunOnUIThread.Execute(() =>
		{
			items = MakeEditableItems();
			var source = TableViewSource.From(items);
			source.GroupBy(o => ((EditablePerson)o).Role);
			tableView = CreateEditableTable(items);
			tableView.ItemsSource = source;
			tableView.IsReadOnly = false;
			LoadContent(tableView);
		});
		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
			Verify.IsTrue(tableView.SortByColumn(tableView.Columns[0], SortDirection.Descending), "Precondition: the Name column sorts."));
		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			var name = tableView.Columns[0];
			tableView.BeginningEdit += (s, e) =>
			{
				if (!requested)
				{
					requested = true;
					name.CanSort = false;
				}
			};
			tableView.CellEditEnding += (s, e) => e.Cancel = vetoClose;

			CaptureSetValue(GetValueProvider(tableView, items[0], columnIndex: 0), "Ana");
			Verify.IsTrue(tableView.IsEditing, "Precondition: the edit stays open.");
			vetoClose = false;
			tableView.IsReadOnly = true;
			Verify.IsFalse(tableView.IsEditing, "Precondition: IsReadOnly closes the edit.");
			Verify.AreEqual(SortDirection.Descending, name.SortDirection, "Precondition: the clear is still deferred.");

			tableView.CollapseAllGroups();
		});
		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			Verify.AreEqual(SortDirection.None, tableView.Columns[0].SortDirection, "The deferred clear applies.");
			Verify.AreEqual(0, GetProjectedElements(tableView).OfType<TableViewRow>().Count(), "The newer CollapseAllGroups must still collapse every group.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies a column sort opt-out deferred past a forced edit close does not clear a TableViewSource sort the app declared before the opt-out was replayed.")]
	public async Task VerifyDeferredColumnOptOutKeepsNewerSourceSort()
	{
		TableView tableView = null;
		TableViewSource source = null;
		List<EditablePerson> items = null;
		bool requested = false;
		bool vetoClose = true;

		RunOnUIThread.Execute(() =>
		{
			items = MakeEditableItems();
			source = TableViewSource.From(items);
			tableView = CreateEditableTable(items);
			tableView.ItemsSource = source;
			tableView.IsReadOnly = false;
			LoadContent(tableView);
		});
		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
			Verify.IsTrue(tableView.SortByColumn(tableView.Columns[0], SortDirection.Descending), "Precondition: the Name column sorts."));
		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			var name = tableView.Columns[0];
			tableView.BeginningEdit += (s, e) =>
			{
				if (!requested)
				{
					requested = true;
					name.CanSort = false;
				}
			};
			tableView.CellEditEnding += (s, e) => e.Cancel = vetoClose;

			CaptureSetValue(GetValueProvider(tableView, items[0], columnIndex: 1), "Pilot");
			Verify.IsTrue(tableView.IsEditing, "Precondition: the edit stays open.");
			vetoClose = false;
			tableView.IsReadOnly = true;
			Verify.IsFalse(tableView.IsEditing, "Precondition: IsReadOnly closes the edit.");
			Verify.AreEqual(SortDirection.Descending, name.SortDirection, "Precondition: the clear is still deferred.");

			source.Sort("Role", SortDirection.Ascending);
		});
		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			Verify.AreEqual(SortDirection.None, tableView.Columns[0].SortDirection, "The opted-out column must not stay sorted.");
			Verify.AreEqual(SortDirection.Ascending, tableView.Columns[1].SortDirection, "The app's newer source sort must stay lit.");
			var names = GetProjectedItems(tableView).OfType<EditablePerson>().Select(person => person.Name).ToList();
			VerifySequence(new[] { "Mei", "Diego", "Asha" }, names, "the app's newer source sort must still order the rows");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies an edit a Sorting handler opens while a deferred sort opt-out is flushed ahead of CollapseAllGroups is not force-closed by that collapse.")]
	public async Task VerifyEditOpenedWhileFlushingBlocksGroupCollapse()
	{
		TableView tableView = null;
		List<EditablePerson> items = null;
		bool requested = false;
		bool reopened = false;
		bool vetoClose = true;

		RunOnUIThread.Execute(() =>
		{
			items = MakeEditableItems();
			var source = TableViewSource.From(items);
			source.GroupBy(o => ((EditablePerson)o).Role);
			tableView = CreateEditableTable(items);
			tableView.ItemsSource = source;
			tableView.IsReadOnly = false;
			LoadContent(tableView);
		});
		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
			Verify.IsTrue(tableView.SortByColumn(tableView.Columns[0], SortDirection.Descending), "Precondition: the Name column sorts."));
		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			tableView.BeginningEdit += (s, e) =>
			{
				if (!requested)
				{
					requested = true;
					tableView.CanUserSortColumns = false;
				}
			};
			tableView.CellEditEnding += (s, e) => e.Cancel = vetoClose;

			CaptureSetValue(GetValueProvider(tableView, items[0], columnIndex: 0), "Ana");
			Verify.IsTrue(tableView.IsEditing, "Precondition: the edit stays open.");
			vetoClose = false;
			tableView.IsReadOnly = true;
			Verify.IsFalse(tableView.IsEditing, "Precondition: IsReadOnly closes the edit.");

			tableView.Sorting += (s, e) =>
			{
				if (!reopened)
				{
					reopened = true;
					e.Cancel = true;
					tableView.IsReadOnly = false;
					vetoClose = true;
					CaptureSetValue(GetValueProvider(tableView, items[1], columnIndex: 0), "Vetoed");
				}
			};

			tableView.CollapseAllGroups();
			Verify.IsTrue(reopened, "Precondition: the flushed clear raised Sorting.");
		});
		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			Verify.IsTrue(tableView.IsEditing, "The vetoed edit must stay open.");
			Verify.AreEqual("Diego", items[1].Name, "The vetoed value must not be committed.");
			Verify.AreEqual(3, GetProjectedElements(tableView).OfType<TableViewRow>().Count(), "The collapse must not run under the open edit.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies an edit a Sorting handler opens while a deferred sort opt-out is flushed ahead of a per-group collapse is not force-closed by that collapse.")]
	public async Task VerifyEditOpenedWhileFlushingBlocksGroupToggle()
	{
		TableView tableView = null;
		List<EditablePerson> items = null;
		bool requested = false;
		bool reopened = false;
		bool vetoClose = true;

		RunOnUIThread.Execute(() =>
		{
			items = MakeEditableItems();
			var source = TableViewSource.From(items);
			source.GroupBy(o => ((EditablePerson)o).Role);
			tableView = CreateEditableTable(items);
			tableView.ItemsSource = source;
			tableView.IsReadOnly = false;
			LoadContent(tableView);
		});
		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
			Verify.IsTrue(tableView.SortByColumn(tableView.Columns[0], SortDirection.Descending), "Precondition: the Name column sorts."));
		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			tableView.BeginningEdit += (s, e) =>
			{
				if (!requested)
				{
					requested = true;
					tableView.CanUserSortColumns = false;
				}
			};
			tableView.CellEditEnding += (s, e) => e.Cancel = vetoClose;

			CaptureSetValue(GetValueProvider(tableView, items[0], columnIndex: 0), "Ana");
			Verify.IsTrue(tableView.IsEditing, "Precondition: the edit stays open.");

			// Posted ahead of the drain the forced close below posts, so it flushes the deferred clear itself.
			var engineer = GetProjectedElements(tableView).OfType<TableViewGroupHeader>()
				.First(header => (string)((TableViewGroupInfo)header.Content).Key == "Engineer");
			((IExpandCollapseProvider)FrameworkElementAutomationPeer.CreatePeerForElement(engineer).GetPattern(PatternInterface.ExpandCollapse)).Collapse();

			vetoClose = false;
			tableView.IsReadOnly = true;
			Verify.IsFalse(tableView.IsEditing, "Precondition: IsReadOnly closes the edit.");

			tableView.Sorting += (s, e) =>
			{
				if (!reopened)
				{
					reopened = true;
					e.Cancel = true;
					tableView.IsReadOnly = false;
					vetoClose = true;
					CaptureSetValue(GetValueProvider(tableView, items[1], columnIndex: 0), "Vetoed");
				}
			};
		});
		await SettleLayout(tableView);

		RunOnUIThread.Execute(() =>
		{
			Verify.IsTrue(reopened, "Precondition: the flushed clear raised Sorting.");
			Verify.IsTrue(tableView.IsEditing, "The vetoed edit must stay open.");
			Verify.AreEqual("Diego", items[1].Name, "The vetoed value must not be committed.");
			Verify.AreEqual(3, GetProjectedElements(tableView).OfType<TableViewRow>().Count(), "The group collapse must not run under the open edit.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies a sort opt-out deferred while another table owns the source still waits when this table's reclaim is refused, also once the table unloads again and a sort request flushes deferred work, and clears the sort once the reclaim succeeds.")]
	[TestProperty("data:Unload", "{StayLoaded, UnloadBeforeRelease, SortWhileUnloaded}")]
	[DataRow("StayLoaded")]
	[DataRow("UnloadBeforeRelease")]
	[DataRow("SortWhileUnloaded")]
	public async Task VerifyDeferredSortOptOutWaitsForRefusedReclaim(string unloadRow)
	{
		var mode = unloadRow.Trim();
		var unload = mode != "StayLoaded";
		int sortedCount = 0;
		bool countingSorted = false;
		TableView first = null;
		TableView second = null;
		TableViewSource shared = null;
		StackPanel host = null;
		List<EditablePerson> items = null;
		bool requested = false;
		bool vetoClose = true;

		RunOnUIThread.Execute(() =>
		{
			items = MakeEditableItems();
			shared = TableViewSource.From(items);
			first = CreateEditableTable(items);
			first.ItemsSource = shared;
			first.IsReadOnly = false;
			host = new StackPanel();
			host.Children.Add(first);
			LoadContent(host);
		});
		await SettleLayout(first);

		RunOnUIThread.Execute(() =>
			Verify.IsTrue(first.SortByColumn(first.Columns[0], SortDirection.Descending), "Precondition: the Name column sorts."));
		await SettleLayout(first);

		RunOnUIThread.Execute(() =>
		{
			first.BeginningEdit += (s, e) =>
			{
				if (!requested)
				{
					requested = true;
					first.CanUserSortColumns = false;
				}
			};
			first.CellEditEnding += (s, e) => e.Cancel = vetoClose;

			CaptureSetValue(GetValueProvider(first, items[0], columnIndex: 1), "Pilot");
			Verify.IsTrue(first.IsEditing, "Precondition: the edit stays open.");
			host.Children.Remove(first);
			second = CreateEditableTable(items);
			second.ItemsSource = shared;
			host.Children.Add(second);
		});
		await SettleLayout(second);

		RunOnUIThread.Execute(() =>
		{
			vetoClose = false;
			first.IsReadOnly = true;
			Verify.IsFalse(first.IsEditing, "Precondition: the edit is closed while another table owns the source.");
		});
		await SettleLayout(second);

		RunOnUIThread.Execute(() => host.Children.Insert(0, first));
		await SettleLayout(first);

		RunOnUIThread.Execute(() =>
		{
			Verify.AreEqual(SortDirection.Descending, first.Columns[0].SortDirection, "The deferred clear must wait while the reclaim is refused.");
			if (unload)
			{
				host.Children.Remove(first);
			}
		});
		await SettleLayout(second);

		// A sort request flushes older deferred work; with no adopted source it must be refused.
		Action requestRefusedSort = () =>
		{
			first.Columns[1].CanSort = mode == "SortWhileUnloaded";
			if (!countingSorted)
			{
				countingSorted = true;
				first.Sorted += (s, e) => sortedCount++;
			}
			Verify.IsFalse(first.SortByColumn(first.Columns[1], SortDirection.Ascending), "A sort request must be refused while the source is not adopted.");
			Verify.AreEqual(SortDirection.None, first.Columns[1].SortDirection, "The refused request must not light its column.");
			Verify.AreEqual(SortDirection.Descending, first.Columns[0].SortDirection, "The deferred clear must wait while the source is not adopted.");
			Verify.AreEqual(0, sortedCount, "Neither the request nor the deferred clear may raise Sorted yet.");
		};

		RunOnUIThread.Execute(() =>
		{
			if (unload)
			{
				requestRefusedSort();
			}
			host.Children.Remove(second);
		});
		await SettleLayout(first);

		if (unload)
		{
			RunOnUIThread.Execute(() =>
			{
				requestRefusedSort();
				host.Children.Insert(0, first);
			});
			await SettleLayout(first);
		}

		RunOnUIThread.Execute(() =>
		{
			Verify.AreEqual(SortDirection.None, first.Columns[0].SortDirection, "The deferred clear applies once the reclaim succeeds.");
			var names = GetProjectedItems(first).OfType<EditablePerson>().Select(person => person.Name).ToList();
			VerifySequence(new[] { "Asha", "Diego", "Mei" }, names, "the cleared sort must leave the source unsorted");
			Verify.IsFalse(first.ClearSort(), "Nothing is left sorted to clear.");
		});
	}
}

// Category 10.5 of the TableView test plan: validation, teardown and reentrancy.
//
// Spec basis: TableView.idl:493-530 - "False when it did not close: a veto or failed validation
// leaves the edit open and IsEditing true", and IsEditing covers the opening and closing windows
// "because the control uses this to reject re-entrant edit operations from inside consumer
// callbacks".
//
// A validation failure is the only API-reachable way to obtain a persistently OPEN edit, since every
// other route into one is a gesture. The teardown tests below use it for exactly that reason.
//
// Spec gap: the IDL does not name INotifyDataErrorInfo, nor state that errors are scoped to the
// edited property rather than the whole object. Both are asserted here from the product's stated
// "failed validation" behaviour plus the recovery requirement, and both deserve IDL text.
[TestClass]
public class TableViewEditValidationTests : TableViewApiTestBase
{
	[TestMethod]
	[TestProperty("Description", "Verifies a validation error blocks the commit, holds the edit open, and rolls the item back to its pre-edit value.")]
	public async Task VerifyValidationErrorBlocksCommitAndLeavesItemUnchanged()
	{
		TableView tableView = null;
		List<EditablePerson> items = null;

		RunOnUIThread.Execute(() =>
		{
			items = MakeEditableItems();
			foreach (var item in items)
			{
				item.RejectPrefix = InvalidPrefix;
			}

			tableView = CreateEditableTable(items);
			tableView.IsReadOnly = false;
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var error = CaptureSetValue(GetValueProvider(tableView, items[0], columnIndex: 0), InvalidPrefix + "Nope");
			Verify.IsTrue(error != null, "A value the item's own validator rejects must not be reported as applied.");
			Log.Comment($"SetValue threw as expected: {error.Message}");

			Verify.IsTrue(tableView.IsEditing, "Failed validation must leave the edit open so the user can correct the value.");

			// The rollback matters as much as the block: without it the item holds data its own
			// validator rejects while the UI shows something else.
			Verify.AreEqual("Asha", items[0].Name, "A rejected value must be rolled back off the item.");
		});

		// Leave no open edit behind for the next test in the class.
		RunOnUIThread.Execute(() => tableView.CancelEdit());
		await TestServices.WindowHelper.WaitForIdle();
	}

	[TestMethod]
	[TestProperty("Description", "Verifies a forced teardown rolls back a pending value that fails validation instead of leaving it on the item.")]
	public async Task VerifyForcedTeardownRollsBackInvalidPendingValue()
	{
		TableView tableView = null;
		List<EditablePerson> items = null;

		RunOnUIThread.Execute(() =>
		{
			items = MakeEditableItems();
			items[0].RejectPrefix = InvalidPrefix;

			tableView = CreateEditableTable(items);
			tableView.IsReadOnly = false;
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			// A rejected value holds the edit open, so the editor stays in the tree.
			CaptureSetValue(GetValueProvider(tableView, items[0], columnIndex: 0), InvalidPrefix + "Nope");
			Verify.IsTrue(tableView.IsEditing, "Precondition: an edit is open.");
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var editor = GetEditorTextBox(tableView, items[0]);
			editor.Text = InvalidPrefix + "NopeX";

			tableView.ItemsSource = new List<EditablePerson>();
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			Verify.AreEqual("Asha", items[0].Name, "A forced teardown must not leave a rejected value on the item.");
			Verify.IsFalse(tableView.IsEditing, "A forced teardown must close the edit.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies a forced teardown that keeps the edited item in the source transfers an invalid pending value, rejects it and leaves the item unchanged.")]
	public async Task VerifyForcedTeardownWithItemPresentRollsBackInvalidPendingValue()
	{
		var items = MakeEditableItems();
		items[0].RejectPrefix = InvalidPrefix;
		var tableView = await LoadEditableTable(items);

		// The vetoed commit wrote nothing, so the only invalid value is the one typed below.
		var actions = await OpenEditHoldingValidValue(tableView, items[0], "Zed");

		RunOnUIThread.Execute(() =>
		{
			var editor = GetEditorTextBox(tableView, items[0]);
			editor.Text = InvalidPrefix + "Nope";

			// Forced and outside layout, with the item still in the source: the pending value is
			// transferred, then rejected by validation.
			tableView.IsReadOnly = true;
		});
		await TestServices.WindowHelper.WaitForIdle();

		VerifyForcedClose(tableView, actions, TableViewEditAction.Cancel, items[0], "Asha", "an invalid pending value must be rolled back off an item that stays in the source");
	}

	[TestMethod]
	[TestProperty("Description", "Verifies recycling the edited row during layout rolls back a pending value that fails validation.")]
	public async Task VerifyRecycleDuringLayoutRollsBackInvalidPendingValue()
	{
		TableView tableView = null;
		List<EditablePerson> items = null;
		Button focusSink = null;

		RunOnUIThread.Execute(() =>
		{
			var names = new string[200];
			names[0] = "Asha";
			for (int i = 1; i < names.Length; i++)
			{
				names[i] = "Person " + i;
			}
			items = MakeEditableItems(names);
			items[0].RejectPrefix = InvalidPrefix;

			tableView = CreateEditableTable(items);
			tableView.IsReadOnly = false;
			focusSink = LoadWithFocusSink(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			CaptureSetValue(GetValueProvider(tableView, items[0], columnIndex: 0), InvalidPrefix + "Nope");
			Verify.IsTrue(tableView.IsEditing, "Precondition: an edit is open.");
		});

		await TestServices.WindowHelper.WaitForIdle();

		// ItemsRepeater never recycles the row holding focus, so move focus off the editor. The
		// focus-loss commit of an invalid value is rejected and leaves the edit open.
		RunOnUIThread.Execute(() =>
		{
			var editor = GetEditorTextBox(tableView, items[0]);
			editor.Text = InvalidPrefix + "NopeX";
			focusSink.Focus(FocusState.Programmatic);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			Verify.AreNotEqual(FocusState.Unfocused, focusSink.FocusState, "Precondition: focus left the editor.");
			Verify.IsTrue(tableView.IsEditing, "Precondition: the rejected focus-loss commit keeps the edit open.");

			var editor = GetEditorTextBox(tableView, items[0]);
			editor.Text = InvalidPrefix + "NopeY";
		});

		// Row 0 leaves the realization window, so the repeater recycles it inside measure.
		await ScrollBodyTo(tableView, ScrollAxis.Vertical, 3000);
		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			Verify.AreEqual("Asha", items[0].Name, "Recycling the edited row must not leave a rejected value on the item.");
			Verify.IsFalse(tableView.IsEditing, "Recycling the edited row must close the edit.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies focus moving into a flyout the editor owns keeps the edit open, and the edit commits once focus leaves for good.")]
	public async Task VerifyFocusInEditorOwnedFlyoutKeepsEditOpen()
	{
		TableView tableView = null;
		List<EditablePerson> items = null;
		Button focusSink = null;
		Flyout flyout = null;
		Button flyoutButton = null;

		(tableView, items, focusSink) = await OpenEditHeldByValidator();

		RunOnUIThread.Execute(() =>
		{
			var editor = GetEditorTextBox(tableView, items[0]);
			editor.Text = "Bea";
			flyoutButton = new Button { Content = "Inside" };
			flyout = new Flyout { Content = flyoutButton };
			FlyoutBase.SetAttachedFlyout(editor, flyout);
			FlyoutBase.ShowAttachedFlyout(editor);
		});
		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() => flyoutButton.Focus(FocusState.Programmatic));
		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			Verify.IsTrue(flyout.IsOpen, "Precondition: the editor's flyout is open.");
			Verify.AreNotEqual(FocusState.Unfocused, flyoutButton.FocusState, "Precondition: focus is inside the flyout.");
			Verify.IsTrue(tableView.IsEditing, "Focus inside a flyout the editor owns must not commit the edit.");
			Verify.AreEqual("Asha", items[0].Name, "The pending value must not reach the item while the flyout is open.");
			flyout.Hide();
		});
		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() => focusSink.Focus(FocusState.Programmatic));
		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			Verify.IsFalse(tableView.IsEditing, "Focus leaving the editor after the flyout closes must commit the edit.");
			Verify.AreEqual("Bea", items[0].Name, "The committed value must reach the item.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies focus moving into a popup the editor did not open commits the edit.")]
	public async Task VerifyFocusInUnrelatedPopupCommitsEdit()
	{
		TableView tableView = null;
		List<EditablePerson> items = null;
		Button focusSink = null;
		Popup popup = null;
		Button popupButton = null;

		(tableView, items, focusSink) = await OpenEditHeldByValidator();

		RunOnUIThread.Execute(() =>
		{
			GetEditorTextBox(tableView, items[0]).Text = "Bea";
			popupButton = new Button { Content = "Elsewhere" };
			popup = new Popup { Child = popupButton, XamlRoot = tableView.XamlRoot };
			popup.IsOpen = true;
		});
		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() => popupButton.Focus(FocusState.Programmatic));
		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			Verify.AreNotEqual(FocusState.Unfocused, popupButton.FocusState, "Precondition: focus is inside the unrelated popup.");
			Verify.IsFalse(tableView.IsEditing, "Focus inside a popup the editor did not open must commit the edit.");
			Verify.AreEqual("Bea", items[0].Name, "The committed value must reach the item.");
			popup.IsOpen = false;
		});
		await TestServices.WindowHelper.WaitForIdle();
	}

	// Opens an edit on the first item that stays open because its value fails validation.
	private async Task<(TableView tableView, List<EditablePerson> items, Button focusSink)> OpenEditHeldByValidator()
	{
		TableView table = null;
		List<EditablePerson> source = null;
		Button sink = null;

		RunOnUIThread.Execute(() =>
		{
			source = MakeEditableItems();
			source[0].RejectPrefix = InvalidPrefix;

			table = CreateEditableTable(source);
			table.IsReadOnly = false;
			sink = LoadWithFocusSink(table);
		});
		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			CaptureSetValue(GetValueProvider(table, source[0], columnIndex: 0), InvalidPrefix + "Nope");
			Verify.IsTrue(table.IsEditing, "Precondition: an edit is open.");
			Verify.IsTrue(GetEditorTextBox(table, source[0]).Focus(FocusState.Programmatic), "Precondition: the editor takes focus.");
		});
		await TestServices.WindowHelper.WaitForIdle();

		return (table, source, sink);
	}

	private static TextBox GetEditorTextBox(TableView tableView, object item)
	{
		var editors = FindVisualChildrenByType<TextBox>(GetRowForItem(tableView, item));
		Verify.AreEqual(1, editors.Count, "The edited row must host exactly one editor TextBox.");
		return editors[0];
	}

	// Loads the table after a focusable button, which tests use to move focus off the editor.
	private Button LoadWithFocusSink(TableView tableView)
	{
		var sink = new Button { Content = "Sink" };
		var host = new StackPanel();
		host.Children.Add(sink);
		host.Children.Add(tableView);
		LoadContent(host);
		return sink;
	}

	[TestMethod]
	[TestProperty("Description", "Verifies a rollback posted from a layout pass does not overwrite a value the app wrote before it ran, when the column's value cannot be snapshotted.")]
	public async Task VerifyPostedRollbackDoesNotOverwriteNewerAppWrite()
	{
		TableView tableView = null;
		List<EditablePerson> items = null;
		Button focusSink = null;
		int appWrites = 0;

		RunOnUIThread.Execute(() =>
		{
			var names = new string[200];
			names[0] = "Asha";
			for (int i = 1; i < names.Length; i++)
			{
				names[i] = "Person " + i;
			}
			items = MakeEditableItems(names);
			items[0].RejectPrefix = InvalidPrefix;

			// An explicit binding Source leaves the control no editing path, so it cannot compare
			// the item's value at rollback time - the same blind spot as an enum or decimal path.
			tableView = CreateTableViewShell(items, 500, 300);
			tableView.Columns.Add(new TableViewTextColumn
			{
				Header = "Name",
				Binding = new Binding { Path = new PropertyPath("Name"), Source = items[0], Mode = BindingMode.OneWay },
			});
			tableView.IsReadOnly = false;

			// A forced close from layout delivers CellEditEnding on the dispatcher ahead of the
			// posted rollback, so this is an app write landing between the two.
			tableView.CellEditEnding += (s, e) =>
			{
				if (e.EditAction == TableViewEditAction.Cancel && appWrites == 0)
				{
					appWrites++;
					items[0].Name = "Rewritten";
				}
			};

			focusSink = LoadWithFocusSink(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			CaptureSetValue(GetValueProvider(tableView, items[0], columnIndex: 0), InvalidPrefix + "Nope");
			Verify.IsTrue(tableView.IsEditing, "Precondition: an edit is open.");
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var editor = GetEditorTextBox(tableView, items[0]);
			editor.Text = InvalidPrefix + "NopeX";
			focusSink.Focus(FocusState.Programmatic);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			Verify.IsTrue(tableView.IsEditing, "Precondition: the rejected focus-loss commit keeps the edit open.");
			var editor = GetEditorTextBox(tableView, items[0]);
			editor.Text = InvalidPrefix + "NopeY";
		});

		// Recycling row 0 inside measure transfers the invalid value and posts its rollback.
		await ScrollBodyTo(tableView, ScrollAxis.Vertical, 3000);
		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			Verify.IsFalse(tableView.IsEditing, "Recycling the edited row must close the edit.");
			Verify.AreEqual(1, appWrites, "Precondition: the app wrote while the rollback was pending.");
			Verify.AreEqual("Rewritten", items[0].Name, "A posted rollback must not overwrite a newer app write.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies the text column's editor TextBox takes its UIA name from the column header text.")]
	public async Task VerifyTextColumnEditorNameComesFromHeader()
	{
		TableView tableView = null;
		List<EditablePerson> items = null;

		RunOnUIThread.Execute(() =>
		{
			items = MakeEditableItems();
			items[0].RejectPrefix = InvalidPrefix;

			tableView = CreateEditableTable(items);
			tableView.IsReadOnly = false;
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			// A rejected value holds the edit open, so the editor stays in the tree.
			CaptureSetValue(GetValueProvider(tableView, items[0], columnIndex: 0), InvalidPrefix + "Nope");
			Verify.IsTrue(tableView.IsEditing, "Precondition: an edit is open.");
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var editor = GetEditorTextBox(tableView, items[0]);
			Verify.AreEqual("Name", AutomationProperties.GetName(editor), "The editor's UIA name must be the column header text.");
		});

		RunOnUIThread.Execute(() => tableView.CancelEdit());
		await TestServices.WindowHelper.WaitForIdle();
	}

	[TestMethod]
	[TestProperty("Description", "Verifies the text column's editor TextBox is named from a templated object header's rendered text.")]
	public async Task VerifyTextColumnEditorNameComesFromTemplatedObjectHeader()
	{
		TableView tableView = null;
		List<EditablePerson> items = null;

		RunOnUIThread.Execute(() =>
		{
			items = MakeEditableItems();
			items[0].RejectPrefix = InvalidPrefix;

			tableView = CreateTableViewShell(items, 500, 300);
			tableView.Columns.Add(new TableViewTextColumn
			{
				Header = new TitledColumnHeader { Title = "Full name" },
				HeaderTemplate = (DataTemplate)XamlReader.Load(
					@"<DataTemplate xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
						  <TextBlock Text=""{Binding Title}"" />
					  </DataTemplate>"),
				Binding = new Binding { Path = new PropertyPath("Name"), Mode = BindingMode.OneWay },
			});
			tableView.IsReadOnly = false;
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			CaptureSetValue(GetValueProvider(tableView, items[0], columnIndex: 0), InvalidPrefix + "Nope");
			Verify.IsTrue(tableView.IsEditing, "Precondition: an edit is open.");
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var editor = GetEditorTextBox(tableView, items[0]);
			Verify.AreEqual("Full name", AutomationProperties.GetName(editor),
				"An object header has no string form; the editor must take the name its rendered header exposes.");
			Verify.AreEqual("Full name", FrameworkElementAutomationPeer.CreatePeerForElement(editor).GetName(),
				"The editor's peer must report the same name.");
		});

		RunOnUIThread.Execute(() => tableView.CancelEdit());
		await TestServices.WindowHelper.WaitForIdle();
	}

	[TestMethod]
	[TestProperty("Description", "Verifies FontWeight set on the TableView reaches the text column's editor TextBox.")]
	public async Task VerifyTableViewFontWeightReachesTextEditor()
	{
		TableView tableView = null;
		List<EditablePerson> items = null;

		RunOnUIThread.Execute(() =>
		{
			items = MakeEditableItems();
			items[0].RejectPrefix = InvalidPrefix;

			tableView = CreateEditableTable(items);
			tableView.IsReadOnly = false;
			tableView.FontWeight = Microsoft.UI.Text.FontWeights.Bold;
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			CaptureSetValue(GetValueProvider(tableView, items[0], columnIndex: 0), InvalidPrefix + "Nope");
			Verify.IsTrue(tableView.IsEditing, "Precondition: an edit is open.");
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var editor = GetEditorTextBox(tableView, items[0]);
			Verify.AreEqual(Microsoft.UI.Text.FontWeights.Bold.Weight, editor.FontWeight.Weight,
				"The editor must not set a local FontWeight that blocks the TableView's value.");
		});

		RunOnUIThread.Execute(() => tableView.CancelEdit());
		await TestServices.WindowHelper.WaitForIdle();
	}

	[TestMethod]
	[TestProperty("Description", "Verifies a cell blocked by validation recovers: a subsequent acceptable value commits and closes the edit.")]
	public async Task VerifyClearingValidationErrorAllowsTheCommit()
	{
		TableView tableView = null;
		List<EditablePerson> items = null;
		var actions = new List<TableViewEditAction>();

		RunOnUIThread.Execute(() =>
		{
			items = MakeEditableItems();
			foreach (var item in items)
			{
				item.RejectPrefix = InvalidPrefix;
			}

			tableView = CreateEditableTable(items);
			tableView.IsReadOnly = false;
			tableView.CellEditEnding += (s, e) => actions.Add(e.EditAction);
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			CaptureSetValue(GetValueProvider(tableView, items[0], columnIndex: 0), InvalidPrefix + "Nope");
			Verify.IsTrue(tableView.IsEditing, "Precondition: the cell is blocked and the edit is open.");
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var error = CaptureSetValue(GetValueProvider(tableView, items[0], columnIndex: 0), "Asha Kapoor");
			Verify.IsTrue(error == null, $"A value the validator accepts must commit. Instead: {error?.Message}");
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			Verify.AreEqual("Asha Kapoor", items[0].Name, "The accepted value must reach the item.");
			Verify.IsFalse(tableView.IsEditing, "A validation failure must not be terminal - the cell recovers once the value is acceptable.");
			Verify.IsTrue(actions.Count > 0, "CellEditEnding must have been raised.");
			Verify.AreEqual(TableViewEditAction.Commit, actions[actions.Count - 1], "The final close must be reported as a commit.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies CancelEdit closes a validation-blocked edit, reporting the Cancel action and preserving the pre-edit value.")]
	public async Task VerifyCancelEditClosesAValidationBlockedEditWithCancelAction()
	{
		TableView tableView = null;
		List<EditablePerson> items = null;
		var actions = new List<TableViewEditAction>();

		RunOnUIThread.Execute(() =>
		{
			items = MakeEditableItems();
			foreach (var item in items)
			{
				item.RejectPrefix = InvalidPrefix;
			}

			tableView = CreateEditableTable(items);
			tableView.IsReadOnly = false;
			tableView.CellEditEnding += (s, e) => actions.Add(e.EditAction);
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			CaptureSetValue(GetValueProvider(tableView, items[0], columnIndex: 0), InvalidPrefix + "Nope");
			Verify.IsTrue(tableView.IsEditing, "Precondition: the edit is held open by the validator.");
			actions.Clear();

			Verify.IsTrue(tableView.CancelEdit(), "CancelEdit must close an open edit and report that it closed.");
			Verify.IsFalse(tableView.IsEditing, "The blocked edit must be gone.");

			Verify.AreEqual(1, actions.Count, "The cancel must raise CellEditEnding exactly once.");
			Verify.AreEqual(TableViewEditAction.Cancel, actions[0], "A cancel must be reported as TableViewEditAction.Cancel.");
			Verify.AreEqual("Asha", items[0].Name, "Discarding the edit must leave the pre-edit value on the item.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies replacing ItemsSource while an edit is open closes the edit and leaves the control usable.")]
	public async Task VerifyItemsSourceResetWhileEditingClosesEditSafely()
	{
		TableView tableView = null;
		List<EditablePerson> items = null;
		List<EditablePerson> replacement = null;

		RunOnUIThread.Execute(() =>
		{
			items = MakeEditableItems();
			foreach (var item in items)
			{
				item.RejectPrefix = InvalidPrefix;
			}

			tableView = CreateEditableTable(items);
			tableView.IsReadOnly = false;
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			CaptureSetValue(GetValueProvider(tableView, items[0], columnIndex: 0), InvalidPrefix + "Nope");
			Verify.IsTrue(tableView.IsEditing, "Precondition: an edit is open over the first row.");

			replacement = MakeEditableItems("Nadia", "Omar", "Priya");
			tableView.ItemsSource = replacement;
			Content.UpdateLayout();
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			Content.UpdateLayout();
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			// The row the editor sat over no longer exists. Leaving IsEditing true would wedge the
			// control: every later edit is rejected as reentrant, and a recycled row keeps a live
			// editor over unrelated data.
			Verify.IsFalse(tableView.IsEditing, "Replacing the source must close any edit that was open over the old items.");

			var rows = GetRealizedRows(tableView);
			Verify.IsTrue(rows.Count > 0, "The replacement source must render.");

			// The real proof the control is not wedged: it can still edit.
			var error = CaptureSetValue(GetValueProvider(tableView, replacement[0], columnIndex: 0), "Nadia Okoye");
			Verify.IsTrue(error == null, $"Editing must still work after a reset during an edit. Instead: {error?.Message}");
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			Verify.AreEqual("Nadia Okoye", replacement[0].Name, "The post-reset edit must commit normally.");
			Verify.AreEqual("Asha", items[0].Name, "The abandoned edit must not have written to the discarded item.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies removing the edited item from the source force-closes the edit as a Cancel and writes nothing to the removed item.")]
	public async Task VerifyForcedCloseCancelsWhenEditedItemIsRemoved()
	{
		var items = new ObservableCollection<EditablePerson>(MakeEditableItems());
		var tableView = await LoadEditableTable(items);
		var edited = items[0];
		var actions = await OpenEditHoldingValidValue(tableView, edited, "Zed");

		RunOnUIThread.Execute(() => items.Remove(edited));
		await TestServices.WindowHelper.WaitForIdle();

		VerifyForcedClose(tableView, actions, TableViewEditAction.Cancel, edited, "Asha", "a removed item must not receive the pending value");
	}

	[TestMethod]
	[TestProperty("Description", "Verifies filtering the edited item out of the projection force-closes the edit as a Cancel and writes nothing to it.")]
	public async Task VerifyForcedCloseCancelsWhenEditedItemIsFilteredOut()
	{
		var items = MakeEditableItems();
		var source = SourceFrom(items);
		var tableView = await LoadEditableTable(source);
		var edited = items[0];
		var actions = await OpenEditHoldingValidValue(tableView, edited, "Zed");

		RunOnUIThread.Execute(() => source.Filter(new TableViewPredicate(o => ((EditablePerson)o).Name != "Asha")));
		await TestServices.WindowHelper.WaitForIdle();

		VerifyForcedClose(tableView, actions, TableViewEditAction.Cancel, edited, "Asha", "a filtered-out item must not receive the pending value");
	}

	[TestMethod]
	[TestProperty("Description", "Verifies replacing ItemsSource force-closes a valid pending edit as a Cancel and writes nothing to the old item.")]
	public async Task VerifyForcedCloseCancelsWhenItemsSourceIsReplaced()
	{
		var items = MakeEditableItems();
		var tableView = await LoadEditableTable(items);
		var edited = items[0];
		var actions = await OpenEditHoldingValidValue(tableView, edited, "Zed");

		RunOnUIThread.Execute(() => tableView.ItemsSource = MakeEditableItems("Nadia", "Omar", "Priya"));
		await TestServices.WindowHelper.WaitForIdle();

		VerifyForcedClose(tableView, actions, TableViewEditAction.Cancel, edited, "Asha", "an item from the replaced source must not receive the pending value");
	}

	[TestMethod]
	[TestProperty("Description", "Verifies a forced close that keeps the edited item (an app sort on the source) still commits a valid pending value.")]
	public async Task VerifyForcedCloseCommitsValidValueWhenItemStillPresent()
	{
		var items = MakeEditableItems();
		var source = SourceFrom(items);
		var tableView = await LoadEditableTable(source);
		var edited = items[0];
		var actions = await OpenEditHoldingValidValue(tableView, edited, "Zed");

		RunOnUIThread.Execute(() => source.Sort("Name", SortDirection.Descending));
		await TestServices.WindowHelper.WaitForIdle();

		VerifyForcedClose(tableView, actions, TableViewEditAction.Commit, edited, "Zed", "an item still in the source must receive the valid pending value");
	}

	[TestMethod]
	[TestProperty("Description", "Verifies a forced close that leaves the edited item only under a collapsed parent (an app ParentBy) still commits a valid pending value.")]
	public async Task VerifyForcedCloseCommitsWhenEditedItemIsOnlyCollapsed()
	{
		var items = MakeEditableItems();
		var source = SourceFrom(items);
		var tableView = await LoadEditableTable(source);
		var parent = items[0];
		var edited = items[1];
		var actions = await OpenEditHoldingValidValue(tableView, edited, "Zed");

		// Nodes start collapsed, so the edited child leaves the visible rows but stays in the tree.
		RunOnUIThread.Execute(() => source.ParentBy(
			new TableViewKeySelector(o => o),
			new TableViewKeySelector(o => ReferenceEquals(o, edited) ? parent : null)));
		await TestServices.WindowHelper.WaitForIdle();

		VerifyForcedClose(tableView, actions, TableViewEditAction.Commit, edited, "Zed", "an item only hidden under a collapsed parent must receive the valid pending value");
	}

	[TestMethod]
	[TestProperty("Description", "Verifies removing the edited item from a grouped source force-closes the edit as a Cancel and writes nothing to the removed item.")]
	public async Task VerifyForcedCloseCancelsWhenEditedItemIsRemovedWhileGrouped()
	{
		var items = new ObservableCollection<EditablePerson>(MakeEditableItems("Asha", "Diego", "Mei", "Ravi"));
		var source = GroupByRole(items);
		var tableView = await LoadEditableTable(source);
		var edited = items[0];
		var actions = await OpenEditHoldingValidValue(tableView, edited, "Zed");

		RunOnUIThread.Execute(() => items.Remove(edited));
		await TestServices.WindowHelper.WaitForIdle();

		VerifyForcedClose(tableView, actions, TableViewEditAction.Cancel, edited, "Asha", "a removed item must not receive the pending value");
	}

	[TestMethod]
	[TestProperty("Description", "Verifies filtering the edited item out of a grouped projection force-closes the edit as a Cancel and writes nothing to it.")]
	public async Task VerifyForcedCloseCancelsWhenEditedItemIsFilteredOutWhileGrouped()
	{
		var items = MakeEditableItems("Asha", "Diego", "Mei", "Ravi");
		var source = GroupByRole(items);
		var tableView = await LoadEditableTable(source);
		var edited = items[0];
		var actions = await OpenEditHoldingValidValue(tableView, edited, "Zed");

		RunOnUIThread.Execute(() => source.Filter(new TableViewPredicate(o => ((EditablePerson)o).Name != "Asha")));
		await TestServices.WindowHelper.WaitForIdle();

		VerifyForcedClose(tableView, actions, TableViewEditAction.Cancel, edited, "Asha", "a filtered-out item must not receive the pending value");
	}

	[TestMethod]
	[TestProperty("Description", "Verifies clearing the parent relation while editing an item that stays in the flat projection still commits a valid pending value.")]
	public async Task VerifyForcedCloseCommitsWhenClearParentByKeepsEditedItem()
	{
		var items = MakeEditableItems();
		var parent = items[0];
		var child = items[1];
		var source = SourceFrom(items);
		RunOnUIThread.Execute(() => source.ParentBy(
			new TableViewKeySelector(o => o),
			new TableViewKeySelector(o => ReferenceEquals(o, child) ? parent : null)));
		var tableView = await LoadEditableTable(source);
		var actions = await OpenEditHoldingValidValue(tableView, parent, "Zed");

		RunOnUIThread.Execute(() => source.ClearParentBy());
		await TestServices.WindowHelper.WaitForIdle();

		VerifyForcedClose(tableView, actions, TableViewEditAction.Commit, parent, "Zed", "an item still in the flat projection must receive the valid pending value");
	}

	[TestMethod]
	[TestProperty("Description", "Verifies filtering an edited child out of a ParentBy projection force-closes the edit as a Cancel and writes nothing to it.")]
	public async Task VerifyForcedCloseCancelsWhenEditedItemIsFilteredOutUnderParentBy()
	{
		var items = MakeEditableItems();
		var parent = items[0];
		var edited = items[1];
		var source = ParentEditedUnder(items, parent, edited, items[2]);
		var tableView = await LoadExpandedTable(source);
		var actions = await OpenEditHoldingValidValue(tableView, edited, "Zed");

		RunOnUIThread.Execute(() => source.Filter(new TableViewPredicate(o => ((EditablePerson)o).Name != "Diego")));
		await TestServices.WindowHelper.WaitForIdle();

		VerifyForcedClose(tableView, actions, TableViewEditAction.Cancel, edited, "Diego", "a filtered-out child must not receive the pending value");
	}

	[TestMethod]
	[TestProperty("Description", "Verifies removing an edited child from a ParentBy source force-closes the edit as a Cancel and writes nothing to it.")]
	public async Task VerifyForcedCloseCancelsWhenEditedItemIsRemovedUnderParentBy()
	{
		var items = new ObservableCollection<EditablePerson>(MakeEditableItems());
		var parent = items[0];
		var edited = items[1];
		var source = ParentEditedUnder(items, parent, edited, items[2]);
		var tableView = await LoadExpandedTable(source);
		var actions = await OpenEditHoldingValidValue(tableView, edited, "Zed");

		RunOnUIThread.Execute(() => items.Remove(edited));
		await TestServices.WindowHelper.WaitForIdle();

		VerifyForcedClose(tableView, actions, TableViewEditAction.Cancel, edited, "Diego", "a removed child must not receive the pending value");
	}

	// The sibling keeps the parent's child list non-empty once the edited child leaves it.
	private static TableViewSource ParentEditedUnder(IEnumerable<EditablePerson> items, EditablePerson parent, EditablePerson child, EditablePerson sibling)
	{
		var source = SourceFrom(items);
		RunOnUIThread.Execute(() => source.ParentBy(
			new TableViewKeySelector(o => o),
			new TableViewKeySelector(o => ReferenceEquals(o, child) || ReferenceEquals(o, sibling) ? parent : null)));
		return source;
	}

	// Nodes start collapsed, so expand them to give the child a realized row to edit.
	private async Task<TableView> LoadExpandedTable(TableViewSource source)
	{
		var tableView = await LoadEditableTable(source);
		RunOnUIThread.Execute(() => tableView.ExpandAllRows());
		await TestServices.WindowHelper.WaitForIdle();
		return tableView;
	}

	// TableViewSource must be created on the UI thread.
	private static TableViewSource SourceFrom(object items)
	{
		TableViewSource source = null;
		RunOnUIThread.Execute(() => source = TableViewSource.From(items));
		return source;
	}

	private static TableViewSource GroupByRole(IEnumerable<EditablePerson> items)
	{
		var source = SourceFrom(items);
		RunOnUIThread.Execute(() => source.GroupBy(o => ((EditablePerson)o).Role));
		return source;
	}

	private async Task<TableView> LoadEditableTable(object itemsSource)
	{
		TableView tableView = null;
		RunOnUIThread.Execute(() =>
		{
			tableView = CreateTableView(itemsSource, headers: new[] { "Name", "Role" }, width: 500, height: 300);
			tableView.IsReadOnly = false;
			LoadContent(tableView);
		});
		await TestServices.WindowHelper.WaitForIdle();
		return tableView;
	}

	// A vetoed commit keeps the editor open holding a valid value that has not reached the item, so
	// only the forced close decides whether it is written.
	private static async Task<List<TableViewEditAction>> OpenEditHoldingValidValue(TableView tableView, EditablePerson item, string pendingValue)
	{
		var actions = new List<TableViewEditAction>();
		bool veto = true;
		RunOnUIThread.Execute(() =>
		{
			var originalName = item.Name;
			tableView.CellEditEnding += (s, e) =>
			{
				actions.Add(e.EditAction);
				e.Cancel = veto;
			};

			CaptureSetValue(GetValueProvider(tableView, item, columnIndex: 0), pendingValue);
			veto = false;

			Verify.IsTrue(tableView.IsEditing, "Precondition: the vetoed commit keeps the edit open.");
			Verify.AreEqual(originalName, item.Name, "Precondition: the vetoed commit wrote nothing.");
			actions.Clear();
		});
		await TestServices.WindowHelper.WaitForIdle();
		return actions;
	}

	private static void VerifyForcedClose(TableView tableView, List<TableViewEditAction> actions, TableViewEditAction expected, EditablePerson item, string expectedName, string reason)
	{
		RunOnUIThread.Execute(() =>
		{
			Verify.IsFalse(tableView.IsEditing, "The forced close must end the edit.");
			Verify.AreEqual(1, actions.Count, "The forced close must raise CellEditEnding exactly once.");
			Verify.AreEqual(expected, actions[0], "The forced close must report " + expected + ".");
			Verify.AreEqual(expectedName, item.Name, "After a forced close " + reason + ".");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies unloading the TableView while an edit is open tears down cleanly and the control edits again after reload.")]
	public async Task VerifyUnloadWhileEditingClosesEditSafely()
	{
		TableView tableView = null;
		List<EditablePerson> items = null;

		RunOnUIThread.Execute(() =>
		{
			items = MakeEditableItems();
			foreach (var item in items)
			{
				item.RejectPrefix = InvalidPrefix;
			}

			tableView = CreateEditableTable(items);
			tableView.IsReadOnly = false;
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			CaptureSetValue(GetValueProvider(tableView, items[0], columnIndex: 0), InvalidPrefix + "Nope");
			Verify.IsTrue(tableView.IsEditing, "Precondition: an edit is open.");

			// Navigating away mid-edit is ordinary user behaviour in a paged app.
			Content = null;
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			Content.UpdateLayout();
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			Verify.IsFalse(tableView.IsEditing, "A reloaded TableView must not still report an edit from before the unload.");

			var error = CaptureSetValue(GetValueProvider(tableView, items[1], columnIndex: 0), "Diego Munoz");
			Verify.IsTrue(error == null, $"Editing must work again after an unload during an edit. Instead: {error?.Message}");
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			Verify.AreEqual("Diego Munoz", items[1].Name, "The post-reload edit must commit normally.");
			Verify.AreEqual("Asha", items[0].Name, "The edit abandoned at unload must not have written anything.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies CommitEdit and CancelEdit called from inside BeginningEdit and CellEditEnding are rejected without corrupting edit state.")]
	public async Task VerifyReentrantEditCallsAreRejectedAndLeaveStateCoherent()
	{
		TableView tableView = null;
		List<EditablePerson> items = null;

		bool? commitFromBeginning = null;
		bool? cancelFromBeginning = null;
		bool? commitFromEnding = null;
		bool? cancelFromEnding = null;

		RunOnUIThread.Execute(() =>
		{
			items = MakeEditableItems();
			tableView = CreateEditableTable(items);
			tableView.IsReadOnly = false;

			tableView.BeginningEdit += (s, e) =>
			{
				if (commitFromBeginning.HasValue)
				{
					return;
				}

				// The IDL says IsEditing is true here so the control can reject reentrant calls.
				commitFromBeginning = tableView.CommitEdit();
				cancelFromBeginning = tableView.CancelEdit();
			};

			tableView.CellEditEnding += (s, e) =>
			{
				if (commitFromEnding.HasValue)
				{
					return;
				}

				commitFromEnding = tableView.CommitEdit();
				cancelFromEnding = tableView.CancelEdit();
			};

			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			var error = CaptureSetValue(GetValueProvider(tableView, items[0], columnIndex: 0), "Asha Kapoor");
			Verify.IsTrue(error == null, $"Reentrant calls from handlers must not derail the outer edit. Instead: {error?.Message}");
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			Verify.IsTrue(commitFromBeginning.HasValue, "BeginningEdit must have run.");
			Verify.IsFalse(commitFromBeginning.Value, "CommitEdit from inside BeginningEdit must be rejected - the edit is still opening.");
			Verify.IsFalse(cancelFromBeginning.Value, "CancelEdit from inside BeginningEdit must be rejected.");

			Verify.IsTrue(commitFromEnding.HasValue, "CellEditEnding must have run.");
			Verify.IsFalse(commitFromEnding.Value, "CommitEdit from inside CellEditEnding must be rejected - the close is already underway.");
			Verify.IsFalse(cancelFromEnding.Value, "CancelEdit from inside CellEditEnding must be rejected.");

			Verify.AreEqual("Asha Kapoor", items[0].Name, "The outer edit must still complete on its own terms.");
			Verify.IsFalse(tableView.IsEditing, "The state machine must be back at rest.");
		});

		// An independent edit afterwards is the real coherence check: a corrupted state machine
		// would reject it as reentrant.
		RunOnUIThread.Execute(() =>
		{
			var error = CaptureSetValue(GetValueProvider(tableView, items[1], columnIndex: 1), "Staff Engineer");
			Verify.IsTrue(error == null, $"A later, independent edit must work. Instead: {error?.Message}");
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			Verify.AreEqual("Staff Engineer", items[1].Role, "The later edit must commit.");
			Verify.IsFalse(tableView.IsEditing, "And must close.");
		});
	}

	[TestMethod]
	[TestProperty("Description", "Verifies a BeginEdit whose editor creation throws rolls back: no edit is left open, the display cell and focus are restored, and a later edit works.")]
	[Ignore("Uno's binding engine logs a converter's exception instead of rethrowing it, so BeginEdit cannot fail this way.")]
	public async Task VerifyBeginEditThatThrowsRollsBackAndAllowsALaterEdit()
	{
		TableView tableView = null;
		List<EditablePerson> items = null;
		var converter = new ArmableThrowingConverter();

		RunOnUIThread.Execute(() =>
		{
			items = MakeEditableItems();
			tableView = CreateTableViewShell(items, 500, 300);
			tableView.IsReadOnly = false;

			// An explicit Source makes the editor's binding evaluate inside the editor factory, so
			// an armed converter throws out of BeginEdit itself rather than into a swallowed refresh.
			tableView.Columns.Add(new TableViewTextColumn
			{
				Header = "Name",
				Binding = new Binding { Path = new PropertyPath("Name"), Source = items[0], Converter = converter, Mode = BindingMode.OneWay },
			});
			tableView.Columns.Add(MakeTextColumn("Role"));
			LoadContent(tableView);
		});

		await TestServices.WindowHelper.WaitForIdle();

		Grid cell = null;
		UIElement displayContent = null;
		RunOnUIThread.Execute(() =>
		{
			cell = GetRowCell(GetRowForItem(tableView, items[0]), 0);
			displayContent = GetCellContent(cell);
			GetCellPeer(tableView, items[0], columnIndex: 0).SetFocus();
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			Verify.IsTrue(ReferenceEquals(cell, FocusManager.GetFocusedElement(tableView.XamlRoot)),
				"Precondition: the cell has focus before the edit is attempted.");

			converter.Armed = true;
			var error = CaptureSetValue(GetValueProvider(tableView, items[0], columnIndex: 0), "Asha Kapoor");
			converter.Armed = false;

			Verify.IsTrue(error != null, "The throwing editor factory must surface as a failed edit.");
			Verify.IsFalse(tableView.IsEditing, "A BeginEdit that threw must not leave an edit open.");
			Verify.IsTrue(ReferenceEquals(displayContent, GetCellContent(cell)), "The cell must show its display content again.");
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			Verify.IsTrue(ReferenceEquals(cell, FocusManager.GetFocusedElement(tableView.XamlRoot)),
				"Focus must be back on the cell after the rollback.");

			var error = CaptureSetValue(GetValueProvider(tableView, items[0], columnIndex: 0), "Asha Kapoor");
			Verify.IsTrue(error == null, $"A later edit on the same cell must start and commit. Instead: {error?.Message}");
		});

		await TestServices.WindowHelper.WaitForIdle();

		RunOnUIThread.Execute(() =>
		{
			Verify.AreEqual("Asha Kapoor", items[0].Name, "The later edit must commit.");
			Verify.IsFalse(tableView.IsEditing, "And must close.");
		});
	}
}

// Throws from Convert only while armed, so the display cells build normally.
internal sealed class ArmableThrowingConverter : IValueConverter
{
	public bool Armed { get; set; }

	public object Convert(object value, Type targetType, object parameter, string language)
	{
		if (Armed)
		{
			throw new InvalidOperationException("Deliberate editor-binding failure.");
		}
		return value;
	}

	public object ConvertBack(object value, Type targetType, object parameter, string language) => value;
}

internal static class TableViewEditingTestHelpers
{
	// Any value starting with this is rejected by EditablePerson's validator once RejectPrefix is set.
	internal const string InvalidPrefix = "!";

	internal static List<EditablePerson> MakeEditableItems(params string[] names)
	{
		if (names == null || names.Length == 0)
		{
			names = new[] { "Asha", "Diego", "Mei" };
		}

		var roles = new[] { "Designer", "Engineer", "Architect" };

		var result = new List<EditablePerson>();
		for (int i = 0; i < names.Length; i++)
		{
			result.Add(new EditablePerson { Name = names[i], Role = roles[i % roles.Length] });
		}

		return result;
	}

	// Two writable text columns. The TableView is left read-only so each test states its own gate.
	internal static TableView CreateEditableTable(IList<EditablePerson> items)
		=> CreateTableView(items, headers: new[] { "Name", "Role" }, width: 500, height: 300);

	// A non-TextBox editor, so the column can no longer honour a text-valued SetValue.
	internal static DataTemplate MakeCustomEditorTemplate()
	{
		return (DataTemplate)XamlReader.Load(
			@"<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'>
                      <ToggleSwitch />
                  </DataTemplate>");
	}

	// Fails the test if the pattern is not advertised - a test that needs to edit has no fallback.
	internal static IValueProvider GetValueProvider(TableView tableView, object item, int columnIndex)
	{
		var peer = GetCellPeer(tableView, item, columnIndex);
		var provider = peer.GetPattern(PatternInterface.Value) as IValueProvider;
		Verify.IsTrue(provider != null, "The cell must advertise the Value pattern for this test to drive an edit.");
		return provider;
	}

	internal static Exception CaptureSetValue(IValueProvider provider, string value)
	{
		try
		{
			provider.SetValue(value);
			return null;
		}
		catch (Exception e)
		{
			return e;
		}
	}

	internal static List<string> GetCellTexts(TableView tableView, object item)
	{
		var row = GetRowForItem(tableView, item);
		return FindVisualChildrenByType<TextBlock>(row).Select(t => t.Text).ToList();
	}
}

// A data item that can be edited and can reject values, so the validation path is reachable without
// any product-internal hook. Implements the XAML INotifyPropertyChanged so a committed value reaches
// the OneWay display binding, and the XAML INotifyDataErrorInfo so the control's validation gate has
// something to consult.
internal sealed class EditablePerson : INotifyPropertyChanged, INotifyDataErrorInfo
{
	private string _name;
	private string _role;

	public event PropertyChangedEventHandler PropertyChanged;

	public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;

	// When set, any Name beginning with this prefix is reported as invalid.
	public string RejectPrefix { get; set; }

	public string Name
	{
		get => _name;
		set
		{
			if (_name != value)
			{
				_name = value;
				Raise(nameof(Name));
			}
		}
	}

	public string Role
	{
		get => _role;
		set
		{
			if (_role != value)
			{
				_role = value;
				Raise(nameof(Role));
			}
		}
	}

	public bool HasErrors => IsNameInvalid;

	public IEnumerable GetErrors(string propertyName)
	{
		if (string.IsNullOrEmpty(propertyName) || propertyName == nameof(Name))
		{
			if (IsNameInvalid)
			{
				return new object[] { $"'{_name}' is not an acceptable {nameof(Name)}." };
			}
		}

		return null;
	}

	private bool IsNameInvalid =>
		!string.IsNullOrEmpty(RejectPrefix) &&
		_name != null &&
		_name.StartsWith(RejectPrefix, StringComparison.Ordinal);

	private void Raise(string propertyName)
	{
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
	}
}
