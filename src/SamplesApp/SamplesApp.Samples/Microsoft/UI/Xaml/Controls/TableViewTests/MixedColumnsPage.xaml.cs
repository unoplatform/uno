// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference Samples\TableViewSampleApp\MixedColumnsPage.xaml.cs, tag winui3/release/2.5.4-experimental, commit 7b127093475

#nullable enable

using Microsoft.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;

namespace MUXControlsTestApp;

// Table mixing Auto, Pixel and Star column widths (plus a Template column).
[Sample("TableView", Name = "TableView_MixedColumns")]
public sealed partial class MixedColumnsPage : Page
{
	public MixedColumnsPage()
	{
		this.InitializeComponent();

		Table.Columns.Add(SampleColumns.Text("Name", nameof(Item.Name), SampleColumns.Auto()));
		Table.Columns.Add(SampleColumns.Text("Role", nameof(Item.Role), SampleColumns.Pixels(120)));
		Table.Columns.Add(SampleColumns.Template("Score", "ScoreCell", SampleColumns.Pixels(120)));
		Table.Columns.Add(SampleColumns.Text("City", nameof(Item.City), SampleColumns.Star(1)));
		Table.Columns.Add(SampleColumns.Text("Bio", nameof(Item.Bio), SampleColumns.Star(2)));

		Table.ItemsSource = Data.Make();
	}
}
