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
	public sealed partial class Binding_xLoad_Twice : Page
	{
		public Binding_xLoad_Twice()
		{
			this.InitializeComponent();
		}

		public bool TopLevelVisiblity
		{
			get { TopLevelVisiblityGetCount++; return (bool)GetValue(TopLevelVisiblityProperty); }
			set { TopLevelVisiblitySetCount++; SetValue(TopLevelVisiblityProperty, value); }
		}

		// Using a DependencyProperty as the backing store for TopLevelVisiblity.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty TopLevelVisiblityProperty =
			DependencyProperty.Register("TopLevelVisiblity", typeof(bool), typeof(Binding_xLoad_Twice), new PropertyMetadata(false));

		public int TopLevelVisiblitySetCount;

		public int TopLevelVisiblityGetCount;
	}
}
