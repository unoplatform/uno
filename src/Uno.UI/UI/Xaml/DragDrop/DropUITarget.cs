#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.ApplicationModel.DataTransfer.DragDrop.Core;
using Windows.Foundation;
using Windows.UI.Input;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Uno.Extensions;
using Uno.Logging;

namespace Windows.UI.Xaml
{
	internal class DropUITarget : ICoreDropOperationTarget
	{
		private static GetHitTestability? _getDropHitTestability;
		private static GetHitTestability GetDropHitTestability => _getDropHitTestability ??= (elt =>
		{
			var visiblity = elt.GetHitTestVisibility();
			return visiblity switch
			{
				HitTestability.Collapsed => (HitTestability.Collapsed, _getDropHitTestability!),
				// Once we reached an element that AllowDrop, we only validate the hit testability for its children
				_ when elt.AllowDrop => (visiblity, VisualTreeHelper.DefaultGetTestability),
				_ => (HitTestability.Invisible, _getDropHitTestability!)
			};
		});


		// Note: As drag events are routed (so they may be received by multiple elements), we might not have an entry for each drop targets.
		//		 We will instead have entry only for leaf (a.k.a. OriginalSource).
		//		 This is valid as UWP does clear the UIOverride as soon as a DragLeave is raised, no matter the number of drop target under pointer.
		private readonly Dictionary<UIElement, (DragUIOverride uiOverride, DataPackageOperation acceptedOperation)> _pendingDropTargets
			= new Dictionary<UIElement, (DragUIOverride uiOverride, DataPackageOperation acceptedOperation)>();

		private readonly Window _window;

		public DropUITarget(Window window)
		{
			_window = window;
		}

		/// <inheritdoc />
		public IAsyncOperation<DataPackageOperation> EnterAsync(CoreDragInfo dragInfo, CoreDragUIOverride dragUIOverride)
			=> EnterOrOverAsync(dragInfo, dragUIOverride);

		/// <inheritdoc />
		public IAsyncOperation<DataPackageOperation> OverAsync(CoreDragInfo dragInfo, CoreDragUIOverride dragUIOverride)
			=> EnterOrOverAsync(dragInfo, dragUIOverride);

		private IAsyncOperation<DataPackageOperation> EnterOrOverAsync(CoreDragInfo dragInfo, CoreDragUIOverride dragUIOverride)
			=> AsyncOperation.FromTask(async ct =>
			{
				var target = await UpdateTarget(dragInfo, dragUIOverride, ct);
				if (!target.HasValue)
				{
					return DataPackageOperation.None;
				}

				var (element, args) = target.Value;
				element.RaiseDragEnterOrOver(args);

				if (args.Deferral is { } deferral)
				{
					await deferral.Completed(ct);
				}

				UpdateState(args);

				return args.AcceptedOperation;
			});

		/// <inheritdoc />
		public IAsyncAction LeaveAsync(CoreDragInfo dragInfo)
			=> AsyncAction.FromTask(async ct =>
			{
				var leaveTasks = _pendingDropTargets.ToArray().Select(RaiseLeave);
				_pendingDropTargets.Clear();
				Task.WhenAll(leaveTasks);

				async Task RaiseLeave(KeyValuePair<UIElement, (DragUIOverride uiOverride, DataPackageOperation acceptedOperation)> target)
				{
					var args = new DragEventArgs(target.Key, dragInfo, target.Value.uiOverride);

					target.Key.RaiseDragLeave(args);

					if (args.Deferral is { } deferral)
					{
						await deferral.Completed(ct);
					}
				}
			});

		/// <inheritdoc />
		public IAsyncOperation<DataPackageOperation> DropAsync(CoreDragInfo dragInfo)
			=> AsyncOperation.FromTask(async ct =>
			{
				var target = await UpdateTarget(dragInfo, null, ct);
				if (!target.HasValue)
				{
					return DataPackageOperation.None;
				}

				var (element, args) = target.Value;
				element.RaiseDrop(args);

				if (args.Deferral is { } deferral)
				{
					await deferral.Completed(ct);
				}

				return args.AcceptedOperation;
			});

		private async Task<(UIElement element, global::Windows.UI.Xaml.DragEventArgs args)?> UpdateTarget(
			CoreDragInfo dragInfo,
			CoreDragUIOverride? dragUIOverride,
			CancellationToken ct)
		{
			var target = VisualTreeHelper.HitTest(
				dragInfo.Position,
				getTestability: GetDropHitTestability,
				isStale: elt => elt.IsDragOver(dragInfo.SourceId));

			// First raise the drag leave event on stale branch if any.
			if (target.stale is { } staleBranch)
			{
				// We need to find the actual leaf drop target to fulfill the leaveArgs
				var leafTarget = staleBranch
					.EnumerateLeafToRoot()
					.Select(elt => (isDragOver: _pendingDropTargets.TryGetValue(staleBranch.Leaf, out var dragState), elt, dragState))
					.FirstOrDefault(t => t.isDragOver);

				var leaveArgs = leafTarget.elt is null // Might happen if the element has been unloaded
					? new DragEventArgs(staleBranch.Leaf, dragInfo, new DragUIOverride(new CoreDragUIOverride()))
					: new DragEventArgs(leafTarget.elt, dragInfo, leafTarget.dragState.uiOverride);

				// We raise the event only up to the stale branch root.
				// This is important for ListView reordering where a Leave would cancel the reordering.
				staleBranch.Leaf.RaiseDragLeave(leaveArgs, upTo: staleBranch.Root);

				if (leaveArgs.Deferral is { } deferral)
				{
					await deferral.Completed(ct);
				}
			}

			if (target.element is null)
			{
				return null;
			}

			DragEventArgs args;
			if (target.element is {} && _pendingDropTargets.TryGetValue(target.element, out var state))
			{
				args = new DragEventArgs(target.element!, dragInfo, state.uiOverride)
				{
					AcceptedOperation = state.acceptedOperation
				};
			}
			else
			{
				args = new DragEventArgs(target.element!, dragInfo, new DragUIOverride(dragUIOverride ?? new CoreDragUIOverride()));
			}

			return (target.element!, args);
		}

		private void UpdateState(DragEventArgs args)
		{
			_pendingDropTargets[(UIElement)args.OriginalSource] = (args.DragUIOverride, args.AcceptedOperation);
		}
	}
}
