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
	public partial class CoreDragInfo 
	{
		private ImmutableList<Action<DataPackageOperation>>? _completions = ImmutableList<Action<DataPackageOperation>>.Empty;
		private int _result = -1;

		internal CoreDragInfo(
			DataPackageView data,
			DataPackageOperation allowedOperations,
			object? dragUI = null,
			object? pointer = null)
		{
			Data = data;
			AllowedOperations = allowedOperations;
			DragUI = dragUI;
			Pointer = pointer;
		}

		public DataPackageView Data { get; }

		public DataPackageOperation AllowedOperations { get; }

		public DragDropModifiers Modifiers { get; internal set; }

		public Point Position { get; internal set; }

		/// <summary>
		/// If this drag operation has been initiated by the current application,
		/// this is expected to be the Windows.UI.Xaml.DragUI built in the DragStarting event.
		/// </summary>
		internal object? DragUI { get; }

		/// <summary>
		/// If this drag operation has been initiated by the current application,
		/// this is expected to be the Windows.UI.Xaml.Input.Pointer used to initiate this operation.
		/// </summary>
		internal object? Pointer { get; }

		/// <summary>
		/// This will contains the current Windows.UI.Xaml.Input.PointerRoutedEventArgs which is triggering an update
		/// on <see cref="ICoreDropOperationTarget"/>.
		/// This is going to be updated before the invocation of any method of the ICoreDropOperationTarget.
		/// </summary>
		/// <remarks>
		/// This is a hack for the UIDropTarget which needs to known the location of the pointer for the Windows.UI.Xaml.DragEventArgs.
		/// </remarks>
		internal object? PointerRoutedEventArgs { get; set; }

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
