using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Uno.UI.Samples.Controls;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using UITests.Shared.Helpers;

namespace UITests.Windows_UI_Xaml.Clipping;

[Sample("Clipping")]
public sealed partial class BorderVisualClipping : Page, IWaitableSample
{
	private const string ImageUrl = "http://lh5.ggpht.com/lxBMauupBiLIpgOgu5apeiX_YStXeHRLK1oneS4NfwwNt7fGDKMP0KpQIMwfjfL9GdHRVEavmg7gOrj5RYC4qwrjh3Y0jCWFDj83jzg";

	public BorderVisualClipping()
	{
		var grid = new Grid
		{
			ColumnDefinitions =
			{
				new ColumnDefinition { Width = new GridLength(80) },
				new ColumnDefinition { Width = new GridLength(80) },
				new ColumnDefinition { Width = new GridLength(80) },
				new ColumnDefinition { Width = new GridLength(80) },
				new ColumnDefinition { Width = new GridLength(80) },
				new ColumnDefinition { Width = new GridLength(80) },
				new ColumnDefinition { Width = new GridLength(80) },
				new ColumnDefinition { Width = new GridLength(80) },
			},
			RowDefinitions =
			{
				new RowDefinition { Height = new GridLength(80) },
				new RowDefinition { Height = new GridLength(80) },
				new RowDefinition { Height = new GridLength(80) },
				new RowDefinition { Height = new GridLength(80) },
				new RowDefinition { Height = new GridLength(80) },
				new RowDefinition { Height = new GridLength(80) },
				new RowDefinition { Height = new GridLength(80) },
				new RowDefinition { Height = new GridLength(80) },
			},
			ColumnSpacing = 10,
			RowSpacing = 10
		};

		Content = new ScrollViewer
		{
			Background = new SolidColorBrush(Windows.UI.Colors.Pink),
			Content = grid
		};

		var imageBrush = new ImageBrush
		{
			ImageSource = new BitmapImage(new Uri(ImageUrl))
		};
		SamplePreparedTask = WaitableSampleImageHelpers.WaitAllImages(imageBrush);

		var brushes = new List<Brush>
		{
			new SolidColorBrush(Windows.UI.Colors.Blue),
			new SolidColorBrush(Windows.UI.Color.FromArgb(0x50, 0, 0, 0xFF)), // blue with low alpha
			imageBrush,
			null
		};

		var counter = 0;
		foreach (var createWrappingBorder in new[] { true, false })
		{
			foreach (var backgroundSizing in new[] { BackgroundSizing.InnerBorderEdge, BackgroundSizing.OuterBorderEdge })
			{
				foreach (var borderBrush in brushes)
				{
					foreach (var backgroundBrush in brushes)
					{
						var border = new Border
						{
							Width = 80,
							Height = 80,
							BorderBrush = borderBrush,
							Background = backgroundBrush,
							BackgroundSizing = backgroundSizing,
							BorderThickness = new Thickness(16),
							CornerRadius = new CornerRadius(40)
						};

						if (createWrappingBorder)
						{
							border = new Border
							{
								Background = new SolidColorBrush(Windows.UI.Colors.Green),
								Child = border
							};
						}

						var containergrid = new Grid
						{
							Children =
							{
								border,
								new TextBlock
								{
									HorizontalAlignment = HorizontalAlignment.Center,
									VerticalAlignment = VerticalAlignment.Center,
									Text =
										$"""
										{backgroundSizing}
										border: {brushes.IndexOf(borderBrush) + 1}
										background: {brushes.IndexOf(backgroundBrush) + 1}
										"""
								}
							}
						};

						Grid.SetRow(containergrid, counter / 8);
						Grid.SetColumn(containergrid, counter % 8);
						counter++;

						grid.Children.Add(containergrid);
					}
				}
			}
		}
	}

	public Task SamplePreparedTask
	{
		get;
	}
}
