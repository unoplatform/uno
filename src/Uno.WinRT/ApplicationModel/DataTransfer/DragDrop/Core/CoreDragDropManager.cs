#nullable enable

using System;
using System.Collections.Concurrent;
using Windows.Foundation;
using Microsoft.UI;
using Microsoft.UI.Windowing;

namespace Windows.ApplicationModel.DataTransfer.DragDrop.Core
{
	public partial class CoreDragDropManager
	{
		private static readonly ConcurrentDictionary<WindowId, CoreDragDropManager> _windowIdMap = new();
		private readonly WindowId _windowId;

		private CoreDragDropManager(WindowId windowId)
		{
			_windowId = windowId;
		}

		public static CoreDragDropManager GetForCurrentView()
		{
			// This is needed to ensure for "current view" there is always a corresponding CoreDragDropManager instance.
			// This means that Uno Islands and WinUI apps can keep using this API for now until we make the breaking change
			// on Uno.WinUI codebase.
			return GetOrCreateForWindowId(AppWindow.MainWindowId);
		}

#pragma warning disable RS0030 // Do not use banned APIs
		public static CoreDragDropManager GetForCurrentViewSafe() => GetForCurrentView();
#pragma warning restore RS0030 // Do not use banned APIs

		internal static CoreDragDropManager GetForWindowId(WindowId windowId)
		{
			if (!_windowIdMap.TryGetValue(windowId, out var coreDragDropManager))
			{
				throw new InvalidOperationException(
					$"CoreDragDropManager corresponding with this window does not exist yet, which usually means " +
					$"the API was called too early in the windowing lifecycle. Try to use CoreDragDropManager later.");
			}

			return coreDragDropManager;
		}

		internal static CoreDragDropManager GetOrCreateForWindowId(WindowId windowId)
		{
			if (!_windowIdMap.TryGetValue(windowId, out var coreDragDropManager))
			{
				coreDragDropManager = new(windowId);
				_windowIdMap[windowId] = coreDragDropManager;
			}

			return coreDragDropManager;
		}

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

		internal void SetUIManager(IDragDropManager manager)
		{
			if (_manager is { })
			{
				throw new InvalidOperationException($"{nameof(IDragDropManager)} is set twice on {nameof(CoreDragDropManager)}.");
			}

			_manager = manager ?? throw new ArgumentNullException(nameof(manager));
		}

		internal void DragStarted(CoreDragInfo info)
		{
			if (_manager is null)
			{
				return;
			}

			if (TargetRequested is { } handler)
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
			=> _manager?.ProcessReleased(src) ?? DataPackageOperation.None;

		/// <summary>
		/// This method is expected to be invoked when pointer involved in a drag operation
		/// is lost for operation initiated by the current app,
		/// or left the window (a.k.a. the "virtual pointer" is lost) for operation initiated by an other app.
		/// </summary>
		/// <returns>
		/// The last accepted operation.
		/// Be aware that due to the async processing of dragging in UWP, this might not be the up to date.
		/// </returns>
		internal DataPackageOperation ProcessAborted(long pointerId)
			=> _manager?.ProcessAborted(pointerId) ?? DataPackageOperation.None;

		/// <summary>
		/// The methods below should only be called by <see cref="CoreDragDropManager"/>. Ideally, this interface should
		/// have been removed or kept private to <see cref="CoreDragDropManager"/>, but, unfortunately, <see cref="CoreDragDropManager"/>
		/// is in Uno.UWP, so it has no Uno.UI visibility and can't interact with UIElements, etc.
		/// </summary>
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
			DataPackageOperation ProcessReleased(IDragEventSource src);

			/// <summary>
			/// This method is expected to be invoked when pointer involved in a drag operation
			/// is lost for operation initiated by the current app,
			/// or left the window (a.k.a. the "virtual pointer" is lost) for operation initiated by an other app.
			/// </summary>
			/// <returns>
			/// The last accepted operation.
			/// Be aware that due to the async processing of dragging in UWP, this might not be the up to date.
			/// </returns>
			DataPackageOperation ProcessAborted(long pointerId);
		}
	}
}
