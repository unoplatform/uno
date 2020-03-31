using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.InProcess;
using Windows.Foundation;
using Windows.Foundation.Collections;
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

		public string ClassFilter { get; set; } = "";

		private void OnRunTests(object sender, object args)
		{
			_ = Dispatcher.RunAsync(
				CoreDispatcherPriority.Normal,
				async () => await Run()
			);
		}

		private async Task Run()
		{
			_logger = new TextBlockLogger(runLogs);

			try
			{
				var config = new CoreConfig(_logger);

				await SetStatus("Discovering benchmarks in " + BenchmarksBaseNamespace);
				var types = EnumerateBenchmarks(config).ToArray();

				foreach (var type in types)
				{
					await SetStatus($"Running benchmarks for {type}");
					var b = BenchmarkRunner.Run(type, config);
				}

				await SetStatus($"Done.");
			}
			catch(Exception e)
			{
				await SetStatus($"Failed {e?.Message}");
				_logger.WriteLine(LogKind.Error, e?.ToString());
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
			   where string.IsNullOrEmpty(ClassFilter) || type.Name.Contains(ClassFilter)
			   select type;

		public class CoreConfig : ManualConfig
		{
			public CoreConfig(ILogger logger)
			{
				Add(logger);

#if __WASM__
				Add(AsciiDocExporter.Default);
#endif

				Add(Job.InProcess
					.WithLaunchCount(1)
					.WithWarmupCount(1)
					.WithIterationCount(5)
					.With(InProcessToolchain.Synchronous)
					.WithId("InProcess")
				);

				ArtifactsPath = Path.GetTempPath();
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

			public TextBlockLogger(TextBlock target)
				=> _target = target;

			public void Flush() { }

			public void Write(LogKind logKind, string text)
			{
				_target.Inlines.Add(new Run { Text = text, Foreground = GetLogKindColor(logKind) });
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
				Write(logKind, text);
				WriteLine();
			}
		}
	}
}
