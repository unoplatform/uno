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
	public sealed partial class Binding_xLoad_Event : Page
	{
		public Binding_xLoad_Event()
		{
			this.InitializeComponent();
		}

		public bool TopLevelVisiblity
		{
			get { return (bool)GetValue(TopLevelVisiblityProperty); }
			set { SetValue(TopLevelVisiblityProperty, value); }
		}

		// Using a DependencyProperty as the backing store for TopLevelVisibility.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty TopLevelVisiblityProperty =
			DependencyProperty.Register("TopLevelVisiblity", typeof(bool), typeof(Binding_xLoad_Event), new PropertyMetadata(false));

		public Binding_xLoad_Event_ViewModel ViewModel { get; } = new Binding_xLoad_Event_ViewModel();
	}

	public class Binding_xLoad_Event_ViewModel
	{
		public int CheckedRaised { get; private set; }
		public int UncheckedRaised { get; private set; }

		public void OnCheckedRaised() => CheckedRaised++;

		public void OnUncheckedRaised(object sender, RoutedEventArgs args) => UncheckedRaised++;
	}
}
