#nullable enable

#pragma warning disable CS8305 // The Tabular types are [Experimental], as in WinUI

// TODO: drop the guard once a WinAppSDK package carries the Tabular winmd, so these also run as a WinUI parity check.
#if !WINAPPSDK

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Tabular;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Microsoft_UI_Xaml_Controls;

[TestClass]
[RunsOnUIThread]
public partial class Given_TableView
{
	private const string TabularGenericUri = "ms-appx:///Microsoft.UI.Xaml.Controls.Tabular/Themes/generic.xaml";

	[TestMethod]
	public void When_Defaults()
	{
		var table = new TableView();

		Assert.AreEqual(TableViewHeadersVisibility.Column, table.HeadersVisibility);
		Assert.AreEqual(TableViewGridLinesVisibility.All, table.GridLinesVisibility);
		Assert.AreEqual(TableViewDensity.Standard, table.Density);
		Assert.IsTrue(table.IsReadOnly);
		Assert.AreEqual(TableViewSelectionMode.Single, table.SelectionMode);
		Assert.AreEqual(-1, table.SelectedIndex);
		Assert.IsNull(table.SelectedItem);
		Assert.IsTrue(table.CanUserSortColumns);
		Assert.IsTrue(table.CanUserResizeColumns);
		Assert.IsFalse(table.IsEditing);
		Assert.IsNull(table.ItemsSource);
		Assert.IsNull(table.RowBackground);
		Assert.IsNull(table.AlternatingRowBackground);
		Assert.IsNull(table.EmptyTemplate);
		Assert.IsNull(table.GroupHeaderTemplate);
		Assert.IsNotNull(table.Columns);
		Assert.AreEqual(0, table.Columns.Count);
	}

	[TestMethod]
	public void When_Column_Defaults()
	{
		var column = new TableViewTextColumn();

		Assert.AreEqual(new GridLength(120.0, GridUnitType.Pixel), column.Width);
		Assert.AreEqual(20.0, column.MinWidth);
		Assert.IsTrue(double.IsPositiveInfinity(column.MaxWidth));
		Assert.AreEqual(120.0, column.ActualWidth);
		Assert.AreEqual(TableViewFrozenEdge.None, column.FrozenEdge);
		Assert.AreEqual(Visibility.Visible, column.Visibility);
		Assert.IsTrue(column.CanSort);
		Assert.IsTrue(column.CanResize);
		Assert.AreEqual(TableViewSortCycle.AscendingDescending, column.SortCycle);
		Assert.AreEqual(SortDirection.None, column.SortDirection);
		Assert.IsFalse(column.IsReadOnly);
		Assert.IsNull(column.Header);
		Assert.IsNull(column.HeaderTemplate);
		Assert.IsNull(column.HeaderToolTip);
		Assert.IsNull(column.CellEditingTemplate);
		Assert.IsNull(column.CustomSortComparer);
		Assert.IsNull(column.CellToolTipBinding);
		Assert.IsNull(column.Binding);

		var templateColumn = new TableViewTemplateColumn();
		Assert.IsNull(templateColumn.CellTemplate);
		Assert.AreEqual(new GridLength(120.0, GridUnitType.Pixel), templateColumn.Width);
	}

	[TestMethod]
	public void When_Xaml_Content_Properties()
	{
		var table = (TableView)XamlReader.Load(
			"""
			<tabular:TableView xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
			                   xmlns:tabular="using:Microsoft.UI.Xaml.Controls.Tabular">
				<tabular:TableViewTextColumn>Name</tabular:TableViewTextColumn>
				<tabular:TableViewTextColumn>City</tabular:TableViewTextColumn>
			</tabular:TableView>
			""");

		Assert.AreEqual(2, table.Columns.Count);
		Assert.IsInstanceOfType(table.Columns[0], typeof(TableViewTextColumn));
		Assert.AreEqual("Name", table.Columns[0].Header);
		Assert.AreEqual("City", table.Columns[1].Header);
	}

	[TestMethod]
	public async Task When_DefaultStyle_Resolves_Through_DefaultStyleResourceUri()
	{
		// The default style is reached through DefaultStyleResourceUri (SetDefaultStyleKeyWorker), not
		// through the app's merged dictionaries. The theme resources it references still come from
		// TabularControlsResources, which the SamplesApp merges app-wide; without them WinUI fails to
		// resolve SortIndicatorForeground (Samples/TableViewSampleApp/App.xaml).
		var table = CreateTable(People(3));

		Assert.AreEqual(TabularGenericUri, ((Uri)table.GetValue(Control.DefaultStyleResourceUriProperty)).OriginalString);

		await LoadAsync(table);

		Assert.IsNotNull(FindByName<FrameworkElement>(table, "PART_HeaderRow"));
		Assert.IsNotNull(FindByName<Panel>(table, "PART_HeaderHost"));
		Assert.IsNotNull(GetRepeater(table));
		Assert.AreEqual(3, GetRealizedRows(table).Count);
	}

	[TestMethod]
	public async Task When_HeadersVisibility_None()
	{
		var table = CreateTable(People(3));
		await LoadAsync(table);

		var headerRow = FindByName<FrameworkElement>(table, "PART_HeaderRow")!;
		var headerHost = FindByName<FrameworkElement>(table, "PART_HeaderHost")!;
		Assert.AreEqual(Visibility.Visible, headerRow.Visibility);
		Assert.AreEqual(Visibility.Visible, headerHost.Visibility);

		table.HeadersVisibility = TableViewHeadersVisibility.None;
		await WindowHelper.WaitForIdle();

		Assert.AreEqual(Visibility.Collapsed, headerRow.Visibility);
		Assert.AreEqual(Visibility.Collapsed, headerHost.Visibility);

		table.HeadersVisibility = TableViewHeadersVisibility.Column;
		await WindowHelper.WaitForIdle();

		Assert.AreEqual(Visibility.Visible, headerRow.Visibility);
		Assert.AreEqual(Visibility.Visible, headerHost.Visibility);
	}

	[TestMethod]
	[DataRow(TableViewGridLinesVisibility.All, true, true)]
	[DataRow(TableViewGridLinesVisibility.Horizontal, true, false)]
	[DataRow(TableViewGridLinesVisibility.Vertical, false, true)]
	[DataRow(TableViewGridLinesVisibility.None, false, false)]
	public async Task When_GridLinesVisibility_Changes(TableViewGridLinesVisibility visibility, bool horizontal, bool vertical)
	{
		var table = CreateTable(People(3));
		await LoadAsync(table);

		// Start from the opposite of the target so the change callback has something to undo.
		table.GridLinesVisibility = visibility == TableViewGridLinesVisibility.None
			? TableViewGridLinesVisibility.All
			: TableViewGridLinesVisibility.None;
		await WindowHelper.WaitForIdle();

		table.GridLinesVisibility = visibility;
		await WindowHelper.WaitForIdle();

		var headerRow = FindByName<Border>(table, "PART_HeaderRow")!;
		Assert.AreEqual(horizontal ? 1.0 : 0.0, headerRow.BorderThickness.Bottom, "header row bottom line");

		foreach (var headerCell in GetHeaderCells(table))
		{
			var gridLine = headerCell.Children.OfType<Border>().Single(b => b.Name == "TableViewHeaderGridLine");
			Assert.AreEqual(vertical ? Visibility.Visible : Visibility.Collapsed, gridLine.Visibility, "header vertical line");
		}

		foreach (var row in GetRealizedRows(table))
		{
			Assert.AreEqual(horizontal ? new Thickness(0, 0, 0, 1) : new Thickness(0), row.BorderThickness, "row bottom line");
			foreach (var cell in GetCells(row))
			{
				Assert.AreEqual(vertical ? new Thickness(0, 0, 1, 0) : new Thickness(0), cell.BorderThickness, "cell vertical line");
			}
		}
	}

	[TestMethod]
	[DataRow(TableViewDensity.Compact, 30.0, 2.0, 2.0)]
	[DataRow(TableViewDensity.Standard, 40.0, 4.0, 4.0)]
	[DataRow(TableViewDensity.Comfortable, 48.0, 8.0, 8.0)]
	public async Task When_Density_Changes(TableViewDensity density, double rowMinHeight, double cellPaddingTop, double headerPaddingTop)
	{
		var table = CreateTable(People(3));
		await LoadAsync(table);

		// Changing away and back exercises the resource-cache invalidation on every value.
		table.Density = density == TableViewDensity.Standard ? TableViewDensity.Compact : TableViewDensity.Standard;
		await WindowHelper.WaitForIdle();
		table.Density = density;
		await WindowHelper.WaitForIdle();

		foreach (var row in GetRealizedRows(table))
		{
			Assert.AreEqual(rowMinHeight, row.MinHeight, "row MinHeight");
			var textBlock = (TextBlock)GetCells(row)[0].Child;
			Assert.AreEqual(new Thickness(8, cellPaddingTop, 8, cellPaddingTop), textBlock.Padding, "cell padding");
		}

		foreach (var headerCell in GetHeaderCells(table))
		{
			Assert.AreEqual(rowMinHeight, headerCell.MinHeight, "header MinHeight");
			var presenter = headerCell.Children.OfType<ContentPresenter>().First();
			Assert.AreEqual(new Thickness(8, headerPaddingTop, 8, headerPaddingTop), presenter.Padding, "header padding");
		}
	}

	[TestMethod]
	public async Task When_Density_Keys_Overridden_In_Ancestor_Resources()
	{
		var table = CreateTable(People(3));
		table.Density = TableViewDensity.Compact;

		// LookupElementResource walks the logical parents before the application resources.
		await LoadAsync(table, configureHost: h => h.Resources["TableViewRowMinHeightCompact"] = 22.0);

		foreach (var row in GetRealizedRows(table))
		{
			Assert.AreEqual(22.0, row.MinHeight);
		}

		Assert.IsTrue(GetHeaderCells(table).All(c => c.MinHeight == 22.0));
	}

	[TestMethod]
	public async Task When_Row_Banding()
	{
		var table = CreateTable(People(6));
		await LoadAsync(table);

		// Opt-in: both brushes null leaves the row on its style background.
		foreach (var row in GetRealizedRows(table))
		{
			Assert.AreEqual(DependencyProperty.UnsetValue, row.ReadLocalValue(Control.BackgroundProperty));
		}

		var alternating = new SolidColorBrush(Colors.Red);
		table.AlternatingRowBackground = alternating;
		await WindowHelper.WaitForIdle();

		foreach (var row in GetRealizedRows(table))
		{
			var index = GetRepeater(table)!.GetElementIndex(row);
			if (index % 2 != 0)
			{
				Assert.AreSame(alternating, row.Background, $"odd row {index}");
			}
			else
			{
				Assert.AreEqual(DependencyProperty.UnsetValue, row.ReadLocalValue(Control.BackgroundProperty), $"even row {index}");
			}
		}

		var baseBrush = new SolidColorBrush(Colors.Green);
		table.RowBackground = baseBrush;
		await WindowHelper.WaitForIdle();

		foreach (var row in GetRealizedRows(table))
		{
			var index = GetRepeater(table)!.GetElementIndex(row);
			Assert.AreSame(index % 2 != 0 ? alternating : baseBrush, row.Background, $"row {index}");
		}

		// RowBackground alone fills every row uniformly.
		table.AlternatingRowBackground = null;
		await WindowHelper.WaitForIdle();

		foreach (var row in GetRealizedRows(table))
		{
			Assert.AreSame(baseBrush, row.Background);
		}
	}

	[TestMethod]
	public async Task When_EmptyTemplate()
	{
		var items = new ObservableCollection<Person>();
		var table = CreateTable(items);
		table.EmptyTemplate = (DataTemplate)XamlReader.Load(
			"""
			<DataTemplate xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation">
				<TextBlock Text="Nothing here" />
			</DataTemplate>
			""");
		await LoadAsync(table);

		var presenter = FindByName<ContentControl>(table, "PART_EmptyStatePresenter")!;
		var repeater = GetRepeater(table)!;

		Assert.AreEqual(Visibility.Visible, presenter.Visibility);
		Assert.AreEqual(Visibility.Collapsed, repeater.Visibility);

		items.Add(new Person("Ada", 36, "London"));
		await WindowHelper.WaitForIdle();

		Assert.AreEqual(Visibility.Collapsed, presenter.Visibility);
		Assert.AreEqual(Visibility.Visible, repeater.Visibility);

		items.Clear();
		await WindowHelper.WaitForIdle();

		Assert.AreEqual(Visibility.Visible, presenter.Visibility);
		Assert.AreEqual(Visibility.Collapsed, repeater.Visibility);

		// Without an EmptyTemplate the empty surface never shows.
		table.EmptyTemplate = null;
		await WindowHelper.WaitForIdle();

		Assert.AreEqual(Visibility.Collapsed, presenter.Visibility);
		Assert.AreEqual(Visibility.Visible, repeater.Visibility);
	}

	[TestMethod]
	public async Task When_EmptyTemplate_Null_ItemsSource()
	{
		// UpdateEmptyState: with no ItemsSourceView the table counts as empty.
		var table = CreateTable(null);
		var emptyTemplate = (DataTemplate)XamlReader.Load(
			"""
			<DataTemplate xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation">
				<TextBlock Text="Nothing here" />
			</DataTemplate>
			""");
		table.EmptyTemplate = emptyTemplate;
		await LoadAsync(table);

		var presenter = FindByName<ContentControl>(table, "PART_EmptyStatePresenter")!;
		var repeater = GetRepeater(table)!;

		Assert.AreEqual(Visibility.Visible, presenter.Visibility);
		Assert.AreEqual(Visibility.Collapsed, repeater.Visibility);
		Assert.AreSame(emptyTemplate, presenter.ContentTemplate);
		Assert.AreEqual("", presenter.Content, "an empty string so the template inflates without a data item");

		table.ItemsSource = People(2);
		await WindowHelper.WaitForIdle();

		Assert.AreEqual(Visibility.Collapsed, presenter.Visibility);
		Assert.AreEqual(Visibility.Visible, repeater.Visibility);

		table.ItemsSource = null;
		await WindowHelper.WaitForIdle();

		Assert.AreEqual(Visibility.Visible, presenter.Visibility);
		Assert.AreEqual(Visibility.Collapsed, repeater.Visibility);
	}

	[TestMethod]
	public async Task When_Column_Collapsed()
	{
		var table = CreateTable(People(3));
		var city = table.Columns[2];
		await LoadAsync(table);

		city.Visibility = Visibility.Collapsed;
		await WindowHelper.WaitForIdle();

		Assert.AreEqual(Visibility.Collapsed, GetHeaderCell(table, city).Visibility);
		foreach (var row in GetRealizedRows(table))
		{
			Assert.AreEqual(Visibility.Collapsed, GetCell(row, city).Visibility);
		}

		city.Visibility = Visibility.Visible;
		await WindowHelper.WaitForIdle();

		Assert.AreEqual(Visibility.Visible, GetHeaderCell(table, city).Visibility);
		foreach (var row in GetRealizedRows(table))
		{
			Assert.AreEqual(Visibility.Visible, GetCell(row, city).Visibility);
		}
	}

	[TestMethod]
	public async Task When_TemplateColumn_Null_Template()
	{
		var table = CreateTable(People(3));
		var templateColumn = new TableViewTemplateColumn { Header = "Empty" };
		table.Columns.Add(templateColumn);

		await LoadAsync(table);

		foreach (var row in GetRealizedRows(table))
		{
			var presenter = (ContentPresenter)GetCell(row, templateColumn).Child;
			Assert.IsNull(presenter.ContentTemplate);
			Assert.IsNull(presenter.Content, "an empty presenter, not the item's ToString()");
		}
	}

	#region Helpers

	public sealed class Person : INotifyPropertyChanged
	{
		private string _name;
		private int _age;
		private string _city;
		private string? _notes;

		public Person(string name, int age, string city, string? notes = null)
		{
			_name = name;
			_age = age;
			_city = city;
			_notes = notes;
		}

		public string Name { get => _name; set => Set(ref _name, value); }

		public int Age { get => _age; set => Set(ref _age, value); }

		public string City { get => _city; set => Set(ref _city, value); }

		public string? Notes { get => _notes; set => Set(ref _notes, value); }

		public event PropertyChangedEventHandler? PropertyChanged;

		public override string ToString() => $"Person({Name})";

		private void Set<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
		{
			if (EqualityComparer<T>.Default.Equals(field, value))
			{
				return;
			}

			field = value;
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}

	private static readonly string[] s_names = { "Mia", "Ada", "Zoe", "Bob", "Eve", "Cy", "Kim", "Liv", "Tom", "Ian" };
	private static readonly string[] s_cities = { "Oslo", "Kyoto", "Oslo", "Lima", "Kyoto", "Oslo" };

	private static List<Person> People(int count)
	{
		List<Person> people = new(count);
		for (var i = 0; i < count; i++)
		{
			people.Add(new Person($"{s_names[i % s_names.Length]}{i / s_names.Length}", 20 + ((i * 7) % 30), s_cities[i % s_cities.Length]));
		}

		return people;
	}

	private static TableViewTextColumn TextColumn(string path, GridLength? width = null)
	{
		var column = new TableViewTextColumn
		{
			Header = path,
			Binding = new Binding { Path = new PropertyPath(path) },
		};

		if (width is { } w)
		{
			column.Width = w;
		}

		return column;
	}

	private static TableView CreateTable(object? itemsSource)
	{
		var table = new TableView();
		table.Columns.Add(TextColumn(nameof(Person.Name)));
		table.Columns.Add(TextColumn(nameof(Person.Age)));
		table.Columns.Add(TextColumn(nameof(Person.City)));
		table.ItemsSource = itemsSource;
		return table;
	}

	private static async Task<Grid> LoadAsync(
		TableView table,
		double width = 500,
		double height = 400,
		FrameworkElement? header = null,
		Action<Grid>? configureHost = null)
	{
		Grid host = new() { Width = width, Height = height };
		host.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		host.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
		configureHost?.Invoke(host);

		if (header is not null)
		{
			host.Children.Add(header);
		}

		Grid.SetRow(table, 1);
		host.Children.Add(table);

		WindowHelper.WindowContent = host;
		await WindowHelper.WaitForLoaded(table);
		await WindowHelper.WaitForIdle();
		return host;
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

	private static T? FindByName<T>(DependencyObject root, string name) where T : FrameworkElement
		=> Descendants(root).OfType<T>().FirstOrDefault(e => e.Name == name);

	private static ItemsRepeater? GetRepeater(TableView table)
		=> FindByName<ItemsRepeater>(table, "PART_RowsRepeater");

	private static ScrollViewer GetBodyScroller(TableView table)
		=> FindByName<ScrollViewer>(table, "PART_BodyScroller")!;

	// Realized and bound rows only: pooled containers report index -1.
	private static List<TableViewRow> GetRealizedRows(TableView table)
	{
		var repeater = GetRepeater(table)!;
		return Descendants(repeater)
			.OfType<TableViewRow>()
			.Select(row => (row, index: repeater.GetElementIndex(row)))
			.Where(x => x.index >= 0)
			.OrderBy(x => x.index)
			.Select(x => x.row)
			.ToList();
	}

	private static List<TableViewGroupHeader> GetRealizedGroupHeaders(TableView table)
	{
		var repeater = GetRepeater(table)!;
		return Descendants(repeater)
			.OfType<TableViewGroupHeader>()
			.Select(header => (header, index: repeater.GetElementIndex(header)))
			.Where(x => x.index >= 0)
			.OrderBy(x => x.index)
			.Select(x => x.header)
			.ToList();
	}

	private static TableViewRow? GetRow(TableView table, int index)
		=> GetRepeater(table)!.TryGetElement(index) as TableViewRow;

	private static List<Border> GetCells(TableViewRow row)
		=> FindByName<Panel>(row, "PART_CellsHost")!.Children.OfType<Border>().ToList();

	private static Border GetCell(TableViewRow row, TableViewColumn column)
		=> GetCells(row).Single(c => ReferenceEquals(c.Tag, column));

	private static List<Grid> GetHeaderCells(TableView table)
		=> FindByName<Panel>(table, "PART_HeaderHost")!.Children.OfType<Grid>().ToList();

	private static Grid GetHeaderCell(TableView table, TableViewColumn column)
		=> GetHeaderCells(table).Single(c => ReferenceEquals(c.Tag, column));

	private static string? GetCellText(TableViewRow row, int columnIndex)
		=> (GetCells(row)[columnIndex].Child as TextBlock)?.Text;

	private static List<string> GetRowNames(TableView table)
		=> GetRealizedRows(table).Select(r => ((Person)r.DataContext).Name).ToList();

	private static int GetFocusedRowIndex(TableView table)
	{
		var focused = FocusManager.GetFocusedElement(WindowHelper.XamlRoot) as DependencyObject;
		while (focused is not null)
		{
			if (focused is TableViewRow row)
			{
				return GetRepeater(table)!.GetElementIndex(row);
			}

			focused = VisualTreeHelper.GetParent(focused);
		}

		return -1;
	}

	private static Point Center(FrameworkElement element)
	{
		var bounds = element.TransformToVisual(null).TransformBounds(new Rect(0, 0, element.ActualWidth, element.ActualHeight));
		return new Point(bounds.X + bounds.Width / 2, bounds.Y + bounds.Height / 2);
	}

	#endregion
}

#endif
