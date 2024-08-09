using System;
using System.Linq;
using System.Threading;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Uno.UI.Samples.Controls;

#if HAS_UNO_WINUI || WINAPPSDK
using Microsoft.UI.Input;
#else
using Windows.Devices.Input;
using Windows.UI.Input;
#endif

namespace UITests.Windows_UI_Xaml.DragAndDrop
{
	[Sample(
		Description =
			"This automated test validate that basic setup of in-app drag and drop works properly. "
			+ "You can select any item in the left (pink) column than drag and drop it over the right (blue) one to validate behavior.",
		IgnoreInSnapshotTests = true)]
	public sealed partial class DragDrop_Basics : UserControl
	{
		public DragDrop_Basics()
		{
			this.InitializeComponent();

			var helper = new DragDropTestHelper(Output);
			helper.SubscribeDragEvents(BasicDragSource);
			helper.SubscribeDragEvents(TextDragSource);
			helper.SubscribeDragEvents(LinkDragSource);
			helper.SubscribeDragEvents(ImageDragSource);

			helper.SubscribeDropEvents(DropTarget);

			AddHandler(PointerPressedEvent, new PointerEventHandler(SleepOnTouchDown), true); // handle too required for the hyperlink
		}

		private static void SleepOnTouchDown(object sender, PointerRoutedEventArgs e)
		{
			if ((((DragDrop_Basics)sender).Automated.IsChecked ?? false) && e.Pointer.PointerDeviceType == PointerDeviceType.Touch)
			{
				// Ugly hack: The test engine does not allows us to perform a custom gesture (hold for 300 ms then drag)
				// So we just freeze the UI thread enough to simulate the delay ...
				const int holdDelay = 300 /* GestureRecognizer.DragWithTouchMinDelayTicks */ + 50 /* safety */;
				Thread.Sleep(holdDelay);
			}
		}
	}
}
