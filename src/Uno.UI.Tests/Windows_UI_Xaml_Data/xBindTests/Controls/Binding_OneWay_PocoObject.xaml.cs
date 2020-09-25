using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
	public sealed partial class Binding_OneWay_PocoObject : Page
	{

		public Binding_OneWay_PocoObject()
		{
			this.InitializeComponent();
		}

		public string MyPlainProperty { get; set; } = "Test01";



		public string MyDependencyProperty
		{
			get { return (string)GetValue(MyDependencyPropertyProperty); }
			set { SetValue(MyDependencyPropertyProperty, value); }
		}

		// Using a DependencyProperty as the backing store for MyProperty.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty MyDependencyPropertyProperty =
			DependencyProperty.Register("MyDependencyProperty", typeof(string), typeof(Binding_OneWay_PocoObject), new PropertyMetadata("Test02"));


	}

	public sealed partial class Binding_OneWay_PocoObject_Control : ContentControl
	{
		public Collection<Binding_OneWay_PocoObject_Poco> ClassCollection { get; set; }
			= new Collection<Binding_OneWay_PocoObject_Poco>();
	}

	public class Binding_OneWay_PocoObject_Poco
	{
		public string SampleString { get; set; }
	}
}
