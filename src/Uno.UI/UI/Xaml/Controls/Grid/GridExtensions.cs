using Windows.UI.Xaml.Controls;

using View = Windows.UI.Xaml.UIElement;

namespace Uno.UI.Controls.Legacy
{
	internal static class GridExtensions
	{
		/// <summary>
		/// Sets the row for the specified control, when included in a Grid control.
		/// </summary>
		/// <param name="row">The row to be set for the control</param>
		/// <returns>The view to be used in a fluent expression.</returns>
		public static T GridRow<T>(this T view, int row)
			where T : View
		{
			Grid.SetRow(view, row);

			return view;
		}

		/// <summary>
		/// Sets the row for the specified control, when included in a Grid control.
		/// </summary>
		/// <param name="rowSpan">The row to be set for the control</param>
		/// <returns>The view to be used in a fluent expression.</returns>
		public static T GridRowSpan<T>(this T view, int rowSpan)
			where T : View
		{
			Grid.SetRowSpan(view, rowSpan);

			return view;
		}

		/// <summary>
		/// Sets the column for the specified control, when included in a Grid control.
		/// </summary>
		/// <param name="column">The column to be set for the control</param>
		/// <returns>The view to be used in a fluent expression.</returns>
		public static T GridColumn<T>(this T view, int column)
			where T : View
		{
			Grid.SetColumn(view, column);

			return view;
		}


		/// <summary>
		/// Sets the column span for the specified control, when included in a Grid control.
		/// </summary>
		/// <param name="columnSpan">The column to be set for the control</param>
		/// <returns>The view to be used in a fluent expression.</returns>
		public static T GridColumnSpan<T>(this T view, int columnSpan)
			where T : View
		{
			Grid.SetColumnSpan(view, columnSpan);

			return view;
		}

		/// <summary>
		/// Sets the column and row for the specified control, when included in a Grid control.
		/// </summary>
		/// <param name="column">The column to be set for the control</param>
		/// <returns>The view to be used in a fluent expression.</returns>
		public static T GridPosition<T>(this T view, int row, int column)
			where T : View
		{
			Grid.SetColumn(view, column);
			Grid.SetRow(view, row);

			return view;
		}
	}
}
