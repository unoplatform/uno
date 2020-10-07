#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using Windows.Devices.Input;
using Windows.Foundation;
using Windows.System;
using Uno.Extensions;
using Uno.Logging;

namespace Windows.ApplicationModel.DataTransfer.DragDrop.Core
{
	internal interface IDragEventSource
	{
		long Id { get; }

		uint FrameId { get; }

		//bool IsExternal { get; }

		(Point location, DragDropModifiers modifier) GetState();

		Point GetPosition(object? relativeTo);
	}

	public partial class CoreDragInfo 
	{
		private ImmutableList<Action<DataPackageOperation>>? _completions = ImmutableList<Action<DataPackageOperation>>.Empty;
		private int _result = -1;
		private IDragEventSource _source;

		internal CoreDragInfo(
			IDragEventSource source,
			DataPackageView data,
			DataPackageOperation allowedOperations,
			object? dragUI = null)
		{
			_source = source;
			(Position, Modifiers) = source.GetState();

			Data = data;
			AllowedOperations = allowedOperations;
			DragUI = dragUI;
		}

		public DataPackageView Data { get; }

		public DataPackageOperation AllowedOperations { get; }

		public DragDropModifiers Modifiers { get; private set; }

		public Point Position { get; private set; }

		internal Point GetPosition(object? relativeTo) => _source.GetPosition(relativeTo);

		/// <summary>
		/// If this drag operation has been initiated by the current application,
		/// this is expected to be the Windows.UI.Xaml.DragUI built in the DragStarting event.
		/// </summary>
		internal object? DragUI { get; }

		/// <summary>
		/// A unique identifier of the source that trigger those drag operation (Pointer.UniqueId for internal drag and drop)
		/// </summary>
		internal long SourceId => _source.Id;

		internal void UpdateSource(IDragEventSource src)
		{
			if (src.Id != SourceId)
			{
				throw new InvalidOperationException("Cannot change the source id of pending drag operation");
			}

			_source = src;
			(Position, Modifiers) = src.GetState();
		}

		internal void RegisterCompletedCallback(Action<DataPackageOperation> onCompleted)
		{
			if (_result > 0
				// If the Update return false, it means that the _completions is null, which means that the _result is now ready!
				|| !ImmutableInterlocked.Update(ref _completions, AddCompletion, onCompleted))
			{
				Debug.Assert(_result >= 0);

				onCompleted((DataPackageOperation)_result);
			}

			ImmutableList<Action<DataPackageOperation>>? AddCompletion(ImmutableList<Action<DataPackageOperation>>? completions, Action<DataPackageOperation> callback)
				=> completions?.Add(callback);
		}

		internal void Complete(DataPackageOperation result)
		{
			if (Interlocked.CompareExchange(ref _result, (int)result, -1) != -1)
			{
				this.Log().Error("This drag operation has already been completed");
				return;
			}

			var completions = Interlocked.Exchange(ref _completions, null);
			foreach (var callback in completions!)
			{
				callback(result);
			}
		}
	}
}
