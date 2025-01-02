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
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Private.Infrastructure;

namespace Benchmarks.Shared.Controls
{
	public sealed partial class BenchmarkDotNetControl : UserControl
	{
		private const string BenchmarksBaseNamespace = "SamplesApp.Benchmarks.Suite";
		private TextBlockLogger _logger;

		public BenchmarkDotNetControl()
		{
			this.InitializeComponent();
		}

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

		private async Task Run()
		{
			_logger = new TextBlockLogger(runLogs, debugLog.IsChecked ?? false);

			try
			{
				var config = new CoreConfig(_logger);

				BenchmarkUIHost.Root = FindName("testHost") as ContentControl;

				await SetStatus("Discovering benchmarks in " + BenchmarksBaseNamespace);
				var types = EnumerateBenchmarks(config).ToArray();

				int currentCount = 0;
				foreach (var type in types)
				{
					runCount.Text = (++currentCount).ToString();

					await SetStatus($"Running benchmarks for {type}");
					var b = BenchmarkRunner.Run(type, config);

					for (int i = 0; i < 3; i++)
					{
						await Dispatcher.RunIdleAsync(_ =>
						{
							GC.Collect();
							GC.WaitForPendingFinalizers();
						});
					}
				}

				await SetStatus($"Finished");

				ArchiveTestResult(config);
			}
			catch (Exception e)
			{
				await SetStatus($"Failed {e?.Message}");
				_logger.WriteLine(LogKind.Error, e?.ToString());
			}
			finally
			{
				BenchmarkUIHost.Root = null;
			}
		}

		private void ArchiveTestResult(CoreConfig config)
		{
			var archiveName = BenchmarkResultArchiveName;

			if (File.Exists(archiveName))
			{
				File.Delete(archiveName);
			}

			ZipFile.CreateFromDirectory(config.ArtifactsPath, archiveName, CompressionLevel.Optimal, false);

			downloadResults.IsEnabled = true;

			ResultsAsBase64 = Convert.ToBase64String(File.ReadAllBytes(BenchmarkResultArchiveName));
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
			await Task.Yield();
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
#if __IOS__
					// Fails on iOS with code generation used by EmitInvokeMultiple
					.WithUnrollFactor(1)
#endif
					.With(InProcessToolchain.Synchronous)
					.WithId("InProcess")
				);

				ArtifactsPath = Path.Combine(Windows.Storage.ApplicationData.Current.LocalFolder.Path, "benchmarks");
			}
		}

		private class TextBlockLogger : ILogger
		{
			private static Dictionary<LogKind, SolidColorBrush> ColorfulScheme { get; } =
			   new Dictionary<LogKind, SolidColorBrush>
			   {
					{ LogKind.Default, new SolidColorBrush(Colors.Gray) },
					{ LogKind.Help, new SolidColorBrush(Colors.DarkGreen) },
					{ LogKind.Header, new SolidColorBrush(Colors.Magenta) },
					{ LogKind.Result, new SolidColorBrush(Colors.DarkCyan) },
					{ LogKind.Statistic, new SolidColorBrush(Colors.Cyan) },
					{ LogKind.Info, new SolidColorBrush(Colors.DarkOrange) },
					{ LogKind.Error, new SolidColorBrush(Colors.Red) },
					{ LogKind.Hint, new SolidColorBrush(Colors.DarkCyan) }
			   };

			private readonly TextBlock _target;
			private LogKind _minLogKind;

			public TextBlockLogger(TextBlock target, bool isDebug)
			{
				_target = target;
				_minLogKind = isDebug ? LogKind.Default : LogKind.Statistic;
			}

			public void Flush() { }

			public void Write(LogKind logKind, string text)
			{
				if (logKind >= _minLogKind)
				{
					_target.Inlines.Add(new Run { Text = text, Foreground = GetLogKindColor(logKind) });
				}
			}

			public static Brush GetLogKindColor(LogKind logKind)
			{
				if (!ColorfulScheme.TryGetValue(logKind, out var brush))
				{
					brush = ColorfulScheme[LogKind.Default];
				}

				return brush;
			}

			public void WriteLine() => _target.Inlines.Add(new LineBreak());

			public void WriteLine(LogKind logKind, string text)
			{
				if (logKind >= _minLogKind)
				{
					Write(logKind, text);
					WriteLine();
				}
			}
		}
	}
}
