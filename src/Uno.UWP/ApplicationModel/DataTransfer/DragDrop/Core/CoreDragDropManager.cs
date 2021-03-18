#nullable enable

using System;
using System.Collections.Generic;
using Windows.Devices.Input;
using Windows.Foundation;
using Uno.Extensions;
using Uno.Logging;

namespace Windows.ApplicationModel.DataTransfer.DragDrop.Core
{
	public partial class CoreDragDropManager
	{
		[ThreadStatic] private static CoreDragDropManager? _current;

		public static CoreDragDropManager? GetForCurrentView()
			=> _current;

		internal static void SetForCurrentView(IDragDropManager manager)
			=> _current = new CoreDragDropManager(manager);

		public event TypedEventHandler<CoreDragDropManager, CoreDropOperationTargetRequestedEventArgs>? TargetRequested;

		private readonly IDragDropManager _manager;

		public bool AreConcurrentOperationsEnabled
		{
			get => _manager.AreConcurrentOperationsEnabled;
			set => _manager.AreConcurrentOperationsEnabled = value;
		}

		private CoreDragDropManager(IDragDropManager manager)
		{
			_manager = manager;
		}

		internal void DragStarted(CoreDragInfo info)
		{
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

		internal interface IDragDropManager
		{
			bool AreConcurrentOperationsEnabled { get; set; }

			void BeginDragAndDrop(CoreDragInfo info, ICoreDropOperationTarget? target = null);
		}
	}
}
