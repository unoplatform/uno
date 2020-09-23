#nullable enable

using System;
using System.Collections.Generic;
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

		private List<DropOperation> _operations = new List<DropOperation>(1);

		public bool AreConcurrentOperationsEnabled { get; set; } = false;

		private CoreDragDropManager()
		{
		}

		internal void DragStarted(CoreDragInfo info, object? dragUI = null)
		{
			// TODO
			// * Here we will maintain a list of pending drag and drop elements
			// * We should also add a callback for the DropCompleted event / native drop result

			ICoreDropOperationTarget? target;
			if (TargetRequested is null)
			{
				target = new UIDropTarget();
			}
			else
			{
				var args = new CoreDropOperationTargetRequestedEventArgs();
				TargetRequested(this, args);

				target = args.Target;

				if (target is null) // This is the UWP behavior!
				{
					info.Complete(DataPackageOperation.None);
					return;
				}
			}

			_operations.Add(new DropOperation(info, target));
		}

		private class DropOperation
		{
			public DropOperation(CoreDragInfo info, ICoreDropOperationTarget target)
			{
				
			}
		}

		private class UIDropTarget : ICoreDropOperationTarget
		{
			/// <inheritdoc />
			public IAsyncOperation<DataPackageOperation> EnterAsync(CoreDragInfo dragInfo, CoreDragUIOverride dragUIOverride)
				=> throw new NotImplementedException();

			/// <inheritdoc />
			public IAsyncOperation<DataPackageOperation> OverAsync(CoreDragInfo dragInfo, CoreDragUIOverride dragUIOverride)
				=> throw new NotImplementedException();

			/// <inheritdoc />
			public IAsyncAction LeaveAsync(CoreDragInfo dragInfo)
				=> throw new NotImplementedException();

			/// <inheritdoc />
			public IAsyncOperation<DataPackageOperation> DropAsync(CoreDragInfo dragInfo)
				=> throw new NotImplementedException();
		}
	}
}
