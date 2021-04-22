using System;
using BenchmarkDotNet.Attributes;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using BenchmarkDotNet.Engines;

namespace SamplesApp.Benchmarks.Suite.Windows_UI_Xaml_Controls.GridBench
{
	[MinColumn, MaxColumn, MeanColumn, MedianColumn]
	[SimpleJob(launchCount: 3, warmupCount: 10, targetCount: 30)]
	public class SimpleGridBenchmark
	{
		private Grid _sut;

		[Params(0, 1, 5, 50)]
		public int ItemsCount { get; set; }

		[IterationSetup]
		public void Setup()
		{
			_sut = new Grid();

			for (var i = 0; i < ItemsCount; i++)
			{
				_sut.Children.Add(new Border { Width = 10, Height = 10 });
			}
		}

		[Benchmark(Baseline = true)]
		public void Superposition_Measure()
		{
			_sut.Measure(new Size(20, 20));
		}

		[Benchmark]
		public void Superposition_Measure_And_Arrange()
		{
			_sut.Measure(new Size(20, 20));
			_sut.Arrange(new Rect(0, 0, 20, 20));
		}

		[Benchmark]
		public void Complex_Measure_And_Arrange()
		{
			for (var i = 0; i < ItemsCount; i++)
			{
				var definition = (i % 5) switch
				{
					0 => new ColumnDefinition { Width = 10 },
					1 => new ColumnDefinition { Width = "*" },
					2 => new ColumnDefinition { Width = "Auto" },
					3 => new ColumnDefinition { Width = 0 },
					4 => new ColumnDefinition { Width = "2*" },
					_ => throw new ArgumentOutOfRangeException()
				};
				_sut.ColumnDefinitions.Add(definition);

				var border = new Border();

				Grid.SetColumn(border, i);
				_sut.Children.Add(border);
			}

			_sut.Measure(new Size(20, 20));
			_sut.Arrange(new Rect(0, 0, 20, 20));
		}
	}
}
