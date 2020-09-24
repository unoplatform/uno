#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.ApplicationModel.DataTransfer.DragDrop.Core;
using Windows.Foundation;
using Windows.UI.Input;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

namespace Windows.UI.Xaml
{
	internal class DropUITarget : ICoreDropOperationTarget
	{
		private readonly Window _window = Window.Current!;

		private readonly Dictionary<UIElement, (DraggingState state, DragUIOverride uiOverride, DataPackageOperation acceptedOperation)> _state
			= new Dictionary<UIElement, (DraggingState state, DragUIOverride uiOverride, DataPackageOperation acceptedOperation)>();

		/// <inheritdoc />
		public IAsyncOperation<DataPackageOperation> EnterAsync(CoreDragInfo dragInfo, CoreDragUIOverride dragUIOverride)
			=> throw new NotImplementedException();

		/// <inheritdoc />
		public IAsyncOperation<DataPackageOperation> OverAsync(CoreDragInfo dragInfo, CoreDragUIOverride dragUIOverride)
			=> throw new NotImplementedException();
		//{
		//	if (!(dragInfo.PointerRoutedEventArgs is PointerRoutedEventArgs pointer))
		//	{
		//		return Task.FromResult(DataPackageOperation.None).AsAsyncOperation();
		//	}

		//	// Find the target!
		//	var originalSource = new Grid();
		//	var target = new Grid();
		//	var args = new DragEventArgs(originalSource, dragInfo, dragUIOverride, pointer);

		//	target.RaiseEvent(UIElement.DragOverEvent, args);

		//	if (args.Deferral is {} deferral)
		//	{
		//		return new AsyncOperation<DataPackageOperation>(LoadAsync);

		//		async Task<DataPackageOperation> LoadAsync(CancellationToken ct)
		//		{
		//			await deferral.Completed();

		//			return args.AcceptedOperation;
		//		}
		//	}
		//	else
		//	{
		//		return Task.FromResult(args.AcceptedOperation).AsAsyncOperation();
		//	}
		//}

		/// <inheritdoc />
		public IAsyncAction LeaveAsync(CoreDragInfo dragInfo)
			=> throw new NotImplementedException();

		/// <inheritdoc />
		public IAsyncOperation<DataPackageOperation> DropAsync(CoreDragInfo dragInfo)
			=> throw new NotImplementedException();

		private async Task<DataPackageOperation> RaiseEvent(RoutedEvent evt, CoreDragInfo dragInfo, CoreDragUIOverride dragUIOverride, CancellationToken ct)
		{
			if (!(dragInfo.PointerRoutedEventArgs is PointerRoutedEventArgs pointer))
			{
				return DataPackageOperation.None;
			}

			// Find the target!
			var originalSource = new Grid();
			var target = new Grid();
			var args = new DragEventArgs(originalSource, dragInfo, dragUIOverride, pointer);

			target.RaiseEvent(UIElement.DragOverEvent, args);

			if (args.Deferral is { } deferral)
			{
				await deferral.Completed(ct);
			}

			return args.AcceptedOperation;
		}
	}
}
