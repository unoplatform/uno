#nullable enable

using System.ComponentModel;
using Microsoft.UI.Xaml.Controls;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Data;

[Bindable(true)]
public sealed partial class BindableNullableValueTypeTestPage : Page, INotifyPropertyChanged
{
	private int? _myProperty;

	public BindableNullableValueTypeTestPage()
	{
		this.InitializeComponent();
		this.DataContext = this;
	}

	public event PropertyChangedEventHandler? PropertyChanged;

	public int? MyProperty
	{
		get => _myProperty;
		set
		{
			if (_myProperty != value)
			{
				_myProperty = value;
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(MyProperty)));
			}
		}
	}

#if !WINAPPSDK // https://github.com/microsoft/microsoft-ui-xaml/issues/5315
	public int MyInitOnlyProperty { get; init; }
#endif
}
