#nullable enable

using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.Dispatching;

namespace Uno.UI.Tests.Dispatching;

/// <summary>
/// <see cref="NativeDispatcher"/>'s gate protects the per-priority queues and the composition-target
/// render registrations, which are written from any thread (a background thread scheduling work, a
/// rendering thread requesting a frame) and read on the UI thread. These tests drive that gate from
/// several threads at once so a broken mutual-exclusion guarantee shows up as a failure here rather
/// than as a corrupted queue or a lost render registration at runtime.
/// </summary>
/// <remarks>
/// Only self-cleaning APIs are exercised: every render registration made here is removed again, so
/// the process-wide <see cref="NativeDispatcher.Main"/> is left exactly as it was found. The queue
/// draining loop is not compiled in the reference assembly used by unit tests, so enqueued work
/// items would otherwise never be consumed.
/// </remarks>
[TestClass]
public class Given_NativeDispatcher_Concurrency
{
	private const int Threads = 8;
	private const int Iterations = 1_000;

	[TestMethod]
	public void When_Concurrent_EnqueueRender_And_Remove_Then_Registrations_Are_Not_Corrupted()
	{
		var dispatcher = NativeDispatcher.Main;
		var errors = new ConcurrentBag<Exception>();

		RunConcurrently(() =>
		{
			for (var i = 0; i < Iterations; i++)
			{
				var compositionTarget = new object();

				dispatcher.EnqueueRender(compositionTarget, static () => { });
				dispatcher.RemoveCompositionTargets(key => ReferenceEquals(key, compositionTarget));
			}
		}, errors);

		Assert.AreEqual(0, errors.Count, string.Join(Environment.NewLine, errors.Select(e => e.ToString())));
	}

	[TestMethod]
	public void When_Remove_Runs_Concurrently_Then_Other_Targets_Are_Untouched()
	{
		var dispatcher = NativeDispatcher.Main;
		var errors = new ConcurrentBag<Exception>();

		var survivor = new object();
		dispatcher.EnqueueRender(survivor, static () => { });

		try
		{
			RunConcurrently(() =>
			{
				for (var i = 0; i < Iterations; i++)
				{
					var compositionTarget = new object();

					dispatcher.EnqueueRender(compositionTarget, static () => { });
					dispatcher.RemoveCompositionTargets(key => ReferenceEquals(key, compositionTarget));
				}
			}, errors);

			Assert.AreEqual(0, errors.Count, string.Join(Environment.NewLine, errors.Select(e => e.ToString())));

			// No predicate above ever matched the survivor, so its registration must still be there.
			var found = false;
			dispatcher.RemoveCompositionTargets(key =>
			{
				var isSurvivor = ReferenceEquals(key, survivor);
				found |= isSurvivor;
				return isSurvivor;
			});

			Assert.IsTrue(found, "The unrelated render registration was lost during concurrent removals.");
		}
		finally
		{
			dispatcher.RemoveCompositionTargets(key => ReferenceEquals(key, survivor));
		}
	}

	private static void RunConcurrently(Action body, ConcurrentBag<Exception> errors)
	{
		using var barrier = new Barrier(Threads);

		var workers = Enumerable
			.Range(0, Threads)
			.Select(_ => Task.Factory.StartNew(
				() =>
				{
					barrier.SignalAndWait();

					try
					{
						body();
					}
					catch (Exception error)
					{
						errors.Add(error);
					}
				},
				TaskCreationOptions.LongRunning))
			.ToArray();

		Task.WaitAll(workers);
	}
}
