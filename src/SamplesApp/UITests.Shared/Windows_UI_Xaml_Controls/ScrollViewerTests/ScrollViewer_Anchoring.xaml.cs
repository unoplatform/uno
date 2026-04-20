using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Uno.UI.Samples.Controls;
using Windows.UI;

namespace UITests.Windows_UI_Xaml_Controls.ScrollViewerTests
{
	[Sample("Scrolling", Description = "Demonstrates ScrollViewer.CurrentAnchor and VerticalAnchorRatio. Insert/remove items above the viewport anchor; the ScrollViewer preserves the anchor's visual position.")]
	public sealed partial class ScrollViewer_Anchoring : UserControl
	{
		private int _counter;

		public ScrollViewer_Anchoring()
		{
			this.InitializeComponent();

			for (int i = 0; i < 15; i++)
			{
				ContentPanel.Children.Add(CreateItem());
			}

			TargetScrollViewer.ViewChanged += OnViewChanged;
			Loaded += OnLoaded;
		}

		private void OnViewChanged(object sender, ScrollViewerViewChangedEventArgs e) => UpdateStatus();

		private Border CreateItem()
		{
			var index = _counter++;
			var border = new Border
			{
				Width = 280,
				Height = 80,
				Margin = new Thickness(4),
				Background = new SolidColorBrush(index % 2 == 0 ? Colors.LightBlue : Colors.LightCoral),
				Child = new TextBlock
				{
					Text = $"Item #{index}",
					HorizontalAlignment = HorizontalAlignment.Center,
					VerticalAlignment = VerticalAlignment.Center,
				},
			};
			border.Loaded += (_, _) => TargetScrollViewer.RegisterAnchorCandidate(border);
			border.Unloaded += (_, _) => TargetScrollViewer.UnregisterAnchorCandidate(border);
			return border;
		}

		private void OnLoaded(object sender, RoutedEventArgs e)
		{
			UpdateStatus();
		}

		private void UpdateStatus()
		{
			var anchor = TargetScrollViewer.CurrentAnchor;
			var tb = (anchor as Border)?.Child as TextBlock;
			CurrentAnchorText.Text = $"CurrentAnchor: {tb?.Text ?? "(none)"}";
			OffsetText.Text = $"VerticalOffset: {TargetScrollViewer.VerticalOffset:F1} / ScrollableHeight: {TargetScrollViewer.ScrollableHeight:F1}";
		}

		private void InsertAbove_Click(object sender, RoutedEventArgs e)
		{
			ContentPanel.Children.Insert(0, CreateItem());
			UpdateStatus();
		}

		private void RemoveAbove_Click(object sender, RoutedEventArgs e)
		{
			if (ContentPanel.Children.Count > 0)
			{
				ContentPanel.Children.RemoveAt(0);
			}
			UpdateStatus();
		}

		private void InsertBelow_Click(object sender, RoutedEventArgs e)
		{
			ContentPanel.Children.Add(CreateItem());
			UpdateStatus();
		}

		private void ScrollTop_Click(object sender, RoutedEventArgs e)
		{
			TargetScrollViewer.ChangeView(null, 0, null, disableAnimation: true);
		}

		private void ScrollBottom_Click(object sender, RoutedEventArgs e)
		{
			TargetScrollViewer.ChangeView(null, TargetScrollViewer.ScrollableHeight, null, disableAnimation: true);
		}

		private void Ratio_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (RatioCombo.SelectedIndex < 0)
			{
				return;
			}
			TargetScrollViewer.VerticalAnchorRatio = RatioCombo.SelectedIndex switch
			{
				0 => double.NaN,
				1 => 0.0,
				2 => 0.5,
				3 => 1.0,
				_ => double.NaN,
			};
			UpdateStatus();
		}
	}
}
