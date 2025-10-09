using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;
using UnoApp4.Presentation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using SwipeItem = Microsoft.UI.Xaml.Controls.SwipeItem;
using SwipeItemInvokedEventArgs = Microsoft.UI.Xaml.Controls.SwipeItemInvokedEventArgs;

namespace UITests.Windows_UI_Xaml_Controls.SwipeControlTests
{
	[Sample("SwipeControl", "ListView", IgnoreInSnapshotTests = true)]
	public sealed partial class SwipeControl_Modes : Page
	{
		public ObservableCollection<SwipeControlModesModel> ItemsUsingReveal { get; set; }

		public ObservableCollection<SwipeControlModesModel> ItemsUsingExecute { get; set; }


		public SwipeControl_Modes()
		{
			this.InitializeComponent();

			ItemsUsingReveal = new ObservableCollection<SwipeControlModesModel>(
				Enumerable.Range(1, 5).Select(i => new SwipeControlModesModel { DisplayName = $"Swipe Mode Reveal {i}" }));

			ItemsUsingExecute = new ObservableCollection<SwipeControlModesModel>(
				Enumerable.Range(1, 5).Select(i => new SwipeControlModesModel { DisplayName = $"Swipe Mode Execute {i}" }));
		}
	}
}
