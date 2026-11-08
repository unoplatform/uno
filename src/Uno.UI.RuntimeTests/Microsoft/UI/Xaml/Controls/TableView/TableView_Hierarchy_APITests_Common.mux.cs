// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\TableView\APITests\TableView_Hierarchy_APITests_Common.cs, tag winui3/main, commit 79209e041

#pragma warning disable CS8305 // The Tabular types are [Experimental], as in WinUI

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Tabular;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Common;
using Private.Infrastructure;


using static Microsoft.UI.Xaml.Tests.MUXControls.ApiTests.TableViewTestHelpers;

namespace Microsoft.UI.Xaml.Tests.MUXControls.ApiTests;

// A non-notifying employee for hierarchy tests. ParentBy(Id, ManagerId) gives:
//   Ada > { Ben > { Dan }, Cy }, Eve > { Fay }, Gus (ManagerId 99 matches nobody, so a root).
internal sealed class TreeEmployee
{
	public int Id { get; set; }
	public int? ManagerId { get; set; }
	public int? MentorId { get; set; }
	public long? BigManagerId { get; set; }
	public string Code { get; set; } = "";
	public string ParentCode { get; set; } = "";
	public TreeEmployee Manager { get; set; }
	public string Name { get; set; } = "";
	public string Dept { get; set; } = "";
	public int Score { get; set; }
}

// Observable row for live shaping; PropertyChanged is the only signal live shaping listens to.
internal sealed class LiveRow : INotifyPropertyChanged
{
	private string _name = "";
	private int _score;
	private string _team = "";
	private int? _parentId;
	private LiveAddress _addr = new LiveAddress("");
	private LiveRow _parentRow;

	public int Id { get; set; }
	public string Name { get => _name; set => Set(ref _name, value); }
	public int Score { get => _score; set => Set(ref _score, value); }
	public string Team { get => _team; set => Set(ref _team, value); }
	public int? ParentId { get => _parentId; set => Set(ref _parentId, value); }
	public LiveAddress Addr { get => _addr; set => Set(ref _addr, value); }

	// Same relation as ParentId, keyed by reference identity instead of by value.
	public LiveRow ParentRow { get => _parentRow; set => Set(ref _parentRow, value); }

	public event PropertyChangedEventHandler PropertyChanged;

	private void Set<T>(ref T field, T value, [CallerMemberName] string name = null)
	{
		if (EqualityComparer<T>.Default.Equals(field, value))
		{
			return;
		}

		field = value;
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
	}
}

// A change to City is raised on the address, not on the row that owns it.
internal sealed class LiveAddress : INotifyPropertyChanged
{
	private string _city;

	public LiveAddress(string city) { _city = city; }

	public string City
	{
		get => _city;
		set
		{
			_city = value;
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(City)));
		}
	}

	public event PropertyChangedEventHandler PropertyChanged;
}

internal sealed class PlainRow
{
	public string Name { get; set; } = "";
	public int Score { get; set; }
}

// Orders LiveRows by Score through the column CustomSortComparer path rather than a key.
internal sealed partial class LiveScoreComparer : ITableViewSortComparer
{
	public int Compare(object left, object right) => ((LiveRow)left).Score.CompareTo(((LiveRow)right).Score);
}

// Use ResettableCollection<T> from TableView_Selection_APITests.cs for a single-Reset replace.

internal static class TableViewTreeTestHelpers
{
	internal static TableViewKeySelector EmpKey(Func<TreeEmployee, object> f) => new TableViewKeySelector(o => f((TreeEmployee)o));
	internal static TableViewKeySelector LiveKey(Func<LiveRow, object> f) => new TableViewKeySelector(o => f((LiveRow)o));

	internal static readonly TableViewKeySelector ById = EmpKey(e => e.Id);
	internal static readonly TableViewKeySelector ByManager = EmpKey(e => e.ManagerId);
	internal static readonly TableViewKeySelector ByMentor = EmpKey(e => e.MentorId);

	internal static readonly TableViewKeySelector LiveById = LiveKey(r => r.Id);
	internal static readonly TableViewKeySelector LiveByParent = LiveKey(r => r.ParentId);
	internal static readonly TableViewKeySelector LiveByScore = LiveKey(r => r.Score);
	internal static readonly TableViewKeySelector LiveByTeam = LiveKey(r => r.Team);
	internal static readonly TableViewKeySelector LiveByObject = LiveKey(r => r);
	internal static readonly TableViewKeySelector LiveByParentObject = LiveKey(r => r.ParentRow);

	internal static TableViewPredicate MinScore(int min) => new TableViewPredicate(o => ((LiveRow)o).Score >= min);

	// Ada > { Ben > { Dan }, Cy }, Eve > { Fay }, Gus. Ada and her reports are Eng; the rest Ops.
	// Mentor relation: Ben > Eve, Fay > Ada.
	internal static ObservableCollection<TreeEmployee> MakeTree()
	{
		var ada = new TreeEmployee { Id = 1, Name = "Ada", Dept = "Eng", Code = "A", Score = 50 };
		var ben = new TreeEmployee { Id = 2, ManagerId = 1, Manager = ada, Name = "Ben", Dept = "Eng", Code = "B", ParentCode = "A", Score = 40 };
		var cy = new TreeEmployee { Id = 3, ManagerId = 1, Manager = ada, Name = "Cy", Dept = "Eng", Code = "C", ParentCode = "A", Score = 30 };
		var dan = new TreeEmployee { Id = 4, ManagerId = 2, Manager = ben, Name = "Dan", Dept = "Eng", Code = "D", ParentCode = "B", Score = 20 };
		var eve = new TreeEmployee { Id = 5, Name = "Eve", Dept = "Ops", Code = "E", Score = 60 };
		var fay = new TreeEmployee { Id = 6, ManagerId = 5, Manager = eve, Name = "Fay", Dept = "Ops", Code = "F", ParentCode = "E", Score = 10 };
		var gus = new TreeEmployee { Id = 7, ManagerId = 99, Name = "Gus", Dept = "Ops", Code = "G", ParentCode = "Z", Score = 70 };
		var all = new[] { ada, ben, cy, dan, eve, fay, gus };
		foreach (var e in all)
		{
			e.BigManagerId = e.ManagerId;
		}

		ben.MentorId = 5;
		fay.MentorId = 1;
		return new ObservableCollection<TreeEmployee>(all);
	}

	// Flat live fixture: source order A B C D, scores ascending, teams alternating X/Y.
	internal static ObservableCollection<LiveRow> MakeLiveFlat() => new ObservableCollection<LiveRow>(new[]
	{
		new LiveRow { Id = 1, Name = "A", Score = 10, Team = "X", Addr = new LiveAddress("Amsterdam") },
		new LiveRow { Id = 2, Name = "B", Score = 20, Team = "Y", Addr = new LiveAddress("Berlin") },
		new LiveRow { Id = 3, Name = "C", Score = 30, Team = "X", Addr = new LiveAddress("Cairo") },
		new LiveRow { Id = 4, Name = "D", Score = 40, Team = "Y", Addr = new LiveAddress("Dublin") },
	});

	// Live tree fixture (ParentId): Ann > { Bob, Cat }, Dee > { Eli }. Children share their root's team.
	internal static ObservableCollection<LiveRow> MakeLiveTree() => new ObservableCollection<LiveRow>(new[]
	{
		new LiveRow { Id = 1, Name = "Ann", Score = 50, Team = "X" },
		new LiveRow { Id = 2, Name = "Bob", Score = 30, Team = "X", ParentId = 1 },
		new LiveRow { Id = 3, Name = "Cat", Score = 10, Team = "X", ParentId = 1 },
		new LiveRow { Id = 4, Name = "Dee", Score = 20, Team = "Y" },
		new LiveRow { Id = 5, Name = "Eli", Score = 5, Team = "Y", ParentId = 4 },
	});

	// MakeLiveTree with ParentRow mirroring ParentId.
	internal static ObservableCollection<LiveRow> MakeLiveObjectTree()
	{
		var list = MakeLiveTree();
		foreach (var r in list)
		{
			r.ParentRow = r.ParentId.HasValue ? list.First(p => p.Id == r.ParentId.Value) : null;
		}

		return list;
	}

	internal static T Named<T>(IEnumerable<T> items, string name) where T : class
		=> items.First(i => NameOf(i) == name);

	internal static string NameOf(object item)
	{
		switch (item)
		{
			case TreeEmployee e: return e.Name;
			case LiveRow l: return l.Name;
			case PlainRow p: return p.Name;
			case ShapedPerson s: return s.Name;
			default: return "?";
		}
	}

	// A single Name column (TwoWay when editable) over the source, loaded and settled.
	internal static TableView CreateTreeTable(object source, bool editable = false)
	{
		var column = new TableViewTextColumn
		{
			Header = "Name",
			Binding = new Microsoft.UI.Xaml.Data.Binding
			{
				Path = new PropertyPath("Name"),
				Mode = editable ? Microsoft.UI.Xaml.Data.BindingMode.TwoWay : Microsoft.UI.Xaml.Data.BindingMode.OneWay,
			},
			Width = new GridLength(1, GridUnitType.Star),
		};

		var tableView = new TableView { IsReadOnly = !editable, Width = 400, Height = 600 };
		tableView.Columns.Add(column);
		tableView.ItemsSource = source;
		return tableView;
	}

	// Projected rows as one space-separated string. Data row: Name, then Level and a state mark
	// when Level > 0 ('+' collapsed, '-' expanded, nothing for a leaf). Group header: [key].
	// Example: "Ada1- Ben2+ Cy2 Eve1+ Gus1".
	internal static string TreeLabels(TableView tableView)
	{
		var parts = new List<string>();
		foreach (var element in GetProjectedElements(tableView))
		{
			if (element is TableViewGroupHeader header)
			{
				var info = header.Content as TableViewGroupInfo;
				parts.Add(info == null ? "[H]" : $"[{info.Key}]");
			}
			else if (element is TableViewRow row)
			{
				parts.Add(RowLabel(row));
			}
			else
			{
				parts.Add("?");
			}
		}

		return string.Join(" ", parts);
	}

	internal static string RowLabel(TableViewRow row)
	{
		var name = NameOf(row.DataContext);
		if (row.Level == 0)
		{
			return name;
		}

		var mark = row.IsExpandable ? (row.IsExpanded ? "-" : "+") : "";
		return $"{name}{row.Level}{mark}";
	}

	internal static void VerifyTree(TableView tableView, string expected, string context)
	{
		var actual = TreeLabels(tableView);
		Verify.AreEqual(expected, actual, $"Projection ({context}).");
	}

	internal static TableViewRow FindTreeRow(TableView tableView, string name)
	{
		var row = GetProjectedElements(tableView).OfType<TableViewRow>().FirstOrDefault(r => NameOf(r.DataContext) == name);
		if (row == null)
		{
			Verify.Fail($"Row '{name}' is not realized. Projection: {TreeLabels(tableView)}");
		}

		return row;
	}

	internal static IExpandCollapseProvider GetRowExpandCollapse(TableViewRow row)
		=> GetRowPeer(row).GetPattern(PatternInterface.ExpandCollapse) as IExpandCollapseProvider;

	// Toggles a row through its peer's ExpandCollapse pattern, the input-free path the chevron uses.
	internal static void ToggleTreeRow(TableView tableView, string name, bool expand)
	{
		var provider = GetRowExpandCollapse(FindTreeRow(tableView, name));
		if (provider == null)
		{
			Verify.Fail($"Row '{name}' must expose ExpandCollapse.");
			return;
		}

		if (expand)
		{
			provider.Expand();
		}
		else
		{
			provider.Collapse();
		}
	}

	// "Name pos/size" per projected data row, from the row peers.
	internal static string PositionLabels(TableView tableView)
		=> string.Join(" ", GetProjectedElements(tableView).OfType<TableViewRow>().Select(r =>
		{
			var peer = GetRowPeer(r);
			return $"{NameOf(r.DataContext)} {peer.GetPositionInSet()}/{peer.GetSizeOfSet()}";
		}));

	internal static string ThrownTypeName(Action action)
	{
		try
		{
			action();
			return "no exception";
		}
		catch (Exception ex)
		{
			return ex.GetType().Name;
		}
	}
}
