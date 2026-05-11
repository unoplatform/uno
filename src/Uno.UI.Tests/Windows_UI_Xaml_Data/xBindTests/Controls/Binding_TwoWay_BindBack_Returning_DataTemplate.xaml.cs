using System.ComponentModel;
using Microsoft.UI.Xaml.Controls;

namespace Uno.UI.Tests.Windows_UI_Xaml_Data.xBindTests.Controls
{
	public sealed partial class Binding_TwoWay_BindBack_Returning_DataTemplate : Page
	{
		public Binding_TwoWay_BindBack_Returning_DataTemplate()
		{
			this.InitializeComponent();
		}
	}

	public class Binding_TwoWay_BindBack_Returning_DataTemplate_Base : INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler PropertyChanged;

		public TwoWay_BindBack_Returning_Model Model { get; } = new TwoWay_BindBack_Returning_Model();

		private int _myIntProperty;
		public int MyIntProperty
		{
			get => _myIntProperty;
			set
			{
				if (_myIntProperty != value)
				{
					_myIntProperty = value;
					PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(MyIntProperty)));
				}
			}
		}

		public int ConvertToNumber(string value) => int.TryParse(value, out var result) ? result : 0;

		public string ConvertToString(int value) => value.ToString();
	}
}
