// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference Samples\TableViewSampleApp\PixelColumnsPage.xaml.cs, tag winui3/release/2.5.4-experimental, commit 7b127093475

#nullable enable

using Microsoft.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;

namespace MUXControlsTestApp;

// Table where every column has a fixed GridUnitType.Pixel width (content-independent).
[Sample("TableView", Name = "TableView_PixelColumns")]
public sealed partial class PixelColumnsPage : Page
{
	public PixelColumnsPage()
	{
		this.InitializeComponent();

		Table.Columns.Add(SampleColumns.Text("Name", nameof(Item.Name), SampleColumns.Pixels(220)));
		Table.Columns.Add(SampleColumns.Text("Role", nameof(Item.Role), SampleColumns.Pixels(120)));
		Table.Columns.Add(SampleColumns.Text("City", nameof(Item.City), SampleColumns.Pixels(160)));
		Table.Columns.Add(SampleColumns.Template("Score", "ScoreCell", SampleColumns.Pixels(140)));
		Table.Columns.Add(SampleColumns.Text("Notes", nameof(Item.Notes), SampleColumns.Pixels(200)));

		Table.ItemsSource = Data.Make();
	}
}
