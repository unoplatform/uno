#nullable enable

using System;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Uno.Extensions;

namespace Uno.UI.RemoteControl.HotReload;

internal sealed class NullableBoolToObjectConverter : IValueConverter
{
	public object? TrueValue { get; set; }

	public object? FalseValue { get; set; }

	public object? NullValue { get; set; }

	public object? Convert(object? value, Type targetType, object parameter, string language)
		=> value switch
		{
			null => NullValue,
			true => TrueValue,
			false => FalseValue,
			_ => throw new NotSupportedException("Only nullable boolean values are supported."),
		};

	public object ConvertBack(object value, Type targetType, object parameter, string language)
		=> throw new NotSupportedException("Only one-way conversion is supported.");
}

internal sealed class NullStringToCollapsedConverter : IValueConverter
{
	public object? Convert(object? value, Type targetType, object parameter, string language)
		=> value is string s && s.IsNullOrEmpty() ? Visibility.Collapsed : Visibility.Visible;

	public object ConvertBack(object value, Type targetType, object parameter, string language)
		=> throw new NotSupportedException("Only one-way conversion is supported.");
}
