#if HAS_UNO
#nullable enable

using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.RuntimeTests.Helpers;

namespace Uno.UI.RuntimeTests.Tests.AssemblyLoadContext;

/// <summary>
/// TEMPORARY MEASUREMENT INSTRUMENT — DELETE BEFORE MERGE.
///
/// Establishes, per platform, how a collectible
/// <see cref="System.Runtime.Loader.AssemblyLoadContext"/> actually behaves, because the
/// reproduction's design depends on it and the repo does not record it.
///
/// The first pass established: emit works on both legs (WASM reports
/// <c>IsDynamicCodeSupported=True, IsDynamicCodeCompiled=False</c>), <c>Assembly.Location</c> is
/// empty on WASM so <c>LoadFromAssemblyPath</c> is unavailable there, an EMPTY collectible ALC dies
/// in 1 collect round on desktop but survived 10 on WASM, and <c>HEAPU8</c> did not move across 5
/// tiny emit/unload cycles.
///
/// This pass characterises the two open questions that first pass raised:
/// <list type="number">
///   <item>Does the WASM ALC die at ANY round count, and is its survival merely Mono's conservative
///   stack scan seeing a stale slot? Hence many more rounds, with deliberate stack churn between
///   them to overwrite the staging frame's slots.</item>
///   <item>Does the linear heap move at all once the collectible payload is substantial rather than
///   a single empty type? Hence a many-type/many-method payload over more cycles, sampling both the
///   linear heap and the managed heap so "managed grew" is distinguishable from "linear grew".</item>
/// </list>
///
/// Asserts nothing. Writes findings to the console, which reaches the CI job log on WASM through
/// Chrome's <c>--enable-logging=stderr</c>.
/// </summary>
[TestClass]
[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaDesktop | RuntimeTestPlatforms.SkiaWasm)]
public class Given_CollectibleAlc_Probe
{
	private const string Tag = "[ALC-PROBE]";

	/// <summary>Upper bound on collect rounds before declaring a context un-reclaimed.</summary>
	private const int MaxCollectRounds = 200;

	/// <summary>Cycles for the memory series.</summary>
	private const int MemoryProbeCycles = 25;

	/// <summary>Shape of the substantial payload: types per emitted assembly, methods per type.</summary>
	private const int PayloadTypes = 40;
	private const int PayloadMethodsPerType = 10;

	[TestMethod]
	[Timeout(900_000)]
	public void Probe_ReportsCollectibleAlcCapabilities()
	{
		Report($"platform={RuntimeTestsPlatformHelper.CurrentPlatform} isBrowser={OperatingSystem.IsBrowser()}");
		Report($"runtime={RuntimeInformation()}");
		Report($"IsDynamicCodeSupported={RuntimeFeature.IsDynamicCodeSupported} IsDynamicCodeCompiled={RuntimeFeature.IsDynamicCodeCompiled}");
		Report($"maxCollectRounds={MaxCollectRounds} memoryCycles={MemoryProbeCycles} payload={PayloadTypes}x{PayloadMethodsPerType}");

		ReportAssemblyLocationAvailability();

		// Question 1: does an unloaded collectible ALC ever die on this leg, with the staging frame's
		// stack slots deliberately overwritten between rounds?
		ReportCollection("emptyAlc", static () => CreateAndUnloadEmptyAlc());
		ReportCollection("emitTinyAssembly", static () => EmitTinyCollectibleAssemblyAndTrack());
		ReportCollection("emitPayloadAssembly", static () => EmitPayloadCollectibleAssemblyAndTrack());

		// Question 2: does either heap move with a substantial collectible payload?
		ReportMemorySeries();

		Report("done");
	}

	private static string RuntimeInformation()
		=> $"{System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription} / {System.Runtime.InteropServices.RuntimeInformation.RuntimeIdentifier}";

	/// <summary>
	/// Confirms whether real bytes are loadable at all on this leg — the alternative substrate to
	/// emit, and the one WASM rules out.
	/// </summary>
	private static void ReportAssemblyLocationAvailability()
	{
		var location = typeof(Given_CollectibleAlc_Probe).Assembly.Location;
		Report($"assemblyLocation='{location}' isEmpty={string.IsNullOrEmpty(location)}");
	}

	/// <summary>
	/// Runs the staging factory, then collects with stack churn between rounds, reporting the round
	/// at which the tracked object died or that it never did.
	/// </summary>
	private static void ReportCollection(string label, Func<WeakReference> stage)
	{
		WeakReference tracker;
		try
		{
			tracker = stage();
		}
		catch (Exception error)
		{
			Report($"{label}: STAGING FAILED {error.GetType().Name}: {error.Message}");
			return;
		}

		var deadAtRound = -1;
		for (var round = 1; round <= MaxCollectRounds; round++)
		{
			// Overwrite the staging frame's stack slots before collecting. Mono's interpreter scans
			// the stack conservatively, so a stale slot still referencing the context would look
			// exactly like a real pin — this is what separates the two.
			ChurnStack(depth: 96);

			GC.Collect();
			GC.WaitForPendingFinalizers();
			GC.Collect();

			if (!tracker.IsAlive)
			{
				deadAtRound = round;
				break;
			}
		}

		Report(deadAtRound > 0
			? $"{label}: collected=True atRound={deadAtRound}"
			: $"{label}: collected=False afterRounds={MaxCollectRounds}");
	}

	/// <summary>
	/// Recurses with live object locals so the stack region the staging frame used is definitively
	/// overwritten, then lets it all become garbage.
	/// </summary>
	[MethodImpl(MethodImplOptions.NoInlining)]
	private static object? ChurnStack(int depth)
	{
		if (depth <= 0)
		{
			return null;
		}

		var a = new object();
		var b = new byte[64];
		var c = new object();

		var deeper = ChurnStack(depth - 1);

		GC.KeepAlive(a);
		GC.KeepAlive(b);
		GC.KeepAlive(c);

		return deeper ?? c;
	}

	// Every staging helper below lives in its own non-inlined frame and returns only a
	// WeakReference: locals in the frame that later runs the GC keep their objects alive, and Debug
	// codegen extends local lifetimes to the end of the method.

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static WeakReference CreateAndUnloadEmptyAlc()
	{
		var alc = new System.Runtime.Loader.AssemblyLoadContext("AlcProbeEmpty", isCollectible: true);
		alc.Unload();

		return new WeakReference(alc);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static WeakReference EmitTinyCollectibleAssemblyAndTrack()
	{
		var probeType = EmitCollectibleType("AlcProbeEmitTiny", types: 1, methodsPerType: 0);

		Report($"  emitTinyAssembly: isCollectible={probeType.Assembly.IsCollectible}");

		// Track the Assembly, whose lifetime is the collectible LoaderAllocator's lifetime.
		return new WeakReference(probeType.Assembly);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static WeakReference EmitPayloadCollectibleAssemblyAndTrack()
	{
		var probeType = EmitCollectibleType("AlcProbeEmitPayload", PayloadTypes, PayloadMethodsPerType);

		Report($"  emitPayloadAssembly: isCollectible={probeType.Assembly.IsCollectible}");

		return new WeakReference(probeType.Assembly);
	}

	/// <summary>
	/// Emits a RunAndCollect assembly of the requested shape and returns its first type, having
	/// instantiated it so the type is genuinely used rather than merely defined.
	/// </summary>
	private static Type EmitCollectibleType(string assemblyName, int types, int methodsPerType)
	{
		var module = AssemblyBuilder
			.DefineDynamicAssembly(new AssemblyName(assemblyName), AssemblyBuilderAccess.RunAndCollect)
			.DefineDynamicModule("main");

		Type? first = null;
		for (var t = 0; t < types; t++)
		{
			var typeBuilder = module.DefineType($"ProbeType{t}", TypeAttributes.Public);

			for (var m = 0; m < methodsPerType; m++)
			{
				var methodBuilder = typeBuilder.DefineMethod(
					$"Method{m}",
					MethodAttributes.Public,
					typeof(void),
					Type.EmptyTypes);

				methodBuilder.GetILGenerator().Emit(OpCodes.Ret);
			}

			var created = typeBuilder.CreateType()!;
			first ??= created;

			GC.KeepAlive(Activator.CreateInstance(created));
		}

		return first!;
	}

	/// <summary>
	/// The series the native-reclamation test would assert on. Samples BOTH heaps after a settling
	/// collect: <c>MemoryManager.AppMemoryUsage</c> is <c>Module.HEAPU8.length</c> on WASM
	/// (<c>MemoryManager.wasm.cs</c>) but <c>GC.GetGCMemoryInfo().MemoryLoadBytes</c> on desktop
	/// Skia (<c>MemoryManager.skia.cs</c>), so it is reported per leg and never compared across
	/// them; <c>GC.GetTotalMemory</c> is the managed heap on both.
	/// </summary>
	private static void ReportMemorySeries()
	{
		if (!RuntimeFeature.IsDynamicCodeSupported)
		{
			Report("memorySeries=skipped (no dynamic code support)");
			return;
		}

		Report($"memorySeries start appMemoryUsage={ReadAppMemoryUsage()} managedHeap={GC.GetTotalMemory(false)}");

		for (var cycle = 1; cycle <= MemoryProbeCycles; cycle++)
		{
			try
			{
				EmitAndDropPayloadAssembly();
			}
			catch (Exception error)
			{
				Report($"memorySeries ABORTED at cycle {cycle}: {error.GetType().Name}: {error.Message}");
				return;
			}

			ChurnStack(depth: 96);
			GC.Collect();
			GC.WaitForPendingFinalizers();
			GC.Collect();

			Report($"cycle={cycle} appMemoryUsage={ReadAppMemoryUsage()} managedHeap={GC.GetTotalMemory(false)}");
		}
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static void EmitAndDropPayloadAssembly()
		=> EmitCollectibleType("AlcProbeCycle", PayloadTypes, PayloadMethodsPerType);

	private static string ReadAppMemoryUsage()
	{
		try
		{
			return global::Windows.System.MemoryManager.AppMemoryUsage.ToString();
		}
		catch (Exception error)
		{
			return $"FAILED {error.GetType().Name}: {error.Message}";
		}
	}

	private static void Report(string message)
		=> Console.WriteLine($"{Tag} {message}");
}

#endif
