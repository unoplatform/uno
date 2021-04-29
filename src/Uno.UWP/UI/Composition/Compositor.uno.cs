#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Windows.System;
using Windows.UI.Core;
using Uno;
using Uno.UI.Composition;

namespace Windows.UI.Composition
{
	public partial class Compositor
	{
		public bool IsDirty { get; private set; }

		internal void InvalidateRender()
		{
			if (!IsDirty)
			{
				IsDirty = true;
				CoreWindow.QueueInvalidateRender();
			}
		}


		private /*VisualDirtyState*/ int _dirtyState;
		private ImmutableList<Visual> _dirtyRoots = ImmutableList<Visual>.Empty; // We have to maintain a List in order to allow UIElement nested in native elements

		internal void InvalidateRoot(Visual rootVisual, CompositionPropertyType kind)
		{
			// TODO: Remove the kind here ... not needed

			Transactional.AddDistinct(ref _dirtyRoots, rootVisual);

			RequestCommit();
			//if (VisualDirtyStateHelper.Invalidate(ref _dirtyState, kind))
			//{
			//	switch (kind)
			//	{
			//		case VisualDirtyState.Dependent:
			//			RequestCommit();
			//			break;

			//		case VisualDirtyState.Independent:
			//			RequestRender();
			//			break;
			//	}
			//}
		}

		private object _gate = new object();
		private Visual? _rootVisual;

		private int _isCommitScheduled;
		private int _isRenderScheduled;

		private Android.Views.View? _view;

		//private int _gateId;
		//private int _gateCount;

		/// <summary>
		/// Set the visual of the content root of the window.
		/// </summary>
		/// <remarks>This might be invoked more than once!</remarks>
		/// <param name="rootVisual">The visual of the root element of the visual tree.</param>
		internal void SetRootVisual(Android.Views.View view, Visual? rootVisual)
		{
			//var count = Interlocked.Increment(ref _gateCount);
			//var id = Interlocked.Increment(ref _gateId);
			//Console.WriteLine($"Acquiring lock ROOT - id: {id} | count: {count}");

			lock (_gate)
			{
				//Console.WriteLine($"Acquired lock ROOT - id: {id} | count: {_gateCount}");

				_view = view;
				_rootVisual = rootVisual;
				SetRootVisualPartial(rootVisual);
			}

			//count = Interlocked.Decrement(ref _gateCount);
			//Console.WriteLine($"Released lock ROOT - id: {id} | count: {count}");


			//var reqId = 0;
			//var timer = DispatcherQueue.GetForCurrentThread().CreateTimer();
			//timer.Tick += (snd, e) =>
			//{
			//	try
			//	{
			//		Console.WriteLine($"Recurrent commit {++reqId}");
			//		Commit();
			//	}
			//	catch (Exception error)
			//	{
			//		Console.WriteLine($"Recurrent commit {reqId} failed: {error}");
			//	}
			//};
			//timer.Interval = TimeSpan.FromSeconds(1);
			//timer.IsRepeating = true;
			//timer.Start();

			//_dispatcher!.RunAsync(CoreDispatcherPriority.Normal, RecurrentCommit);
			//async void RecurrentCommit()
			//{
			//	try
			//	{
			//		await Task.Delay(1000);
			//		Console.WriteLine($"Recurrent commit {++reqId}");
			//		Commit();
			//		_dispatcher!.RunAsync(CoreDispatcherPriority.Normal, RecurrentCommit);
			//	}
			//	catch (Exception error)
			//	{
			//		Console.WriteLine($"Recurrent commit {reqId} failed: {error}");
			//	}
			//}
		}
		partial void SetRootVisualPartial(Visual? rootVisual);


		//private int _commitScheduleCount;
		private int _commitRunCount;


		private void RequestCommit()
		{
			if (Interlocked.CompareExchange(ref _isCommitScheduled, 1, 0) == 0)
			{

			}
			{
				//Console.WriteLine("*********** Scheduling commit");
				ScheduleCommit();
			}
			//else
			//{
			//	Console.WriteLine($"*********** [{Thread.CurrentThread.ManagedThreadId}] Commit already requested");
			//}
		}

		partial void ScheduleCommit();

		/// <summary>
		/// Commits the **dependent** pending changes made on the visuals to the compositor thread.
		/// </summary>
		/// <remarks>This has to be invoked from the UI thread and is expected to be incredibly fast.</remarks>
		internal void Commit()
		{
			Console.WriteLine($"*********** [{Thread.CurrentThread.ManagedThreadId}] Running commit {Interlocked.Increment(ref _commitRunCount)}");

#if DEBUG
			CoreDispatcher.CheckThreadAccess();
#endif

			// We clear the flag immediately so invalidate works again ...
			// it's preferable to loop once for nothing than forgetting some changes
			_isCommitScheduled = 0;

			//_view?.Layout(0, 0, 1920 * 2, 1080 * 2);

			//var count = Interlocked.Increment(ref _gateCount);
			//var id = Interlocked.Increment(ref _gateId);
			//Console.WriteLine($"Acquiring lock COMMIT - id: {id} | count: {count}");

			lock (_gate)
			{
				//Console.WriteLine($"Acquired lock COMMIT - id: {id} | count: {_gateCount}");

				VisualDirtyStateHelper.Reset(ref _dirtyState, CompositionPropertyType.Dependent);
				var dirtyRoots = Interlocked.Exchange(ref _dirtyRoots, ImmutableList<Visual>.Empty);

				foreach (var root in dirtyRoots)
				{
					root.Commit();

					root.CheckChildrenPendingDirtyState();
				}
			}

			//count = Interlocked.Decrement(ref _gateCount);
			//Console.WriteLine($"Released lock COMMIT - id: {id} | count: {count}");

			RequestRender();
		}

		private void RequestRender()
		{
			if (Uno.CompositionConfiguration.UseCompositorThread)
			{
				if (Interlocked.CompareExchange(ref _isRenderScheduled, 1, 0) == 0)
				{
					ScheduleRender();
				}
			}
			else
			{
				Render(DateTime.UtcNow.Ticks);
			}
		}

		partial void ScheduleRender();

		/// <summary>
		/// Renders the visual root onto the native drawing surface.
		/// </summary>
		/// <remarks>If the platform has a compositor thread, this is expected to be invoked on that thread.</remarks>
		internal void Render(long frameTimeTicks)
		{
			// We clear the flag immediately so invalidate works again ...
			// it's preferable to loop once for nothing than forgetting some changes
			_isRenderScheduled = 0;

			//var count = Interlocked.Increment(ref _gateCount);
			//var id = Interlocked.Increment(ref _gateId);
			//Console.WriteLine($"Acquiring lock RENDER - id: {id} | count: {count}");

			lock (_gate)
			{
				//Console.WriteLine($"Acquired lock RENDER - id: {id} | count: {_gateCount}");

				VisualDirtyStateHelper.Reset(ref _dirtyState, CompositionPropertyType.Independent);

				// TODO: Update animations!

				//using var session = Visual.Edit(RootNode);
				//session?.Canvas.DrawARGB(255,0,255,0);

				_rootVisual?.Render();
			}

			//count = Interlocked.Decrement(ref _gateCount);
			//Console.WriteLine($"Released lock RENDER - id: {id} | count: {count}");

			// TODO: if(_pendingAnimations.Any()) { RequestRender(); }
		}
	}
}
