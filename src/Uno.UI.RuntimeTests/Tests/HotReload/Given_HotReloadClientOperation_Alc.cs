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
}
#endif
