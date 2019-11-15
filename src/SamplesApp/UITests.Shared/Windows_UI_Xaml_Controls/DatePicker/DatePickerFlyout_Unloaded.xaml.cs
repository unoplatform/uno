using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
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

namespace UITests.Shared.Windows_UI_Xaml_Controls.DatePicker
{
	[SampleControlInfo("Date Picker")]
	public sealed partial class DatePickerFlyout_Unloaded : UserControl
    {
        public DatePickerFlyout_Unloaded()
        {
            this.InitializeComponent();

#if DEBUG
			_ = Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () => {
				await Task.Delay(5000);
				root.Children.Remove(TestDatePickerFlyoutButton);
				root.Children.Remove(theDatePicker);
			});
#endif
		}

		public DateTimeOffset Date => new DateTimeOffset(2019, 05, 04, 0, 0, 0, TimeSpan.Zero);
	}
}
