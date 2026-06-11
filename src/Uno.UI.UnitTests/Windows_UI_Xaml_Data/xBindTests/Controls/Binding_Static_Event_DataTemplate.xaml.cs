using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Uno.UI.Tests.Windows_UI_Xaml_Data.xBindTests.Controls
{
	/// <summary>
	/// An empty page that can be used on its own or navigated to within a Frame.
	/// </summary>
	public sealed partial class Binding_Static_Event_DataTemplate : Page
	{
		public Binding_Static_Event_DataTemplate()
		{
			this.InitializeComponent();
		}

	}

	public class Binding_Static_Event_DataTemplate_Model { }

	public static class Binding_Static_Event_DataTemplate_Model_Class
	{
		public static int CheckedRaised { get; private set; }
		public static int UncheckedRaised { get; private set; }

		internal static void OnCheckedRaised()
		{
			CheckedRaised++;
		}

		internal static void OnUncheckedRaised(object sender, RoutedEventArgs args)
		{
			UncheckedRaised++;
		}
	}
}
