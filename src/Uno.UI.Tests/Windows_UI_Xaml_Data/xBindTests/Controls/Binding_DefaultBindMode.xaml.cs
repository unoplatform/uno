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
	public sealed partial class Binding_DefaultBindMode : Page
	{
		public Binding_DefaultBindMode()
		{
			this.InitializeComponent();
		}

		public string Default_undefined_Property
		{
			get { return (string)GetValue(Default_undefined_PropertyProperty); }
			set { SetValue(Default_undefined_PropertyProperty, value); }
		}

		public static readonly DependencyProperty Default_undefined_PropertyProperty =
			DependencyProperty.Register("Default_undefined_Property", typeof(string), typeof(Binding_DefaultBindMode), new FrameworkPropertyMetadata(null));

		public string Default_undefined_OneWay_Property
		{
			get { return (string)GetValue(Default_undefined_OneWay_PropertyProperty); }
			set { SetValue(Default_undefined_OneWay_PropertyProperty, value); }
		}

		public static readonly DependencyProperty Default_undefined_OneWay_PropertyProperty =
			DependencyProperty.Register("Default_undefined_OneWay_Property", typeof(string), typeof(Binding_DefaultBindMode), new FrameworkPropertyMetadata(null));

		public string Default_undefined_TwoWay_Property
		{
			get { return (string)GetValue(Default_undefined_TwoWay_PropertyProperty); }
			set { SetValue(Default_undefined_TwoWay_PropertyProperty, value); }
		}

		public static readonly DependencyProperty Default_undefined_TwoWay_PropertyProperty =
			DependencyProperty.Register("Default_undefined_TwoWay_Property", typeof(string), typeof(Binding_DefaultBindMode), new FrameworkPropertyMetadata(null));

		public string Default_OneWay_Property
		{
			get { return (string)GetValue(Default_OneWay_PropertyProperty); }
			set { SetValue(Default_OneWay_PropertyProperty, value); }
		}

		public static readonly DependencyProperty Default_OneWay_PropertyProperty =
			DependencyProperty.Register("Default_OneWay_Property", typeof(string), typeof(Binding_DefaultBindMode), new FrameworkPropertyMetadata(null));

		public string Default_OneWay_OneWay_Property
		{
			get { return (string)GetValue(Default_OneWay_OneWay_PropertyProperty); }
			set { SetValue(Default_OneWay_OneWay_PropertyProperty, value); }
		}

		public static readonly DependencyProperty Default_OneWay_OneWay_PropertyProperty =
			DependencyProperty.Register("Default_OneWay_OneWay_Property", typeof(string), typeof(Binding_DefaultBindMode), new FrameworkPropertyMetadata(null));


		public string Default_OneWay_TwoWay_Property
		{
			get { return (string)GetValue(Default_OneWay_TwoWay_PropertyProperty); }
			set { SetValue(Default_OneWay_TwoWay_PropertyProperty, value); }
		}

		public static readonly DependencyProperty Default_OneWay_TwoWay_PropertyProperty =
			DependencyProperty.Register("Default_OneWay_TwoWay_Property", typeof(string), typeof(Binding_DefaultBindMode), new FrameworkPropertyMetadata(null));

		public string Default_TwoWay_Property
		{
			get { return (string)GetValue(Default_TwoWay_PropertyProperty); }
			set { SetValue(Default_TwoWay_PropertyProperty, value); }
		}

		public static readonly DependencyProperty Default_TwoWay_PropertyProperty =
			DependencyProperty.Register("Default_TwoWay_Property", typeof(string), typeof(Binding_DefaultBindMode), new FrameworkPropertyMetadata(null));

		public string Default_TwoWay_OneWay_Property
		{
			get { return (string)GetValue(Default_TwoWay_OneWay_PropertyProperty); }
			set { SetValue(Default_TwoWay_OneWay_PropertyProperty, value); }
		}

		public static readonly DependencyProperty Default_TwoWay_OneWay_PropertyProperty =
			DependencyProperty.Register("Default_TwoWay_OneWay_Property", typeof(string), typeof(Binding_DefaultBindMode), new FrameworkPropertyMetadata(null));

		public string Default_TwoWay_TwoWay_Property
		{
			get { return (string)GetValue(Default_TwoWay_TwoWay_PropertyProperty); }
			set { SetValue(Default_TwoWay_TwoWay_PropertyProperty, value); }
		}

		public static readonly DependencyProperty Default_TwoWay_TwoWay_PropertyProperty =
			DependencyProperty.Register("Default_TwoWay_TwoWay_Property", typeof(string), typeof(Binding_DefaultBindMode), new FrameworkPropertyMetadata(null));

		public string Nested_Default_1_Property
		{
			get { return (string)GetValue(Nested_Default_1_PropertyProperty); }
			set { SetValue(Nested_Default_1_PropertyProperty, value); }
		}

		public static readonly DependencyProperty Nested_Default_1_PropertyProperty =
			DependencyProperty.Register("Nested_Default_1_Property", typeof(string), typeof(Binding_DefaultBindMode), new FrameworkPropertyMetadata(null));

		public string Nested_Default_2_Property
		{
			get { return (string)GetValue(Nested_Default_2_PropertyProperty); }
			set { SetValue(Nested_Default_2_PropertyProperty, value); }
		}

		public static readonly DependencyProperty Nested_Default_2_PropertyProperty =
			DependencyProperty.Register("Nested_Default_2_Property", typeof(string), typeof(Binding_DefaultBindMode), new FrameworkPropertyMetadata(null));

		public string Nested_Default_OneWay_OneWay_Property
		{
			get { return (string)GetValue(Nested_Default_OneWay_OneWay_PropertyProperty); }
			set { SetValue(Nested_Default_OneWay_OneWay_PropertyProperty, value); }
		}

		public static readonly DependencyProperty Nested_Default_OneWay_OneWay_PropertyProperty =
			DependencyProperty.Register("Nested_Default_OneWay_OneWay_Property", typeof(string), typeof(Binding_DefaultBindMode), new FrameworkPropertyMetadata(null));

		public string Nested_Default_OneWay_TwoWay_Property
		{
			get { return (string)GetValue(Nested_Default_OneWay_TwoWay_PropertyProperty); }
			set { SetValue(Nested_Default_OneWay_TwoWay_PropertyProperty, value); }
		}

		public static readonly DependencyProperty Nested_Default_OneWay_TwoWay_PropertyProperty =
			DependencyProperty.Register("Nested_Default_OneWay_TwoWay_Property", typeof(string), typeof(Binding_DefaultBindMode), new FrameworkPropertyMetadata(null));

		public string Nested_Default_OneWay_OneTime_Property
		{
			get { return (string)GetValue(Nested_Default_OneWay_OneTime_PropertyProperty); }
			set { SetValue(Nested_Default_OneWay_OneTime_PropertyProperty, value); }
		}

		public static readonly DependencyProperty Nested_Default_OneWay_OneTime_PropertyProperty =
			DependencyProperty.Register("Nested_Default_OneWay_OneTime_Property", typeof(string), typeof(Binding_DefaultBindMode), new FrameworkPropertyMetadata(null));
	}
}
