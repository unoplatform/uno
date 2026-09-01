#nullable enable

using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.RemoteControl.HotReload;
using Uno.UI.RuntimeTests.Tests.HotReload.Frame.Pages;
using _HR = Uno.UI.RuntimeTests.Tests.HotReload.HotReloadHelper;

namespace Uno.UI.RuntimeTests.Tests.HotReload.Frame.HRApp.Tests;

/// <summary>
/// Establishes a wall-clock baseline for the hot-reload cycle across three edit tiers.
/// </summary>
/// <remarks>
/// <para>These tests assert <b>completeness of the measurement record</b>, never a wall-clock
/// threshold. The suite runs on shared CI hardware where absolute timings are noise; an absolute
/// baseline comes from running the same tests on controlled hardware and reading the
/// <c>HRPERF|</c> lines out of the captured output.</para>
/// <para>The failure this guards against is the one that motivated the work: a measurement that
/// silently stops being taken. If instrumentation regresses, these fail.</para>
/// <para>Scope, stated plainly so the numbers are not over-read: this measures the
/// dev-server-owned path only (the IDE-owned Visual Studio / VS Code / Rider paths never reach
/// <see cref="HotReloadHelper"/>), and it ends when the local hot-reload operation completes
/// rather than when a frame is presented. See <c>specs/057-hotreload-end-to-end-instrumentation</c>.</para>
/// </remarks>
[TestClass]
[RunsOnUIThread]
public class Given_HotReloadPerformance : BaseTestClass
{
	private const int WarmupIterations = 1;
	private const int MeasuredIterations = 5;

	private const string SmallTier = "small-leaf-page-attribute";
	private const string MediumTier = "medium-shared-usercontrol";
	private const string LargeTier = "large-app-resourcedictionary";
	private const string RepeatScenario = "repeat-same-edit";

	/// <summary>
	/// Tier 1 — one attribute on one element in a leaf page. Best case: dominated by the fixed
	/// constants (the 250 ms watcher buffer and the 100 ms trailing delay) rather than by the
	/// size of the change.
	/// </summary>
	[TestMethod]
	public Task When_Measure_SmallEdit()
		=> MeasureTierAsync(
			SmallTier,
			XamlPathOf(new HR_Frame_Pages_Page1()),
			"First page",
			i => $"First page #{i:D2}");

	/// <summary>
	/// Tier 2 — a UserControl instantiated inside another page. Exercises partial tree reload:
	/// the control should reload without its parents being torn down.
	/// </summary>
	[TestMethod]
	public Task When_Measure_MediumEdit()
		=> MeasureTierAsync(
			MediumTier,
			XamlPathOf(new HR_Frame_Pages_UC1()),
			"Control 1",
			i => $"Control 1 #{i:D2}");

	/// <summary>
	/// Tier 3 — an app-level ResourceDictionary entry. Exercises the resource pass, which walks
	/// every content root rather than just the current window.
	/// </summary>
	[TestMethod]
	public Task When_Measure_LargeEdit()
		=> MeasureTierAsync(
			LargeTier,
			Path.Combine(_HR.ProjectPath, "AppResources.xaml"),
			"** HR_Frame_Pages_AppResources Original String **",
			i => $"** HR_Frame_Pages_AppResources Updated String#{i:D2} **");

	/// <summary>
	/// Repeats one edit many times to expose a degradation curve. Off by default: it is a
	/// measurement run rather than a pass/fail gate, and it is far too slow for every CI build.
	/// Enable it locally to check whether cycle cost grows with reload count — the observable
	/// signature of the accumulators recorded in spec 044.
	/// </summary>
	[TestMethod]
	[Ignore("Measurement run, not a gate. Enable locally to record the degradation curve.")]
	public async Task When_Measure_RepeatedEdits()
	{
		using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(600));
		var ct = cts.Token;
		var path = XamlPathOf(new HR_Frame_Pages_Page1());

		HotReloadTiming.BeginScenario(RepeatScenario);
		try
		{
			for (var i = 0; i < 15; i++)
			{
				await EditAndRevertAsync(path, "First page", $"First page #{i:D2}", ct);
			}
		}
		finally
		{
			HotReloadTiming.EndScenario();
		}

		Console.WriteLine(HotReloadTiming.Summarize(RepeatScenario));

		var samples = HotReloadTiming.SamplesFor(RepeatScenario);
		Assert.AreEqual(15, samples.Count, "Every cycle should have produced a sample.");
	}

	private static async Task MeasureTierAsync(
		string scenario,
		string filePath,
		string originalText,
		Func<int, string> replacement)
	{
		using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(300));
		var ct = cts.Token;
		var total = WarmupIterations + MeasuredIterations;

		Assert.IsTrue(File.Exists(filePath), $"Edit target not found: {filePath}");

		HotReloadTiming.BeginScenario(scenario);
		try
		{
			for (var i = 0; i < total; i++)
			{
				await EditAndRevertAsync(filePath, originalText, replacement(i), ct);
			}
		}
		finally
		{
			HotReloadTiming.EndScenario();
		}

		Console.WriteLine(HotReloadTiming.Summarize(scenario));

		// R7: fail on an incomplete record. A benchmark that silently stops measuring reads as
		// a pass, which is the exact failure mode this exists to prevent.
		var samples = HotReloadTiming.SamplesFor(scenario);
		Assert.AreEqual(
			total,
			samples.Count,
			$"Expected {total} timing samples for '{scenario}' but recorded {samples.Count}.");
		// A positive duration is vacuous -- every path to Record has already awaited a round
		// trip. The 250 ms watcher buffer plus the 100 ms trailing delay put a real floor under
		// any cycle that genuinely reached the app, so assert against that instead.
		Assert.IsTrue(
			samples.All(s => s.ElapsedMs >= 300),
			$"Every sample for '{scenario}' must clear the fixed constants; "
			+ $"got [{string.Join(", ", samples.Select(s => s.ElapsedMs.ToString("F1")))}].");
	}

	/// <summary>
	/// Applies an edit, waits for it to land, then reverts — so each iteration starts from the
	/// same source state and the tier stays repeatable.
	/// </summary>
	/// <remarks>
	/// Only the forward edit is timed. The revert runs through <c>FileUpdate.DisposeAsync</c>,
	/// which calls <c>TryUpdateFilesAsync</c> on the processor directly rather than going back
	/// through <see cref="HotReloadHelper.UpdateAsync(UpdateRequest, CancellationToken)"/> — so it
	/// is never recorded as a sample. That is load-bearing, not incidental: if the revert were
	/// timed it would double every tier's sample count.
	/// </remarks>
	private static async Task EditAndRevertAsync(string filePath, string oldText, string newText, CancellationToken ct)
	{
		await using var update = await _HR.UpdateAsync([new FileEdit(filePath, oldText, newText)], ct);
	}

	/// <summary>
	/// Resolves the on-disk .xaml path of a page from its parse context, so no path is hard-coded.
	/// </summary>
	private static string XamlPathOf(FrameworkElement element)
		=> FrameworkElementExtensions.GetDebugParseContext(element).FileName;
}
