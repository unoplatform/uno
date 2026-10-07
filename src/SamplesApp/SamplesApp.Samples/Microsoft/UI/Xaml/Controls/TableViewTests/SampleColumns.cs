// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference Samples\TableViewSampleApp\SampleColumns.cs, tag winui3/release/2.5.4-experimental, commit 7b127093475

#nullable enable

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Controls.Tabular;

namespace MUXControlsTestApp;

// Small factory used by the column-width sample pages so each page only has to declare the
// header / bound property / width for its columns. Centralizes the Tabular type aliases.
internal static class SampleColumns
{
	// A text column bound one-way to an Item property, with the given width.
	public static TableViewTextColumn Text(string header, string propertyPath, GridLength width) =>
		new TableViewTextColumn
		{
			Header = header,
			Binding = new Binding { Path = new PropertyPath(propertyPath) },
			Width = width,
		};

	// A template column using a DataTemplate declared in App.xaml resources, with the given width.
	// TODO Uno: resolved from TableViewSampleResources rather than Application.Current.Resources.
	// CellTemplate = (DataTemplate)Application.Current.Resources[templateKey],
	public static TableViewTemplateColumn Template(string header, string templateKey, GridLength width) =>
		new TableViewTemplateColumn
		{
			Header = header,
			CellTemplate = TableViewSampleResources.Template(templateKey),
			Width = width,
		};

	// Convenience width factories.
	public static GridLength Auto() => new GridLength(1, GridUnitType.Auto);
	public static GridLength Star(double factor = 1) => new GridLength(factor, GridUnitType.Star);
	public static GridLength Pixels(double px) => new GridLength(px, GridUnitType.Pixel);
}
