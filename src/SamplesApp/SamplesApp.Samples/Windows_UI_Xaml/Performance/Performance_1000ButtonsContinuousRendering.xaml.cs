using System;
using System.Threading.Tasks;
using Microsoft.UI.Composition;
using Uno.UI.Samples.Controls;
using Microsoft.UI.Xaml.Controls;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace UITests.Windows_UI_Xaml.Performance
{
	/// <summary>
	/// An empty page that can be used on its own or navigated to within a Frame.
	/// </summary>
	[Sample("Performance")]
	public sealed partial class Performance_1000ButtonsContinuousRendering : Page
	{
		private EventHandler<object> _fpsHandler;
		private int _fpsFrames;
		private DateTime _fpsWindowStart;

		public Performance_1000ButtonsContinuousRendering()
		{
			this.InitializeComponent();

			Loaded += (s, e) =>
			{
				colorStoryboard.Begin();
#if __SKIA__
				// Benchmark hooks: UNO_PERF_BUTTONS overrides the button count, UNO_LOG_FPS=1 prints frames/sec
				// to the console (hooking Rendering also keeps the render loop at full rate).
				var count = int.TryParse(Environment.GetEnvironmentVariable("UNO_PERF_BUTTONS"), out var n) ? n : 100;
				Console.WriteLine($"PERF-SAMPLE: loaded, buttons={count}, fpslog={Environment.GetEnvironmentVariable("UNO_LOG_FPS")}");

				// UNO_PERF_MAXIMIZE=1: benchmark at a realistic full-screen surface size.
				if (Environment.GetEnvironmentVariable("UNO_PERF_MAXIMIZE") is "1" or "true"
					&& SamplesApp.App.MainWindow?.AppWindow?.Presenter is Microsoft.UI.Windowing.OverlappedPresenter presenter)
				{
					presenter.Maximize();
					Console.WriteLine("PERF-SAMPLE: window maximized");
				}
				// Drive the count through the NumberBox so its own initial ValueChanged can't race this back
				// to the default, and the UI shows the effective count.
				if (numberBox.Value != count)
				{
					numberBox.Value = count;
				}
				else
				{
					NumberBoxValueChanged(this, new NumberBoxValueChangedEventArgs(numberBox.Value, count));
				}

				if (Environment.GetEnvironmentVariable("UNO_LOG_FPS") is "1" or "true" && _fpsHandler is null)
				{
					// On the browser the console isn't harvestable by scripts, so the series is also POSTed to
					// the runtime-tests companion file server after a fixed measurement window.
					var browserSeries = OperatingSystem.IsBrowser() ? new System.Collections.Generic.List<double>() : null;
					_fpsWindowStart = DateTime.UtcNow;
					_fpsHandler = (_, _) =>
					{
						_fpsFrames++;
						var elapsed = (DateTime.UtcNow - _fpsWindowStart).TotalSeconds;
						if (elapsed >= 1)
						{
							var fps = _fpsFrames / elapsed;
							Console.WriteLine($"FPS: {fps:F1}");
							browserSeries?.Add(fps);
							_fpsFrames = 0;
							_fpsWindowStart = DateTime.UtcNow;
						}
					};
					Microsoft.UI.Xaml.Media.CompositionTarget.Rendering += _fpsHandler;

					if (browserSeries is not null)
					{
						_ = Task.Run(async () =>
						{
							await Task.Delay(TimeSpan.FromSeconds(45));
							var content = string.Join("\n", browserSeries.ConvertAll(f => f.ToString("F1", System.Globalization.CultureInfo.InvariantCulture)));
							await Uno.UI.Samples.UITests.Helpers.SkiaSamplesAppHelper.SaveFile("wasm-fps-results.txt", content);
						});
					}
				}
#endif
			};

			Unloaded += (s, e) =>
			{
				colorStoryboard.Stop();
				if (_fpsHandler is not null)
				{
					Microsoft.UI.Xaml.Media.CompositionTarget.Rendering -= _fpsHandler;
					_fpsHandler = null;
				}
			};
		}

		private async void NumberBoxValueChanged(object sender, NumberBoxValueChangedEventArgs e)
		{
#if __SKIA__
			wp.Children.Clear();
			var val = (int)Math.Round(Math.Max(0, e.NewValue));
			for (var i = 0; i < val; i++)
			{
				wp.Children.Add(new Button { Content = i.ToString() });
			}

			await Task.Delay(TimeSpan.FromSeconds(1));
			tb.Text = $"Number of visuals in WrapPanel: {wp.Visual.GetSubTreeVisualCount()}";
#else
			await Task.CompletedTask;
#endif
		}
	}
}
