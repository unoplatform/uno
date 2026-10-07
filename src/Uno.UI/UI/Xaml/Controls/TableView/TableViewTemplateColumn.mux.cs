// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\TableView\TableViewTemplateColumn.cpp, tag winui3/release/2.5.4-experimental, commit 7b127093475

#nullable enable

namespace Microsoft.UI.Xaml.Controls.Tabular;

partial class TableViewTemplateColumn
{
	/// <summary>
	/// Initializes a new instance of the <see cref="TableViewTemplateColumn"/> class.
	/// </summary>
	public TableViewTemplateColumn()
	{
	}

	/// <inheritdoc />
	protected override FrameworkElement? GenerateElementCore(object? dataItem)
	{
		// Use a presenter so templates bind against the row data item.
		ContentPresenter presenter = new();
		presenter.HorizontalAlignment = HorizontalAlignment.Stretch;
		presenter.VerticalAlignment = VerticalAlignment.Stretch;

		// Only wire content when a template exists; otherwise leave the presenter empty instead of
		// rendering ToString().
		if (CellTemplate is { } cellTemplate)
		{
			presenter.ContentTemplate = cellTemplate;
			// Content is wired by TableViewRow::RebuildCells to a binding against the cell WRAPPER's
			// inherited DataContext, so recycled rows update reactively. It deliberately is NOT bound to
			// the presenter's own DataContext: ContentPresenter pins its DataContext to its Content, so a
			// self-referential Content binding would freeze after the first item (stale cells on recycle).
		}

		return presenter;
	}

	// TODO Uno: `new` because the C++ OnPropertyChanged is non-virtual and hides the base one; the
	// TableViewColumn DP callbacks keep dispatching to the base method, as in C++.
	private new void OnPropertyChanged(DependencyPropertyChangedEventArgs args)
	{
		if (args.Property == CellTemplateProperty)
		{
			// Realized cells were generated from the previous template.
			NotifyCellContentChanged();
			return;
		}

		base.OnPropertyChanged(args);
	}
}
