#nullable enable

using System;
using System.Collections.Generic;
using Windows.Devices.Input;
using Windows.Foundation;
using Uno.Extensions;
using Uno.Foundation.Logging;

namespace Windows.ApplicationModel.DataTransfer.DragDrop.Core
{
	public partial class CoreDragDropManager
	{
		[ThreadStatic] private static CoreDragDropManager? _current;

		public static CoreDragDropManager? GetForCurrentView()
			=> _current;

		internal static CoreDragDropManager CreateForCurrentView()
			=> _current = new CoreDragDropManager();

		public event TypedEventHandler<CoreDragDropManager, CoreDropOperationTargetRequestedEventArgs>? TargetRequested;

		private IDragDropManager? _manager;

		public bool AreConcurrentOperationsEnabled
		{
			get => _manager?.AreConcurrentOperationsEnabled ?? false;
			set
			{
				if (_manager is null)
				{
					return;
				}

				_manager.AreConcurrentOperationsEnabled = value;
			}
		}

		private CoreDragDropManager()
		{
		}

		internal void SetUIManager(IDragDropManager manager)
			=> _manager ??= manager ?? throw new ArgumentNullException(nameof(manager));

		internal void DragStarted(CoreDragInfo info)
		{
			if (_manager is null)
			{
				return;
			}

			if (TargetRequested is {} handler)
			{
				var args = new CoreDropOperationTargetRequestedEventArgs();
				handler(this, args);

				if (args.Target is null) // This is the UWP behavior!
				{
					info.Complete(DataPackageOperation.None);
					return;
				}

				_manager.BeginDragAndDrop(info, args.Target);
			}
			else
			{
				_manager.BeginDragAndDrop(info);
			}
		}

		/// <summary>
		/// This method is expected to be invoked each time a pointer involved in a drag operation is moved,
		/// no matter if the drag operation has been initiated from this app or from an external app.
		/// </summary>
		/// <returns>
		/// The last accepted operation.
		/// Be aware that due to the async processing of dragging in UWP, this might not be the up to date.
		/// </returns>
		internal DataPackageOperation ProcessMoved(IDragEventSource src)
			=> _manager?.ProcessMoved(src) ?? DataPackageOperation.None;

		/// <summary>
		/// This method is expected to be invoked when pointer involved in a drag operation is released,
		/// no matter if the drag operation has been initiated from this app or from an external app.
		/// </summary>
		/// <returns>
		/// The last accepted operation.
		/// Be aware that due to the async processing of dragging in UWP, this might not be the up to date.
		/// </returns>
		internal DataPackageOperation ProcessDropped(IDragEventSource src)
			=> _manager?.ProcessDropped(src) ?? DataPackageOperation.None;

		/// <summary>
		/// This method is expected to be invoked when pointer involved in a drag operation
		/// is lost for operation initiated by the current app,
		/// or left the window (a.k.a. the "virtual pointer" is lost) for operation initiated by an other app.
		/// </summary>
		/// <returns>
		/// The last accepted operation.
		/// Be aware that due to the async processing of dragging in UWP, this might not be the up to date.
		/// </returns>
		internal DataPackageOperation ProcessAborted(IDragEventSource src)
			=> _manager?.ProcessAborted(src) ?? DataPackageOperation.None;

		internal interface IDragDropManager
		{
			bool AreConcurrentOperationsEnabled { get; set; }

			/// <summary>
			/// This method initiate a new dragging operation.
			/// This method must be followed by either ProcessDropped, either ProcessAborted. 
			/// </summary>
			void BeginDragAndDrop(CoreDragInfo info, ICoreDropOperationTarget? target = null);

			/// <summary>
			/// This method is expected to be invoked each time a pointer involved in a drag operation is moved,
			/// no matter if the drag operation has been initiated from this app or from an external app.
			/// </summary>
			/// <returns>
			/// The last accepted operation.
			/// Be aware that due to the async processing of dragging in UWP, this might not be the up to date.
			/// </returns>
			DataPackageOperation ProcessMoved(IDragEventSource src);

			/// <summary>
			/// This method is expected to be invoked when pointer involved in a drag operation is released,
			/// no matter if the drag operation has been initiated from this app or from an external app.
			/// </summary>
			/// <returns>
			/// The last accepted operation.
			/// Be aware that due to the async processing of dragging in UWP, this might not be the up to date.
			/// </returns>
			DataPackageOperation ProcessDropped(IDragEventSource src);

			/// <summary>
			/// This method is expected to be invoked when pointer involved in a drag operation
			/// is lost for operation initiated by the current app,
			/// or left the window (a.k.a. the "virtual pointer" is lost) for operation initiated by an other app.
			/// </summary>
			/// <returns>
			/// The last accepted operation.
			/// Be aware that due to the async processing of dragging in UWP, this might not be the up to date.
			/// </returns>
			DataPackageOperation ProcessAborted(IDragEventSource src);
		}
	}
}
