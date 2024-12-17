using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Windows.UI.Xaml.Data;

namespace Uno.UI.Samples.Converters
{
	// FromBoolToObjectConverter and FromBoolToValueConverter do not share code to avoid using virtual methods (perf).

	public abstract class FromBoolToObjectConverter<T> : IValueConverter
		where T : class
	{
		public T NullValue { get; set; }

		public T FalseValue { get; set; }

		public T TrueValue { get; set; }

		public T NullOrFalseValue
		{
			get => FalseValue;
			set => FalseValue = NullValue = value;
		}

		public T NullOrTrueValue
		{
			get => TrueValue;
			set => TrueValue = NullValue = value;
		}

		public object Convert(object value, Type targetType, object parameter, string language)
		{
			if (value == null)
			{
				return NullValue;
			}

			if (System.Convert.ToBoolean(value, CultureInfo.InvariantCulture))
			{
				return TrueValue;
			}
			else
			{
				return FalseValue;
			}
		}

		public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
	}
}
