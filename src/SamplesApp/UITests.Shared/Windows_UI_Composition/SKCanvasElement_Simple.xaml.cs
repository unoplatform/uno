using System;
using Windows.Foundation;
using Microsoft.UI.Xaml;
using Uno.UI.Samples.Controls;
using Microsoft.UI.Xaml.Controls;
using SamplesApp.UITests;
using SkiaSharp;
using Uno.WinUI.Graphics2DSK;

namespace UITests.Shared.Windows_UI_Composition
{
	[Sample("Microsoft.UI.Composition")]
	public sealed partial class SKCanvasElement_Simple : UserControl
	{
		public SKCanvasElement_Simple()
		{
			this.InitializeComponent();
			if (SKCanvasElement.IsSupportedOnCurrentPlatform())
			{
				Slider slider;
				grid.Children.Add(new Grid
				{
					RowDefinitions =
					{
						new RowDefinition { Height = GridLength.Auto },
						new RowDefinition { Height = new GridLength(1.0f, GridUnitType.Star) }
					},
					Children =
					{
						(slider = new Slider { Header = "Sample", Minimum = 0, Maximum = SKCanvasElementImpl.SampleCount }),
						new SKCanvasElementImpl().Apply(s =>
						{
							Grid.SetRow(s, 1);
							slider.ValueChanged += (_, _) => s.Sample = (int)slider.Value;
						})
					}
				});
			}
			else
			{
				grid.Children.Add(new TextBlock { Text = "This sample is not supported on this platform." });
			}
		}
	}
}

public partial class SKCanvasElementImpl : SKCanvasElement
{
	public static int SampleCount => 3;

	public static DependencyProperty SampleProperty { get; } = DependencyProperty.Register(
		nameof(Sample),
		typeof(int),
		typeof(SKCanvasElementImpl),
		new PropertyMetadata(0, (o, args) => ((SKCanvasElementImpl)o).SampleChanged((int)args.NewValue)));

	public int Sample
	{
		get => (int)GetValue(SampleProperty);
		set => SetValue(SampleProperty, value);
	}

	private void SampleChanged(int newIndex)
	{
		Sample = Math.Min(Math.Max(0, newIndex), SampleCount - 1);
		Invalidate();
	}

	protected override void RenderOverride(SKCanvas canvas, Size area)
	{
		var minDim = Math.Min(area.Width, area.Height);
		// rescale to fit the given area, assuming each drawing is 260x260
		canvas.Scale((float)(minDim / 260), (float)(minDim / 260));

		switch (Sample)
		{
			case 0:
				SkiaDrawing0(canvas);
				break;
			case 1:
				SkiaDrawing1(canvas);
				break;
			case 2:
				SkiaDrawing2(canvas);
				break;
		}
	}

	// https://fiddle.skia.org/c/@shapes
	private void SkiaDrawing0(SKCanvas canvas)
	{
		var paint = new SKPaint();
		paint.Style = SKPaintStyle.Fill;
		paint.IsAntialias = true;
		paint.StrokeWidth = 4;
		paint.Color = new SKColor(0xff4285F4);

		var rect = SKRect.Create(10, 10, 100, 160);
		canvas.DrawRect(rect, paint);

		var oval = new SKPath();
		oval.AddRoundRect(rect, 20, 20);
		oval.Offset(new SKPoint(40, 80));
		paint.Color = new SKColor(0xffDB4437);
		canvas.DrawPath(oval, paint);

		paint.Color = new SKColor(0xff0F9D58);
		canvas.DrawCircle(180, 50, 25, paint);

		rect.Offset(80, 50);
		paint.Color = new SKColor(0xffF4B400);
		paint.Style = SKPaintStyle.Stroke;
		canvas.DrawRoundRect(rect, 10, 10, paint);
	}

	// https://fiddle.skia.org/c/@bezier_curves
	private void SkiaDrawing1(SKCanvas canvas)
	{
		var paint = new SKPaint();
		paint.Style = SKPaintStyle.Stroke;
		paint.StrokeWidth = 8;
		paint.Color = new SKColor(0xff4285F4);
		paint.IsAntialias = true;
		paint.StrokeCap = SKStrokeCap.Round;

		var path = new SKPath();
		path.MoveTo(10, 10);
		path.QuadTo(256, 64, 128, 128);
		path.QuadTo(10, 192, 250, 250);
		canvas.DrawPath(path, paint);
	}

	// https://fiddle.skia.org/c/@shader
	private void SkiaDrawing2(SKCanvas canvas)
	{
		var paint = new SKPaint();
		using var pathEffect = SKPathEffect.CreateDiscrete(10.0f, 4.0f);
		paint.PathEffect = pathEffect;
		SKPoint[] points =
		{
			new SKPoint(0.0f, 0.0f),
			new SKPoint(256.0f, 256.0f)
		};
		SKColor[] colors =
		{
			new SKColor(66, 133, 244),
			new SKColor(15, 157, 88)
		};
		paint.Shader = SKShader.CreateLinearGradient(points[0], points[1], colors, SKShaderTileMode.Clamp);
		paint.IsAntialias = true;
		var path = Star();
		canvas.DrawPath(path, paint);

		SKPath Star()
		{
			const float R = 60.0f, C = 128.0f;
			var path = new SKPath();
			path.MoveTo(C + R, C);
			for (var i = 1; i < 15; ++i)
			{
				var a = 0.44879895f * i;
				var r = R + R * (i % 2);
				path.LineTo((float)(C + r * Math.Cos(a)), (float)(C + r * Math.Sin(a)));
			}
			return path;
		}
	}
}
