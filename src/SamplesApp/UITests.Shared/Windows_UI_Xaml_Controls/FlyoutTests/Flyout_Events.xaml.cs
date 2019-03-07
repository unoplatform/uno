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

namespace Uno.UI.Samples.Content.UITests.Flyout
{
	[SampleControlInfo("Flyout", "Flyout_Events")]
	public sealed partial class Flyout_Events : UserControl
    {
        public Flyout_Events()
        {
            this.InitializeComponent();

			MyFlyout.Opening += (s, e) =>
			{
				Log("Opening");
			};

			MyFlyout.Closing += (s, e) =>
			{
				Log("Closing");
				if (CancelClosing.IsChecked == true)
				{
					e.Cancel = true;
					Log("Closing canceled");
				}
			};

			MyFlyout.Opened += (s, e) =>
			{
				Log("Opened");
			};

			MyFlyout.Closed += (s, e) =>
			{
				Log("Closed");
			};
        }

		private void Log(string message)
		{
			Events.Text += (message += "\n");
		}
    }
}
