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

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml.Controls
{
	/// <summary>
	/// An empty page that can be used on its own or navigated to within a Frame.
	/// </summary>
	public sealed partial class Binding_xLoad_Nested : Page
	{
		public Binding_xLoad_Nested()
		{
			this.InitializeComponent();
		}

		public bool TopLevelVisiblity1
		{
			get { TopLevelVisiblity1GetCount++; return (bool)GetValue(TopLevelVisiblity1Property); }
			set { TopLevelVisiblity1SetCount++; SetValue(TopLevelVisiblity1Property, value); }
		}

		// Using a DependencyProperty as the backing store for TopLevelVisiblity.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty TopLevelVisiblity1Property =
			DependencyProperty.Register("TopLevelVisiblity1", typeof(bool), typeof(Binding_xLoad_Nested), new PropertyMetadata(false));

		public bool TopLevelVisiblity2
		{
			get { TopLevelVisiblity2GetCount++; return (bool)GetValue(TopLevelVisiblity2Property); }
			set { TopLevelVisiblity2SetCount++; SetValue(TopLevelVisiblity2Property, value); }
		}

		// Using a DependencyProperty as the backing store for TopLevelVisiblity2.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty TopLevelVisiblity2Property =
			DependencyProperty.Register("TopLevelVisiblity2", typeof(bool), typeof(Binding_xLoad_Nested), new PropertyMetadata(false));

		public bool TopLevelVisiblity3
		{
			get { TopLevelVisiblity3GetCount++; return (bool)GetValue(TopLevelVisiblity3Property); }
			set { TopLevelVisiblity3SetCount++; SetValue(TopLevelVisiblity3Property, value); }
		}

		// Using a DependencyProperty as the backing store for TopLevelVisiblity3.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty TopLevelVisiblity3Property =
			DependencyProperty.Register("TopLevelVisiblity3", typeof(bool), typeof(Binding_xLoad_Nested), new PropertyMetadata(false));


		public int TopLevelVisiblity1SetCount;
		public int TopLevelVisiblity1GetCount;
		public int TopLevelVisiblity2GetCount;
		public int TopLevelVisiblity2SetCount;
		public int TopLevelVisiblity2_2GetCount;
		public int TopLevelVisiblity2_2SetCount;
		public int TopLevelVisiblity3GetCount;
		public int TopLevelVisiblity3SetCount;
	}
}
