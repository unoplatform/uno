#nullable enable

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Loader;
using Microsoft.UI.Xaml.Media;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Uno.UI.Tests.Windows_UI_Xaml_Media;

/// <summary>
/// Ownership predicate of the <c>CompositionTarget.Rendering</c> ALC sweep
/// (<see cref="CompositionTargetHandlerSweep"/>): the delegate's <c>Target</c> and its
/// <c>Method.DeclaringType</c> are INDEPENDENT ownership sources and must both be checked —
/// coalescing them lets a collectible-ALC method survive behind a default-ALC target. Covers the
/// all-non-default (global teardown) and single-dying-ALC (scoped) paths.
/// </summary>
[TestClass]
public class Given_CompositionTargetHandlerSweep
{
	/// <summary>Hosts the handler shapes; also re-loaded into collectible ALCs by the tests.</summary>
	public class HandlerHost
	{
		public void OnRendering(object? sender, object e)
		{
		}

		public static void OnRenderingStatic(object? sender, object e)
		{
		}

		// Closed-static shape: Delegate.CreateDelegate(..., firstArgument, method) binds the first
		// parameter, producing an EventHandler<object> whose Target is the bound argument while
		// Method.DeclaringType remains this type.
		public static void OnRenderingWithState(object state, object? sender, object e)
		{
		}
	}

	[TestMethod]
	public void When_Collectible_Target_Then_Removed_And_Defaults_Kept()
	{
		var collectibleAlc = new AssemblyLoadContext("HandlerSweep.target", isCollectible: true);
		try
		{
			var collectibleHandler = CreateInstanceHandler(collectibleAlc);
			var defaultInstanceHandler = (EventHandler<object>)new HandlerHost().OnRendering;
			var defaultStaticHandler = (EventHandler<object>)HandlerHost.OnRenderingStatic;

			Assert.IsNull(defaultStaticHandler.Target, "Pre-condition: an open static handler must have a null Target.");

			var handlers = new List<EventHandler<object>> { collectibleHandler, defaultInstanceHandler, defaultStaticHandler };

			var removed = CompositionTargetHandlerSweep.RemoveAlcHandlers(handlers, scope: null);

			Assert.AreEqual(1, removed, "Exactly the collectible-ALC-owned handler must be removed.");
			CollectionAssert.AreEqual(
				new[] { defaultInstanceHandler, defaultStaticHandler },
				handlers,
				"Default-ALC handlers — including the static (Target == null) shape, which pins nothing — must survive the sweep.");
		}
		finally
		{
			collectibleAlc.Unload();
		}
	}

	[TestMethod]
	public void When_Collectible_Method_Behind_Default_Target_Then_Removed()
	{
		// The coalescing-bug shape: a closed delegate pairing a DEFAULT-ALC Target with a
		// collectible-ALC method. Judging ownership by `target ?? method` would let the
		// collectible method survive behind the non-null default-ALC target.
		var collectibleAlc = new AssemblyLoadContext("HandlerSweep.method", isCollectible: true);
		try
		{
			var collectibleHostType = LoadHostType(collectibleAlc);
			var method = collectibleHostType.GetMethod(nameof(HandlerHost.OnRenderingWithState), BindingFlags.Public | BindingFlags.Static)!;

			var defaultAlcState = new object();
			var handler = (EventHandler<object>)Delegate.CreateDelegate(typeof(EventHandler<object>), defaultAlcState, method);

			Assert.AreSame(defaultAlcState, handler.Target, "Pre-condition: the closed-static delegate's Target must be the default-ALC bound argument.");
			Assert.IsTrue(handler.Method.DeclaringType!.IsCollectible, "Pre-condition: the delegate's method must be declared on the collectible-ALC type.");

			var handlers = new List<EventHandler<object>> { handler };

			var removed = CompositionTargetHandlerSweep.RemoveAlcHandlers(handlers, scope: null);

			Assert.AreEqual(
				1,
				removed,
				"A collectible-ALC method must not survive behind a default-ALC target: Target and Method.DeclaringType are independent ownership sources.");
			Assert.AreEqual(0, handlers.Count);
		}
		finally
		{
			collectibleAlc.Unload();
		}
	}

	[TestMethod]
	public void When_Scoped_To_Dying_Alc_Then_Sibling_Alc_Handler_Kept()
	{
		// Removal is destructive (a dropped handler is never re-subscribed): tearing down one
		// previewed app must not stop a live sibling secondary app's rendering.
		var dyingAlc = new AssemblyLoadContext("HandlerSweep.dying", isCollectible: true);
		var siblingAlc = new AssemblyLoadContext("HandlerSweep.sibling", isCollectible: true);
		try
		{
			var dyingHandler = CreateInstanceHandler(dyingAlc);
			var siblingHandler = CreateInstanceHandler(siblingAlc);
			var defaultHandler = (EventHandler<object>)new HandlerHost().OnRendering;

			var handlers = new List<EventHandler<object>> { dyingHandler, siblingHandler, defaultHandler };

			var removed = CompositionTargetHandlerSweep.RemoveAlcHandlers(handlers, scope: dyingAlc);

			Assert.AreEqual(1, removed, "The scoped sweep must remove exactly the dying ALC's handler.");
			CollectionAssert.AreEqual(
				new[] { siblingHandler, defaultHandler },
				handlers,
				"A live sibling secondary ALC's handler must survive a sweep scoped to a different dying ALC.");
		}
		finally
		{
			dyingAlc.Unload();
			siblingAlc.Unload();
		}
	}

	private static Type LoadHostType(AssemblyLoadContext alc)
	{
		var hostType = typeof(HandlerHost);
		var assembly = alc.LoadFromAssemblyPath(hostType.Assembly.Location);
		return assembly.GetType(hostType.FullName!, throwOnError: true)!;
	}

	private static EventHandler<object> CreateInstanceHandler(AssemblyLoadContext alc)
	{
		var hostType = LoadHostType(alc);
		var instance = Activator.CreateInstance(hostType)!;
		var method = hostType.GetMethod(nameof(HandlerHost.OnRendering), BindingFlags.Public | BindingFlags.Instance)!;
		return (EventHandler<object>)Delegate.CreateDelegate(typeof(EventHandler<object>), instance, method);
	}
}
