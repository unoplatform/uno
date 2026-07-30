#if HAS_UNO_WINUI
#nullable enable

using System;
using System.Runtime.CompilerServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.RemoteControl.HotReload;
using _Op = Uno.UI.RemoteControl.HotReload.ClientHotReloadProcessor.HotReloadClientOperation;
using _Source = Uno.UI.RemoteControl.HotReload.ClientHotReloadProcessor.HotReloadSource;

namespace Uno.UI.RuntimeTests.Tests.HotReload;

/// <summary>
/// A local hot-reload operation holds the array of hot-reloaded <see cref="Type"/> objects. In a
/// downstream host that loads previewed apps into their own collectible <see cref="System.Runtime.Loader.AssemblyLoadContext"/>s
/// those are the app's collectible types, so a retained (historical) operation would pin the context
/// after unload. Once an operation reaches a terminal state it must drop the raw type array while
/// keeping its curated (string) display list.
/// </summary>
[TestClass]
public class Given_HotReloadClientOperation_Alc
{
	[TestMethod]
	public void When_Operation_Completed_Then_Raw_Types_Released_But_Curated_Kept()
	{
		// The raw-Type[] release this test guards is type-agnostic, so the assertions below hold for any
		// type. Where the platform can materialize a *genuinely collectible* type (desktop/CoreCLR: a real
		// on-disk Assembly.Location that can be re-loaded into a collectible ALC) we use one so the
		// previewed-app scenario is mirrored exactly. On WASM/Android assemblies are bundled with no
		// loadable on-disk path, so we fall back to an ordinary type; the operation's release behaviour —
		// what this test asserts — is identical. Collectible-ALC *collection* itself is covered by
		// AlcUnloadMemoryRuntimeTests.
		global::System.Runtime.Loader.AssemblyLoadContext? collectibleAlc = null;
		try
		{
			var type = typeof(Given_HotReloadClientOperation_Alc);
			var assemblyLocation = type.Assembly.Location;
			if (assemblyLocation is { Length: > 0 })
			{
				try
				{
					// A type loaded into a collectible ALC stands in for a previewed-app hot-reloaded type.
					collectibleAlc = new global::System.Runtime.Loader.AssemblyLoadContext("Given_HotReloadClientOperation_Alc.collectible", isCollectible: true);
					type = collectibleAlc
						.LoadFromAssemblyPath(assemblyLocation)
						.GetType(type.FullName!, throwOnError: true)!;

					Assert.AreSame(collectibleAlc, global::System.Runtime.Loader.AssemblyLoadContext.GetLoadContext(type.Assembly), "Pre-condition: the type must belong to the collectible ALC.");
				}
				catch (Exception ex) when (ex is System.IO.FileNotFoundException or System.IO.FileLoadException or BadImageFormatException or NotSupportedException or ArgumentException)
				{
					// The platform reported a path but cannot re-load the assembly into a separate
					// collectible ALC (e.g. a bundled/embedded assembly). Fall back to the ordinary type.
					collectibleAlc?.Unload();
					collectibleAlc = null;
					type = typeof(Given_HotReloadClientOperation_Alc);
				}
			}

			var op = new _Op(_Source.Manual, new[] { type }, static () => { });

			Assert.AreEqual(1, op.Types.Length, "Pre-condition: the live operation must retain its raw types.");
			var curatedBefore = op.CuratedTypes;
			CollectionAssert.Contains(curatedBefore, nameof(Given_HotReloadClientOperation_Alc), "Pre-condition: the curated list must contain the type's display name.");

			op.ReportCompleted();

			Assert.AreEqual(
				0,
				op.Types.Length,
				"A terminal operation must drop its raw Type[]; otherwise a retained operation pins every collectible previewed-app ALC that was hot-reloaded.");
			CollectionAssert.AreEqual(
				curatedBefore,
				op.CuratedTypes,
				"The curated (string) display list must survive the raw-type release so history is preserved.");
		}
		finally
		{
			collectibleAlc?.Unload();
		}
	}

	[TestMethod]
	public void When_Operation_Failed_Then_Exception_Graph_Detached()
	{
		// A previewed-app exception reported to an operation pins the app's collectible ALC even
		// after the raw Type[] release: the exception's runtime type, InnerException chain, Data
		// entries and TargetSite all reference the ALC. A terminal operation must therefore detach
		// the graph, keeping only a default-ALC summary (type name + message + stack text).
		var original = new InvalidOperationException("boom", new InvalidOperationException("inner"));

		var op = new _Op(_Source.Manual, new[] { typeof(Given_HotReloadClientOperation_Alc) }, static () => { });
		op.ReportError(original);
		op.ReportCompleted();

		Assert.AreEqual(1, op.Exceptions.Count, "The terminal detach must preserve the exception count.");

		var detached = op.Exceptions[0];
		Assert.IsFalse(
			ReferenceEquals(original, detached),
			"A terminal operation must not retain the original exception instance: its type, InnerException, Data and TargetSite all pin the previewed app's collectible ALC.");
		Assert.IsNull(detached.InnerException, "The detached summary must not carry the original InnerException graph.");
		StringAssert.Contains(detached.Message, "boom", "The summary message must preserve the original message text.");
		StringAssert.Contains(detached.Message, nameof(InvalidOperationException), "The summary message must preserve the original type name.");
		StringAssert.Contains(detached.Message, "inner", "The summary message must preserve the inner exception's text.");
	}

	[TestMethod]
	public void When_TypeCorrelationScope_Active_Then_Types_Retained_Until_Sweep()
	{
		// The raw Type[] is the pause-correlation payload read by the client API AFTER awaiting
		// completion (pauseHandle.Drop). While a type-correlation scope is active, a terminal
		// operation must retain the array; the scope owner's explicit sweep then releases it,
		// preserving the collectible-ALC release guarantee.
		var op = new _Op(_Source.Manual, new[] { typeof(Given_HotReloadClientOperation_Alc) }, static () => { });

		var scope = _Op.EnterTypeCorrelationScope();
		try
		{
			op.ReportCompleted();

			Assert.AreEqual(
				1,
				op.Types.Length,
				"While a type-correlation scope is active, a terminal operation must retain its raw Type[] so the scope owner can correlate it with a UI pause.");
		}
		finally
		{
			scope.Dispose();
		}

		_Op.ReleaseRetainedTypesForTerminalOperations(new[] { op });

		Assert.AreEqual(
			0,
			op.Types.Length,
			"The scope owner's sweep must release the retained raw Type[]; otherwise a retained operation pins the collectible previewed-app ALC.");
	}
}
#endif
