// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\Common\GridCoordinateHelper.h, tag winui3/release/2.5.4-experimental, commit 7b127093475

#nullable enable

using Microsoft.UI.Xaml.Input;

namespace Microsoft.UI.Xaml.Controls;

// Internal C++ helper: row/column <-> flat-index + focus-navigation math used by
// TableView's keyboard navigation. Not projected (no public runtimeclass).
// TODO Uno: a C++ value type used as a stack local, so it ports as a struct. GridCoordinateHelper() = default
// is the implicit parameterless struct constructor; GridCoordinateHelper(int, int) is in GridCoordinateHelper.mux.cs.
internal partial struct GridCoordinateHelper
{
	public int RowCount() => m_rowCount;
	public int ColumnCount() => m_columnCount;

	public partial void Resize(int rowCount, int columnCount);

	public partial bool IsValidCellAddress(int row, int column);

	public partial int GetFlatIndex(int row, int column);

	public partial bool TryGetCellAddress(int flatIndex, out int row, out int column);

	public partial bool TryGetNextFocusableCell(
		int currentRow,
		int currentColumn,
		FocusNavigationDirection direction,
		bool wrap,
		out int nextRow,
		out int nextColumn);

	private int m_rowCount;
	private int m_columnCount;
}
