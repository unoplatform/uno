#nullable enable

using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.DataBinding;

namespace Uno.UI.Tests;

/// <summary>
/// <see cref="WeakReferencePool"/> is a process-wide pool reached from every binding and weak-event
/// subscription, on any thread. These tests hammer it from several threads at once so a broken
/// mutual-exclusion guarantee (e.g. handing the same pooled <c>ManagedGCHandle</c> to two renters,
/// or corrupting the backing <c>Stack&lt;T&gt;</c>) fails here instead of surfacing as a random
/// binding pointing at someone else's object.
/// </summary>
[TestClass]
public class Given_WeakReferencePool_Concurrency
{
	private const int Threads = 8;
	private const int Iterations = 2_000;

	// ClearCache drops the pooled handles, so this run allocates a fresh GCHandle almost every
	// iteration; keep it shorter to bound the finalization pressure it creates.
	private const int ClearCacheIterations = 500;

	[TestInitialize]
	public void Initialize() => WeakReferencePool.ClearCache();

	[TestCleanup]
	public void Cleanup() => WeakReferencePool.ClearCache();

	[TestMethod]
	public void When_Concurrent_Rent_And_Return_Then_Handles_Are_Never_Shared()
	{
		var failures = new ConcurrentBag<string>();

		RunConcurrently(() =>
		{
			for (var i = 0; i < Iterations; i++)
			{
				var owner = new object();
				var target = new object();

				var reference = WeakReferencePool.RentWeakReference(owner, target);

				// A pooled handle handed out twice would have had its Target overwritten by the
				// other renter between the Pop and this read.
				if (!ReferenceEquals(reference.GetUnsafeTargetHandle().Target, target))
				{
					failures.Add("Rented target handle does not point at the requested target.");
				}

				if (!ReferenceEquals(reference.Owner, owner))
				{
					failures.Add("Rented owner handle does not point at the requested owner.");
				}

				WeakReferencePool.ReturnWeakReference(owner, reference);

				GC.KeepAlive(owner);
				GC.KeepAlive(target);
			}
		});

		Assert.AreEqual(0, failures.Count, string.Join(Environment.NewLine, failures.Distinct()));
	}

	[TestMethod]
	public void When_Concurrent_Return_Then_Pool_Never_Exceeds_MaxReferences()
	{
		var max = WeakReferencePool.MaxReferences;

		RunConcurrently(() =>
		{
			for (var i = 0; i < Iterations; i++)
			{
				var owner = new object();
				WeakReferencePool.ReturnWeakReference(owner, WeakReferencePool.RentWeakReference(owner, new object()));
			}
		});

		// Each Return pushes two handles under a single Count check, so the pool can overshoot the
		// limit by one entry, but a lost update would let it grow without bound.
		Assert.IsTrue(
			WeakReferencePool.PooledReferences <= max + 1,
			$"Pool grew to {WeakReferencePool.PooledReferences} entries, above the {max} limit.");
	}

	[TestMethod]
	public void When_Concurrent_Rent_And_ClearCache_Then_Rented_References_Stay_Valid()
	{
		var failures = new ConcurrentBag<string>();
		using var stop = new CancellationTokenSource();

		var clearing = Task.Factory.StartNew(
			() =>
			{
				while (!stop.IsCancellationRequested)
				{
					WeakReferencePool.ClearCache();
				}
			},
			TaskCreationOptions.LongRunning);

		try
		{
			RunConcurrently(() =>
			{
				for (var i = 0; i < ClearCacheIterations; i++)
				{
					var owner = new object();
					var target = new object();

					var reference = WeakReferencePool.RentWeakReference(owner, target);
					if (!ReferenceEquals(reference.Target, target))
					{
						failures.Add("A reference rented while the cache was cleared lost its target.");
					}

					WeakReferencePool.ReturnWeakReference(owner, reference);

					GC.KeepAlive(owner);
					GC.KeepAlive(target);
				}
			});
		}
		finally
		{
			stop.Cancel();
			clearing.Wait();
		}

		Assert.AreEqual(0, failures.Count, string.Join(Environment.NewLine, failures.Distinct()));
	}

	private static void RunConcurrently(Action body)
	{
		using var barrier = new Barrier(Threads);

		var workers = Enumerable
			.Range(0, Threads)
			.Select(_ => Task.Factory.StartNew(
				() =>
				{
					barrier.SignalAndWait();
					body();
				},
				TaskCreationOptions.LongRunning))
			.ToArray();

		Task.WaitAll(workers);
	}
}
