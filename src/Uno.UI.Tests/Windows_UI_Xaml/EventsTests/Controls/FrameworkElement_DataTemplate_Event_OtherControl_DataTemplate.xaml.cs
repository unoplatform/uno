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

namespace Uno.UI.Tests.Windows_UI_Xaml.EventsTests.Controls
{
	/// <summary>
	/// An empty page that can be used on its own or navigated to within a Frame.
	/// </summary>
	public sealed partial class FrameworkElement_DataTemplate_Event_OtherControl_DataTemplate : Page
	{
		public FrameworkElement_DataTemplate_Event_OtherControl_DataTemplate()
		{
			this.InitializeComponent();
		}

		public DataTemplate GetTemplate() => Resources["testTemplate"] as DataTemplate;

		private void OnLoaded(object sender, RoutedEventArgs e)
		{
			testTextBlock.Text = "Fired!";
		}
	}
}
