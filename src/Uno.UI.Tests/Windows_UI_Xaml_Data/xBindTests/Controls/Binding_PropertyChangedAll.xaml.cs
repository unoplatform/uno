using System;
using System.Collections.Generic;
using System.ComponentModel;
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

namespace Uno.UI.Tests.Windows_UI_Xaml_Data.xBindTests.Controls
{
	public sealed partial class Binding_PropertyChangedAll : Page
	{
		public Binding_PropertyChangedAll()
		{
			this.InitializeComponent();
		}

		public PropertyChangedAllViewModel Model { get; } = new PropertyChangedAllViewModel();
	}

	public class PropertyChangedAllViewModel : System.ComponentModel.INotifyPropertyChanged
	{
		public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;

		public int Value { get; set; } = 0;

		public string Text { get; set; } = "";

		public void RaisePropertyChanged(string propertyName)
		{
			PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
		}
	}
}
