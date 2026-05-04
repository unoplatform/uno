using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Uno.UI.Benchmarks.Publisher;

/// <summary>
/// Reads per-benchmark JSONL files emitted by <c>BenchmarkRunnerHost</c> in CI artifacts and
/// stamps them with run-level identity (runId, commit, branch, platform, timestamp), then
/// concatenates into a single per-platform JSONL ready for the merge step.
/// </summary>
internal static class NormalizeCommand
{
	public static async Task<int> RunAsync(string[] args)
	{
		var input = ArgParser.Required(args, "--input");
		var output = ArgParser.Required(args, "--output");
		var target = ArgParser.Required(args, "--target");
		var commit = ArgParser.Required(args, "--commit");
		var branch = ArgParser.Optional(args, "--branch") ?? "master";

		var runId = $"{DateTime.UtcNow:yyyyMMdd-HHmmss}-{target}-{commit[..Math.Min(10, commit.Length)]}";
		var timestamp = DateTime.UtcNow.ToString("O");

		var rows = new List<ResultRow>();
		foreach (var jsonlFile in Directory.EnumerateFiles(input, "results.jsonl", SearchOption.AllDirectories))
		{
			foreach (var line in await File.ReadAllLinesAsync(jsonlFile))
			{
				if (string.IsNullOrWhiteSpace(line))
				{
					continue;
				}
				var row = JsonSerializer.Deserialize<ResultRow>(line);
				if (row is null)
				{
					continue;
				}
				row.RunId = runId;
				row.Commit = commit;
				row.Branch = branch;
				row.Platform = target;
				row.Timestamp ??= timestamp;
				rows.Add(row);
			}
		}

		Directory.CreateDirectory(Path.GetDirectoryName(output)!);
		await using var writer = new StreamWriter(output, append: false);
		foreach (var row in rows.OrderBy(r => r.Benchmark, StringComparer.Ordinal))
		{
			await writer.WriteLineAsync(JsonSerializer.Serialize(row, JsonOpts.Default));
		}

		Console.WriteLine($"Normalized {rows.Count} rows for platform={target} commit={commit[..Math.Min(10, commit.Length)]} -> {output}");
		return 0;
	}
}
