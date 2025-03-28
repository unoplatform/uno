using System;
using System.Collections.Generic;
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
	public sealed partial class Binding_xLoad_FallbackValue : Page
	{
		public Binding_xLoad_FallbackValue()
		{
			this.InitializeComponent();
		}

		public Binding_xLoad_FallbackValue_Model Model
		{
			get => (Binding_xLoad_FallbackValue_Model)GetValue(ModelProperty);
			set => SetValue(ModelProperty, value);
		}

		// Using a DependencyProperty as the backing store for TopLevelVisiblity.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty ModelProperty =
			DependencyProperty.Register("Model",
				typeof(Binding_xLoad_FallbackValue_Model),
				typeof(Binding_xLoad_FallbackValue),
				new PropertyMetadata(null));

		public string InnerText
		{
			get => (string)GetValue(InnerTextProperty);
			set => SetValue(InnerTextProperty, value);
		}

		// Using a DependencyProperty as the backing store for InnerText.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty InnerTextProperty =
			DependencyProperty.Register("InnerText", typeof(string), typeof(Binding_xLoad_FallbackValue), new PropertyMetadata("My inner text"));
	}

	public class Binding_xLoad_FallbackValue_Model : System.ComponentModel.INotifyPropertyChanged
	{
		private bool visible;

		public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;

		public bool Visible
		{
			get => visible; set
			{
				visible = value;
				PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(Visible)));
			}
		}
	}
}
