using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;

namespace Windows.Foundation;

[EditorBrowsable(EditorBrowsableState.Never)]
internal class SizeConverter : TypeConverter
{
#if NETSTANDARD
	private static readonly char[] _commaArray = new[] { ',' };
#endif
#if !NETSTANDARD
	[DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(Size))]
#endif
	public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
	{
		var canConvert = sourceType == typeof(string);

		if (canConvert)
		{
			return true;
		}

		return base.CanConvertFrom(context, sourceType);
	}

#if !NETSTANDARD
	[DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(Size))]
#endif
	public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
	{
		var stringValue = value as string;

		if (stringValue != null)
		{
#if NETSTANDARD
			var values = stringValue
				.Split(_commaArray)
				.Select(s => double.Parse(s, CultureInfo.InvariantCulture))
				.ToArray();

			if (values.Length == 2)
			{
				return new Size(values[0], values[1]);
			}
#else
			// This is equivalent to the above code, but is more efficient.
			// On .NET Standard (for source generators), there is no double.Parse overload that accepts ReadOnlySpan<char>
			var firstIndexOfComma = stringValue.IndexOf(',');
			if (firstIndexOfComma != -1)
			{
				var lastIndexOfComma = stringValue.LastIndexOf(',');
				if (firstIndexOfComma == lastIndexOfComma)
				{
					var span = stringValue.AsSpan();
					var width = double.Parse(span.Slice(0, firstIndexOfComma), CultureInfo.InvariantCulture);
					var height = double.Parse(span.Slice(start: firstIndexOfComma + 1), CultureInfo.InvariantCulture);
					return new Size(width, height);
				}
			}
#endif
		}

		return base.ConvertFrom(context, culture, value);
	}
}
