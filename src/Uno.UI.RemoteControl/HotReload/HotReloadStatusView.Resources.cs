#nullable enable

using System;
using System.Linq;
using Windows.UI.Xaml.Data;

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
