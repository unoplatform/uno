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
	public Type LastConvertBackTargetType { get; private set; }

	public object Convert(object value, Type targetType, object parameter, string language)
	{
		if (value is Enum e)
		{
			return e.ToString();
		}
		return value?.ToString();
	}

	public object ConvertBack(object value, Type targetType, object parameter, string language)
	{
		LastConvertBackTargetType = targetType;

		if (value is string s && targetType != null && targetType.IsEnum)
		{
			return Enum.Parse(targetType, s);
		}

		return value;
	}
}

public sealed partial class When_XBind_TwoWay_Enum_Converter : Page, INotifyPropertyChanged
{
	private XBindTestEnum _enumValue = XBindTestEnum.Hello;

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

	public XBindEnumToStringConverter Converter =>
		(XBindEnumToStringConverter)Resources["EnumConverter"];

	public event PropertyChangedEventHandler PropertyChanged;

	public When_XBind_TwoWay_Enum_Converter()
	{
		this.InitializeComponent();
	}
}
