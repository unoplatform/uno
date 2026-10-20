#nullable enable

using System;
using System.Collections.Generic;
using Windows.Foundation;

namespace Windows.ApplicationModel.DataTransfer.DragDrop.Core
{
	public partial class CoreDragInfo
	{
		internal delegate void Completed(DataPackageOperation result);

		private readonly List<Completed> _completions = [];
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

		internal void RegisterCompletedCallback(Completed onCompleted) => _completions.Add(onCompleted);

		internal void Complete(DataPackageOperation result)
		{
			foreach (var callback in _completions)
			{
				callback(result);
			}
			_completions.Clear();
		}
	}
}
