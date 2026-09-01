#nullable enable

using System.Diagnostics;

namespace Uno.UI.RuntimeTests.Tests.HotReload;

/// <summary>
/// Collects wall-clock samples for hot-reload cycles so a baseline can be established.
/// </summary>
/// <remarks>
/// <para>What a sample actually measures: the span of <see cref="HotReloadHelper.UpdateAsync(UpdateRequest, CancellationToken)"/>,
/// which brackets the request to the dev server, the server writing the file, the 250 ms
/// <c>FileSystemObserver</c> buffer, the solution update (including source generators), the EnC emit,
/// the WebSocket hop, <c>MetadataUpdater.ApplyUpdate</c>, the visual-tree pass, and the 100 ms trailing
/// delay in <c>ClientHotReloadProcessor.ClientApi</c> &#8212; which is reached on the fully-successful
/// path only, since six early returns bypass it.</para>
/// <para>What it does NOT measure: frame presentation. The span ends when the local hot-reload
/// operation completes, not when a frame containing the change is on screen. It is therefore an
/// edit-to-tree-updated figure, not edit-to-pixel — see <c>specs/057-hotreload-end-to-end-instrumentation</c>
/// R5. It also only covers the dev-server-owned path on the platform the suite runs on; the
/// IDE-owned paths (Visual Studio, VS Code, Rider) never go through this helper.</para>
/// <para>Samples are emitted as <c>HRPERF|{scenario}|{iteration}|{elapsedMs}</c>, following the
/// <c>TIMELINE|</c> convention used by the dev-server startup work, so a run can be scraped from
/// captured output without a debugger attached.</para>
/// </remarks>
internal static class HotReloadTiming
{
	private static readonly object _gate = new();
	private static readonly List<Sample> _samples = new();

	internal record Sample(string Scenario, int Iteration, double ElapsedMs);

	/// <summary>
	/// When set, <see cref="HotReloadHelper"/> records each update under this scenario name.
	/// Null disables recording, so ordinary correctness tests contribute no samples.
	/// </summary>
	internal static string? CurrentScenario { get; private set; }

	private static int _iteration;

	/// <summary>Starts a scenario and resets its iteration counter.</summary>
	internal static void BeginScenario(string scenario)
	{
		lock (_gate)
		{
			CurrentScenario = scenario;
			_iteration = 0;
		}
	}

	/// <summary>Stops recording. Always call this in a <c>finally</c>.</summary>
	internal static void EndScenario()
	{
		lock (_gate)
		{
			CurrentScenario = null;
		}
	}

	internal static void Record(TimeSpan elapsed)
	{
		string scenario;
		int iteration;

		lock (_gate)
		{
			if (CurrentScenario is not { } current)
			{
				return;
			}

			scenario = current;
			iteration = _iteration++;
			_samples.Add(new Sample(scenario, iteration, elapsed.TotalMilliseconds));
		}

		var line = $"HRPERF|{scenario}|{iteration}|{elapsed.TotalMilliseconds:F1}";
		Console.WriteLine(line);
		Debug.WriteLine(line);
	}

	internal static IReadOnlyList<Sample> SamplesFor(string scenario)
	{
		lock (_gate)
		{
			return _samples.Where(s => s.Scenario == scenario).ToList();
		}
	}

	internal static void Clear()
	{
		lock (_gate)
		{
			_samples.Clear();
		}
	}

	/// <summary>
	/// Renders a fixed-width summary. The first iteration of a scenario is reported separately:
	/// the first edit of a session pays warm-up costs (baseline capture, first generator run) that
	/// no later edit repeats, so folding it into the median would misrepresent both.
	/// </summary>
	internal static string Summarize(params string[] scenarios)
	{
		var sb = new System.Text.StringBuilder();
		sb.AppendLine();
		sb.AppendLine("HRPERF SUMMARY (ms) — edit-to-tree-updated, dev-server-owned path");
		sb.AppendLine("scenario                       n     first       min    median       max");
		sb.AppendLine(new string('-', 78));

		foreach (var scenario in scenarios)
		{
			var samples = SamplesFor(scenario);
			if (samples.Count == 0)
			{
				sb.AppendLine($"{scenario,-28} {"—",3} {"(no samples recorded)",-40}");
				continue;
			}

			var first = samples[0].ElapsedMs;
			var steady = samples.Skip(1).Select(s => s.ElapsedMs).OrderBy(v => v).ToArray();

			if (steady.Length == 0)
			{
				sb.AppendLine($"{scenario,-28} {samples.Count,3} {first,9:F1} {"—",9} {"—",9} {"—",9}");
				continue;
			}

			var median = steady.Length % 2 == 1
				? steady[steady.Length / 2]
				: (steady[steady.Length / 2 - 1] + steady[steady.Length / 2]) / 2;

			sb.AppendLine($"{scenario,-28} {samples.Count,3} {first,9:F1} {steady[0],9:F1} {median,9:F1} {steady[^1],9:F1}");
		}

		sb.AppendLine(new string('-', 78));
		sb.AppendLine("'first' is the warm-up iteration and is excluded from min/median/max.");
		return sb.ToString();
	}
}
