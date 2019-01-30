using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
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

namespace Uno.UI.Samples.Content.UITests.Flyout
{
	[SampleControlInfo("Flyout", "Flyout_Target")]
    public sealed partial class Flyout_Target : UserControl
    {
		private Windows.UI.Xaml.Controls.Flyout _flyout;

        public Flyout_Target()
        {
            this.InitializeComponent();

			_flyout = new Windows.UI.Xaml.Controls.Flyout()
			{
				Content = new Border
				{
					Height = 100,
					Width = 100,
					Background = new SolidColorBrush(Colors.Red)
				}
			};
        }

		private void OnClick(object s, RoutedEventArgs e)
		{
			var target = s as FrameworkElement;
			_flyout.ShowAt(target);
			var success = _flyout.Target == target;
			var unused = new Windows.UI.Popups.MessageDialog(success ? "Flyout.Target updated correctly." : "Flyout.Target updated incorrectly.").ShowAsync();
		}
    }
}
