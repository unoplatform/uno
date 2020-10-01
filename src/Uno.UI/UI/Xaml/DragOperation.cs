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
using Windows.UI.Input;
using Windows.UI.Xaml.Input;

namespace Windows.UI.Xaml
{
	internal class DragOperation
	{
		private static readonly TimeSpan _deferralTimeout = TimeSpan.FromSeconds(30); // Random value!

		private readonly ICoreDropOperationTarget _target;
		private readonly DragView _view;
		private readonly IDisposable _viewHandle;
		private readonly CoreDragUIOverride _viewOverride;
		private readonly LinkedList<TargetAsyncTask> _queue = new LinkedList<TargetAsyncTask>();

		private bool _isRunning;
		private State _state = State.None;

		private enum State
		{
			None,
			Over,
			Completing,
			Completed
		}

		public DragOperation(Window window, CoreDragInfo info, ICoreDropOperationTarget? target = null)
		{
			Info = info;
			Pointer = info.Pointer as Pointer;

			_target = target ?? new DropUITarget(window); // The DropUITarget must be re-created for each drag operation! (Caching of the drag ui-override)
			_view = new DragView(info.DragUI as DragUI);
			_viewHandle = window.OpenDragAndDrop(_view);
			_viewOverride = new CoreDragUIOverride(); // UWP does re-use the same instance for each update on _target
		}

		public CoreDragInfo Info { get; }

		public Pointer? Pointer { get; private set; }

		internal void Entered(PointerRoutedEventArgs args)
			=> EnteredOrMoved(args);

		internal void Moved(PointerRoutedEventArgs args)
			=> EnteredOrMoved(args);

		private void EnteredOrMoved(PointerRoutedEventArgs args)
		{
			if (_state >= State.Completing)
			{
				return;
			}

			Update(args); // It's required to do that as soon as possible in order to update the view's location
			Enqueue(Over, isIgnorable: _state == State.Over); // This is ignorable only if we already over

			async Task Over(CancellationToken ct)
			{
				if (_state >= State.Completing)
				{
					return;
				}

				var isOver = _state == State.Over;
				_state = State.Over;

				var acceptedOperation = isOver
					? await _target.OverAsync(Info, _viewOverride).AsTask(ct)
					: await _target.EnterAsync(Info, _viewOverride).AsTask(ct);
				acceptedOperation &= Info.AllowedOperations;

				_view.Update(acceptedOperation, _viewOverride);
			}
		}

		internal void Exited(PointerRoutedEventArgs args)
		{
			if (_state >= State.Completing)
			{
				return;
			}

			Update(args);
			Enqueue(Leave);

			async Task Leave(CancellationToken ct)
			{
				if (_state != State.Over)
				{
					return;
				}

				_state = State.None;
				await _target.LeaveAsync(Info).AsTask(ct);

				// When the pointer goes out of the window, we hide our internal control and,
				// if supported by the OS, we request a Drag and Drop operation with the native UI.
				// TODO: Request native D&D
				_view.Hide();
			}
		}

		internal void Dropped(PointerRoutedEventArgs args)
		{
			if (_state >= State.Completing)
			{
				return;
			}

			Update(args);
			Enqueue(Drop);

			async Task Drop(CancellationToken ct)
			{
				var result = DataPackageOperation.None;
				try
				{
					if (_state != State.Over)
					{
						return;
					}

					_state = State.Completing;
					result = await _target.DropAsync(Info).AsTask(ct);
					result &= Info.AllowedOperations;
				}
				finally
				{
					Complete(result);
				}
			}
		}

		internal void Aborted(PointerRoutedEventArgs args)
		{
			if (_state >= State.Completing)
			{
				return;
			}

			Update(args);
			Enqueue(Abort);

			async Task Abort(CancellationToken ct)
			{
				try
				{
					if (_state != State.Over)
					{
						return;
					}

					_state = State.Completing;
					await _target.LeaveAsync(Info).AsTask(ct);
				}
				finally
				{
					Complete(DataPackageOperation.None);
				}
			}
		}

		/// <summary>
		/// This is used by the manager to abort a pending D&D for any consideration without an event args for the given pointer
		/// It ** MUST ** be invoked on the UI thread.
		/// </summary>
		internal void Abort()
		{
			if (Pointer == null || _state == State.None)
			{
				// The D&D didn't had time to be started (or has already left), we can just complete
				Complete(DataPackageOperation.None);
				return;
			}

			Enqueue(Abort);

			async Task Abort(CancellationToken ct)
			{
				if (_state != State.Over)
				{
					return;
				}

				try
				{
					await _target.LeaveAsync(Info).AsTask(ct);
				}
				finally
				{
					_state = State.Completing;
					Complete(DataPackageOperation.None);
				}
			}
		}

		private void Update(PointerRoutedEventArgs args)
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
			Info.PointerRoutedEventArgs = args;

			// When the drag was initiated from an external app, the Pointer property won't be set.
			// In that case the first pointer event received will be used for this drag operation.
			// If so, we associate this selected pointer for future events.
			Pointer ??= args.Pointer;

			// Updates the view location to follow the pointer
			_view.SetLocation(point.Position);
		}

		private void Enqueue(Func<CancellationToken, Task> action, bool isIgnorable = false)
		{
			// If possible we debounce multiple "over" update.
			// This might happen when the app uses the DragEventsArgs.GetDeferral (or customized the _target).
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

			if (_isRunning || _state == State.Completed)
			{
				return;
			}

			try
			{
				_isRunning = true;
				while (_state != State.Completed && _queue.First is { } first)
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
			if (_state == State.Completed)
			{
				return;
			}
			_state = State.Completed;

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
