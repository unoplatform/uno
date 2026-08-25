#nullable enable

using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Foundation.Logging;

namespace Uno.UI.Tests.Foundation
{
	/// <summary>
	/// <see cref="LogExtensionPoint"/> memoizes one <see cref="Logger"/> per <see cref="Type"/> in a
	/// process-lifetime cache. A <see cref="Type"/> key from a collectible
	/// <see cref="AssemblyLoadContext"/> roots that context's LoaderAllocator, so with a strongly
	/// keyed cache any type of a previewed app that logs even once keeps the app's whole context —
	/// assemblies, metadata and everything they reference — resident for the process lifetime. The
	/// cache is a pure memoization, so its keys must be weak.
	/// </summary>
	[TestClass]
	public class Given_LogExtensionPoint_Alc
	{
		[TestMethod]
		public void When_Collectible_Alc_Type_Logs_Then_Alc_Is_Collected()
		{
			var weakAlc = LogFromCollectibleAlcAndUnload();

			Assert.IsTrue(
				TryWaitUntilCollected(weakAlc),
				"A type that logged once must not keep its collectible AssemblyLoadContext alive: a strongly keyed logger cache holds the Type key, which roots the context's LoaderAllocator.");
		}

		[TestMethod]
		public void When_Live_Type_Logs_Twice_Then_Weak_Cache_Memoizes_Logger()
		{
			// Non-regression guard for the memoization itself: LoggerFactory also caches by type
			// NAME, so comparing the two returned loggers alone cannot tell a working per-type cache
			// from no cache at all — the entry is therefore asserted at the cache.
			var first = LogExtensionPoint.Log(new LoggerCacheProbe());
			var second = LogExtensionPoint.Log(new LoggerCacheProbe());

			Assert.IsNotNull(first, "Logging must return a logger.");
			Assert.AreSame(first, second, "The same live type must keep resolving to the same logger.");

			Assert.IsTrue(
				TryGetCachedLogger(typeof(LoggerCacheProbe), out var cached),
				"A logged type must have an entry in the logger cache while it is alive.");
			Assert.AreSame(
				first,
				cached,
				"The cached entry must be the logger handed to callers — the weak-keyed table must still memoize, not merely pass through to the factory.");
		}

		/// <summary>
		/// Logs from a type of a collectible <see cref="AssemblyLoadContext"/>, unloads it, and
		/// returns a weak reference to the context.
		/// </summary>
		/// <remarks>
		/// Every strong reference to the context, its assembly and its types must stay in a
		/// separate, non-inlined frame: locals in the frame that later runs the GC keep their
		/// objects alive (especially under Debug codegen, which extends lifetimes to the end of the
		/// method).
		/// </remarks>
		[MethodImpl(MethodImplOptions.NoInlining)]
		private static WeakReference LogFromCollectibleAlcAndUnload()
		{
			var alc = new AssemblyLoadContext("Given_LogExtensionPoint_Alc.probe", isCollectible: true);
			var probeType = alc
				.LoadFromAssemblyPath(typeof(Given_LogExtensionPoint_Alc).Assembly.Location)
				.GetType(typeof(LoggerCacheProbe).FullName!, throwOnError: true)!;

			Assert.IsTrue(probeType.IsCollectible, "Pre-condition: the probe type must belong to the collectible ALC.");

			// The Type-as-instance path (`instance as Type`) caches `probeType` under exactly the
			// same key an instance of it calling `this.Log()` would (`typeof(T)`), while keeping the
			// probe a pure metadata load: nothing from the ALC's copy of this assembly is
			// instantiated, so the context is pinned by the logger cache alone.
			Assert.IsNotNull(LogExtensionPoint.Log<object>(probeType), "Pre-condition: logging the collectible type must return a logger.");

			alc.Unload();

			return new WeakReference(alc);
		}

		/// <summary>
		/// Reads the per-type logger cache. Fails loudly if it is not weak-keyed — that is the
		/// property this fixture exists for, and a strongly keyed cache would silently pin every
		/// collectible context again.
		/// </summary>
		private static bool TryGetCachedLogger(Type type, out Logger? logger)
		{
			var field = typeof(LogExtensionPoint).GetField("_loggers", BindingFlags.Static | BindingFlags.NonPublic)
				?? throw new InvalidOperationException("LogExtensionPoint._loggers field was not found.");
			var cache = field.GetValue(null) as ConditionalWeakTable<Type, Logger>
				?? throw new InvalidOperationException(
					"LogExtensionPoint._loggers must be a ConditionalWeakTable<Type, Logger>: a strongly keyed cache pins collectible AssemblyLoadContexts.");

			return cache.TryGetValue(type, out logger);
		}

		private static bool TryWaitUntilCollected(WeakReference reference)
		{
			// Unloading is asynchronous and takes several collections to walk the whole graph.
			for (var i = 0; i < 10 && reference.IsAlive; i++)
			{
				GC.Collect(2, GCCollectionMode.Forced, blocking: true);
				GC.WaitForPendingFinalizers();
			}

			return !reference.IsAlive;
		}
	}

	/// <summary>
	/// Stand-in for a previewed app's type: logged both from the default ALC and from copies of this
	/// assembly loaded into a collectible ALC.
	/// </summary>
	public class LoggerCacheProbe
	{
	}
}
