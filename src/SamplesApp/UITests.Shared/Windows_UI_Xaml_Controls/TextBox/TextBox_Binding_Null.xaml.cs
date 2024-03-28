using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Uno.UI.Samples.Controls;
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

namespace UITests.Shared.Windows_UI_Xaml_Controls.TextBoxControl
{
	[SampleControlInfo("TextBox", description: "Demonstrates that TextBox shouldn't push binding on losing focus unless user has edited text")]
	public sealed partial class TextBox_Binding_Null : UserControl
	{
		public TextBox_Binding_Null()
		{
			this.InitializeComponent();

			TargetTextBox.DataContext = this;
		}

		private void OnClick(object sender, RoutedEventArgs args)
		{
			MappedText.Text = "reset";
		}

		public string MyString
		{
			get { return (string)GetValue(MyStringProperty); }
			set { SetValue(MyStringProperty, value); }
		}

		public static DependencyProperty MyStringProperty { get; } =
			DependencyProperty.Register("MyString", typeof(string), typeof(TextBox_Binding_Null), new PropertyMetadata(null, OnMyStringChanged));

		private static void OnMyStringChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var ctrl = d as TextBox_Binding_Null;

			var newValue = (string)e.NewValue;
			ctrl.MappedText.Text = newValue == "" ?
				"[empty]" :
				newValue ?? "[null]";
		}
	}
}
