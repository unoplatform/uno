#nullable enable

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Uno.UI.Samples.Controls;
using Windows.UI;

namespace UITests.Shared.Windows_UI_Xaml_Controls.ScrollViewerTests;

[Sample("Scrolling", Name = "ScrollSmoothnessTester", IsManualTest = true, Description = "Stress-tests scroll smoothness with many 300x300 colored rectangles, hosted in a StackPanel (vertical/horizontal), a ListView and an ItemsRepeater")]
public sealed partial class ScrollViewer_SmoothnessTester : Page, INotifyPropertyChanged
{
	private const int ItemCount = 200;

	private readonly List<TextBlock> _stackPanelIndexLabels = new();

	public sealed record RectangleItem(int Index, Brush Brush);

	public event PropertyChangedEventHandler? PropertyChanged;

	private Visibility _indexLabelVisibility = Visibility.Collapsed;

	// Collapsed by default: this sample is about scroll smoothness, so the initial UI stays as
	// lightweight as possible. The toggle lets you superpose the index labels on demand.
	public Visibility IndexLabelVisibility
	{
		get => _indexLabelVisibility;
		private set
		{
			_indexLabelVisibility = value;
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IndexLabelVisibility)));
		}
	}

	public ScrollViewer_SmoothnessTester()
	{
		this.InitializeComponent();

		var random = new Random(1234567890);

		for (var i = 0; i < ItemCount; i++)
		{
			VerticalStack.Children.Add(CreateRectangleWithIndex(i, random));
			HorizontalStack.Children.Add(CreateRectangleWithIndex(i, random));
		}

		ListViewHost.ItemsSource = Enumerable.Range(0, ItemCount).Select(i => new RectangleItem(i, RandomBrush(random))).ToList();
		ItemsRepeaterHost.ItemsSource = Enumerable.Range(0, ItemCount).Select(i => new RectangleItem(i, RandomBrush(random))).ToList();
	}

	private void ShowIndexToggle_Click(object sender, RoutedEventArgs e)
	{
		IndexLabelVisibility = ShowIndexToggle.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;

		foreach (var label in _stackPanelIndexLabels)
		{
			label.Visibility = IndexLabelVisibility;
		}
	}

	private UIElement CreateRectangleWithIndex(int index, Random random)
	{
		var label = new TextBlock
		{
			Text = index.ToString(),
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
			FontSize = 24,
			Foreground = new SolidColorBrush(Colors.White),
			Visibility = IndexLabelVisibility,
		};
		_stackPanelIndexLabels.Add(label);

		return new Grid
		{
			Width = 300,
			Height = 300,
			Children =
			{
				new Rectangle
				{
					Fill = RandomBrush(random)
				},
				label
			}
		};
	}

	private static SolidColorBrush RandomBrush(Random random) =>
		new(Color.FromArgb(255, (byte)random.Next(256), (byte)random.Next(256), (byte)random.Next(256)));
}
