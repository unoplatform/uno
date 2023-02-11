using System;
using System.Globalization;
using System.Linq;
#if WPF_APP
using System.Windows;
using System.Windows.Controls;
#else
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
#endif

#nullable enable

namespace XamlGenerationTests.Dedoose.Extensions
{
	public static class GridExtensions
	{
		#region ColumnsAndRows

		public static readonly DependencyProperty ColumnsAndRowsProperty =
			DependencyProperty.RegisterAttached(
				nameof(ColumnsAndRowsProperty).Replace("Property", string.Empty),
				typeof(string),
				typeof(GridExtensions),
				new PropertyMetadata(string.Empty, OnColumnsAndRowsChanged));

		public static string? GetColumnsAndRows(DependencyObject element)
		{
			return (string?)element.GetValue(ColumnsAndRowsProperty);
		}

		public static void SetColumnsAndRows(DependencyObject element, string? value)
		{
			element.SetValue(ColumnsAndRowsProperty, value);
		}

		private static void OnColumnsAndRowsChanged(
			DependencyObject element,
			DependencyPropertyChangedEventArgs args)
		{
			var grid = (Grid)element;
			var columnsAndRows = (string)args.NewValue;

			grid.ColumnDefinitions.Clear();
			grid.RowDefinitions.Clear();

			if (string.IsNullOrWhiteSpace(columnsAndRows))
			{
				return;
			}

			var values = columnsAndRows.Split(';');
			foreach (var value in (values.ElementAtOrDefault(0) ?? "*").Split(','))
			{
				grid.ColumnDefinitions.Add(new ColumnDefinition
				{
					Width = GridLengthConverter.ConvertFromInvariantString(value),
				});
			}
			foreach (var value in (values.ElementAtOrDefault(1) ?? "*").Split(','))
			{
				grid.RowDefinitions.Add(new RowDefinition
				{
					Height = GridLengthConverter.ConvertFromInvariantString(value),
				});
			}
		}

		#endregion

		#region Columns

		public static readonly DependencyProperty ColumnsProperty =
			DependencyProperty.RegisterAttached(
				nameof(ColumnsProperty).Replace("Property", string.Empty),
				typeof(string),
				typeof(GridExtensions),
				new PropertyMetadata(string.Empty, OnColumnsChanged));

		public static string? GetColumns(DependencyObject element)
		{
			return (string?)element.GetValue(ColumnsProperty);
		}

		public static void SetColumns(DependencyObject element, string? value)
		{
			element.SetValue(ColumnsProperty, value);
		}

		private static void OnColumnsChanged(
			DependencyObject element,
			DependencyPropertyChangedEventArgs args)
		{
			var grid = (Grid)element;
			var columns = (string)args.NewValue;

			element.SetValue(ColumnsAndRowsProperty, $"{columns};*");
		}

		#endregion

		#region Rows

		public static readonly DependencyProperty RowsProperty =
			DependencyProperty.RegisterAttached(
				nameof(RowsProperty).Replace("Property", string.Empty),
				typeof(string),
				typeof(GridExtensions),
				new PropertyMetadata(string.Empty, OnRowsChanged));

		public static string? GetRows(DependencyObject element)
		{
			return (string?)element.GetValue(RowsProperty);
		}

		public static void SetRows(DependencyObject element, string? value)
		{
			element.SetValue(RowsProperty, value);
		}

		private static void OnRowsChanged(
			DependencyObject element,
			DependencyPropertyChangedEventArgs args)
		{
			var rows = (string)args.NewValue;
			element.SetValue(ColumnsAndRowsProperty, $"*;{rows}");
		}

		#endregion

		#region GridLengthConverter

		/// <summary>
		/// https://referencesource.microsoft.com/#PresentationFramework/src/Framework/System/Windows/GridLengthConverter.cs
		/// </summary>
		private class GridLengthConverter
		{
			public static GridLength ConvertFromInvariantString(string text)
			{
				if (string.IsNullOrWhiteSpace(text))
				{
					return new GridLength(1, GridUnitType.Star);
				}
				if (text.ToUpperInvariant() == "AUTO")
				{
					return GridLength.Auto;
				}

				if (text.Contains("*"))
				{
					var value = text.Replace("*", string.Empty);

					return new GridLength(
						string.IsNullOrWhiteSpace(value)
							? 1
							: Convert.ToDouble(value, CultureInfo.InvariantCulture),
						GridUnitType.Star);
				}

				return new GridLength(Convert.ToDouble(text, CultureInfo.InvariantCulture));
			}
		}

		#endregion
	}
}
