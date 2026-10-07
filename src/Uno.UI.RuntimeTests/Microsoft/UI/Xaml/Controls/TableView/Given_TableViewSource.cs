#nullable enable

#pragma warning disable CS8305 // The Tabular types are [Experimental], as in WinUI

#if !WINAPPSDK

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Tabular;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Microsoft_UI_Xaml_Controls;

[TestClass]
[RunsOnUIThread]
public class Given_TableViewSource
{
	public sealed class Row
	{
		public Row(string name, string group, int score)
		{
			Name = name;
			Group = group;
			Score = score;
		}

		public string Name { get; }

		public string Group { get; }

		public int Score { get; }
	}

	[TestMethod]
	public void When_TableViewSource_From_Invalid()
	{
		// Unlike the TableView command surface, the shaping verbs reject bad input (E_INVALIDARG).
		Assert.Throws<ArgumentException>(() => TableViewSource.From(null!));

		var source = TableViewSource.From(new List<Row>());
		Assert.Throws<ArgumentException>(() => source.Filter(null!));
		Assert.Throws<ArgumentException>(() => source.GroupBy(null!));
		Assert.Throws<ArgumentException>(() => source.GroupBy(null!, null));
	}

	[TestMethod]
	public void When_Verbs_Are_Fluent()
	{
		var source = TableViewSource.From(new List<Row> { new("A", "x", 1) });

		Assert.AreSame(source, source.Filter(new TableViewPredicate(_ => true)));
		Assert.AreSame(source, source.GroupBy(new TableViewKeySelector(i => ((Row)i!).Group)));
		Assert.AreSame(source, source.GroupBy(new TableViewKeySelector(i => ((Row)i!).Group), new TableViewIdentitySelector(k => k?.ToString() ?? "")));
		Assert.AreSame(source, source.Sort(nameof(Row.Name), SortDirection.Ascending));
		Assert.AreSame(source, source.Sort(new TableViewKeySelector(i => ((Row)i!).Score), SortDirection.Descending));
		Assert.AreSame(source, source.ClearFilter());
		Assert.AreSame(source, source.ClearGroupBy());
		Assert.AreSame(source, source.ClearSort());
	}

	[TestMethod]
	public void When_Same_Object_On_Two_Rows()
	{
		// Rows are identified by object identity, so a shaped projection cannot tell two rows backed
		// by one object apart and fails fast.
		var shared = new Row("A", "x", 1);
		var source = TableViewSource.From(new List<Row> { shared, new("B", "x", 2), shared });

		Assert.Throws<ArgumentException>(() => source.Sort(nameof(Row.Name), SortDirection.Ascending));
	}

	[TestMethod]
	public async Task When_Filter_Group_Sort_Compose()
	{
		var items = new ObservableCollection<Row>
		{
			new("Delta", "b", 40),
			new("Alpha", "a", 90),
			new("Charlie", "b", 10),
			new("Bravo", "a", 70),
			new("Echo", "c", 20),
		};
		var source = TableViewSource.From(items);

		var table = new TableView { ItemsSource = source };
		table.Columns.Add(new TableViewTextColumn { Header = "Name", Binding = new Binding { Path = new PropertyPath(nameof(Row.Name)) } });
		table.Columns.Add(new TableViewTextColumn { Header = "Score", Binding = new Binding { Path = new PropertyPath(nameof(Row.Score)) } });

		var host = new Grid { Width = 400, Height = 600 };
		host.Children.Add(table);
		WindowHelper.WindowContent = host;
		await WindowHelper.WaitForLoaded(table);
		await WindowHelper.WaitForIdle();

		CollectionAssert.AreEqual(new[] { "Delta", "Alpha", "Charlie", "Bravo", "Echo" }, RowNames(table));

		source.Filter(new TableViewPredicate(i => ((Row)i!).Score >= 20));
		await WindowHelper.WaitForIdle();
		CollectionAssert.AreEqual(new[] { "Delta", "Alpha", "Bravo", "Echo" }, RowNames(table));

		source.Sort(nameof(Row.Name), SortDirection.Ascending);
		await WindowHelper.WaitForIdle();
		CollectionAssert.AreEqual(new[] { "Alpha", "Bravo", "Delta", "Echo" }, RowNames(table));

		// The source keeps observing the collection through the shaping stages.
		items.Add(new Row("Aaron", "c", 50));
		items.Add(new Row("Zed", "c", 5));
		await WindowHelper.WaitForIdle();
		CollectionAssert.AreEqual(new[] { "Aaron", "Alpha", "Bravo", "Delta", "Echo" }, RowNames(table));

		// Grouping buckets what survives the filter, in first-seen order.
		source.ClearSort();
		source.GroupBy(new TableViewKeySelector(i => ((Row)i!).Group));
		await WindowHelper.WaitForIdle();

		var repeater = Descendants(table).OfType<ItemsRepeater>().First(r => r.Name == "PART_RowsRepeater");
		var headers = Descendants(repeater).OfType<TableViewGroupHeader>()
			.Select(h => (header: h, index: repeater.GetElementIndex(h)))
			.Where(x => x.index >= 0)
			.OrderBy(x => x.index)
			.Select(x => (TableViewGroupInfo)x.header.Content)
			.Select(i => $"{i.KeyText} {i.ItemCountText}")
			.ToList();
		CollectionAssert.AreEqual(new[] { "b (1)", "a (2)", "c (2)" }, headers);

		source.ClearGroupBy();
		source.ClearFilter();
		await WindowHelper.WaitForIdle();
		Assert.AreEqual(items.Count, RowNames(table).Count);
	}

	private static List<string> RowNames(TableView table)
	{
		var repeater = Descendants(table).OfType<ItemsRepeater>().First(r => r.Name == "PART_RowsRepeater");
		return Descendants(repeater)
			.OfType<TableViewRow>()
			.Select(row => (row, index: repeater.GetElementIndex(row)))
			.Where(x => x.index >= 0)
			.OrderBy(x => x.index)
			.Select(x => ((Row)x.row.DataContext).Name)
			.ToList();
	}

	private static IEnumerable<DependencyObject> Descendants(DependencyObject root)
	{
		var count = VisualTreeHelper.GetChildrenCount(root);
		for (var i = 0; i < count; i++)
		{
			var child = VisualTreeHelper.GetChild(root, i);
			yield return child;
			foreach (var descendant in Descendants(child))
			{
				yield return descendant;
			}
		}
	}
}

#endif
