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

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Uno.UI.Samples.Content.UITests.CommandBar.BackGesture
{
	/// <summary>
	/// An empty page that can be used on its own or navigated to within a Frame.
	/// </summary>
	public sealed partial class BackGesture_Chooser : Page
	{
		public BackGesture_Chooser()
		{
			this.InitializeComponent();
		}

		private void Back_Click(object sender, RoutedEventArgs e)
		{
			(XamlRoot?.Content as Microsoft.UI.Xaml.Controls.Frame).GoBack();
		}

		private void Normal_Click(object sender, RoutedEventArgs e)
		{
			(XamlRoot?.Content as Microsoft.UI.Xaml.Controls.Frame).Navigate(typeof(BackGesture_Normal));
		}

		private void Collapsed_Click(object sender, RoutedEventArgs e)
		{
			(XamlRoot?.Content as Microsoft.UI.Xaml.Controls.Frame).Navigate(typeof(BackGesture_Collapsed));
		}

		private void NavigationCommand_Click(object sender, RoutedEventArgs e)
		{
			(XamlRoot?.Content as Microsoft.UI.Xaml.Controls.Frame).Navigate(typeof(BackGesture_NavigationCommand));
		}

		private void CollapsedNavigationCommand_Click(object sender, RoutedEventArgs e)
		{
			(XamlRoot?.Content as Microsoft.UI.Xaml.Controls.Frame).Navigate(typeof(BackGesture_CollapsedNavigationCommand));
		}
	}
}
