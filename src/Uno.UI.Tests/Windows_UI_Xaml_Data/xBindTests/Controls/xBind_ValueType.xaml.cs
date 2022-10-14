using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Uno.UI.Tests.Windows_UI_Xaml_Data.xBindTests.Controls
{
	public sealed partial class xBind_ValueType : UserControl
	{
		public xBind_ValueType()
		{
			this.InitializeComponent();
		}

		public xBind_ValueType_MyModel1 VM { get; } = new();
	}

	public class xBind_ValueType_MyModel1 : System.ComponentModel.INotifyPropertyChanged
	{
		private xBind_ValueType_MyModel2 model2;

		public xBind_ValueType_MyModel2 Model2
		{
			get => model2;
			set
			{
				model2 = value;
				PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(Model2)));
			}
		}

		public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
	}

	public class xBind_ValueType_MyModel2 : System.ComponentModel.INotifyPropertyChanged
	{
		public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;

		private DateTime myDateTime;

		public DateTime MyDateTime
		{
			get => myDateTime;
			set
			{
				myDateTime = value;
				PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(MyDateTime)));
			}
		}
	}
}
