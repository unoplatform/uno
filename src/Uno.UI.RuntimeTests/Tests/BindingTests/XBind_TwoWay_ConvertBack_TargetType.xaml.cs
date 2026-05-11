#nullable enable

using System;
using System.ComponentModel;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;

namespace Uno.UI.RuntimeTests.Tests;

public sealed partial class XBind_TwoWay_ConvertBack_TargetType : Page
{
	public XBindConvertBackViewModel ViewModel { get; } = new();

	public XBind_TwoWay_ConvertBack_TargetType()
	{
		this.InitializeComponent();
	}
}

public class XBindConvertBackViewModel : INotifyPropertyChanged
{
	private string? _stringValue = "initial";
	private int _intValue = 42;

	public event PropertyChangedEventHandler? PropertyChanged;

	public string? StringValue
	{
		get => _stringValue;
		set
		{
			_stringValue = value;
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(StringValue)));
		}
	}

	public int IntValue
	{
		get => _intValue;
		set
		{
			_intValue = value;
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IntValue)));
		}
	}
}

public class RecordingConverter : IValueConverter
{
	public Type? LastConvertTargetType { get; private set; }
	public Type? LastConvertBackTargetType { get; private set; }
	public object? LastConvertBackValue { get; private set; }

	public object? Convert(object? value, Type targetType, object parameter, string language)
	{
		LastConvertTargetType = targetType;
		return value?.ToString() ?? string.Empty;
	}

	public object? ConvertBack(object? value, Type targetType, object parameter, string language)
	{
		LastConvertBackTargetType = targetType;
		LastConvertBackValue = value;

		if (targetType == typeof(int) && value is string s && int.TryParse(s, out var i))
		{
			return i;
		}

		return value;
	}
}
