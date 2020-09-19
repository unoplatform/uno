#nullable enable

using System;
using Windows.Foundation;

namespace Windows.ApplicationModel.DataTransfer.DragDrop.Core
{
	public partial class CoreDragDropManager
	{
		[ThreadStatic]
		private static CoreDragDropManager? _current;

		public static CoreDragDropManager GetForCurrentView()
			=> _current ??= new CoreDragDropManager();

		public event TypedEventHandler<CoreDragDropManager, CoreDropOperationTargetRequestedEventArgs>? TargetRequested;

		public bool AreConcurrentOperationsEnabled { get; set; }

		private CoreDragDropManager()
		{
		}

		internal void DragStarted(CoreDragInfo info, object? dragUI = null)
		{
			// TODO
			// * Here we will maintain a list of pending drag and drop elements
			// * We should also add a callback for the DropCompleted event / native drop result

			TargetRequested?.ToString(); // event not used!
		}
	}
}
