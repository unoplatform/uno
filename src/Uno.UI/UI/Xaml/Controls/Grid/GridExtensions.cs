#if !SILVERLIGHT
using System;
using System.Collections.Generic;
using System.Text;

#if XAMARIN_ANDROID
using View = Android.Views.View;
using Font = Android.Graphics.Typeface;
using Android.Graphics;
#elif XAMARIN_IOS_UNIFIED
using View = UIKit.UIView;
using Color = UIKit.UIColor;
using Font = UIKit.UIFont;
#elif XAMARIN_IOS
using View = MonoTouch.UIKit.UIView;
using Color = MonoTouch.UIKit.UIColor;
using Font = MonoTouch.UIKit.UIFont;
#elif NETFX_CORE
using View = Windows.UI.Xaml.FrameworkElement;
using Windows.UI.Xaml.Controls;
#else
using View = Windows.UI.Xaml.UIElement;
#endif

namespace Windows.UI.Xaml.Controls
{
	public static class GridExtensions
	{
		public static Grid ColumnDefinitions(this Grid grid, params string[] definitions)
		{
			foreach (var def in definitions)
			{
				grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = (GridLength)def });
			}

			return grid;
		}

		public static Grid RowDefinitions(this Grid grid, params string[] definitions)
		{
			foreach (var def in definitions)
			{
				grid.RowDefinitions.Add(new RowDefinition() { Height = (GridLength)def });
			}

			return grid;
		}

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
#endif
