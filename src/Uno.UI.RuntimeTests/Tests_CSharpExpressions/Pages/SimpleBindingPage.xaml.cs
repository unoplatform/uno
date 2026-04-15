#nullable enable

using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml.Controls;

namespace Uno.UI.RuntimeTests.Tests_CSharpExpressions.Pages;

public sealed partial class SimpleBindingPage : Page, INotifyPropertyChanged
{
	private string _firstName = "Ada";
	private string _lastName = "Lovelace";

	public SimpleBindingPage()
	{
		InitializeComponent();
	}

	public string FirstName
	{
		get => _firstName;
		set
		{
			if (_firstName != value)
			{
				_firstName = value;
				OnPropertyChanged();
				OnPropertyChanged(nameof(FullName));
			}
		}
	}

	public string LastName
	{
		get => _lastName;
		set
		{
			if (_lastName != value)
			{
				_lastName = value;
				OnPropertyChanged();
				OnPropertyChanged(nameof(FullName));
			}
		}
	}

	public string FullName => _firstName + " " + _lastName;

	public string WindowTitle => "Captured Once";

	public event PropertyChangedEventHandler? PropertyChanged;

	private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
		=> PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
