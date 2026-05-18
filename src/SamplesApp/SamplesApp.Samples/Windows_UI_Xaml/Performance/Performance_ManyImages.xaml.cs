using System;
using System.Linq;
using Microsoft.UI.Xaml;
using Uno.UI.Samples.Controls;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using SamplesApp.UITests;
using CollectionExtensions = Uno.Extensions.CollectionExtensions;

namespace UITests.Windows_UI_Xaml.Performance
{
	[Sample("Performance", IsManualTest = true, Description = "Try different column counts and make sure that the frame time (right value) in the FPS indicator stays low. Make sure to test on different DPIs.")]
	public sealed partial class Performance_ManyImages : Page
	{
		public Performance_ManyImages()
		{
			this.InitializeComponent();

			Loaded += (s, e) =>
			{
				colorStoryboard.Begin();
#if __SKIA__
				Update();
#endif
			};

			Unloaded += (s, e) =>
			{
				colorStoryboard.Stop();
			};
		}

		private void NumberBoxValueChanged(object sender, NumberBoxValueChangedEventArgs e) => Update();
		private void OnSelectionChanged(object sender, SelectionChangedEventArgs e) => Update();

		private void Update()
		{
#if __SKIA__
			grid.Children.Clear();
			(grid as Grid).ColumnDefinitions.Clear();
			var index = (cb as ComboBox).SelectedIndex;
			var val = (int)Math.Round(Math.Max(0, (nb as NumberBox).Value));
			for (var i = 0; i < val; i++)
			{
				(grid as Grid).ColumnDefinitions.Add(new ColumnDefinition { Width = GridLengthHelper.OneStar });
				grid.Children.Add(new StackPanel().Apply(sp =>
				{
					Grid.SetColumn(sp, i);
					CollectionExtensions.AddRange(sp.Children, Enumerable.Range(0, 50).Select<int, UIElement>(_ =>
					{
						switch (index)
						{
							case 0:
								return new Image
								{
									Width = 200,
									Height = 200,
									Source = new Uri("ms-appx:/Assets/LargeWisteria.jpg")
								};
							case 1:
								return new Border
								{
									Width = 200,
									Height = 200,
									Background = new ImageBrush { ImageSource = new Uri("ms-appx:/Assets/LargeWisteria.jpg") }
								};
							default:
								throw new IndexOutOfRangeException();
						}
					}));
				}));
			}
#endif
		}
	}
}
