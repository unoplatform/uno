using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using BenchmarkDotNet.Loggers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using Uno.UI.RuntimeTests.Tests.Benchmarks;
using Uno.UI.Samples.Controls;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage.Pickers;
using Windows.UI;

namespace UITests.Windows_UI_Xaml.Performance;

/// <summary>
/// In-app benchmark runner. Designed for side-by-side window comparison: a developer can run
/// the SamplesApp on two cloned worktrees, eyeball the results table on each, and confirm a
/// regression visually before reaching for the dashboard's compare view.
/// </summary>
[Sample("Performance", Name = "Benchmarks")]
public sealed partial class BenchmarksPage : Page
{
	private readonly ObservableCollection<RowVm> _rows = new();
	private RunResult? _lastResult;

	public BenchmarksPage()
	{
		InitializeComponent();
		resultsList.ItemsSource = _rows;
		PopulateBranchLabel();
	}

	private void PopulateBranchLabel()
	{
		// Best-effort: read the embedded informational version assembly attribute. Falls back
		// to "(unknown)" — we don't reach into the file system from a sandboxed app.
		var asm = typeof(BenchmarksPage).Assembly;
		var info = asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
		branchLabel.Text = string.IsNullOrEmpty(info)
			? "branch: (unknown) · commit: (unknown)"
			: $"build: {info}";
	}

	private void OnRunAll(object sender, RoutedEventArgs e) => _ = RunAsync(filter: null);

	private void OnRunFiltered(object sender, RoutedEventArgs e) => _ = RunAsync(filter: filterBox.Text?.Trim());

	private async Task RunAsync(string? filter)
	{
		runAllButton.IsEnabled = false;
		runFilteredButton.IsEnabled = false;
		copyMarkdownButton.IsEnabled = false;
		saveZipButton.IsEnabled = false;
		_rows.Clear();
		logText.Inlines.Clear();
		statusLabel.Text = $"Running {(string.IsNullOrEmpty(filter) ? "all" : $"filter '{filter}'")}…";

		var logger = new InAppLogger(logText, logScroll);

		try
		{
			var result = await Task.Run(() =>
				BenchmarkRunnerHost.RunAllAsync(filter, logger).GetAwaiter().GetResult());
			_lastResult = result;

			foreach (var row in result.Rows)
			{
				_rows.Add(RowVm.From(row));
			}

			statusLabel.Text = $"Finished — {result.Rows.Count} row(s) collected.";
			copyMarkdownButton.IsEnabled = result.Rows.Count > 0;
			saveZipButton.IsEnabled = result.Rows.Count > 0 && File.Exists(result.ZipPath);
		}
		catch (Exception ex)
		{
			statusLabel.Text = $"Failed: {ex.Message}";
			logger.WriteLineError(ex.ToString());
		}
		finally
		{
			runAllButton.IsEnabled = true;
			runFilteredButton.IsEnabled = true;
		}
	}

	private void OnCopyMarkdown(object sender, RoutedEventArgs e)
	{
		var sb = new StringBuilder();
		sb.AppendLine("| Benchmark | Mean | StdDev | Allocated | N |");
		sb.AppendLine("| --- | ---: | ---: | ---: | ---: |");
		foreach (var row in _rows)
		{
			sb.AppendLine($"| {row.Name} | {row.Mean} | {row.Stddev} | {row.Allocated} | {row.N} |");
		}
		var pkg = new DataPackage();
		pkg.SetText(sb.ToString());
		Clipboard.SetContent(pkg);
		statusLabel.Text = "Copied results to clipboard.";
	}

	private async void OnSaveZip(object sender, RoutedEventArgs e)
	{
		if (_lastResult is null || !File.Exists(_lastResult.Value.ZipPath))
		{
			return;
		}

		var picker = new FileSavePicker();
		picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
		picker.FileTypeChoices.Add("Zip Archive", new List<string> { ".zip" });
		picker.SuggestedFileName = $"benchmarks-{DateTime.UtcNow:yyyyMMdd-HHmm}";

		var file = await picker.PickSaveFileAsync();
		if (file is null)
		{
			return;
		}

		using var sourceStream = File.OpenRead(_lastResult.Value.ZipPath);
		using var targetStream = await file.OpenStreamForWriteAsync();
		await sourceStream.CopyToAsync(targetStream);

		statusLabel.Text = $"Saved {file.Name}.";
	}

	public sealed class RowVm
	{
		public string Name { get; init; } = "";
		public string Mean { get; init; } = "—";
		public string Stddev { get; init; } = "—";
		public string Allocated { get; init; } = "—";
		public string N { get; init; } = "—";

		public static RowVm From(ResultRow row) => new()
		{
			Name = row.Benchmark ?? "(unknown)",
			Mean = row.Stats != null ? FmtTime(row.Stats.MeanNs) : (row.Error ?? "—"),
			Stddev = row.Stats != null ? FmtTime(row.Stats.StddevNs) : "—",
			Allocated = row.Memory != null ? FmtBytes(row.Memory.AllocatedBytesPerOp) : "—",
			N = row.Stats?.N.ToString() ?? "—",
		};

		private static string FmtTime(double ns) => ns switch
		{
			< 1_000 => $"{ns:F1} ns",
			< 1_000_000 => $"{ns / 1_000:F2} µs",
			< 1_000_000_000 => $"{ns / 1_000_000:F2} ms",
			_ => $"{ns / 1_000_000_000:F2} s",
		};

		private static string FmtBytes(long bytes) => bytes switch
		{
			< 1_024 => $"{bytes} B",
			< 1_024 * 1_024 => $"{bytes / 1_024.0:F1} KB",
			_ => $"{bytes / (1_024.0 * 1_024.0):F2} MB",
		};
	}

	private sealed class InAppLogger : ILogger
	{
		private readonly TextBlock _target;
		private readonly ScrollViewer _scroller;

		public InAppLogger(TextBlock target, ScrollViewer scroller)
		{
			_target = target;
			_scroller = scroller;
		}

		public string Id => nameof(InAppLogger);
		public int Priority => 0;

		public void Write(string text) => Append(text, Colors.Gray);
		public void Write(LogKind logKind, string text) => Append(text, ColorFor(logKind));
		public void WriteLine() => DispatchAppend(new LineBreak());
		public void WriteLine(string text) { Append(text, Colors.Gray); WriteLine(); }
		public void WriteLine(LogKind logKind, string text) { Append(text, ColorFor(logKind)); WriteLine(); }
		public void WriteLineError(string text) { Append(text, Colors.OrangeRed); WriteLine(); }
		public void Flush() { }

		private void Append(string text, Color color)
			=> DispatchAppend(new Run { Text = text, Foreground = new SolidColorBrush(color) });

		private void DispatchAppend(Inline inline)
		{
			if (_target.DispatcherQueue.HasThreadAccess)
			{
				_target.Inlines.Add(inline);
				_scroller.ChangeView(null, _scroller.ScrollableHeight, null, disableAnimation: true);
			}
			else
			{
				_target.DispatcherQueue.TryEnqueue(() =>
				{
					_target.Inlines.Add(inline);
					_scroller.ChangeView(null, _scroller.ScrollableHeight, null, disableAnimation: true);
				});
			}
		}

		private static Color ColorFor(LogKind logKind) => logKind switch
		{
			LogKind.Header => Colors.MediumPurple,
			LogKind.Result => Colors.DarkCyan,
			LogKind.Statistic => Colors.SteelBlue,
			LogKind.Info => Colors.DarkOrange,
			LogKind.Error => Colors.OrangeRed,
			LogKind.Hint => Colors.Teal,
			LogKind.Help => Colors.ForestGreen,
			_ => Colors.Gray,
		};
	}
}
