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

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace XamlGenerationTests.Shared
{
	public sealed partial class DataTemplate_Events : UserControl
	{
		public DataTemplate_Events()
		{
			this.InitializeComponent();
		}

		public void OnInnerClick(object sender, RoutedEventArgs args)
		{

		}

		public void OnClick(object sender, RoutedEventArgs args)
		{

		}

		public void OnTappedInner(object sender, RoutedEventArgs args)
		{

		}
	}
}
