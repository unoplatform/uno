using System;
using System.ComponentModel;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Data;

public enum XBindTestEnum
{
	Hello,
	World
}

public class XBindEnumToStringConverter : IValueConverter
{
	public Type LastConvertTargetType { get; private set; }

	public Type LastConvertBackTargetType { get; private set; }

	public object Convert(object value, Type targetType, object parameter, string language)
	{
		LastConvertTargetType = targetType;
		return value?.ToString();
	}

	public object ConvertBack(object value, Type targetType, object parameter, string language)
	{
		LastConvertBackTargetType = targetType;

		// Native WinUI's classic {Binding} passes Object as targetType, so fall back to the known enum.
		var enumType = Nullable.GetUnderlyingType(targetType ?? typeof(object)) ?? targetType;
		return value is string s
			? Enum.Parse(enumType is { IsEnum: true } ? enumType : typeof(XBindTestEnum), s)
			: value;
	}
}

public sealed partial class When_XBind_TwoWay_Enum_Converter : Page, INotifyPropertyChanged
{
	private XBindTestEnum _enumValue = XBindTestEnum.Hello;
	private XBindTestEnum? _nullableEnumValue = XBindTestEnum.Hello;

	public When_XBind_TwoWay_Enum_Converter()
	{
		this.InitializeComponent();
	}

	public event PropertyChangedEventHandler PropertyChanged;

	public XBindTestEnum EnumValue
	{
		get => _enumValue;
		set
		{
			if (_enumValue != value)
			{
				_enumValue = value;
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(EnumValue)));
			}
		}
	}

	public XBindTestEnum? NullableEnumValue
	{
		get => _nullableEnumValue;
		set
		{
			if (_nullableEnumValue != value)
			{
				_nullableEnumValue = value;
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(NullableEnumValue)));
			}
		}
	}

	public XBindEnumToStringConverter EnumConverter => (XBindEnumToStringConverter)Resources["EnumConverter"];

	public XBindEnumToStringConverter NullableEnumConverter => (XBindEnumToStringConverter)Resources["NullableEnumConverter"];
}
