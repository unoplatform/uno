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
/// Establishes, per platform, which collectible-<see cref="System.Runtime.Loader.AssemblyLoadContext"/>
/// shapes are actually available, because the answer decides how the real tests can be written and
/// the repo does not record it anywhere:
///
/// <list type="bullet">
///   <item>the real-assembly path is Skia-desktop-only — <c>AlcApp</c> is built by shelling out to
///   <c>dotnet build</c> at test time and loaded with <c>LoadFromAssemblyPath</c>, and
///   <c>Given_HotReloadClientOperation_Alc</c> records that <c>Assembly.Location</c> is empty on
///   WASM;</item>
///   <item>the emit path (<c>AssemblyBuilderAccess.RunAndCollect</c>) is guarded on
///   <see cref="RuntimeFeature.IsDynamicCodeSupported"/> throughout the repo, but nothing states
///   its value on the WASM interpreter leg.</item>
/// </list>
///
/// This asserts nothing. It writes findings to the console so they can be read out of the CI job
/// log for each leg. Console output reaches the log on WASM through Chrome's
/// <c>--enable-logging=stderr</c>.
/// </summary>
[TestClass]
[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaDesktop | RuntimeTestPlatforms.SkiaWasm)]
public class Given_CollectibleAlc_Probe
{
	private const string Tag = "[ALC-PROBE]";

	/// <summary>Cycles to sample for the memory series. Small: this is a probe, not the measurement.</summary>
	private const int MemoryProbeCycles = 5;

	[TestMethod]
	[Timeout(300_000)]
	public void Probe_ReportsCollectibleAlcCapabilities()
	{
		Report($"platform={RuntimeTestsPlatformHelper.CurrentPlatform} isBrowser={OperatingSystem.IsBrowser()}");
		Report($"runtime={RuntimeInformation()}");

		ReportDynamicCodeSupport();
		ReportEmitCollectibility();
		ReportAssemblyLocationAvailability();
		ReportEmptyAlcCollection();
		ReportMemoryReadPaths();
		ReportMemorySeries();

		Report("done");
	}

	private static string RuntimeInformation()
		=> $"{System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription} / {System.Runtime.InteropServices.RuntimeInformation.RuntimeIdentifier}";

	private static void ReportDynamicCodeSupport()
		=> Report($"IsDynamicCodeSupported={RuntimeFeature.IsDynamicCodeSupported} IsDynamicCodeCompiled={RuntimeFeature.IsDynamicCodeCompiled}");

	/// <summary>
	/// The decisive question for the managed test: can a collectible assembly be produced at all on
	/// this leg? Reports the exception rather than letting it escape, since a throw here is itself
	/// the finding.
	/// </summary>
	private static void ReportEmitCollectibility()
	{
		try
		{
			var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(
				new AssemblyName("AlcProbeEmit"),
				AssemblyBuilderAccess.RunAndCollect);

			var probeType = assemblyBuilder
				.DefineDynamicModule("main")
				.DefineType("ProbeType", TypeAttributes.Public)
				.CreateType()!;

			var instance = Activator.CreateInstance(probeType);

			Report($"emit=ok isCollectible={probeType.Assembly.IsCollectible} instantiated={instance is not null}");
		}
		catch (Exception error)
		{
			Report($"emit=FAILED {error.GetType().Name}: {error.Message}");
		}
	}

	/// <summary>
	/// The other candidate substrate: loading real bytes. Reports whether this assembly even has a
	/// location to load from (expected empty on WASM), which is what rules the
	/// <c>LoadFromAssemblyPath</c> shape in or out.
	/// </summary>
	private static void ReportAssemblyLocationAvailability()
	{
		var assembly = typeof(Given_CollectibleAlc_Probe).Assembly;
		var location = assembly.Location;

		Report($"assemblyLocation='{location}' isEmpty={string.IsNullOrEmpty(location)}");

		if (string.IsNullOrEmpty(location))
		{
			return;
		}

		try
		{
			var alc = new System.Runtime.Loader.AssemblyLoadContext("AlcProbeLoad", isCollectible: true);
			try
			{
				var loaded = alc.LoadFromAssemblyPath(location);
				Report($"loadFromAssemblyPath=ok isCollectible={loaded.IsCollectible}");
			}
			finally
			{
				alc.Unload();
			}
		}
		catch (Exception error)
		{
			Report($"loadFromAssemblyPath=FAILED {error.GetType().Name}: {error.Message}");
		}
	}

	/// <summary>
	/// Baseline for the managed assertion: does a collectible ALC with NOTHING loaded into it die
	/// after Unload within the bounded loop this repo uses elsewhere? If even this does not die on a
	/// leg, a WeakReference-based assertion cannot be written there at all.
	/// </summary>
	private static void ReportEmptyAlcCollection()
	{
		var tracker = CreateAndUnloadEmptyAlc();
		var collected = TryWaitUntilCollected(tracker, out var iterations);

		Report($"emptyAlcCollected={collected} iterations={iterations}");
	}

	// Separate non-inlined frame so no local in the frame that runs the GC keeps the context alive;
	// Debug codegen extends local lifetimes to the end of the method.
	[MethodImpl(MethodImplOptions.NoInlining)]
	private static WeakReference CreateAndUnloadEmptyAlc()
	{
		var alc = new System.Runtime.Loader.AssemblyLoadContext("AlcProbeEmpty", isCollectible: true);
		alc.Unload();

		return new WeakReference(alc);
	}

	private static bool TryWaitUntilCollected(WeakReference reference, out int iterations)
	{
		// Matches the sibling ALC fixtures: unloading is asynchronous and takes several collections
		// to walk the graph, and a second collect after finalizers can release the last reference.
		for (iterations = 0; iterations < 10 && reference.IsAlive; iterations++)
		{
			GC.Collect();
			GC.WaitForPendingFinalizers();
			GC.Collect();
		}

		return !reference.IsAlive;
	}

	/// <summary>
	/// Reports the memory read the real test will use. Note it is NOT the same quantity on every
	/// leg: <c>MemoryManager.AppMemoryUsage</c> is <c>Module.HEAPU8.length</c> on WASM
	/// (<c>MemoryManager.wasm.cs</c>) but <c>GC.GetGCMemoryInfo().MemoryLoadBytes</c> on desktop
	/// Skia (<c>MemoryManager.skia.cs</c>) — so no assertion may span both.
	///
	/// <c>WebAssemblyImports.EvalString("Module.HEAPU8.length")</c> is deliberately NOT used as a
	/// cross-check: it needs <c>System.Runtime.InteropServices.JavaScript</c>, which the
	/// Skia-flavoured test assembly does not target.
	/// </summary>
	private static void ReportMemoryReadPaths()
		=> Report($"appMemoryUsage={ReadAppMemoryUsage()}");

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

	/// <summary>
	/// The series the native-reclamation test would assert on. Emits and unloads a collectible
	/// assembly per cycle and samples memory after a settling collect, so the real test's growth
	/// floor can be derived from measurement rather than guessed from another application's numbers.
	/// </summary>
	private static void ReportMemorySeries()
	{
		if (!RuntimeFeature.IsDynamicCodeSupported)
		{
			Report("memorySeries=skipped (no dynamic code support, so no per-cycle collectible load)");
			return;
		}

		for (var cycle = 1; cycle <= MemoryProbeCycles; cycle++)
		{
			try
			{
				EmitAndDropCollectibleAssembly();
			}
			catch (Exception error)
			{
				Report($"memorySeries=ABORTED at cycle {cycle}: {error.GetType().Name}: {error.Message}");
				return;
			}

			GC.Collect();
			GC.WaitForPendingFinalizers();
			GC.Collect();

			Report($"cycle={cycle} appMemoryUsage={ReadAppMemoryUsage()}");
		}
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static void EmitAndDropCollectibleAssembly()
	{
		var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(
			new AssemblyName("AlcProbeCycle"),
			AssemblyBuilderAccess.RunAndCollect);

		var probeType = assemblyBuilder
			.DefineDynamicModule("main")
			.DefineType("ProbeType", TypeAttributes.Public)
			.CreateType()!;

		// Instantiate so the type is genuinely used, not merely defined.
		GC.KeepAlive(Activator.CreateInstance(probeType));
	}

	private static void Report(string message)
		=> Console.WriteLine($"{Tag} {message}");
}

#endif
