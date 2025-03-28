using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Uno.UI.Samples.Controls;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Private.Infrastructure;

namespace UITests.Shared.Windows_UI_Xaml_Controls
{
	[SampleControlInfo("ListView", "ListView_Header_DataContextChanging")]
	public sealed partial class ListView_Header_DataContextChanging : UserControl
	{
		public ListView_Header_DataContextChanging()
		{
			this.InitializeComponent();

			// Delay to refresh the content
			_ = Dispatcher.RunIdleAsync(
				_ =>
				{
					DataContext = "InitialDataContext";
				}
			);
		}

		public void OnChangedDataContext(object sender, object args)
		{
			DataContext = "UpdatedDataContext";
		}

		public void OnDataContextChanged(object sender, object args)
		{
			// On android text updates from the header are not propagated during
			// DataContext changed ?
			_ = UnitTestDispatcherCompat.From(this).RunAsync(
				UnitTestDispatcherCompat.Priority.Normal,
				() =>
				{
					var dobj = (FrameworkElement)sender;
					var change = dobj.DataContext?.ToString() ?? "null";

					MyTextBlock.Text += " " + change;
				}
			);
		}
	}
}
