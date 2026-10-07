#nullable enable

#pragma warning disable CS8305 // The Tabular types are [Experimental], as in WinUI

#if !WINAPPSDK

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Tabular;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Uno.UI.DevTools.Input;
using Windows.Foundation;
using Windows.System;
using Windows.UI.Input.Preview.Injection;
using Windows.UI.ViewManagement;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Microsoft_UI_Xaml_Controls;

public partial class Given_TableView
{
	[TestMethod]
	public async Task When_IsReadOnly_Blocks_Edit()
	{
		var items = People(5);
		var table = CreateTable(items);
		await LoadAsync(table);

		// Read-only is the default: F2 does nothing.
		await FocusRowAsync(table, 1);
		await PressAsync("f2");

		Assert.IsFalse(table.IsEditing);
		Assert.IsNull(FindEditor(table));

		// Opt in.
		table.IsReadOnly = false;
		var original = items[1].Name;

		await FocusRowAsync(table, 1);
		await PressAsync("f2");

		Assert.IsTrue(table.IsEditing, "F2 opens an editor on the focused row");
		var editor = FindEditor(table)!;
		Assert.IsNotNull(editor);
		Assert.AreEqual(original, editor.Text);
		Assert.AreEqual(editor.Text.Length, editor.SelectionLength, "the built-in editor selects all text");

		// Enter commits to the source.
		editor.Text = "Renamed";
		await KeyboardHelper.PressKeySequence("$d$_enter#$u$_enter", editor);
		await WindowHelper.WaitForIdle();

		Assert.IsFalse(table.IsEditing);
		Assert.AreEqual("Renamed", items[1].Name);
		Assert.IsNull(FindEditor(table));
		Assert.AreEqual("Renamed", GetCellText(GetRow(table, 1)!, 0), "the display cell shows the committed value");

		// Escape reverts.
		await FocusRowAsync(table, 1);
		await PressAsync("f2");
		editor = FindEditor(table)!;
		editor.Text = "Discarded";
		await KeyboardHelper.PressKeySequence("$d$_esc#$u$_esc", editor);
		await WindowHelper.WaitForIdle();

		Assert.IsFalse(table.IsEditing);
		Assert.AreEqual("Renamed", items[1].Name);

		// A read-only column refuses even when the table is editable.
		table.Columns[0].IsReadOnly = true;
		table.Columns[1].IsReadOnly = true;
		table.Columns[2].IsReadOnly = true;
		await FocusRowAsync(table, 1);
		await PressAsync("f2");
		Assert.IsFalse(table.IsEditing);
	}

	[TestMethod]
	public async Task When_BeginningEdit_Cancel()
	{
		var items = People(5);
		var table = CreateTable(items);
		table.IsReadOnly = false;
		await LoadAsync(table);

		var raised = new List<TableViewBeginningEditEventArgs>();
		table.BeginningEdit += (_, e) =>
		{
			raised.Add(e);
			e.Cancel = true;
		};

		await FocusRowAsync(table, 2);
		await PressAsync("f2");

		Assert.AreEqual(1, raised.Count);
		Assert.AreSame(items[2], raised[0].Item);
		Assert.AreSame(table.Columns[0], raised[0].Column, "the keyboard path falls back to the first editable column");
		Assert.IsFalse(table.IsEditing);
		Assert.IsNull(FindEditor(table));
	}

	[TestMethod]
	public async Task When_CellEditEnding_Cancel_Vetoes()
	{
		var items = People(5);
		var original = items[0].Name;
		var table = CreateTable(items);
		table.IsReadOnly = false;
		await LoadAsync(table);

		var veto = true;
		var raised = new List<TableViewCellEditEndingEventArgs>();
		table.CellEditEnding += (_, e) =>
		{
			raised.Add(e);
			e.Cancel = veto;
		};

		await FocusRowAsync(table, 0);
		await PressAsync("f2");
		var editor = FindEditor(table)!;
		editor.Text = "Vetoed";

		Assert.IsFalse(table.CommitEdit());

		Assert.AreEqual(1, raised.Count);
		Assert.AreEqual(TableViewEditAction.Commit, raised[0].EditAction);
		Assert.AreSame(items[0], raised[0].Item);
		Assert.IsTrue(table.IsEditing, "a vetoed commit keeps the edit open");
		Assert.AreEqual(original, items[0].Name, "the source is untouched");

		// The cancel is vetoable as well.
		Assert.IsFalse(table.CancelEdit());
		Assert.AreEqual(TableViewEditAction.Cancel, raised[1].EditAction);
		Assert.IsTrue(table.IsEditing);

		veto = false;
		Assert.IsTrue(table.CancelEdit());
		Assert.IsFalse(table.IsEditing);
		Assert.AreEqual(original, items[0].Name);

		// Nothing open: both are no-ops.
		Assert.IsFalse(table.CommitEdit());
		Assert.IsFalse(table.CancelEdit());
	}

	[TestMethod]
	public async Task When_CommitEdit_Validation_Error()
	{
		var item = new ValidatingPerson("Ada");
		var table = new TableView { IsReadOnly = false };
		table.Columns.Add(TextColumn(nameof(ValidatingPerson.Name)));
		table.ItemsSource = new List<ValidatingPerson> { item };
		await LoadAsync(table);

		await FocusRowAsync(table, 0);
		await PressAsync("f2");
		var editor = FindEditor(table)!;

		// The validator rejects an empty name.
		editor.Text = "";

		Assert.IsFalse(table.CommitEdit(), "a blocking validation error keeps the edit open");
		Assert.IsTrue(table.IsEditing);
		Assert.AreEqual("Ada", item.Name, "the rejected write is undone");
		Assert.IsFalse(item.HasErrors);

		editor = FindEditor(table)!;
		editor.Text = "Grace";
		Assert.IsTrue(table.CommitEdit());
		Assert.IsFalse(table.IsEditing);
		Assert.AreEqual("Grace", item.Name);
	}

	[TestMethod]
	public async Task When_Focus_Leaves_Commits()
	{
		var items = People(5);
		var table = CreateTable(items);
		table.IsReadOnly = false;
		var outside = new Button { Content = "Outside" };
		await LoadAsync(table, header: outside);

		await FocusRowAsync(table, 3);
		await PressAsync("f2");
		var editor = FindEditor(table)!;
		Assert.IsTrue(editor.FocusState != FocusState.Unfocused, "the editor takes focus");

		editor.Text = "Committed";
		outside.Focus(FocusState.Programmatic);

		// The commit is posted, not synchronous: it is re-evaluated once focus has settled.
		await WindowHelper.WaitFor(() => !table.IsEditing, message: "focus leaving the editor did not commit");
		Assert.AreEqual("Committed", items[3].Name);
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.Skia)]
	public async Task When_Double_Click_Begins_Edit()
	{
		var items = People(5);
		var table = CreateTable(items);
		var city = table.Columns[2];
		table.IsReadOnly = false;
		await LoadAsync(table);

		var beginning = new List<TableViewBeginningEditEventArgs>();
		table.BeginningEdit += (_, e) => beginning.Add(e);

		var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
		using var mouse = injector.GetMouse();

		// OnPointerPressedForEditing: two left presses on one cell within UISettings.DoubleClickTime
		// and the double-click slop. The injected clock advances 1 ms per press/release.
		var point = Center(GetCell(GetRow(table, 2)!, city));
		mouse.Press(point);
		mouse.Release();
		mouse.Press(point);
		mouse.Release();
		await WindowHelper.WaitForIdle();

		Assert.IsTrue(table.IsEditing);
		Assert.AreEqual(1, beginning.Count);
		Assert.AreSame(items[2], beginning[0].Item);
		Assert.AreSame(city, beginning[0].Column, "the column comes from the pressed cell");
		Assert.IsTrue(Descendants(GetCell(GetRow(table, 2)!, city)).OfType<TextBox>().Any(), "the editor is hosted in the City cell");

		Assert.IsTrue(table.CancelEdit());
		await WindowHelper.WaitForIdle();
		beginning.Clear();

		// Too slow: the second press lands after the double-click time.
		point = Center(GetCell(GetRow(table, 0)!, city));
		mouse.Press(point);
		mouse.Release();
		mouse.MoveTo(new Point(point.X + 1, point.Y), 1, new UISettings().DoubleClickTime + 100);
		mouse.MoveTo(point, 1, 1);
		mouse.Press(point);
		mouse.Release();
		await WindowHelper.WaitForIdle();

		Assert.IsFalse(table.IsEditing, "presses further apart than DoubleClickTime");
		Assert.AreEqual(0, beginning.Count);

		// Too far: the second press is outside the slop, inside the same cell.
		point = Center(GetCell(GetRow(table, 1)!, city));
		mouse.Press(point);
		mouse.Release();
		mouse.Press(new Point(point.X + 10, point.Y));
		mouse.Release();
		await WindowHelper.WaitForIdle();

		Assert.IsFalse(table.IsEditing, "presses further apart than the double-click slop");
		Assert.AreEqual(0, beginning.Count);

		// Secondary button: a right double-click opens no editor.
		point = Center(GetCell(GetRow(table, 3)!, city));
		mouse.PressRight(point);
		mouse.ReleaseRight();
		mouse.PressRight(point);
		mouse.ReleaseRight();
		await WindowHelper.WaitForIdle();

		Assert.IsFalse(table.IsEditing, "a right double-click");
		Assert.AreEqual(0, beginning.Count);
	}

	[TestMethod]
	public async Task When_CommitEdit_Validation_Scoped_To_Edited_Property()
	{
		// HasBlockingValidationErrors scopes GetErrors to the edited property: "Object-level HasErrors
		// lets an unrelated, pre-existing error on a different property block this cell permanently".
		var item = new ValidatingPerson("Ada");
		item.SetError(nameof(ValidatingPerson.City), "City is unknown.");
		var table = new TableView { IsReadOnly = false };
		table.Columns.Add(TextColumn(nameof(ValidatingPerson.Name)));
		table.ItemsSource = new List<ValidatingPerson> { item };
		await LoadAsync(table);

		await FocusRowAsync(table, 0);
		await PressAsync("f2");
		FindEditor(table)!.Text = "Grace";

		Assert.IsTrue(table.CommitEdit(), "an error on another property does not block this cell");
		Assert.IsFalse(table.IsEditing);
		Assert.AreEqual("Grace", item.Name);
		Assert.IsTrue(item.HasErrors, "the unrelated error stands");
	}

	[TestMethod]
	public async Task When_CommitEdit_Validation_Dotted_Path_Uses_Leaf()
	{
		// GetErrors takes a property name, not a path: the leaf segment of Address.Street is asked.
		var item = new AddressedPerson("Main St");
		var table = new TableView { IsReadOnly = false };
		table.Columns.Add(TextColumn($"{nameof(AddressedPerson.Address)}.{nameof(Address.Street)}"));
		table.ItemsSource = new List<AddressedPerson> { item };
		await LoadAsync(table);

		await FocusRowAsync(table, 0);
		await PressAsync("f2");
		FindEditor(table)!.Text = "";

		Assert.IsFalse(table.CommitEdit(), "the error reported under the leaf name blocks the commit");
		Assert.IsTrue(table.IsEditing);
		Assert.AreEqual("Main St", item.Address.Street, "the rejected write is undone");

		FindEditor(table)!.Text = "Side St";
		Assert.IsTrue(table.CommitEdit());
		Assert.AreEqual("Side St", item.Address.Street);
	}

	[TestMethod]
	public async Task When_CommitEdit_Validation_Without_Editing_Path_Uses_HasErrors()
	{
		// A binding with an explicit Source has no editing path (GetEditingPropertyPath), so the control
		// falls back to object-level HasErrors and the unrelated City error blocks the commit.
		var item = new ValidatingPerson("Ada");
		item.SetError(nameof(ValidatingPerson.City), "City is unknown.");
		var table = new TableView { IsReadOnly = false };
		table.Columns.Add(new TableViewTextColumn
		{
			Header = "Name",
			Binding = new Binding { Source = item, Path = new PropertyPath(nameof(ValidatingPerson.Name)) },
		});
		table.ItemsSource = new List<ValidatingPerson> { item };
		await LoadAsync(table);

		await FocusRowAsync(table, 0);
		await PressAsync("f2");
		FindEditor(table)!.Text = "Grace";

		Assert.IsFalse(table.CommitEdit());
		Assert.IsTrue(table.IsEditing);

		item.SetError(nameof(ValidatingPerson.City), null);
		FindEditor(table)!.Text = "Grace";
		Assert.IsTrue(table.CommitEdit());
		Assert.AreEqual("Grace", item.Name);
	}

	[TestMethod]
	[DataRow(ForcedClose.TableIsReadOnly)]
	[DataRow(ForcedClose.ColumnIsReadOnly)]
	[DataRow(ForcedClose.ItemsSourceReplaced)]
	public async Task When_IsReadOnly_Set_While_Editing(ForcedClose trigger)
	{
		// TerminateEditWithoutVisualRestore -> TerminateEditForReset(force: true): the pending value is
		// written first, the close cannot be vetoed (honorCancel == false), and Commit is reported.
		var items = People(5);
		var table = CreateTable(items);
		table.IsReadOnly = false;
		await LoadAsync(table);

		var raised = new List<TableViewCellEditEndingEventArgs>();
		table.CellEditEnding += (_, e) =>
		{
			raised.Add(e);
			e.Cancel = true;
		};

		await FocusRowAsync(table, 1);
		await PressAsync("f2");
		FindEditor(table)!.Text = "Forced";

		switch (trigger)
		{
			case ForcedClose.TableIsReadOnly:
				table.IsReadOnly = true;
				break;
			case ForcedClose.ColumnIsReadOnly:
				table.Columns[0].IsReadOnly = true;
				break;
			case ForcedClose.ItemsSourceReplaced:
				table.ItemsSource = People(5);
				break;
		}

		Assert.IsFalse(table.IsEditing, "the veto is ignored on a forced close");
		Assert.IsNull(FindEditor(table));
		Assert.AreEqual("Forced", items[1].Name, "the valid pending value reaches the item");
		Assert.AreEqual(1, raised.Count);
		Assert.AreEqual(TableViewEditAction.Commit, raised[0].EditAction);
		Assert.AreSame(items[1], raised[0].Item);
	}

	[TestMethod]
	public async Task When_IsReadOnly_Set_On_Other_Column_While_Editing()
	{
		var items = People(5);
		var table = CreateTable(items);
		table.IsReadOnly = false;
		await LoadAsync(table);

		await FocusRowAsync(table, 1);
		await PressAsync("f2");
		Assert.IsTrue(table.IsEditing);

		// OnColumnIsReadOnlyChanged only closes the edit when it is on that column.
		table.Columns[1].IsReadOnly = true;

		Assert.IsTrue(table.IsEditing);
		Assert.IsNotNull(FindEditor(table));
		Assert.IsTrue(table.CancelEdit());
	}

	[TestMethod]
	public async Task When_IsReadOnly_Set_While_Editing_Invalid_Value()
	{
		var item = new ValidatingPerson("Ada");
		var table = new TableView { IsReadOnly = false };
		table.Columns.Add(TextColumn(nameof(ValidatingPerson.Name)));
		table.ItemsSource = new List<ValidatingPerson> { item };
		await LoadAsync(table);

		var raised = new List<TableViewCellEditEndingEventArgs>();
		table.CellEditEnding += (_, e) =>
		{
			raised.Add(e);
			e.Cancel = true;
		};

		await FocusRowAsync(table, 0);
		await PressAsync("f2");
		FindEditor(table)!.Text = "";

		// The forced close writes, sees the blocking error, and reports Cancel. TerminateEditForReset's write
		// does not set m_editSourceWritten, so FinishEditTeardown's cancel path skips the rollback and the
		// rejected value stays on the item, as in WinUI.
		table.IsReadOnly = true;

		Assert.IsFalse(table.IsEditing);
		Assert.AreEqual(1, raised.Count);
		Assert.AreEqual(TableViewEditAction.Cancel, raised[0].EditAction);
		Assert.AreEqual("", item.Name);
	}

	[TestMethod]
	public async Task When_Editor_Handles_Escape_And_Enter()
	{
		var items = People(5);
		var original = items[1].Name;
		var table = CreateTable(items);
		table.IsReadOnly = false;
		await LoadAsync(table);

		await FocusRowAsync(table, 1);
		await PressAsync("f2");
		var editor = FindEditor(table)!;
		editor.KeyDown += (_, e) =>
		{
			if (e.Key is VirtualKey.Escape or VirtualKey.Enter)
			{
				e.Handled = true;
			}
		};
		editor.Text = "Typed";

		// OnKeyDownForEditing: Escape defers to an editor that consumed it (a ComboBox closing its popup).
		await KeyboardHelper.PressKeySequence("$d$_esc#$u$_esc", editor);
		await WindowHelper.WaitForIdle();

		Assert.IsTrue(table.IsEditing, "a handled Escape does not cancel the edit");
		Assert.AreEqual(original, items[1].Name);

		// Enter commits even when the editor marked it handled.
		await KeyboardHelper.PressKeySequence("$d$_enter#$u$_enter", editor);
		await WindowHelper.WaitForIdle();

		Assert.IsFalse(table.IsEditing, "a handled Enter still commits");
		Assert.AreEqual("Typed", items[1].Name);
	}

	[TestMethod]
	public async Task When_Focused_Element_Handles_F2()
	{
		var table = CreateTable(People(5));
		table.IsReadOnly = false;
		await LoadAsync(table);

		var beginning = 0;
		table.BeginningEdit += (_, _) => beginning++;

		var row = GetRow(table, 1)!;
		row.KeyDown += (_, e) =>
		{
			if (e.Key == VirtualKey.F2)
			{
				e.Handled = true;
			}
		};

		await FocusRowAsync(table, 1);
		await PressAsync("f2");

		Assert.AreEqual(0, beginning, "F2 belongs to whatever focused control claimed it first");
		Assert.IsFalse(table.IsEditing);
	}

	[TestMethod]
	public async Task When_Focus_Settles_On_Row_Container_Editor_Refocused()
	{
		var items = People(5);
		var table = CreateTable(items);
		table.IsReadOnly = false;
		var actionColumn = new TableViewTemplateColumn
		{
			Header = "Action",
			CellTemplate = (DataTemplate)XamlReader.Load(
				"""
				<DataTemplate xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation">
					<Button Content="Act" />
				</DataTemplate>
				"""),
		};
		table.Columns.Add(actionColumn);
		await LoadAsync(table, width: 700);

		await FocusRowAsync(table, 1);
		await PressAsync("f2");
		var editor = FindEditor(table)!;
		editor.Text = "Typed";

		// CompleteFocusLossCommit: focus landing on the row container itself re-focuses the editor.
		var row = GetRow(table, 1)!;
		row.Focus(FocusState.Programmatic);
		await WindowHelper.WaitForIdle();

		Assert.IsTrue(table.IsEditing, "focus settling on the row container does not commit");
		Assert.IsTrue(IsWithin(FocusManager.GetFocusedElement(WindowHelper.XamlRoot) as DependencyObject, editor), "the editor is re-focused");

		// A control in another cell of the same row is a genuine focus target.
		var button = Descendants(GetCell(row, actionColumn)).OfType<Button>().Single();
		button.Focus(FocusState.Programmatic);
		await WindowHelper.WaitFor(() => !table.IsEditing, message: "focus moving to another cell's control did not commit");

		Assert.AreEqual("Typed", items[1].Name);
		Assert.AreSame(button, FocusManager.GetFocusedElement(WindowHelper.XamlRoot), "focus is not stolen back");

		static bool IsWithin(DependencyObject? candidate, DependencyObject ancestor)
		{
			while (candidate is not null)
			{
				if (candidate == ancestor)
				{
					return true;
				}

				candidate = VisualTreeHelper.GetParent(candidate);
			}

			return false;
		}
	}

	[TestMethod]
	[DataRow(false)]
	[DataRow(true)]
	public async Task When_BeginEdit_While_Editing_Commits_First(bool veto)
	{
		var items = People(5);
		var original = items[0].Name;
		var table = CreateTable(items);
		table.IsReadOnly = false;
		await LoadAsync(table);

		var log = new List<string>();
		table.BeginningEdit += (_, e) => log.Add($"Beginning {items.IndexOf((Person)e.Item!)}");
		table.CellEditEnding += (_, e) =>
		{
			log.Add($"Ending {e.EditAction} {items.IndexOf((Person)e.Item!)}");
			e.Cancel = veto;
		};

		await FocusRowAsync(table, 0);
		await PressAsync("f2");
		FindEditor(table)!.Text = "Typed";
		log.Clear();

		// The double-click / F2 entry point: moving to another item commits the open edit first, and a
		// commit that fails aborts the move.
		var moved = table.BeginEdit(items[2], table.Columns[0]);

		if (veto)
		{
			Assert.IsFalse(moved);
			CollectionAssert.AreEqual(new[] { "Ending Commit 0" }, log, "no BeginningEdit after a vetoed commit");
			Assert.IsTrue(table.IsEditing);
			Assert.AreEqual(original, items[0].Name);
			Assert.IsTrue(Descendants(GetCell(GetRow(table, 0)!, table.Columns[0])).OfType<TextBox>().Any(), "row 0 stays in edit");
		}
		else
		{
			Assert.IsTrue(moved);
			CollectionAssert.AreEqual(new[] { "Ending Commit 0", "Beginning 2" }, log);
			Assert.AreEqual("Typed", items[0].Name);
			Assert.IsTrue(table.IsEditing);
			Assert.IsTrue(Descendants(GetCell(GetRow(table, 2)!, table.Columns[0])).OfType<TextBox>().Any(), "the editor moved to row 2");
		}
	}

	[TestMethod]
	[DataRow(false)]
	[DataRow(true)]
	public async Task When_Sort_From_CellEditEnding_Is_Replayed(bool vetoOuterCommit)
	{
		var items = People(6);
		var table = CreateTable(items);
		var name = table.Columns[0];
		table.IsReadOnly = false;
		await LoadAsync(table);

		var sorting = 0;
		var sorted = new List<TableViewSortedEventArgs>();
		table.Sorting += (_, _) => sorting++;
		table.Sorted += (_, e) => sorted.Add(e);

		bool? sortResultInsideHandler = null;
		var sortingInsideHandler = -1;
		var veto = vetoOuterCommit;
		table.CellEditEnding += (_, e) =>
		{
			if (sortResultInsideHandler is null)
			{
				// The state is Ending: the request is queued (QueueCoalescedEditReshape), not applied.
				sortResultInsideHandler = table.SortByColumn(name, SortDirection.Descending);
				sortingInsideHandler = sorting;
			}

			e.Cancel = veto;
		};

		await FocusRowAsync(table, 1);
		await PressAsync("f2");
		FindEditor(table)!.Text = "Zed";

		var committed = table.CommitEdit();

		Assert.AreEqual(false, sortResultInsideHandler);
		Assert.AreEqual(0, sortingInsideHandler, "no Sorting is raised from inside the handler");

		if (vetoOuterCommit)
		{
			// A vetoed close clears the queue.
			Assert.IsFalse(committed);
			Assert.IsTrue(table.IsEditing);

			veto = false;
			Assert.IsTrue(table.CancelEdit());
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(SortDirection.None, name.SortDirection);
			Assert.AreEqual(0, sorting);
			Assert.AreEqual(0, sorted.Count);
			AssertRowOrder(table, items, SortDirection.None);
		}
		else
		{
			// DrainCoalescedEditReshape replays the request once the edit has closed.
			Assert.IsTrue(committed);
			Assert.IsFalse(table.IsEditing);
			Assert.AreEqual(SortDirection.Descending, name.SortDirection);
			Assert.AreEqual(1, sorting);
			Assert.AreEqual(1, sorted.Count);
			Assert.AreSame(name, sorted[0].Column);
			Assert.AreEqual(SortDirection.Descending, sorted[0].Direction);

			await WindowHelper.WaitForIdle();
			Assert.AreEqual("Zed", items[1].Name);
			AssertRowOrder(table, items, SortDirection.Descending);
		}
	}

	public enum ForcedClose
	{
		TableIsReadOnly,
		ColumnIsReadOnly,
		ItemsSourceReplaced,
	}

	public sealed class ValidatingPerson : INotifyPropertyChanged, INotifyDataErrorInfo
	{
		private string _name;
		private string _city = "";
		private readonly Dictionary<string, List<string>> _errors = new();

		public ValidatingPerson(string name) => _name = name;

		public string Name
		{
			get => _name;
			set
			{
				if (_name == value)
				{
					return;
				}

				_name = value;
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Name)));
				SetError(nameof(Name), string.IsNullOrEmpty(value) ? "Name is required." : null);
			}
		}

		public string City
		{
			get => _city;
			set
			{
				_city = value;
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(City)));
			}
		}

		public bool HasErrors => _errors.Count > 0;

		public event PropertyChangedEventHandler? PropertyChanged;

		public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

		public IEnumerable GetErrors(string? propertyName)
			=> propertyName is not null && _errors.TryGetValue(propertyName, out var errors) ? errors : Array.Empty<string>();

		public void SetError(string propertyName, string? error)
		{
			if (error is null)
			{
				_errors.Remove(propertyName);
			}
			else
			{
				_errors[propertyName] = new List<string> { error };
			}

			ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
		}
	}

	public sealed class Address : INotifyPropertyChanged
	{
		private readonly Action<string> _onStreetChanged;
		private string _street;

		public Address(string street, Action<string> onStreetChanged)
		{
			_street = street;
			_onStreetChanged = onStreetChanged;
		}

		public string Street
		{
			get => _street;
			set
			{
				if (_street == value)
				{
					return;
				}

				_street = value;
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Street)));
				_onStreetChanged(value);
			}
		}

		public event PropertyChangedEventHandler? PropertyChanged;
	}

	// Reports Address.Street errors under the leaf name, the way a flattened view model would.
	public sealed class AddressedPerson : INotifyDataErrorInfo
	{
		private string? _streetError;

		public AddressedPerson(string street)
			=> Address = new Address(street, value =>
			{
				_streetError = string.IsNullOrEmpty(value) ? "Street is required." : null;
				ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(nameof(Address.Street)));
			});

		public Address Address { get; }

		public bool HasErrors => _streetError is not null;

		public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

		public IEnumerable GetErrors(string? propertyName)
			=> propertyName == nameof(Address.Street) && _streetError is not null ? new[] { _streetError } : Array.Empty<string>();
	}

	private static async Task FocusRowAsync(TableView table, int index)
	{
		var row = GetRow(table, index)!;
		Assert.IsTrue(row.Focus(FocusState.Keyboard), $"row {index} did not take focus");
		await WindowHelper.WaitForIdle();
	}

	private static TextBox? FindEditor(TableView table)
		=> GetRealizedRows(table).SelectMany(r => Descendants(r).OfType<TextBox>()).FirstOrDefault();
}

#endif
