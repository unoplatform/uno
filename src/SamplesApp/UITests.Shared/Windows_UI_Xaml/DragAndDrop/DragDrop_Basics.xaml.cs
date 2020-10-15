using System;
using System.Linq;
using Windows.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;

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
		}
	}
}
