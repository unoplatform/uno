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
	public sealed partial class Binding_OneTime_PocoObject_Static : Page
	{

		public Binding_OneTime_PocoObject_Static()
		{
			this.InitializeComponent();
		}

		public static string LocalStatic { get; set; } = "Test01";
	}

	public sealed partial class Binding_OneTime_PocoObject_Static_Control : ContentControl
	{
		public Collection<Binding_OneTime_PocoObject_Static_Poco> ClassCollection { get; set; }
			= new Collection<Binding_OneTime_PocoObject_Static_Poco>();
	}

	public class Binding_OneTime_PocoObject_Static_Poco
	{
		public string SampleString { get; set; }
	}

	public static class Binding_OneTime_PocoObject_Static_Resource
	{
		public static string MyStaticProperty { get; set; } = "Test02";
	}
}
