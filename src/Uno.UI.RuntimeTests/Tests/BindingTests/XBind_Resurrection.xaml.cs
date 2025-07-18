using System.ComponentModel;
using Microsoft.UI.Xaml.Controls;

namespace Uno.UI.RuntimeTests.Tests;

public sealed partial class XBind_Resurrection : Page
{
	public VM ViewModel { get; } = new VM();

	public XBind_Resurrection()
	{
		this.InitializeComponent();
	}

	public class VM : INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler PropertyChanged;

		private int _myValue;
		public int MyValue
		{
			get => _myValue;
			set
			{
				if (_myValue != value)
				{
					_myValue = value;
					PropertyChanged?.Invoke(this, new(nameof(MyValue)));
				}
			}
		}
	}
}
