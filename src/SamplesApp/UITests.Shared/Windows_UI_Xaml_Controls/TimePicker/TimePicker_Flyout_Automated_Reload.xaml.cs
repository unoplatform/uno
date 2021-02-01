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

namespace UITests.Shared.Windows_UI_Xaml_Controls.TimePicker
{

	[SampleControlInfo("Time Picker")]
	public sealed partial class TimePicker_Flyout_Automated_Reload : UserControl
	{
		public TimePicker_Flyout_Automated_Reload()
		{
			this.InitializeComponent();

			_ = Dispatcher.RunAsync(
				Windows.UI.Core.CoreDispatcherPriority.Normal,
				() => {
					root.Children.Remove(TestTimePicker);

					_ = Dispatcher.RunAsync(
						Windows.UI.Core.CoreDispatcherPriority.Normal,
						() =>
						{
							root.Children.Add(TestTimePicker);

							this.TestTimePicker.Time = new TimeSpan(3, 12, 0);
						});
				}
			);
		}
	}
}
