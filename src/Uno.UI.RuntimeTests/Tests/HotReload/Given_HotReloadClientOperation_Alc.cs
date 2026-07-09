#if HAS_UNO_WINUI
#nullable enable

using System;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.RemoteControl.HotReload;
using _Op = Uno.UI.RemoteControl.HotReload.ClientHotReloadProcessor.HotReloadClientOperation;
using _Source = Uno.UI.RemoteControl.HotReload.ClientHotReloadProcessor.HotReloadSource;

namespace Uno.UI.RuntimeTests.Tests.HotReload;

/// <summary>
/// A local hot-reload operation holds the array of hot-reloaded <see cref="Type"/> objects. In a
/// downstream host that loads previewed apps into their own collectible <see cref="AssemblyLoadContext"/>s
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
		var collectibleAlc = new AssemblyLoadContext("Given_HotReloadClientOperation_Alc.collectible", isCollectible: true);
		try
		{
			// A type loaded into a collectible ALC stands in for a previewed-app hot-reloaded type.
			var collectibleType = collectibleAlc
				.LoadFromAssemblyPath(typeof(Given_HotReloadClientOperation_Alc).Assembly.Location)
				.GetType(typeof(Given_HotReloadClientOperation_Alc).FullName!, throwOnError: true)!;

			Assert.AreSame(collectibleAlc, AssemblyLoadContext.GetLoadContext(collectibleType.Assembly), "Pre-condition: the type must belong to the collectible ALC.");

			var op = new _Op(_Source.Manual, new[] { collectibleType }, static () => { });

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
			collectibleAlc.Unload();
		}
	}
}
#endif
