using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Uno.UI.Tests.Windows_UI_Xaml_Markup.XamlReaderTests
{
	public partial class When_xBind_TwoWay : Page
	{
		public VM MyVM { get; } = new VM();

		public class VM : System.ComponentModel.INotifyPropertyChanged
		{
			public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;

			private bool _myBool;
			public bool MyBool
			{
				get => _myBool;
				set
				{
					if (_myBool != value)
					{
						_myBool = value;
						PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(MyBool)));
					}
				}
			}
		}
	}
}
