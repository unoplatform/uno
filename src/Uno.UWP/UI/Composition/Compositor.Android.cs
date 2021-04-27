#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Windows.UI.Core;
using Android.Graphics;
using Android.Views;
using Uno.UI;
using Uno.UI.Composition;

namespace Windows.UI.Composition
{
	partial class Compositor
	{
		private readonly UIAsyncOperation _commitOperation;

		private Window? _window;
		private CompositorThread? _compositor;
		private CoreDispatcher? _dispatcher;

		internal RenderNode RootNode { get; } = new RenderNode("ui-root");

		public Compositor()
		{
			_commitOperation = new UIAsyncOperation(Commit, null);
		}

		internal void SetActivity(Android.App.Activity applicationActivity)
		{
			if (_window is { } current)
			{
				// This might happen if the application activity is re-created!

				Debug.Assert(current == applicationActivity.Window);
				return;
			}

			_window = applicationActivity.Window ?? throw new ArgumentException("The activity does not have a window yet.");

			_dispatcher = CoreDispatcher.Main ?? throw new ArgumentException("Dispatcher is not valid.");
			ScheduleCommit();

			if (Uno.CompositionConfiguration.UseCompositorThread)
			{
				_compositor = CompositorThread.Start(this, _window);
				ScheduleRender();
			}
		}

		partial void SetRootVisualPartial(Visual? rootVisual)
		{
			// If rootVisual is null, we draw using an empty Visual to clear the RootNode content.
			(rootVisual ?? new Visual(this)).DrawOn(RootNode);
		}

		/// <summary>
		/// Renders a visual onto a native canvas
		/// </summary>
		internal void Render(Visual visual, Canvas canvas)
		{
			// Note:
			//	We do NOT Commit() here!
			//	This method is used either by explicitly calling the Android.View.Draw() method (to take screen-shot for instance),
			//	in that case rendering the view at it's current state is fine;
			//	either when we have a managed view nested in a native view, then the Commit() should have already been invoked.

			var count = Interlocked.Increment(ref _gateCount);
			var id = Interlocked.Increment(ref _gateId);
			Console.WriteLine($"Acquiring lock RENDER NATIVE - id: {id} | count: {count}");

			lock (_gate)
			{
				Console.WriteLine($"Acquired lock RENDER NATIVE  - id: {id} | count: {_gateCount}");

				visual.DrawOn(canvas);
			}

			count = Interlocked.Decrement(ref _gateCount);
			Console.WriteLine($"Released lock RENDER NATIVE - id: {id} | count: {count}");
		}


		private int _commitScheduleCount;
		partial void ScheduleCommit()
		{
			//_dispatcher?.RunAnimation(_commitOperation);

			var d = _dispatcher;
			if (d is null)
			{
				Console.WriteLine($"*********** [{Thread.CurrentThread.ManagedThreadId}] CANNOT Scheduling commit: No dispatcher !!!");
				return;
			}

			var id = Interlocked.Increment(ref _commitScheduleCount);
			Console.WriteLine($"*********** [{Thread.CurrentThread.ManagedThreadId}] Scheduling commit #{id}");
			d.RunAsync(CoreDispatcherPriority.High, () =>
			{
				Console.WriteLine($"*********** [{Thread.CurrentThread.ManagedThreadId}] Running commit #{id}");
				Commit();
			});
		}

		partial void ScheduleRender()
			=> _compositor?.RequestFrame();
	}
}
