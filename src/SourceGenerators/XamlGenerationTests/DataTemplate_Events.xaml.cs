using System;
using System.Collections.Generic;
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
