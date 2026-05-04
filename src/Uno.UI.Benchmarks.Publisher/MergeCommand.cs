using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Uno.UI.Benchmarks.Publisher;

/// <summary>
/// Downstream merge step: takes a directory of per-platform JSONL files (the artifacts
/// from each platform's benchmark stage) and appends them into the data repo's
/// <c>by-platform/</c> and <c>by-benchmark/</c> trees, regenerates <c>index.json</c>,
/// and (if --push and a token are present) commits + pushes once to the data branch.
/// Graceful no-op if BENCHMARK_REPO_TOKEN is unset.
/// </summary>
internal static class MergeCommand
{
	public static async Task<int> RunAsync(string[] args)
	{
		var inputDir = ArgParser.Required(args, "--input-dir");
		var commit = ArgParser.Required(args, "--commit");
		var branch = ArgParser.Optional(args, "--branch") ?? "master";
		var dataRepo = ArgParser.Optional(args, "--data-repo");
		var repoUrl = ArgParser.Optional(args, "--repo-url");
		var shouldPush = args.Contains("--push");

		var token = Environment.GetEnvironmentVariable("BENCHMARK_REPO_TOKEN");
		if (shouldPush && string.IsNullOrEmpty(token))
		{
			Console.WriteLine("BENCHMARK_REPO_TOKEN not set; skipping push (graceful no-op).");
			return 0;
		}

		dataRepo ??= Path.Combine(Path.GetTempPath(), $"uno-benchmarks-data-{Guid.NewGuid():N}");
		if (shouldPush)
		{
			if (string.IsNullOrEmpty(repoUrl))
			{
				Console.Error.WriteLine("--repo-url is required when --push is supplied");
				return 1;
			}
			await CloneOrFetchDataBranchAsync(repoUrl!, token!, dataRepo);
		}
		else
		{
			Directory.CreateDirectory(dataRepo);
		}

		var allRows = new List<ResultRow>();
		foreach (var file in Directory.EnumerateFiles(inputDir, "*.jsonl", SearchOption.AllDirectories))
		{
			foreach (var line in await File.ReadAllLinesAsync(file))
			{
				if (string.IsNullOrWhiteSpace(line))
				{
					continue;
				}
				var row = JsonSerializer.Deserialize<ResultRow>(line);
				if (row is not null)
				{
					allRows.Add(row);
				}
			}
		}

		Console.WriteLine($"Merging {allRows.Count} rows into {dataRepo}");

		var byPlatformDir = Path.Combine(dataRepo, "data", "by-platform");
		var byBenchmarkDir = Path.Combine(dataRepo, "data", "by-benchmark");
		Directory.CreateDirectory(byPlatformDir);
		Directory.CreateDirectory(byBenchmarkDir);

		foreach (var group in allRows.GroupBy(r => (r.Platform, r.Benchmark)))
		{
			var (platform, benchmark) = group.Key;
			if (platform is null || benchmark is null)
			{
				continue;
			}
			var platformDir = Path.Combine(byPlatformDir, platform);
			Directory.CreateDirectory(platformDir);
			await AppendRowsAsync(Path.Combine(platformDir, $"{benchmark}.jsonl"), group);
			await AppendRowsAsync(Path.Combine(byBenchmarkDir, $"{benchmark}.jsonl"), group);
		}

		await WriteIndexAsync(dataRepo, byPlatformDir);

		if (shouldPush)
		{
			await CommitAndPushAsync(dataRepo, commit, branch, repoUrl!, token!);
		}

		return 0;
	}

	private static async Task AppendRowsAsync(string path, IEnumerable<ResultRow> rows)
	{
		await using var writer = new StreamWriter(path, append: true);
		foreach (var row in rows)
		{
			await writer.WriteLineAsync(JsonSerializer.Serialize(row, JsonOpts.Default));
		}
	}

	private static async Task WriteIndexAsync(string dataRepo, string byPlatformDir)
	{
		var entries = new List<object>();
		foreach (var platformDir in Directory.EnumerateDirectories(byPlatformDir))
		{
			var platform = Path.GetFileName(platformDir);
			foreach (var jsonlPath in Directory.EnumerateFiles(platformDir, "*.jsonl"))
			{
				var benchmark = Path.GetFileNameWithoutExtension(jsonlPath);
				entries.Add(new { platform, benchmark, file = $"by-platform/{platform}/{benchmark}.jsonl" });
			}
		}

		var index = new
		{
			schemaVersion = 1,
			updatedAt = DateTime.UtcNow.ToString("O"),
			entries = entries.OrderBy(e => e.GetType().GetProperty("platform")!.GetValue(e)),
		};
		await File.WriteAllTextAsync(
			Path.Combine(dataRepo, "data", "index.json"),
			JsonSerializer.Serialize(index, JsonOpts.Indented));
	}

	private static async Task CloneOrFetchDataBranchAsync(string repoUrl, string token, string targetDir)
	{
		var authedUrl = repoUrl.Replace("https://", $"https://x-access-token:{token}@");
		if (Directory.Exists(Path.Combine(targetDir, ".git")))
		{
			await RunGitAsync(targetDir, "fetch", "origin", "data");
			await RunGitAsync(targetDir, "checkout", "data");
			await RunGitAsync(targetDir, "pull", "origin", "data");
		}
		else
		{
			await RunGitAsync(Path.GetDirectoryName(targetDir)!, "clone", "--branch", "data", authedUrl, targetDir);
		}
	}

	private static async Task CommitAndPushAsync(string repoRoot, string commit, string branch, string repoUrl, string token)
	{
		await RunGitAsync(repoRoot, "config", "user.email", "uno-benchmarks-publisher@noreply.unoplatform.io");
		await RunGitAsync(repoRoot, "config", "user.name", "uno-benchmarks-publisher");
		await RunGitAsync(repoRoot, "add", "data/");
		var diff = await RunGitWithOutputAsync(repoRoot, "status", "--porcelain");
		if (string.IsNullOrWhiteSpace(diff))
		{
			Console.WriteLine("No changes to publish.");
			return;
		}
		await RunGitAsync(repoRoot, "commit", "-m", $"data: {branch} {commit[..Math.Min(10, commit.Length)]} @ {DateTime.UtcNow:O}");
		var authedUrl = repoUrl.Replace("https://", $"https://x-access-token:{token}@");
		await RunGitAsync(repoRoot, "push", authedUrl, "HEAD:data");
		Console.WriteLine("Pushed.");
	}

	private static Task RunGitAsync(string cwd, params string[] args) => RunProcessAsync("git", cwd, args, captureStdout: false);

	private static async Task<string> RunGitWithOutputAsync(string cwd, params string[] args)
		=> await RunProcessAsync("git", cwd, args, captureStdout: true);

	private static async Task<string> RunProcessAsync(string fileName, string cwd, string[] args, bool captureStdout)
	{
		var psi = new ProcessStartInfo
		{
			FileName = fileName,
			WorkingDirectory = cwd,
			RedirectStandardOutput = captureStdout,
			RedirectStandardError = true,
			UseShellExecute = false,
		};
		foreach (var a in args)
		{
			psi.ArgumentList.Add(a);
		}
		using var process = Process.Start(psi)!;
		var stderr = await process.StandardError.ReadToEndAsync();
		var stdout = captureStdout ? await process.StandardOutput.ReadToEndAsync() : "";
		await process.WaitForExitAsync();
		if (process.ExitCode != 0)
		{
			throw new InvalidOperationException(
				$"{fileName} {string.Join(' ', args)} exited {process.ExitCode}: {stderr}");
		}
		return stdout;
	}
}
