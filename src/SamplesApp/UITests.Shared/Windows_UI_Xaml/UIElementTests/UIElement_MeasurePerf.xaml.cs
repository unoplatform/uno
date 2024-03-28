using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;
using SampleApps.Utilities;
using Uno.UI;
using Uno.UI.Samples.Controls;
using Color = System.Drawing.Color;

namespace UITests.Windows_UI_Xaml.UIElementTests
{
	[Sample("UIElement", IgnoreInSnapshotTests = true, IsManualTest = true)]
	public sealed partial class UIElement_MeasurePerf : Page
	{
		public UIElement_MeasurePerf()
		{
			this.InitializeComponent();

#if !WINAPPSDK
			bool originalUseInvalidateMeasurePath = FeatureConfiguration.UIElement.UseInvalidateMeasurePath;
			bool originalUseInvalidateArrangePath = FeatureConfiguration.UIElement.UseInvalidateArrangePath;

			Loaded += (_, _) =>
			{
				optimizeMeasure.IsChecked = FeatureConfiguration.UIElement.UseInvalidateMeasurePath;
				optimizeArrange.IsChecked = FeatureConfiguration.UIElement.UseInvalidateArrangePath;
			};

			Unloaded += (_, _) =>
			{
				FeatureConfiguration.UIElement.UseInvalidateMeasurePath = originalUseInvalidateMeasurePath;
				FeatureConfiguration.UIElement.UseInvalidateArrangePath = originalUseInvalidateArrangePath;
			};
#endif
		}

		private void BuildUI1(object sender, RoutedEventArgs e)
		{
			var (root, leaves, mostInner) = BuildTest1(0);

			_root = root;
			_leaves = leaves.ToArray();
			_mostInner = mostInner;

			testPlaceHolder.Child = _root;

			result.Text = "Model 1 created";
		}

		private FrameworkElement _root;
		private RainbowMeasures[] _leaves;
		private RainbowMeasures _mostInner;

		private async void GoTest1(object sender, RoutedEventArgs e)
		{
			result.Text = "Testing Invalidations...";

			await Task.Yield();
			await Task.Yield();
			await Task.Yield();

			var sw = new Stopwatch();
			sw.Start();

			var loops = (int)iterations.Value;

			for (var i = 0; i < loops; i++)
			{
				for (var j = 0; j < _leaves.Length; j++)
				{
					var leaf = _leaves[j];

					//await leaf.InvalidateAndWaitUntilNextMeasure();

					leaf.InvalidateMeasure();
					await Task.Yield();
				}
			}

			await Task.Yield();
			sw.Stop();

			result.Text = $"Took {sw.ElapsedMilliseconds} ms";
		}

		private async void GoTest2(object sender, RoutedEventArgs e)
		{
			result.Text = "Testing Resizes...";

			await Task.Yield();
			await Task.Yield();
			await Task.Yield();

			var sw = new Stopwatch();
			sw.Start();

			var loops = (int)iterations.Value;

			var modulo = deepness.Value * 4 + 10;

			for (var i = 0; i < loops; i++)
			{
				var size = i % modulo + modulo;

				_root.Height = size;

				await Task.Yield();
			}

			await Task.Yield();
			sw.Stop();

			_root.ClearValue(HeightProperty);

			result.Text = $"Resizes took {sw.ElapsedMilliseconds} ms";
		}

		private (FrameworkElement Root, ICollection<RainbowMeasures> Leaves, RainbowMeasures mostInner) BuildTest1(int dept)
		{
			var root = new RainbowMeasures { Name = $"Root_{dept}" };

			var leaves = new List<RainbowMeasures>((int)wideness.Value);
			leaves.Add(root);

			var grid = new Grid { Margin = new Thickness(2) };
			grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
			grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

			root.Children.Add(grid);

			var background = new Rectangle
			{
				Fill = new SolidColorBrush(Colors.Gray),
				Opacity = 0.1d
			};

			Grid.SetColumnSpan(background, 3);
			grid.Children.Add(background);

			var left = new RainbowMeasures
			{
				Children = { new Button { Width = 3 } },
				Name = $"Left_{dept}"
			};
			grid.Children.Add(left);
			leaves.Add(left);

			RainbowMeasures mostInner;

			var deepnessValue = (int)deepness.Value;
			if (dept < deepnessValue)
			{
				var (subRoot, subChildren, inner) = BuildTest1(dept + 1);
				grid.Children.Add(subRoot);
				Grid.SetColumn(subRoot, 1);
				leaves.AddRange(subChildren);

				mostInner = inner;
			}
			else
			{
				var text = new TextBlock { Text = $"most inner, dept={dept}" };
				mostInner = new RainbowMeasures
				{
					Children =
					{
						new Border { Child = text, Background = new SolidColorBrush(Colors.WhiteSmoke) }
					}
				};

				Grid.SetColumn(mostInner, 1);
				grid.Children.Add(mostInner);
			}

			for (var i = 0; i < wideness.Value; i++)
			{
				grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
				var el = new Button { Width = 2 };
				Grid.SetColumn(el, 2 + i);
				grid.Children.Add(el);
			}


			return (root, leaves, mostInner);
		}

		private void changeOptimizeMeasure(object sender, RoutedEventArgs e)
		{
#if !WINAPPSDK
			if (optimizeMeasure.IsChecked is true)
			{
				FeatureConfiguration.UIElement.UseInvalidateMeasurePath = true;
			}
			else
			{
				FeatureConfiguration.UIElement.UseInvalidateMeasurePath = false;
			}

			if (optimizeArrange.IsChecked is true)
			{
				FeatureConfiguration.UIElement.UseInvalidateArrangePath = true;
			}
			else
			{
				FeatureConfiguration.UIElement.UseInvalidateArrangePath = false;
			}
#endif
		}
	}
}
