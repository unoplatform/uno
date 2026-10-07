// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\TableView\APITests\TableView_Editing_APITests.cs, tag winui3/main, commit dc28206ea35

#pragma warning disable CS8305 // The Tabular types are [Experimental], as in WinUI

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Tabular;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Markup;
using MUXControlsTestApp.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
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
