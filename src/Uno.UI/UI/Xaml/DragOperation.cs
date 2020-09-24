#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.ApplicationModel.DataTransfer.DragDrop;
using Windows.ApplicationModel.DataTransfer.DragDrop.Core;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Xaml.Input;

namespace Windows.UI.Xaml
{
	internal class DragOperation
	{
		private static readonly TimeSpan _deferralTimeout = TimeSpan.FromSeconds(30); // Random value!

		private readonly ICoreDropOperationTarget _target;
		private readonly DragView _view;
		private readonly IDisposable _viewHandle;
		private readonly LinkedList<TargetAsyncTask> _queue = new LinkedList<TargetAsyncTask>();

		private bool _isRunning;
		private bool _isCompleted;

		public DragOperation(Window window, CoreDragInfo info, ICoreDropOperationTarget? target = null)
		{
			Info = info;
			Pointer = info.Pointer as Pointer;

			_target = target ?? new DropUITarget(); // This must be re-created for each drag info! (Caching of the drag ui-override)
			_view = new DragView(info.DragUI as DragUI);
			_viewHandle = window.OpenDragAndDrop(_view);
		}

		public CoreDragInfo Info { get; }

		public Pointer? Pointer { get; private set; }

		internal void Entered(PointerRoutedEventArgs args)
		{
			UpdateState(args);
			Enqueue(EnteredCore, isIgnorable: true);

			Task EnteredCore(CancellationToken ct)
				=> _target.EnterAsync(Info, new CoreDragUIOverride()).AsTask(ct);
		}

		internal void Moved(PointerRoutedEventArgs args)
		{
			UpdateState(args); // It's required to do that as soon as possible in order to update the view's location
			Enqueue(MovedCore, isIgnorable: true);

			Task MovedCore(CancellationToken ct)
				=> _target.OverAsync(Info, new CoreDragUIOverride()).AsTask(ct);
		}

		internal void Exited(PointerRoutedEventArgs args)
		{
			UpdateState(args);
			Enqueue(ExitedCore);

			Task ExitedCore(CancellationToken ct)
				=> _target.LeaveAsync(Info).AsTask(ct);
		}

		internal void Dropped(PointerRoutedEventArgs args)
		{
			UpdateState(args);
			Enqueue(DroppedCore);

			async Task DroppedCore(CancellationToken ct)
			{
				var result = DataPackageOperation.None;
				try
				{
					result = await _target.DropAsync(Info).AsTask(ct);
				}
				finally
				{
					Complete(result);
				}
			}
		}

		internal void Aborted(PointerRoutedEventArgs args)
		{
			UpdateState(args);
			Enqueue(AbortedCore);

			async Task AbortedCore(CancellationToken ct)
			{
				try
				{
					await _target.LeaveAsync(Info).AsTask(ct);
				}
				finally
				{
					Complete(DataPackageOperation.None);
				}
			}
		}

		private void UpdateState(PointerRoutedEventArgs args)
		{
			var point = args.GetCurrentPoint(null);
			var mods = DragDropModifiers.None;

			var props = point.Properties;
			if (props.IsLeftButtonPressed)
			{
				mods |= DragDropModifiers.LeftButton;
			}
			if (props.IsMiddleButtonPressed)
			{
				mods |= DragDropModifiers.MiddleButton;
			}
			if (props.IsRightButtonPressed)
			{
				mods |= DragDropModifiers.RightButton;
			}

			var window = Window.Current.CoreWindow;
			if (window.GetAsyncKeyState(VirtualKey.Shift) == CoreVirtualKeyStates.Down)
			{
				mods |= DragDropModifiers.Shift;
			}
			if (window.GetAsyncKeyState(VirtualKey.Control) == CoreVirtualKeyStates.Down)
			{
				mods |= DragDropModifiers.Control;
			}
			if (window.GetAsyncKeyState(VirtualKey.Menu) == CoreVirtualKeyStates.Down)
			{
				mods |= DragDropModifiers.Alt;
			}

			// As ugly as it is, it's the behavior of UWP, the CoreDragInfo is a mutable object!
			Info.Modifiers = mods;
			Info.Position = point.Position;

			// When the drag was initiated from an external app, the Pointer property won't be set.
			// In that case the first pointer event received will be used for this drag operation.
			// If so, we associate this selected pointer for future events.
			Pointer ??= args.Pointer;

			// Updates the view location to follow the pointer
			_view.SetLocation(point.Position);
		}

		private void Enqueue(Func<CancellationToken, Task> action, bool isIgnorable = false)
		{
			if (_queue.Last?.Value.IsIgnorable ?? false)
			{
				_queue.RemoveLast();
			}

			_queue.AddLast(new TargetAsyncTask(action, isIgnorable));

			Run();
		}

		private async void Run()
		{
			// This ** MUST ** be run on the UI thread

			if (_isRunning || _isCompleted)
			{
				return;
			}

			try
			{
				_isRunning = true;
				while (!_isCompleted && _queue.First is { } first)
				{
					_queue.RemoveFirst();

					try
					{
						using var ct = new CancellationTokenSource(_deferralTimeout);
						await first.Value.Invoke(ct.Token);
						ct.Cancel();
					}
					catch (OperationCanceledException)
					{
						Application.Current.RaiseRecoverableUnhandledException(new Exception("A Drag async took too long and has been cancelled"));
					}
					catch (Exception e)
					{
						Application.Current.RaiseRecoverableUnhandledException(new Exception("Drag async callback failed", e));
					}
				}
			}
			catch (Exception e)
			{
				Application.Current.RaiseRecoverableUnhandledException(new Exception("Drag event dispatch process failed", e));
			}
			finally
			{
				_isRunning = false;
			}
		}

		private void Complete(DataPackageOperation result)
		{
			_isCompleted = true;

			_viewHandle.Dispose();
			Info.Complete(result);
		}

		private struct TargetAsyncTask
		{
			private readonly Func<CancellationToken, Task> _action;

			public bool IsIgnorable { get; }

			public TargetAsyncTask(Func<CancellationToken, Task> action, bool isIgnorable)
			{
				IsIgnorable = isIgnorable;
				_action = action;
			}

			public Task Invoke(CancellationToken ct)
				=> _action(ct);
		}
	}
}
