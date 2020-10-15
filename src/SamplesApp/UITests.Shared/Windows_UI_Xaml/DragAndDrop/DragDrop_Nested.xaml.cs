using System;
using System.Linq;
using Windows.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;


namespace UITests.Windows_UI_Xaml.DragAndDrop
{
	[Sample(Description = "This automated test validates the drag and drop behavior on nested elements.")]
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
	}
}
