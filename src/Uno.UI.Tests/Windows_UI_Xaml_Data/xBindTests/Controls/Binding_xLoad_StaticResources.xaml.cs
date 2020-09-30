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
	public sealed partial class Binding_xLoad_StaticResources : Page
	{
		public Binding_xLoad_StaticResources()
		{
			this.InitializeComponent();
		}

		public string[] Items = { "1", "2", "3" };

		public bool IsTestGridLoaded
		{
			get { return (bool)GetValue(IsTestGridLoadedProperty); }
			set { SetValue(IsTestGridLoadedProperty, value); }
		}

		// Using a DependencyProperty as the backing store for IsTestGridLoaded.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty IsTestGridLoadedProperty =
			DependencyProperty.Register("IsTestGridLoaded", typeof(bool), typeof(Binding_xLoad_StaticResources), new PropertyMetadata(false));
	}
}
