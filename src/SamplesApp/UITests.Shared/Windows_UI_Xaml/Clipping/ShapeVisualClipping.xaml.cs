using System;
using System.Collections.Generic;
using System.Numerics;
using Windows.UI.Composition;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Media;
using Uno.UI.Samples.Controls;

namespace UITests.Windows_UI_Xaml.Clipping;

[Sample]
public sealed partial class ShapeVisualClipping : UserControl
{
	private readonly List<StackPanel> _samples = new();
	private int _sampleIndex;

	public ShapeVisualClipping()
	{
		InitializeComponent();

		foreach (var shapeVisualSize in (ReadOnlySpan<int>)[20, 40, 80])
		{
			foreach (var spriteOffset in (ReadOnlySpan<int>)[-40, -20, 20, 40])
			{
				foreach (var viewboxSize in (ReadOnlySpan<int>)[20, 40, 80, 160])
				{
					foreach (var viewboxOffset in (ReadOnlySpan<int>)[20, 40, 80])
					{
						var element = new Border
						{
							Height = 40,
							Width = 40,
							Background = new SolidColorBrush(Windows.UI.Colors.Red)
						};

						var elementVisual = ElementCompositionPreview.GetElementVisual(element);
						var compositor = elementVisual.Compositor;
						var shapeVisual = compositor.CreateShapeVisual();

						var spriteShape = compositor.CreateSpriteShape();
						spriteShape.Geometry = compositor.CreateRectangleGeometry();
						((CompositionRectangleGeometry)spriteShape.Geometry).Size = new Vector2(40, 40);
						spriteShape.Offset = new Vector2(spriteOffset, spriteOffset);
						spriteShape.FillBrush = compositor.CreateColorBrush(Windows.UI.Colors.Blue);
						shapeVisual.Shapes.Add(spriteShape);
						shapeVisual.Size = new Vector2(shapeVisualSize, shapeVisualSize);
						shapeVisual.ViewBox = compositor.CreateViewBox();
						shapeVisual.ViewBox.Size = new Vector2(viewboxSize, viewboxSize);
						shapeVisual.ViewBox.Offset = new Vector2(viewboxOffset, viewboxOffset);
						((ContainerVisual)elementVisual).Children.InsertAtTop(shapeVisual);

						_samples.Add(new StackPanel
						{
							VerticalAlignment = VerticalAlignment.Center,
							HorizontalAlignment = HorizontalAlignment.Center,
							Children =
							{
								new TextBlock
								{
									Text =
										$"""
										 Sample [{_samples.Count + 1}]
										 shapeVisualSize = {shapeVisualSize}
										 spriteOffset = {spriteOffset}
										 viewboxSize = {viewboxSize}
										 viewboxOffset = {viewboxOffset}
										 """
								},
								element
							}
						});
					}
				}
			}
		}

		border.Child = _samples[_sampleIndex];
	}

	private void Prev(object sender, RoutedEventArgs e)
	{
		_sampleIndex = Math.Max(0, _sampleIndex - 1);
		border.Child = _samples[_sampleIndex];
	}

	private void Next(object sender, RoutedEventArgs e)
	{
		_sampleIndex = Math.Min(_samples.Count - 1, _sampleIndex + 1);
		border.Child = _samples[_sampleIndex];
	}
}
