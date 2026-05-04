using System;
using System.Threading.Tasks;
using Uno.UI.Benchmarks.Publisher;

if (args.Length == 0)
{
	PrintUsage();
	return 1;
}

try
{
	return args[0] switch
	{
		"normalize" => await NormalizeCommand.RunAsync(args.AsSpan(1).ToArray()),
		"merge" => await MergeCommand.RunAsync(args.AsSpan(1).ToArray()),
		"compare" => await CompareCommand.RunAsync(args.AsSpan(1).ToArray()),
		"--help" or "-h" or "help" => Help(),
		_ => Help($"Unknown command: {args[0]}"),
	};
}
catch (Exception ex)
{
	Console.Error.WriteLine($"ERROR: {ex.Message}");
	Console.Error.WriteLine(ex);
	return 2;
}

static int Help(string? prefix = null)
{
	if (prefix is not null)
	{
		Console.Error.WriteLine(prefix);
	}
	PrintUsage();
	return prefix is null ? 0 : 1;
}

static void PrintUsage()
{
	Console.WriteLine("""
		uno-bench-publisher — benchmark result post-processor.

		Usage:
		  uno-bench-publisher normalize --input <bdn-artifacts-dir> --output <jsonl-path>
		                                --target <platform> --commit <sha> [--branch <name>]
		      Read BDN's *-report-full.json files in the input directory, transform to v1
		      schema JSONL, and write to the output path. Used per-platform in CI.

		  uno-bench-publisher merge --input-dir <dir-with-jsonl-files> --commit <sha>
		                            [--branch <name>] [--data-repo <local-path>]
		                            [--repo-url <url>] [--push]
		      Concatenate all per-platform JSONL files into the data repo's by-platform/
		      and by-benchmark/ folders, regenerate index.json, optionally git push.
		      Skips push if BENCHMARK_REPO_TOKEN is unset (graceful no-op).

		  uno-bench-publisher compare <a.zip> <b.zip> [--output md|csv|json]
		      Compare two benchmark result zips locally. Prints a delta table to stdout.

		Schema version: 1.
		""");
}
