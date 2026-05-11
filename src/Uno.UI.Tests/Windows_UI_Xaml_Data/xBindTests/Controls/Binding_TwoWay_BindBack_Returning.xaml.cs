using System.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Uno.UI.Tests.Windows_UI_Xaml_Data.xBindTests.Controls
{
	// Covers https://github.com/unoplatform/uno/issues/19122
	// x:Bind TwoWay with a function path and a BindBack method that RETURNS the converted source value
	// (instead of imperatively pushing it back). The generator should assign the return value back to
	// the bound property.
	public sealed partial class Binding_TwoWay_BindBack_Returning : Page, INotifyPropertyChanged
	{
		public Binding_TwoWay_BindBack_Returning()
		{
			this.InitializeComponent();
		}

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

		// Pure converter: returns the source value, does NOT mutate any property.
		public int ConvertToNumber(string value) => int.TryParse(value, out var result) ? result : 0;

		public string ConvertToString(int value) => value.ToString();
	}

	public class TwoWay_BindBack_Returning_TestObject : Border
	{
		public string MyProperty
		{
			get { return (string)GetValue(MyPropertyProperty); }
			set { SetValue(MyPropertyProperty, value); }
		}

		public static readonly DependencyProperty MyPropertyProperty =
			DependencyProperty.Register("MyProperty", typeof(string), typeof(TwoWay_BindBack_Returning_TestObject), new FrameworkPropertyMetadata(""));
	}

	public class TwoWay_BindBack_Returning_Model : INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler PropertyChanged;

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
	}
}
