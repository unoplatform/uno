namespace Windows.UI.Xaml.Automation;

/// <summary>
/// Specifies whether data in a table should be read primarily by row or by column.
/// </summary>
public enum RowOrColumnMajor
{
	/// <summary>
	/// Data in the table should be read row by row.
	/// </summary>
	RowMajor = 0,

	/// <summary>
	/// Data in the table should be read column by column.
	/// </summary>
	ColumnMajor = 1,

	/// <summary>
	/// The best way to present the data is indeterminate.
	/// </summary>
	Indeterminate = 2,
}
