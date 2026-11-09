// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference Samples\TableViewSampleApp\AutoColumnsPage.xaml.cs, tag winui3/release/2.5.4-experimental, commit 7b127093475

#nullable enable
#pragma warning disable CS8305 // TableView is [Experimental]

using Microsoft.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;

namespace MUXControlsTestApp;

// Table where every column is GridUnitType.Auto — each column sizes to the widest realized cell.
[Sample("TableView", Name = "TableView_AutoColumns")]
public sealed partial class AutoColumnsPage : Page
{
	public AutoColumnsPage()
	{
		this.InitializeComponent();

		Table.Columns.Add(SampleColumns.Text("Name", nameof(Item.Name), SampleColumns.Auto()));
		Table.Columns.Add(SampleColumns.Text("Role", nameof(Item.Role), SampleColumns.Auto()));
		Table.Columns.Add(SampleColumns.Text("City", nameof(Item.City), SampleColumns.Auto()));
		Table.Columns.Add(SampleColumns.Text("Score", nameof(Item.Score), SampleColumns.Auto()));
		// Editable multi-line TextBox: type a longer line to grow the Auto column width, add lines
		// (Enter) to grow the Auto row height -- exercises both column and row resizing live.
		Table.Columns.Add(SampleColumns.Template("Notes (edit)", "AutoEditCell", SampleColumns.Auto()));

		Table.ItemsSource = Data.Make();
	}
}
