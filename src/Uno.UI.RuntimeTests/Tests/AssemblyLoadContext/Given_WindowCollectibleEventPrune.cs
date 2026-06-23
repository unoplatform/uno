#if HAS_UNO
#nullable enable

using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.UI.Xaml;
using Private.Infrastructure;
using Windows.Foundation;

namespace Uno.UI.RuntimeTests.Tests.AssemblyLoadContext;

/// <summary>
/// Window-level events (Activated / SizeChanged / VisibilityChanged / Closed) live on the window
/// implementation object, which is not part of any visual tree — a secondary-ALC subscriber on
/// the HOST window survives unload (the visual-tree event prune never reaches it) and pins its
/// AssemblyLoadContext. The ALC teardown cleanup must prune window-level subscriptions whose
/// targets are collectible.
/// </summary>
[TestClass]
[RunsOnUIThread]
// The prune runs identically on every Skia target, but the assertion depends on GC reclaiming the
// now-unreferenced collectible target within a bounded GC.Collect() loop. The WASM and UIKit (Mono)
// runtimes have non-deterministic/conservative GC and don't reliably reclaim it, so the assertion is
// unreliable there. Coverage stays on CoreCLR-backed Skia (Desktop/Android) + WinAppSDK.
[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.SkiaWasm | RuntimeTestPlatforms.SkiaUIKit)]
public class Given_WindowCollectibleEventPrune
{
	[TestMethod]
	public void When_Collectible_Subscriber_On_Window_Closed_Then_Cleanup_Prunes_It()
	{
		var window = TestServices.WindowHelper.CurrentTestWindow;
		Assert.IsNotNull(window, "Pre-condition: a test window must be available");

		// Only the event subscription itself may hold the target strongly — the test keeps just
		// a WeakReference, so the assertion measures whether the prune removed the subscription.
		var weakTarget = SubscribeCollectibleHandler(window!);
		try
		{
			Application.CleanupNonDefaultAlcCaches();

			for (var i = 0; i < 10 && weakTarget.IsAlive; i++)
			{
				GC.Collect();
				GC.WaitForPendingFinalizers();
			}

			Assert.IsFalse(
				weakTarget.IsAlive,
				"ALC teardown cleanup must prune window-level event subscriptions whose target " +
				"lives in a collectible assembly; otherwise the host window pins the unloaded ALC.");
		}
		finally
		{
			// Failure path only: the subscription is still attached, so the target is still
			// reachable — re-create an equivalent delegate from the weak target to detach it and
			// not leak into later tests. On the success path the target is gone and this no-ops.
			if (weakTarget.Target is { } survivingTarget)
			{
				var method = survivingTarget.GetType().GetMethod("OnClosed")!;
				window!.Closed -= (TypedEventHandler<object, WindowEventArgs>)Delegate.CreateDelegate(
					typeof(TypedEventHandler<object, WindowEventArgs>),
					survivingTarget,
					method);
			}
		}
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static WeakReference SubscribeCollectibleHandler(Window window)
	{
		var target = CreateCollectibleHandlerTarget(out var method);
		var handler = (TypedEventHandler<object, WindowEventArgs>)Delegate.CreateDelegate(
			typeof(TypedEventHandler<object, WindowEventArgs>),
			target,
			method);

		window.Closed += handler;

		return new WeakReference(target);
	}

	/// <summary>
	/// Emits a handler type with an instance method compatible with
	/// <see cref="TypedEventHandler{TSender, TResult}"/> of (object, WindowEventArgs) into a
	/// RunAndCollect (collectible) assembly — the same collectibility shape as an unloaded
	/// secondary app's subscriber.
	/// </summary>
	[MethodImpl(MethodImplOptions.NoInlining)]
	private static object CreateCollectibleHandlerTarget(out MethodInfo method)
	{
		if (!RuntimeFeature.IsDynamicCodeSupported)
		{
			Assert.Inconclusive("Reflection.Emit (RunAndCollect) is not supported on this target.");
		}

		var typeBuilder = AssemblyBuilder
			.DefineDynamicAssembly(new AssemblyName("CollectibleWindowHandlerProbe"), AssemblyBuilderAccess.RunAndCollect)
			.DefineDynamicModule("main")
			.DefineType("CollectibleHandler", TypeAttributes.Public);

		var methodBuilder = typeBuilder.DefineMethod(
			"OnClosed",
			MethodAttributes.Public | MethodAttributes.HideBySig,
			typeof(void),
			new[] { typeof(object), typeof(WindowEventArgs) });
		methodBuilder.GetILGenerator().Emit(OpCodes.Ret);

		var handlerType = typeBuilder.CreateType()!;
		Assert.IsTrue(handlerType.Assembly.IsCollectible, "Pre-condition: the probe handler type must be collectible");

		method = handlerType.GetMethod("OnClosed")!;
		return Activator.CreateInstance(handlerType)!;
	}
}
#endif
