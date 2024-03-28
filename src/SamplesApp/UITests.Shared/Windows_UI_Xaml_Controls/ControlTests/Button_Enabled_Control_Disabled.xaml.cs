using System;
using System.Linq;
using System.Windows.Input;
using Uno.UI.Samples.Controls;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Uno.UI.Samples.Presentation.SamplePages;

using System.Collections.Generic;

namespace Uno.UI.Samples.Content.UITests.ButtonTestsControl
{
	[Sample("Control", Name = "Button_Enabled_Control_Disabled",
		Description = "A control should be disabled if the surrounding control is disabled even if it's explicity set to enabled.")]
	public sealed partial class Button_Enabled_Control_Disabled : UserControl
	{
		public Button_Enabled_Control_Disabled()
		{
			this.InitializeComponent();
		}
		private void Button_OnClick(object sender, RoutedEventArgs e)
		{
			cc1.IsEnabled = !cc1.IsEnabled;
		}

		private void Button2_OnClick(object sender, RoutedEventArgs e)
		{
			btn.IsEnabled = !btn.IsEnabled;
		}
	}
}
