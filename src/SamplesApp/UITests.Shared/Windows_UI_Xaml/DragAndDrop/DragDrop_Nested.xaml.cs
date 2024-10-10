using System;
using System.Linq;
using System.Threading;
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
		Description = "This automated test validates the drag and drop behavior on nested elements.",
		IgnoreInSnapshotTests = true)]
	public sealed partial class DragDrop_Nested : Page
	{
		public DragDrop_Nested()
		{
			this.InitializeComponent();

			var helper = new DragDropTestHelper(Output);
			helper.SubscribeDragEvents(ParentDragSource);
			helper.SubscribeDragEvents(NestedDragSource);

			helper.SubscribeDropEvents(ParentDropTarget);
			helper.SubscribeDropEvents(NestedDropTarget);

			// We also subscribe on this to detect raise on element which does not have the CanDrag and AllowDrop flags set.
			helper.SubscribeDragEvents(this);
			helper.SubscribeDropEvents(this);
		}

		protected override void OnPointerPressed(PointerRoutedEventArgs e)
		{
			if ((Automated.IsChecked ?? false) && e.Pointer.PointerDeviceType == PointerDeviceType.Touch)
			{
				// Ugly hack: The test engine does not allows us to perform a custom gesture (hold for 300 ms then drag)
				// So we just freeze the UI thread enough to simulate the delay ...
				const int holdDelay = 300 /* GestureRecognizer.DragWithTouchMinDelayTicks */ + 50 /* safety */;
				Thread.Sleep(holdDelay);
			}

			base.OnPointerPressed(e);
		}
	}
}
