using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Uno.UI.Tests.Windows_UI_Xaml_Data.xBindTests.Controls
{
#pragma warning disable UXAML0002
	public sealed partial class xBind_NullableRecordStruct : UserControl
#pragma warning restore UXAML0002
	{
		public xBind_NullableRecordStruct()
		{
			this.InitializeComponent();
		}

		public MyRecord? MyProperty
		{
			get => (MyRecord?)GetValue(MyPropertyProperty);
			set => SetValue(MyPropertyProperty, value);
		}

		public static readonly DependencyProperty MyPropertyProperty =
			DependencyProperty.Register(
				nameof(MyProperty),
				typeof(MyRecord?),
				typeof(xBind_NullableRecordStruct),
				new PropertyMetadata(null, DataReady));

		private static void DataReady(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
		{

		}

		public readonly record struct MyRecord(string OtherProperty);
	}
}
