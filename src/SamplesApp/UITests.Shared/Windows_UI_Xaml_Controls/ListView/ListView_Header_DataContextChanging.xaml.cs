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
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Private.Infrastructure;

namespace UITests.Shared.Windows_UI_Xaml_Controls
{
	[Sample("ListView", "ListView_Header_DataContextChanging")]
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
