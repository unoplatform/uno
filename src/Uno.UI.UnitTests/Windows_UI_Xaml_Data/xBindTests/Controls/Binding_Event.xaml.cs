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
	public sealed partial class Binding_Event : Page
	{
		public Binding_Event()
		{
			this.InitializeComponent();
		}

		public int CheckedRaised { get; private set; }
		public int UncheckedRaised { get; private set; }

		private void OnCheckedRaised()
		{
			CheckedRaised++;
		}

		private void OnUncheckedRaised(object sender, RoutedEventArgs args)
		{
			UncheckedRaised++;
		}
	}
}
