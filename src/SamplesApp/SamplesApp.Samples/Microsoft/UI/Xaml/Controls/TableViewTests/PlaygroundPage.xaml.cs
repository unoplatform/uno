// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference Samples\TableViewSampleApp\PlaygroundPage.xaml.cs, tag winui3/release/2.5.4-experimental, commit 7b127093475

#nullable enable
#pragma warning disable CS8305 // TableView is [Experimental]

using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Windows.UI;
using Microsoft.UI.Xaml.Controls.Tabular;
using Uno.UI.Samples.Controls;

namespace MUXControlsTestApp;

// Interactive playground: builds a TableView (control) with one column of each
// kind, then wires the left panel so every public API can be exercised live. Data lives in Data.cs.
[Sample("TableView", Name = "TableView_Playground")]
public sealed partial class PlaygroundPage : Page
{
	private TableViewTextColumn _name = null!;
	private TableViewTextColumn _role = null!;
	private TableViewTextColumn _city = null!;
	private TableViewTemplateColumn _score = null!;
	private int _added;
	private bool _ready;   // guards combo SelectionChanged that fires during XAML load
	private readonly System.Collections.Generic.List<Item> _watchedItems = new();

	// TODO Uno: resolved from TableViewSampleResources rather than Application.Current.Resources.
	// private static DataTemplate Tmpl(string key) => (DataTemplate)Application.Current.Resources[key];
	private static DataTemplate Tmpl(string key) => TableViewSampleResources.Template(key);

	public PlaygroundPage()
	{
		this.InitializeComponent();

		BuildColumns();
		Table.EmptyTemplate = Tmpl("EmptyState");
		Table.ItemsSource = Data.Make();
		AttachSourceWatch(Table.ItemsSource);

		// Editing is opt-in (TableView.IsReadOnly defaults to true). Start editable so double-click
		// and F2 can be exercised without first toggling, and seed the ToggleButton from the live
		// value so the two do not disagree.
		Table.IsReadOnly = false;
		ReadOnlyToggle.IsChecked = false;

		_ready = true;
	}

	private void BuildColumns()
	{
		_name = new TableViewTextColumn
		{
			Header = "Name",
			Binding = new Binding { Path = new PropertyPath(nameof(Item.Name)) },
			Width = new GridLength(1, GridUnitType.Auto),
		};
		_role = new TableViewTextColumn
		{
			Header = "Role",
			Binding = new Binding { Path = new PropertyPath(nameof(Item.Role)) },
			Width = new GridLength(1, GridUnitType.Star),
		};
		_city = new TableViewTextColumn
		{
			Header = "City",
			Binding = new Binding { Path = new PropertyPath(nameof(Item.City)) },
			Width = new GridLength(1, GridUnitType.Star),
		};
		_score = new TableViewTemplateColumn
		{
			Header = "Score",
			CellTemplate = Tmpl("ScoreCell"),
			Width = new GridLength(120, GridUnitType.Pixel),
		};

		var bio = new TableViewTemplateColumn
		{
			Header = "Bio",
			CellTemplate = Tmpl("BioCell"),
			Width = new GridLength(1, GridUnitType.Star),
		};
		var joined = new TableViewTemplateColumn
		{
			Header = "Joined",
			CellTemplate = Tmpl("JoinedCell"),
			Width = new GridLength(240, GridUnitType.Pixel),
		};
		var notes = new TableViewTemplateColumn
		{
			Header = "Notes",
			CellTemplate = Tmpl("NotesCell"),
			CellEditingTemplate = Tmpl("NotesEditCell"),
			Width = new GridLength(160, GridUnitType.Auto),
		};
		var image = new TableViewTemplateColumn
		{
			Header = "Image",
			CellTemplate = Tmpl("ImageCell"),
			Width = new GridLength(72, GridUnitType.Pixel),
		};

		Table.Columns.Add(_name);
		Table.Columns.Add(_role);
		Table.Columns.Add(_city);
		Table.Columns.Add(_score);
		Table.Columns.Add(bio);
		Table.Columns.Add(joined);
		Table.Columns.Add(notes);
		Table.Columns.Add(image);
	}

	// ---- Name column width / min / max ----
	private void Width_Changed(object sender, SelectionChangedEventArgs e)
	{
		if (!_ready)
		{
			return;
		}
		_name.Width = WidthCombo.SelectedIndex switch
		{
			1 => new GridLength(200, GridUnitType.Pixel),
			2 => new GridLength(1, GridUnitType.Star),
			3 => new GridLength(2, GridUnitType.Star),
			_ => new GridLength(1, GridUnitType.Auto),
		};
	}

	private void MinW_Toggle(object sender, RoutedEventArgs e)
		=> _name.MinWidth = (MinW.IsChecked == true) ? 120 : 20;

	private void MaxW_Toggle(object sender, RoutedEventArgs e)
		=> _name.MaxWidth = (MaxW.IsChecked == true) ? 160 : double.PositiveInfinity;

	// ---- Columns ----
	private void CityVis_Toggle(object sender, RoutedEventArgs e)
		=> _city.Visibility = (CityVis.IsChecked == true) ? Visibility.Visible : Visibility.Collapsed;

	private void AddCol_Click(object sender, RoutedEventArgs e)
	{
		_added++;
		Table.Columns.Add(new TableViewTextColumn
		{
			Header = $"Extra {_added}",
			Binding = new Binding { Path = new PropertyPath(nameof(Item.Role)) },
			Width = new GridLength(1, GridUnitType.Auto),
		});
	}

	private void RemoveCol_Click(object sender, RoutedEventArgs e)
	{
		if (Table.Columns.Count > 1)
		{
			Table.Columns.RemoveAt(Table.Columns.Count - 1);
		}
	}

	// ---- Headers / gridlines / density ----
	private void Headers_Changed(object sender, SelectionChangedEventArgs e)
	{
		if (!_ready)
		{
			return;
		}
		Table.HeadersVisibility = HeadersCombo.SelectedIndex == 1
			? TableViewHeadersVisibility.None
			: TableViewHeadersVisibility.Column;
	}

	private void GridLines_Changed(object sender, SelectionChangedEventArgs e)
	{
		if (!_ready)
		{
			return;
		}
		Table.GridLinesVisibility = GridLinesCombo.SelectedIndex switch
		{
			1 => TableViewGridLinesVisibility.Horizontal,
			2 => TableViewGridLinesVisibility.Vertical,
			3 => TableViewGridLinesVisibility.None,
			_ => TableViewGridLinesVisibility.All,
		};
	}

	private void Density_Changed(object sender, SelectionChangedEventArgs e)
	{
		if (!_ready)
		{
			return;
		}
		Table.Density = DensityCombo.SelectedIndex switch
		{
			0 => TableViewDensity.Compact,
			2 => TableViewDensity.Comfortable,
			_ => TableViewDensity.Standard,
		};
	}

	// ---- Row banding ----
	private void RowBg_Click(object sender, RoutedEventArgs e)
		=> Table.RowBackground = new SolidColorBrush(Color.FromArgb(24, 0, 120, 215));

	private void AltBg_Click(object sender, RoutedEventArgs e)
		=> Table.AlternatingRowBackground = new SolidColorBrush(Color.FromArgb(20, 128, 128, 128));

	private void ClearBg_Click(object sender, RoutedEventArgs e)
	{
		Table.RowBackground = null;
		Table.AlternatingRowBackground = null;
	}

	// ---- Frozen / read-only / header template / empty ----
	private void Freeze_Toggle(object sender, RoutedEventArgs e)
		=> _name.FrozenEdge = (FreezeToggle.IsChecked == true) ? TableViewFrozenEdge.Leading : TableViewFrozenEdge.None;

	private void ReadOnly_Toggle(object sender, RoutedEventArgs e)
		=> Table.IsReadOnly = ReadOnlyToggle.IsChecked == true;

	private void HeaderTmpl_Toggle(object sender, RoutedEventArgs e)
		=> _score.HeaderTemplate = (HeaderTmplToggle.IsChecked == true) ? Tmpl("StarHeader") : null;

	// Reads the bound Item objects, not the cells, so a committed edit is visibly confirmed to have
	// reached the source rather than only the element that displayed it. Subscribed per item so the
	// readout tracks a write the moment the setter raises PropertyChanged.
	private void AttachSourceWatch(object? source)
	{
		foreach (var watched in _watchedItems)
		{
			watched.PropertyChanged -= OnWatchedItemChanged;
		}
		_watchedItems.Clear();

		if (source is System.Collections.IEnumerable items)
		{
			foreach (var entry in items)
			{
				if (entry is Item item)
				{
					item.PropertyChanged += OnWatchedItemChanged;
					_watchedItems.Add(item);
				}
			}
		}

		RefreshSourceDump();
	}

	private void OnWatchedItemChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
		=> RefreshSourceDump();

	private void RefreshSourceDump()
	{
		if (_watchedItems.Count == 0)
		{
			SourceDump.Text = "ItemsSource is empty.";
			return;
		}

		var sb = new System.Text.StringBuilder();
		var shown = System.Math.Min(_watchedItems.Count, 6);
		for (var i = 0; i < shown; i++)
		{
			var item = _watchedItems[i];
			sb.AppendLine(
				$"[{i}] Name='{item.Name}'  Role='{item.Role}'  City='{item.City}'  " +
				$"Score={item.Score}  " +
				$"Joined={item.Joined.ToString("d", System.Globalization.CultureInfo.CurrentCulture)}  " +
				$"Notes='{item.Notes}'");
		}

		if (_watchedItems.Count > shown)
		{
			sb.Append($"... {_watchedItems.Count - shown} more");
		}

		SourceDump.Text = sb.ToString().TrimEnd();
	}

	private void Empty_Toggle(object sender, RoutedEventArgs e)
	{
		if (EmptyToggle.IsChecked == true)
		{
			Table.ItemsSource = null;
			EmptyToggle.Content = "Load data";
		}
		else
		{
			Table.ItemsSource = Data.Make();
			EmptyToggle.Content = "Clear data";
		}

		AttachSourceWatch(Table.ItemsSource);
	}
}
