using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using Uno.UI.Converters;
using Windows.UI.Xaml;

namespace Uno.UI.Tests.Converters
{
	/// <summary>
	/// This converter will output a visibility based on if a nullable bool is set to true or otherwise.
	/// 
	/// VisibilityIfTrue (VisibilityIfTrue) : Determines the visibility value that will be returned if the value is true.
	/// 
	/// By default, VisibilityIfTrue is set to visible.
	/// 
	/// This converter may be used to display or hide some content based on a nullable boolean value.
	/// </summary>
	public class FromNullableBoolToVisibilityConverter : ConverterBase
	{
		public FromNullableBoolToVisibilityConverter()
		{
			this.VisibilityIfTrue = Converters.VisibilityIfTrue.Visible;
		}

		public VisibilityIfTrue VisibilityIfTrue { get; set; }

		[SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", Justification = "Not for end user")]
		protected override object Convert(object value, Type targetType, object parameter)
		{
			if (parameter != null)
			{
				throw new ArgumentException($"This converter does not use any parameters. You should remove \"{parameter}\" passed as parameter.");
			}

			bool inverse = this.VisibilityIfTrue == VisibilityIfTrue.Collapsed;

			Visibility visibilityOnTrue = (!inverse) ? Visibility.Visible : Visibility.Collapsed;
			Visibility visibilityOnFalse = (!inverse) ? Visibility.Collapsed : Visibility.Visible;

			if (value != null && !(value is bool))
			{
				throw new ArgumentException($"Value must either be null or of type bool. Got {value} ({value.GetType().FullName})");
			}

			var valueToConvert = value != null && System.Convert.ToBoolean(value, CultureInfo.InvariantCulture);

			return valueToConvert ? visibilityOnTrue : visibilityOnFalse;
		}

		[SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", Justification = "Not for end user")]
		protected override object ConvertBack(object value, Type targetType, object parameter)
		{
			if (value == null)
			{
				return null;
			}

			if (parameter != null)
			{
				throw new ArgumentException($"This converter does not use any parameters. You should remove \"{parameter}\" passed as parameter.");
			}

			bool inverse = this.VisibilityIfTrue == VisibilityIfTrue.Collapsed;

			Visibility visibilityOnTrue = (!inverse) ? Visibility.Visible : Visibility.Collapsed;

			var visibility = (Visibility)value;

			return visibilityOnTrue.Equals(visibility);
		}
	}

	public enum VisibilityIfTrue
	{
		Visible,
		Collapsed
	}
}
