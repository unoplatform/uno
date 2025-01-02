using System;
using System.Linq;
using BenchmarkDotNet.Attributes;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using BenchmarkDotNet.Engines;

namespace SamplesApp.Benchmarks.Suite.Windows_UI_Xaml_Controls.GridBench
{
	public class SimpleGridBenchmark
	{
		private Grid _sut;

		[Params(0, 1, 5)]
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

		[IterationCleanup]
		public void Cleanup()
		{
			_sut = null;
			GC.Collect();
			GC.WaitForPendingFinalizers();
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
		public void Complex_Measure_And_Arrange_Pixels()
		{
			Complex_Measure_And_Arrange(new GridLength(10));
		}

		[Benchmark]
		public void Complex_Measure_And_Arrange_Auto()
		{
			Complex_Measure_And_Arrange(new GridLength(0, GridUnitType.Auto));
		}

		[Benchmark]
		public void Complex_Measure_And_Arrange_Star()
		{
			Complex_Measure_And_Arrange(new GridLength(1, GridUnitType.Star));
		}

		private void Complex_Measure_And_Arrange(GridLength gridLength)
		{
			var children = _sut.Children;

			for (var i = 0; i < ItemsCount; i++)
			{
				var colDef = new ColumnDefinition { Width = gridLength };
				_sut.ColumnDefinitions.Add(colDef);

				var border = children[i] as FrameworkElement;

				Grid.SetColumn(border, i);
			}

			_sut.Measure(new Size(500, 500));
			_sut.Arrange(new Rect(0, 0, 500, 500));
		}

		[Benchmark]
		public void Complex_MultiDimension_Measure_And_Arrange_Pixels()
		{
			Complex_MultiDimension_Measure_And_Arrange(new GridLength(10));
		}

		[Benchmark]
		public void Complex_MultiDimension_Measure_And_Arrange_Auto()
		{
			Complex_MultiDimension_Measure_And_Arrange(new GridLength(0, GridUnitType.Auto));
		}

		[Benchmark]
		public void Complex_MultiDimension_Measure_And_Arrange_Star()
		{
			Complex_MultiDimension_Measure_And_Arrange(new GridLength(1, GridUnitType.Star));
		}

		private void Complex_MultiDimension_Measure_And_Arrange(GridLength gridLength)
		{
			var children = _sut.Children;

			for (var i = 0; i < ItemsCount; i++)
			{
				var colDef = new ColumnDefinition { Width = gridLength };
				_sut.ColumnDefinitions.Add(colDef);

				var rowDef = new RowDefinition { Height = gridLength };
				_sut.RowDefinitions.Add(rowDef);

				var border = children[i] as FrameworkElement;

				Grid.SetColumn(border, i);
				Grid.SetColumnSpan(border, i % 4 + 1);

				Grid.SetRow(border, i);
				Grid.SetRowSpan(border, i % 3 + 1);
			}

			_sut.Measure(new Size(2000, 2000));
			_sut.Arrange(new Rect(0, 0, 2000, 2000));
		}
	}
}
