#nullable enable

using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Uno.UI.RuntimeTests.Tests_CSharpExpressions.ViewModels;

public sealed class ExpressionsTestViewModel : INotifyPropertyChanged
{
	private string _firstName = "Ada";
	private string _lastName = "Lovelace";
	private bool _isVip;
	private string? _nickname;
	private decimal _price = 9.99m;
	private int _quantity = 1;
	private decimal _balance = 1234.5m;
	private int _a = 2;
	private int _b = 3;
	private int _count;
	private bool _isEnabled = true;
	private int _counter;
	private decimal _taxRate = 0.2m;
	private AddressViewModel _user = new() { Address = new AddressInfo { City = "London" } };

	public string FirstName { get => _firstName; set => Set(ref _firstName, value); }
	public string LastName { get => _lastName; set => Set(ref _lastName, value); }
	public bool IsVip { get => _isVip; set => Set(ref _isVip, value); }
	public string? Nickname { get => _nickname; set => Set(ref _nickname, value); }
	public decimal Price { get => _price; set => Set(ref _price, value); }
	public int Quantity { get => _quantity; set => Set(ref _quantity, value); }
	public decimal Balance { get => _balance; set => Set(ref _balance, value); }
	public int A { get => _a; set => Set(ref _a, value); }
	public int B { get => _b; set => Set(ref _b, value); }
	public int Count { get => _count; set => Set(ref _count, value); }
	public bool IsEnabled { get => _isEnabled; set => Set(ref _isEnabled, value); }
	public int Counter { get => _counter; set => Set(ref _counter, value); }
	public decimal TaxRate { get => _taxRate; set => Set(ref _taxRate, value); }
	public AddressViewModel User { get => _user; set => Set(ref _user, value); }

	public event PropertyChangedEventHandler? PropertyChanged;

	private void Set<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
	{
		if (!EqualityComparer<T>.Default.Equals(field, value))
		{
			field = value;
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}

public sealed class AddressViewModel : INotifyPropertyChanged
{
	private AddressInfo _address = new();

	public AddressInfo Address
	{
		get => _address;
		set
		{
			if (!ReferenceEquals(_address, value))
			{
				_address = value;
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Address)));
			}
		}
	}

	public event PropertyChangedEventHandler? PropertyChanged;
}

public sealed class AddressInfo : INotifyPropertyChanged
{
	private string _city = "";

	public string City
	{
		get => _city;
		set
		{
			if (_city != value)
			{
				_city = value;
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(City)));
			}
		}
	}

	public event PropertyChangedEventHandler? PropertyChanged;
}
