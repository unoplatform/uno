// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Uno.UI.Samples.Controls;

namespace UITests.Microsoft_UI_Xaml_Controls.ParallaxViewTests;

[Sample("ParallaxView", "MUX")]
public sealed partial class ParallaxViewPage : Page
{
	private UIElement _savedParallaxViewChild;

	public ParallaxViewPage()
	{
		this.InitializeComponent();

		// Populate the ScrollViewer items so there is something scrollable under the parallax header.
		for (int i = 0; i < 10; i++)
		{
			itemsStack.Children.Add(new TextBlock
			{
				Text = $"Item {i + 1}",
				Margin = new Thickness(20, 8, 20, 8),
				FontSize = 16,
			});
		}
	}

	private void btnClearParallaxViewChild_Click(object sender, RoutedEventArgs e)
	{
		_savedParallaxViewChild = parallaxView1.Child;
		parallaxView1.Child = null;
		tbStatus.Text = "parallaxView1.Child cleared";
	}

	private void btnSetParallaxViewChild_Click(object sender, RoutedEventArgs e)
	{
		if (_savedParallaxViewChild is not null)
		{
			parallaxView1.Child = _savedParallaxViewChild;
			tbStatus.Text = "parallaxView1.Child restored";
		}
		else
		{
			// If no saved child, create a new rectangle.
			var newRect = new Rectangle
			{
				Width = 500,
				Height = 300,
				Fill = new SolidColorBrush(Microsoft.UI.Colors.SeaGreen),
			};
			parallaxView1.Child = newRect;
			tbStatus.Text = "parallaxView1.Child set to a new Rectangle";
		}
	}

	private void btnRefreshOffsets_Click(object sender, RoutedEventArgs e)
	{
		parallaxView1.RefreshAutomaticHorizontalOffsets();
		parallaxView1.RefreshAutomaticVerticalOffsets();
		parallaxViewHeader.RefreshAutomaticVerticalOffsets();
		tbStatus.Text = "Automatic offsets refreshed";
	}
}
