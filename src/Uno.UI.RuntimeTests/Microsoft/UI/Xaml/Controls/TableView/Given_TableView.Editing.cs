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

	public sealed class ValidatingPerson : INotifyPropertyChanged, INotifyDataErrorInfo
	{
		private string _name;
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

				if (string.IsNullOrEmpty(value))
				{
					_errors[nameof(Name)] = new List<string> { "Name is required." };
				}
				else
				{
					_errors.Remove(nameof(Name));
				}

				ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(nameof(Name)));
			}
		}

		public bool HasErrors => _errors.Count > 0;

		public event PropertyChangedEventHandler? PropertyChanged;

		public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

		public IEnumerable GetErrors(string? propertyName)
			=> propertyName is not null && _errors.TryGetValue(propertyName, out var errors) ? errors : Array.Empty<string>();
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
