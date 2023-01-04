using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Security.AccessControl;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Uno.UI.Tests.Windows_UI_Xaml_Data.xBindTests.Controls
{
	/// <summary>
	/// An empty page that can be used on its own or navigated to within a Frame.
	/// </summary>
	public sealed partial class Binding_Converter_DataTemplate : Page
	{
		public Binding_Converter_DataTemplate()
		{
			this.InitializeComponent();
		}
	}

	public class Binding_Converter_DataTempate_Model : System.ComponentModel.INotifyPropertyChanged
	{
		public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged { add { } remove { } }

		private int myVar;

		public int MyIntProperty
		{
			get { return myVar; }
			set { myVar = value; }
		}

	}

	public sealed class Binding_Converter_DataTempate_TextConverterDebug : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, string language)
		{
			return $"v:{value?.ToString()} p:{parameter?.ToString()}";
		}

		public object ConvertBack(object value, Type targetType, object parameter, string language)
		{
			return "Converted Back";
		}
	}
}
