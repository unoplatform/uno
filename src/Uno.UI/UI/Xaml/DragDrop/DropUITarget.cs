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
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Uno.Extensions;
using Uno.Foundation.Logging;
using Uno.UI.Extensions;

namespace Microsoft.UI.Xaml
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

		public DropUITarget()
		{
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
					dragUIOverride.Clear(); // For safety only, this should have already de done in the 'UpdateTarget' if needed.
					return DataPackageOperation.None;
				}

				var (element, args) = target.Value;
				(args.OriginalSource as UIElement)!.RaiseDragEnterOrOver(args);

				if (args.Deferral is { } deferral)
				{
					await deferral.Completed(ct);
				}

				UpdateState(element, args);

				return args.AcceptedOperation;
			});

		/// <inheritdoc />
		public IAsyncAction LeaveAsync(CoreDragInfo dragInfo)
			=> AsyncAction.FromTask(ct =>
			{
				var leaveTasks = _pendingDropTargets.ToArray().Select(RaiseLeave);
				_pendingDropTargets.Clear();
				Task.WhenAll(leaveTasks);
				return Task.CompletedTask;

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

				(_, var args) = target.Value;
				(args.OriginalSource as UIElement)!.RaiseDrop(args);

				if (args.Deferral is { } deferral)
				{
					await deferral.Completed(ct);
				}

				return args.AcceptedOperation;
			});

		private async Task<(UIElement dropTarget, global::Microsoft.UI.Xaml.DragEventArgs args)?> UpdateTarget(
			CoreDragInfo dragInfo,
			CoreDragUIOverride? dragUIOverride,
			CancellationToken ct)
		{
			//TODO: Multi-window support #13982
			if (Window.CurrentSafe is null)
			{
				return null;
			}

			var target = VisualTreeHelper.HitTest(
				dragInfo.Position,
				Window.CurrentSafe.RootElement?.XamlRoot,
				getTestability: GetDropHitTestability,
				isStale: new StalePredicate(elt => elt.IsDragOver(dragInfo.SourceId), "IsDragOver"));

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

				// Note: We don't clear the 'dragUIOverride' here as even if we are leaving some UI elements,
				//		 the 'dropTarget' (computed below) might still be the same.
			}

			if (target.element is null)
			{
				// When we don't find any target, we should make sure to reset the uiOverride data
				// as it's the same instance which is re-used by the DragOperation (like UWP).
				dragUIOverride?.Clear();

				return null;
			}

			// We search here for the real drop target in order to properly associate the 'state' to the element that effectively allows the drop,
			// so we won't reset 'state' and clear 'dragUiOverride' when we are only moving from a nested element to another of the same 'dropTarget'.
			// (like from a LVItem to another one from the same LV).
			var dropTarget = target.element.FindFirstParent<UIElement>(elt => elt.AllowDrop, includeCurrent: true);
			global::System.Diagnostics.Debug.Assert(dropTarget is not null);
			dropTarget ??= target.element; // Safety only!

			DragEventArgs args;
			if (_pendingDropTargets.TryGetValue(dropTarget, out var state))
			{
				args = new DragEventArgs(target.element!, dragInfo, state.uiOverride)
				{
					AcceptedOperation = state.acceptedOperation
				};
			}
			else
			{
				// When we reach a new target UI element, we should make sure to reset the uiOverride data
				// as it's the same instance which is re-used by the DragOperation (like UWP).
				// It's the responsibility to the new 'target.element', to configure the whole UI override.
				dragUIOverride?.Clear();

				args = new DragEventArgs(target.element!, dragInfo, new DragUIOverride(dragUIOverride ?? new CoreDragUIOverride()));
			}

			return (dropTarget, args);
		}

		private void UpdateState(UIElement dropTarget, DragEventArgs args)
		{
			_pendingDropTargets[dropTarget] = (args.DragUIOverride, args.AcceptedOperation);
		}
	}
}
