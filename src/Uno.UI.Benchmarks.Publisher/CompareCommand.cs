using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Uno.UI.Benchmarks.Publisher;

/// <summary>
/// Local A/B compare: takes two benchmark result zips emitted either by SamplesApp's
/// BenchmarksPage or by a CI artifact, and prints a delta table.
/// </summary>
internal static class CompareCommand
{
	public static async Task<int> RunAsync(string[] args)
	{
		if (args.Length < 2)
		{
			Console.Error.WriteLine("Usage: uno-bench-publisher compare <a.zip> <b.zip> [--output md|csv|json]");
			return 1;
		}

		var aPath = args[0];
		var bPath = args[1];
		var format = ArgParser.Optional(args, "--output") ?? "md";

		var a = await LoadZipAsync(aPath);
		var b = await LoadZipAsync(bPath);
		var keys = a.Keys.Union(b.Keys, StringComparer.Ordinal).OrderBy(k => k, StringComparer.Ordinal).ToList();

		var rows = keys.Select(k =>
		{
			a.TryGetValue(k, out var ar);
			b.TryGetValue(k, out var br);
			return new CompareRow(k, ar, br);
		}).ToList();

		Console.Write(format switch
		{
			"csv" => RenderCsv(rows, aPath, bPath),
			"json" => RenderJson(rows, aPath, bPath),
			_ => RenderMarkdown(rows, aPath, bPath),
		});
		return 0;
	}

	private static async Task<Dictionary<string, ResultRow>> LoadZipAsync(string zipPath)
	{
		await using var stream = File.OpenRead(zipPath);
		using var archive = new ZipArchive(stream, ZipArchiveMode.Read);
		var entry = archive.GetEntry("results.jsonl")
			?? throw new InvalidOperationException($"results.jsonl not found in {zipPath}");
		using var reader = new StreamReader(entry.Open());
		var dict = new Dictionary<string, ResultRow>(StringComparer.Ordinal);
		string? line;
		while ((line = await reader.ReadLineAsync()) is not null)
		{
			if (string.IsNullOrWhiteSpace(line))
			{
				continue;
			}
			var row = JsonSerializer.Deserialize<ResultRow>(line);
			if (row?.Benchmark is null)
			{
				continue;
			}
			dict[row.Benchmark] = row;
		}
		return dict;
	}

	private static string RenderMarkdown(IReadOnlyList<CompareRow> rows, string aPath, string bPath)
	{
		var sb = new StringBuilder();
		sb.AppendLine($"# Benchmark comparison");
		sb.AppendLine($"- A: `{aPath}`");
		sb.AppendLine($"- B: `{bPath}`");
		sb.AppendLine();
		sb.AppendLine("| Benchmark | Mean A | Mean B | Δ | Δ% | Alloc Δ |");
		sb.AppendLine("| --- | ---: | ---: | ---: | ---: | ---: |");
		foreach (var r in rows)
		{
			sb.AppendLine($"| {r.Name} | {Fmt(r.A?.Stats?.MeanNs)} | {Fmt(r.B?.Stats?.MeanNs)} | {DeltaTime(r)} | {DeltaPercent(r)} | {DeltaAlloc(r)} |");
		}
		return sb.ToString();
	}

	private static string RenderCsv(IReadOnlyList<CompareRow> rows, string aPath, string bPath)
	{
		var sb = new StringBuilder();
		sb.AppendLine("benchmark,mean_a_ns,mean_b_ns,delta_ns,delta_pct,alloc_delta_bytes");
		foreach (var r in rows)
		{
			sb.AppendLine(string.Join(",",
				r.Name,
				r.A?.Stats?.MeanNs.ToString("F2") ?? "",
				r.B?.Stats?.MeanNs.ToString("F2") ?? "",
				DeltaNumeric(r),
				DeltaPercentNumeric(r),
				DeltaAllocNumeric(r)));
		}
		return sb.ToString();
	}

	private static string RenderJson(IReadOnlyList<CompareRow> rows, string aPath, string bPath)
		=> JsonSerializer.Serialize(new { a = aPath, b = bPath, rows }, JsonOpts.Indented);

	private static string Fmt(double? ns)
	{
		if (ns is null)
		{
			return "—";
		}
		var v = ns.Value;
		return v switch
		{
			< 1_000 => $"{v:F1} ns",
			< 1_000_000 => $"{v / 1_000:F2} µs",
			< 1_000_000_000 => $"{v / 1_000_000:F2} ms",
			_ => $"{v / 1_000_000_000:F2} s",
		};
	}

	private static string DeltaTime(CompareRow r)
	{
		if (r.A?.Stats is null || r.B?.Stats is null)
		{
			return "—";
		}
		var d = r.B.Stats.MeanNs - r.A.Stats.MeanNs;
		return (d >= 0 ? "+" : "") + Fmt(d);
	}

	private static string DeltaPercent(CompareRow r)
	{
		if (r.A?.Stats is null || r.B?.Stats is null || r.A.Stats.MeanNs == 0)
		{
			return "—";
		}
		var pct = (r.B.Stats.MeanNs - r.A.Stats.MeanNs) / r.A.Stats.MeanNs * 100;
		return (pct >= 0 ? "+" : "") + pct.ToString("F1") + "%";
	}

	private static string DeltaAlloc(CompareRow r)
	{
		if (r.A?.Memory is null || r.B?.Memory is null)
		{
			return "—";
		}
		var d = r.B.Memory.AllocatedBytesPerOp - r.A.Memory.AllocatedBytesPerOp;
		return (d >= 0 ? "+" : "") + d + " B";
	}

	private static string DeltaNumeric(CompareRow r)
		=> r.A?.Stats is null || r.B?.Stats is null ? "" : (r.B.Stats.MeanNs - r.A.Stats.MeanNs).ToString("F2");

	private static string DeltaPercentNumeric(CompareRow r)
		=> r.A?.Stats is null || r.B?.Stats is null || r.A.Stats.MeanNs == 0
			? ""
			: ((r.B.Stats.MeanNs - r.A.Stats.MeanNs) / r.A.Stats.MeanNs * 100).ToString("F2");

	private static string DeltaAllocNumeric(CompareRow r)
		=> r.A?.Memory is null || r.B?.Memory is null
			? ""
			: (r.B.Memory.AllocatedBytesPerOp - r.A.Memory.AllocatedBytesPerOp).ToString();

	private sealed record CompareRow(string Name, ResultRow? A, ResultRow? B);
}
