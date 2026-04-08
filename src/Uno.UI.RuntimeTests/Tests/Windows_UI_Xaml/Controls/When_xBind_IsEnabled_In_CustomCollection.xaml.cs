using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml.Controls;

/// <summary>
/// Custom container control with an Items collection property.
/// Used to reproduce issue #11815 where x:Bind on IsEnabled
/// doesn't work for controls inside a custom collection.
/// </summary>
[ContentProperty(Name = nameof(Items))]
public partial class TestToolbar : Control
{
	public ObservableCollection<Control> Items { get; } = new();

	public TestToolbar()
	{
		DefaultStyleKey = typeof(TestToolbar);
	}
}

public partial class IsEnabledTestViewModel : INotifyPropertyChanged
{
	private bool _isEnabled = true;

	public bool IsEnabled
	{
		get => _isEnabled;
		set
		{
			if (_isEnabled != value)
			{
				_isEnabled = value;
				OnPropertyChanged();
			}
		}
	}

	public event PropertyChangedEventHandler PropertyChanged;

	protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
	{
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}
}

public sealed partial class When_xBind_IsEnabled_In_CustomCollection : Page
{
	public IsEnabledTestViewModel ViewModel { get; } = new();

	public When_xBind_IsEnabled_In_CustomCollection()
	{
		this.InitializeComponent();
	}
}
