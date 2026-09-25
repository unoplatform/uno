using System;
using System.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Data;

public sealed partial class BindingExpression_With_Converter : Page, INotifyPropertyChanged
{
	private TestItem _item1;

	public TestItem Item1
	{
		get => _item1;
		set
		{
			_item1 = value;
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Item1)));
		}
	}

	private TestItem _item2;

	public TestItem Item2
	{
		get => _item2;
		set
		{
			_item2 = value;
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Item2)));
		}
	}

	public event PropertyChangedEventHandler PropertyChanged;

	public BindingExpression_With_Converter()
	{
		this.InitializeComponent();
	}
}

public class BoolToVisibilityConverter : IValueConverter
{
	public static bool CanReturnNull { get; set; }
	public static bool ReceivedNull
	{
		get;
		set;
	}
	public object Convert(object value, Type targetType, object parameter, string language)
	{
		if (value is null)
		{

		}
		ReceivedNull |= value is null;
		return value?.ToString() ?? (CanReturnNull ? null : "convertervalue");
	}

	public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
}

public class BlankItem
{

}

public class TestItem : INotifyPropertyChanged
{
	private NestedItem _outer = new NestedItem();
	public NestedItem Outer
	{
		get => _outer;
		set
		{
			_outer = value;
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Outer)));
		}
	}

	public event PropertyChangedEventHandler PropertyChanged;
}

public class NestedItem : INotifyPropertyChanged
{
	private string _inner = "default";
	public string Inner
	{
		get => _inner;
		set
		{
			_inner = value;
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Inner)));
		}
	}

	public event PropertyChangedEventHandler PropertyChanged;
}


public partial class MyElement : UserControl
{
	public static DependencyProperty PropProperty { get; } =
		DependencyProperty.Register(
			nameof(Prop),
			typeof(string),
			typeof(MyElement),
			new PropertyMetadata(null));

	public string Prop
	{
		get => (string)GetValue(PropProperty);
		set => SetValue(PropProperty, value);
	}
}
