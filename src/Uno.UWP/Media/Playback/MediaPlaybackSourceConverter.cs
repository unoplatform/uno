using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Windows.Media.Core;

namespace Windows.Media.Playback
{
	public partial class MediaPlaybackSourceConverter : TypeConverter
	{
		public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
		{
			return sourceType == typeof(string) ||
				 sourceType == typeof(Uri) ||
				 base.CanConvertFrom(context, sourceType);
		}
		public override bool CanConvertTo(ITypeDescriptorContext? context, [NotNullWhen(true)] Type? sourceType)
		{
			return sourceType is not null && (sourceType == typeof(MediaSource) || base.CanConvertFrom(context, sourceType));
		}

		public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
		{
			if (value is string stringValue && !string.IsNullOrWhiteSpace(stringValue))
			{
				return MediaSource.CreateFromUri(new Uri(stringValue));
			}

			if (value is Uri uriValue)
			{
				return MediaSource.CreateFromUri(uriValue);
			}

			return base.ConvertFrom(context, culture, value);
		}

		public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType)
		{
			if (value is MediaSource source)
			{
				if (destinationType == typeof(string))
				{
					return source.Uri.ToString();
				}

				if (destinationType == typeof(Uri))
				{
					return source.Uri;
				}
			}
			return base.ConvertTo(context, culture, value, destinationType);
		}
	}
}
