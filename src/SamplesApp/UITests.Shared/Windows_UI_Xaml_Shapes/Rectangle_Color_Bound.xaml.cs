using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Uno.UI.Samples.Controls;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace UITests.Shared.Windows_UI_Xaml_Shapes
{
	[SampleControlInfo("Shapes", nameof(Rectangle_Color_Bound), description: "Late-bound color should be applied to the Rectangle")]
	public sealed partial class Rectangle_Color_Bound : UserControl
	{
		public Rectangle_Color_Bound()
		{
			this.InitializeComponent();
			Loaded += Rectangle_Color_Bound_Loaded;
		}

		private async void Rectangle_Color_Bound_Loaded(object sender, RoutedEventArgs e)
		{
			await Task.Yield();
			DataContext = new { MyColor = Colors.Blue, MyText = "Is bound" };
		}
	}
}
