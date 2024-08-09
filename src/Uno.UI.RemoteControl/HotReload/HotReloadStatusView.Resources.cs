#nullable enable

using System;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;
using Uno.Extensions;

namespace Uno.UI.RemoteControl.HotReload;

internal sealed class EntryIconToObjectConverter : IValueConverter
{
	public object? SuccessValue { get; set; }
	public object? FailedValue { get; set; }
	public object? ConnectionSuccessValue { get; set; }
	public object? ConnectionFailedValue { get; set; }
	public object? ConnectionWarningValue { get; set; }

	public object? Convert(object? value, Type targetType, object parameter, string language)
	{
		if (value is not null)
		{
			var ei = (EntryIcon)value;

			if (ei.HasFlag(EntryIcon.Connection))
			{
				if (ei.HasFlag(EntryIcon.Success)) return ConnectionSuccessValue;
				if (ei.HasFlag(EntryIcon.Error)) return ConnectionFailedValue;
				if (ei.HasFlag(EntryIcon.Warning)) return ConnectionWarningValue;
			}
			else if (ei.HasFlag(EntryIcon.HotReload))
			{
				if (ei.HasFlag(EntryIcon.Success)) return SuccessValue;
				if (ei.HasFlag(EntryIcon.Error)) return FailedValue;
			}
		}

		return ConnectionWarningValue;
	}

	public object ConvertBack(object value, Type targetType, object parameter, string language)
		=> throw new NotSupportedException("Only one-way conversion is supported.");
}

internal sealed class NullStringToCollapsedConverter : IValueConverter
{
	public object? Convert(object? value, Type targetType, object parameter, string language)
	{
		if (value is string s && !string.IsNullOrEmpty(s))
		{
			return Visibility.Visible;
		}

		return Visibility.Collapsed;
	}

	public object ConvertBack(object value, Type targetType, object parameter, string language)
		=> throw new NotSupportedException("Only one-way conversion is supported.");
}
