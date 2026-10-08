using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Exporters.Csv;
using BenchmarkDotNet.Exporters.Json;
using BenchmarkDotNet.Horology;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.InProcess;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Provider;
using Microsoft.UI;
using Windows.UI;
using Windows.UI.Core;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Private.Infrastructure;
using Uno.UI.Samples.Helper;

namespace Benchmarks.Shared.Controls
{
	public sealed partial class BenchmarkDotNetControl : UserControl
	{
		private const string BenchmarksBaseNamespace = "SamplesApp.Benchmarks.Suite";
		private TextBlockLogger _logger;

		// Below this width the controls stack and the log and UI host share a column.
		private const double NarrowWidth = 600;
		private const double MinOutputHeight = 220;
		// Fixed so benchmarks rendering into testHost always get a finite measure.
		internal const double NarrowHostHeight = 240;
		private const double LogFollowThreshold = 32;
		private bool? _isNarrow;
		private bool _logScrollQueued;

		public BenchmarkDotNetControl()
		{
			this.InitializeComponent();

			ActualThemeChanged += (_, _) => _logger?.ApplyTheme(ActualTheme);

			if (ShellFunctions.IsTouchShell)
			{
				// The Fluent template centres the box but top-aligns the label, so a taller CheckBox needs a centred label.
				debugLog.MinHeight = 40;
				debugLog.VerticalContentAlignment = VerticalAlignment.Center;
				debugLog.Padding = new Thickness(8, 0, 0, 0);
			}
		}

		public bool ShowHeader
		{
			get => (bool)GetValue(ShowHeaderProperty);
			set => SetValue(ShowHeaderProperty, value);
		}

		public static DependencyProperty ShowHeaderProperty { get; } =
			DependencyProperty.Register(nameof(ShowHeader), typeof(bool), typeof(BenchmarkDotNetControl), new PropertyMetadata(true, OnShowHeaderChanged));

		private static void OnShowHeaderChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
			=> ((BenchmarkDotNetControl)d).ShellBenchTitle.Visibility = (bool)e.NewValue ? Visibility.Visible : Visibility.Collapsed;

		public string ResultsAsBase64
		{
			get => (string)GetValue(ResultsAsBase64Property);
			set => SetValue(ResultsAsBase64Property, value);
		}

		public static readonly DependencyProperty ResultsAsBase64Property =
			DependencyProperty.Register("ResultsAsBase64", typeof(string), typeof(BenchmarkDotNetControl), new PropertyMetadata(""));

		public string ClassFilter { get; set; } = "";

		private void OnRunTests(object sender, object args)
		{
			_ = UnitTestDispatcherCompat.From(this).RunAsync(
				UnitTestDispatcherCompat.Priority.Normal,
				async () => await Run()
			);
		}

		internal async Task Run()
		{
			_logger = new TextBlockLogger(runLogs, debugLog.IsChecked ?? false, ActualTheme, OnLogAppended);
			runLogs.Inlines.Clear();
			SetRunning(true);

			try
			{
				var config = new CoreConfig(_logger);

				BenchmarkUIHost.Root = FindName("testHost") as ContentControl;

				await SetStatus("Discovering benchmarks in " + BenchmarksBaseNamespace);
				var types = EnumerateBenchmarks(config).ToArray();

				int currentCount = 0;
				SetRunCount(0);
				ShellBenchCountPanel.Visibility = Visibility.Visible;

				if (types.Length == 0)
				{
					await SetStatus(string.IsNullOrEmpty(ClassFilter)
						? $"No benchmarks found in {BenchmarksBaseNamespace}"
						: $"No benchmarks match \"{ClassFilter}\"");
					return;
				}

				// Earlier runs' reports would otherwise end up in this run's archive.
				if (Directory.Exists(config.ArtifactsPath))
				{
					Directory.Delete(config.ArtifactsPath, recursive: true);
				}

				foreach (var type in types)
				{
					SetRunCount(++currentCount);

					await SetStatus($"Running benchmarks for {type}");
					var b = BenchmarkRunner.Run(type, config);

					for (int i = 0; i < 3; i++)
					{
						await CollectGarbageWhenIdleAsync();
					}
				}

				await CompleteRun(config.ArtifactsPath);
			}
			catch (Exception e)
			{
				await SetStatus($"Failed {e?.Message}");
				_logger.WriteLine(LogKind.Error, e?.ToString());
			}
			finally
			{
				BenchmarkUIHost.Root = null;
				SetRunning(false);
			}
		}

		private async Task CollectGarbageWhenIdleAsync()
		{
			static void Collect()
			{
				GC.Collect();
				GC.WaitForPendingFinalizers();
			}

#if WINAPPSDK
			// UIElement.Dispatcher is null on WinAppSDK and it has no idle priority.
			var completion = new TaskCompletionSource();
			if (!DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
			{
				Collect();
				completion.SetResult();
			}))
			{
				completion.SetResult();
			}

			await completion.Task;
#else
			await Dispatcher.RunIdleAsync(_ => Collect());
#endif
		}

		// The shell must not react to testHost.Content changes: benchmarks set it inside their measured loops.
		private void SetRunning(bool isRunning)
		{
			runButton.IsEnabled = !isRunning;
			ShellBenchProgress.IsActive = isRunning;
			ShellBenchProgress.Visibility = isRunning ? Visibility.Visible : Visibility.Collapsed;
			runStatus.Margin = isRunning ? new Thickness(12, 0, 0, 0) : new Thickness(0);
			ShellBenchHostEmpty.Visibility = isRunning ? Visibility.Collapsed : Visibility.Visible;
		}

		private void SetRunCount(int count)
		{
			runCount.Text = count.ToString();
			AutomationProperties.SetName(runCount, $"Types run: {count}");
		}

		private void OnScrollerSizeChanged(object sender, SizeChangedEventArgs e) => ApplyLayout();

		private void OnCardSizeChanged(object sender, SizeChangedEventArgs e) => ApplyLayout();

		// Fills the viewport when there is room, otherwise keeps a usable output area and scrolls the page.
		private void ApplyLayout()
		{
			var width = ShellBenchScroller.ActualWidth;
			if (width <= 0)
			{
				return;
			}

			var narrow = width < NarrowWidth;
			if (narrow != _isNarrow)
			{
				_isNarrow = narrow;
				ApplyNarrow(narrow);
			}

			var padding = ShellBenchRoot.Padding;
			var topHeight = padding.Top + padding.Bottom + ShellBenchTitleHost.ActualHeight + ShellBenchCard.ActualHeight + (2 * ShellBenchRoot.RowSpacing);
			var viewport = ShellBenchScroller.ActualHeight;
			var hostHeight = narrow ? NarrowHostHeight + ShellBenchOutput.RowSpacing : 0;
			var needed = topHeight + MinOutputHeight + hostHeight;
			ShellBenchRoot.Height = Math.Max(viewport, needed);
		}

		private void ApplyNarrow(bool narrow)
		{
			ShellBenchRoot.Padding = narrow ? new Thickness(16, 12, 16, 12) : new Thickness(24, 16, 24, 16);
			ShellBenchStatusGrid.RowSpacing = narrow ? 8 : 0;
			ShellBenchCountPanel.Margin = narrow ? new Thickness(0) : new Thickness(12, 0, 0, 0);
			downloadResults.Margin = narrow ? new Thickness(0) : new Thickness(12, 0, 0, 0);

			// Touch-sized controls on phones.
			var minHeight = narrow || ShellFunctions.IsTouchShell ? 40d : 0d;
			ShellBenchFilter.MinWidth = narrow ? 0 : 240;
			runButton.MinHeight = minHeight;
			downloadResults.MinHeight = minHeight;

			if (narrow)
			{
				Place(ShellBenchFilter, 0, 0, 3);
				Place(debugLog, 1, 0, 3);
				Place(runButton, 2, 0, 3);
				Place(ShellBenchStatusGrid, 3, 0, 3);
				runButton.HorizontalAlignment = HorizontalAlignment.Stretch;
				runButton.HorizontalContentAlignment = HorizontalAlignment.Center;
				downloadResults.HorizontalAlignment = HorizontalAlignment.Stretch;
				downloadResults.HorizontalContentAlignment = HorizontalAlignment.Center;
				Place(ShellBenchProgress, 0, 0, 1);
				Place(runStatus, 0, 1, 3);
				Place(ShellBenchCountPanel, 1, 0, 4);
				Place(downloadResults, 2, 0, 4);
				EnsureRows(ShellBenchControls, 4);
			}
			else
			{
				Place(ShellBenchFilter, 0, 0, 1);
				Place(debugLog, 0, 1, 1);
				Place(runButton, 0, 2, 1);
				Place(ShellBenchStatusGrid, 1, 0, 3);
				runButton.HorizontalAlignment = HorizontalAlignment.Left;
				downloadResults.HorizontalAlignment = HorizontalAlignment.Left;
				Place(ShellBenchProgress, 0, 0, 1);
				Place(runStatus, 0, 1, 1);
				Place(ShellBenchCountPanel, 0, 2, 1);
				Place(downloadResults, 0, 3, 1);
				EnsureRows(ShellBenchControls, 2);
			}

			ShellBenchOutput.RowDefinitions[1].Height = new GridLength(narrow ? NarrowHostHeight : 0);
			Place(ShellBenchHostCard, narrow ? 1 : 0, narrow ? 0 : 1, narrow ? 2 : 1);
			Grid.SetColumnSpan(ShellBenchLogCard, narrow ? 2 : 1);
		}

		private static void Place(FrameworkElement element, int row, int column, int columnSpan)
		{
			Grid.SetRow(element, row);
			Grid.SetColumn(element, column);
			Grid.SetColumnSpan(element, columnSpan);
		}

		private static void EnsureRows(Grid grid, int count)
		{
			while (grid.RowDefinitions.Count < count)
			{
				grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
			}

			while (grid.RowDefinitions.Count > count)
			{
				grid.RowDefinitions.RemoveAt(grid.RowDefinitions.Count - 1);
			}
		}

		// "Finished" goes last: BenchmarkDotNetTests (SamplesApp.UITests) reads ResultsAsBase64 as soon as it sees it.
		internal async Task CompleteRun(string artifactsPath)
			=> await SetStatus(ArchiveTestResult(artifactsPath) ? "Finished" : "Failed: no benchmark results were written");

		private bool ArchiveTestResult(string artifactsPath)
		{
			var archiveName = BenchmarkResultArchiveName;

			if (!Directory.Exists(artifactsPath))
			{
				_logger?.WriteLine(LogKind.Error, $"No benchmark artifacts were written to {artifactsPath}.");
				return false;
			}

			if (File.Exists(archiveName))
			{
				File.Delete(archiveName);
			}

			ZipFile.CreateFromDirectory(artifactsPath, archiveName, CompressionLevel.Optimal, false);

			downloadResults.IsEnabled = true;

			ResultsAsBase64 = Convert.ToBase64String(File.ReadAllBytes(BenchmarkResultArchiveName));
			return true;
		}

		private static string BenchmarkResultArchiveName
			=> Path.Combine(Windows.Storage.ApplicationData.Current.LocalFolder.Path, "benchmarks-results.zip");

		private async void OnDownloadResults()
		{
			FileSavePicker savePicker = new FileSavePicker();

			savePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;

			// Dropdown of file types the user can save the file as
			savePicker.FileTypeChoices.Add("Zip Archive", new List<string>() { ".zip" });

			// Default file name if the user does not type one in or select a file to replace
			savePicker.SuggestedFileName = "benchmarks-results";

			var file = await savePicker.PickSaveFileAsync();
			if (file != null)
			{
				CachedFileManager.DeferUpdates(file);

				await FileIO.WriteBytesAsync(file, File.ReadAllBytes(BenchmarkResultArchiveName));

				await CachedFileManager.CompleteUpdatesAsync(file);
			}
		}

		private async Task SetStatus(string status)
		{
			runStatus.Text = status;

			// Neither WinUI nor Uno raises LiveRegionChanged by itself when the text changes.
			var peer = FrameworkElementAutomationPeer.FromElement(runStatus)
				?? FrameworkElementAutomationPeer.CreatePeerForElement(runStatus);
			peer?.RaiseAutomationEvent(AutomationEvents.LiveRegionChanged);

			await Task.Yield();
		}

		private void OnLogAppended()
		{
			ShellBenchLogEmpty.Visibility = Visibility.Collapsed;

			// Follow the log only while the reader is at its end; ScrollableHeight is still pre-append here.
			if (_logScrollQueued
				|| ShellBenchLogScroller.VerticalOffset < ShellBenchLogScroller.ScrollableHeight - LogFollowThreshold)
			{
				return;
			}

			_logScrollQueued = true;
			DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
			{
				_logScrollQueued = false;
				ShellBenchLogScroller.UpdateLayout();
				ShellBenchLogScroller.ChangeView(null, ShellBenchLogScroller.ScrollableHeight, null, disableAnimation: true);
			});
		}

		private IEnumerable<Type> EnumerateBenchmarks(IConfig config)
			=> from type in GetType().GetTypeInfo().Assembly.GetTypes()
			   where !type.IsGenericType
			   where type.Namespace?.StartsWith(BenchmarksBaseNamespace) ?? false
			   where BenchmarkConverter.TypeToBenchmarks(type, config).BenchmarksCases.Length != 0
			   where string.IsNullOrEmpty(ClassFilter)
					 || type.Name.IndexOf(ClassFilter, StringComparison.InvariantCultureIgnoreCase) >= 0
			   select type;

		public class CoreConfig : ManualConfig
		{
			public CoreConfig(ILogger logger)
			{
				Add(logger);

				Add(AsciiDocExporter.Default);
				Add(JsonExporter.Full);
				Add(CsvExporter.Default);
				Add(BenchmarkDotNet.Exporters.Xml.XmlExporter.Full);

				Add(Job.InProcess
					.WithLaunchCount(1)
					.WithWarmupCount(1)
					.WithIterationCount(5)
					.WithIterationTime(TimeInterval.FromMilliseconds(100))
#if __APPLE_UIKIT__
					// Fails on iOS with code generation used by EmitInvokeMultiple
					.WithUnrollFactor(1)
#endif
					.With(InProcessToolchain.Synchronous)
					.WithId("InProcess")
				);

				ArtifactsPath = Path.Combine(Windows.Storage.ApplicationData.Current.LocalFolder.Path, "benchmarks");
			}
		}

		internal static string GetLogBrushKey(LogKind logKind) => logKind switch
		{
			LogKind.Header => "AccentTextFillColorPrimaryBrush",
			LogKind.Result => "SystemFillColorSuccessBrush",
			LogKind.Statistic or LogKind.Info => "TextFillColorPrimaryBrush",
			LogKind.Error => "SystemFillColorCriticalBrush",
			_ => "TextFillColorSecondaryBrush",
		};

		private class TextBlockLogger : ILogger
		{
			private readonly TextBlock _target;
			private readonly LogKind _minLogKind;
			private readonly List<(Run Run, LogKind Kind)> _runs = new();
			private readonly Dictionary<LogKind, Brush> _brushes = new();
			private readonly Action _appended;
			private ElementTheme _theme;

			public TextBlockLogger(TextBlock target, bool isDebug, ElementTheme theme, Action appended)
			{
				_target = target;
				_theme = theme;
				_appended = appended;
				_minLogKind = isDebug ? LogKind.Default : LogKind.Statistic;
			}

			public void Flush() { }

			public void ApplyTheme(ElementTheme theme)
			{
				_theme = theme;
				_brushes.Clear();
				foreach (var (run, kind) in _runs)
				{
					run.Foreground = GetBrush(kind);
				}
			}

			public void Write(LogKind logKind, string text)
			{
				if (logKind < _minLogKind)
				{
					return;
				}

				RunOnUIThread(() => Append(logKind, text));
			}

			public void WriteLine() => RunOnUIThread(() =>
			{
				_target.Inlines.Add(new LineBreak());
				_appended();
			});

			public void WriteLine(LogKind logKind, string text)
			{
				if (logKind < _minLogKind)
				{
					return;
				}

				RunOnUIThread(() =>
				{
					Append(logKind, text);
					_target.Inlines.Add(new LineBreak());
				});
			}

			private void Append(LogKind logKind, string text)
			{
				var run = new Run { Text = text, Foreground = GetBrush(logKind) };
				_runs.Add((run, logKind));
				_target.Inlines.Add(run);
				_appended();
			}

			// ShellThemeBrushes and the inline collection are UI-thread only.
			private void RunOnUIThread(Action action)
			{
				if (_target.DispatcherQueue.HasThreadAccess)
				{
					action();
				}
				else
				{
					_target.DispatcherQueue.TryEnqueue(() => action());
				}
			}

			private Brush GetBrush(LogKind logKind)
			{
				if (!_brushes.TryGetValue(logKind, out var brush))
				{
					brush = ShellThemeBrushes.Get(GetLogBrushKey(logKind), _theme);
					if (brush is not null)
					{
						_brushes[logKind] = brush;
					}
				}

				return brush;
			}
		}
	}
}
