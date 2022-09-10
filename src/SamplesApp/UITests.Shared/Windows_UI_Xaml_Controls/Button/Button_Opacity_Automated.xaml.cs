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

namespace Uno.UI.Samples.Content.UITests.ButtonTestsControl
{

	[SampleControlInfo("Buttons", nameof(Button_Opacity_Automated))]
	public sealed partial class Button_Opacity_Automated : UserControl
	{
		private int increment = 0;

		public Button_Opacity_Automated()
		{
			this.InitializeComponent();
		}

		private void AddButtonClick(object sender, RoutedEventArgs args)
		{
			increment++;
			TotalClicks.Text = increment.ToString();
		}

		private void ApplyButtonClick(object sender, RoutedEventArgs args)
		{
			double valueOfOpacity = Convert.ToDouble(ValueOfOpacity.Text);
			ButtonToChangeOpacity.Opacity = valueOfOpacity;
		}
	}
}
