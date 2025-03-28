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

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Uno.UI.Tests.Windows_UI_Xaml_Data.xBindTests.Controls
{
	/// <summary>
	/// An empty page that can be used on its own or navigated to within a Frame.
	/// </summary>
	public sealed partial class Binding_xLoad_DataTemplate : Page
	{
		public Binding_xLoad_DataTemplate()
		{
			this.InitializeComponent();
		}
	}

	public class Binding_xLoad_DataTemplate_Data : System.ComponentModel.INotifyPropertyChanged
	{
		private bool topLevelVisiblity;

		public bool TopLevelVisiblity
		{
			get => topLevelVisiblity;
			set
			{
				topLevelVisiblity = value;
				PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(TopLevelVisiblity)));
			}
		}

		public string InnerText { get; set; }

		public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
	}
}
