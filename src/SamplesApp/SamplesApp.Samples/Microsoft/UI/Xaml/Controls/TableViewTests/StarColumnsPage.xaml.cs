// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference Samples\TableViewSampleApp\StarColumnsPage.xaml.cs, tag winui3/release/2.5.4-experimental, commit 7b127093475

#nullable enable

using Microsoft.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;

namespace MUXControlsTestApp;

// Table where every column is GridUnitType.Star — columns split the viewport width by their factors.
[Sample("TableView", Name = "TableView_StarColumns")]
public sealed partial class StarColumnsPage : Page
{
	public StarColumnsPage()
	{
		this.InitializeComponent();

		Table.Columns.Add(SampleColumns.Text("Name", nameof(Item.Name), SampleColumns.Star(1)));
		Table.Columns.Add(SampleColumns.Text("Role", nameof(Item.Role), SampleColumns.Star(1)));
		Table.Columns.Add(SampleColumns.Text("City", nameof(Item.City), SampleColumns.Star(2)));
		Table.Columns.Add(SampleColumns.Text("Bio", nameof(Item.Bio), SampleColumns.Star(3)));

		Table.ItemsSource = Data.Make();
	}
}
