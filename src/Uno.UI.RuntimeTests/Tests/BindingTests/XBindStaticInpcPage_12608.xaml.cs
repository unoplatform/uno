using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml.Controls;

namespace Uno.UI.RuntimeTests.Tests
{
	public sealed partial class XBindStaticInpcPage_12608 : Page
	{
		public XBindStaticInpcPage_12608()
		{
			this.InitializeComponent();
		}

		public TextBlock BoundTextBlock => BoundText;
	}

	public class XBindStaticInpcObject_12608 : INotifyPropertyChanged
	{
		private int _value;

		public int Value
		{
			get => _value;
			set
			{
				if (_value != value)
				{
					_value = value;
					OnPropertyChanged();
				}
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;

		private void OnPropertyChanged([CallerMemberName] string name = null)
			=> PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
	}

	internal static class XBindStaticInpcApp_12608
	{
		public static XBindStaticInpcObject_12608 MyObj { get; set; } = new XBindStaticInpcObject_12608 { Value = 0 };
	}
}
