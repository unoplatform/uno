// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\Generated\TableViewTemplateColumn.properties.cpp, tag winui3/release/2.5.4-experimental, commit 7b127093475

#nullable enable

namespace Microsoft.UI.Xaml.Controls.Tabular;

partial class TableViewTemplateColumn
{
	/// <summary>
	/// Template used to generate display cell content.
	/// </summary>
	public DataTemplate? CellTemplate
	{
		get => (DataTemplate?)GetValue(CellTemplateProperty);
		set => SetValue(CellTemplateProperty, value);
	}

	/// <summary>
	/// Identifies the CellTemplate dependency property.
	/// </summary>
	public static DependencyProperty CellTemplateProperty { get; } =
		DependencyProperty.Register(
			nameof(CellTemplate),
			typeof(DataTemplate),
			typeof(TableViewTemplateColumn),
			new FrameworkPropertyMetadata(default(DataTemplate), OnPropertyChanged));

	private static void OnPropertyChanged(
		DependencyObject sender,
		DependencyPropertyChangedEventArgs args)
	{
		var owner = (TableViewTemplateColumn)sender;
		owner.OnPropertyChanged(args);
	}
}
