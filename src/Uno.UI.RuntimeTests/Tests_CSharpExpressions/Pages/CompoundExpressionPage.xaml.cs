#nullable enable

using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml.Controls;

namespace Uno.UI.RuntimeTests.Tests_CSharpExpressions.Pages;

public partial class CompoundExpressionPage : Page, INotifyPropertyChanged
{
	private decimal _price = 9.99m;
	private int _quantity = 2;
	private decimal _taxRate = 1.21m;
	private bool _isVip;

	public CompoundExpressionPage()
	{
		InitializeComponent();
	}

	public decimal Price
	{
		get => _price;
		set { if (_price != value) { _price = value; OnPropertyChanged(); } }
	}

	public virtual int Quantity
	{
		get => _quantity;
		set { if (_quantity != value) { _quantity = value; OnPropertyChanged(); } }
	}

	public decimal TaxRate
	{
		get => _taxRate;
		set { if (_taxRate != value) { _taxRate = value; OnPropertyChanged(); } }
	}

	public virtual bool IsVip
	{
		get => _isVip;
		set { if (_isVip != value) { _isVip = value; OnPropertyChanged(); } }
	}

	public event PropertyChangedEventHandler? PropertyChanged;

	protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
		=> PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
