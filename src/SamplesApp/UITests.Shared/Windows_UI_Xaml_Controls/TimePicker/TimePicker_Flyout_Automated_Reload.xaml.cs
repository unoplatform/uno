using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Uno.UI.Samples.Controls;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Private.Infrastructure;

namespace UITests.Shared.Windows_UI_Xaml_Controls.TimePicker
{

	[Sample("Pickers")]
	public sealed partial class TimePicker_Flyout_Automated_Reload : UserControl
	{
		private readonly UnitTestDispatcherCompat _dispatcher;

		public TimePicker_Flyout_Automated_Reload()
		{
			this.InitializeComponent();
			_dispatcher = UnitTestDispatcherCompat.From(this);

			_ = _dispatcher.RunAsync(
				UnitTestDispatcherCompat.Priority.Normal,
				() =>
				{
					root.Children.Remove(TestTimePicker);

					_ = _dispatcher.RunAsync(
						UnitTestDispatcherCompat.Priority.Normal,
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
